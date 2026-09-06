using System.Text.Json.Nodes;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class CatalogTests
{
    private static readonly string ValidCatalogPath = FixturePath("armory.json");

    [Fact]
    public void ValidCatalogLoads()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        Assert.Equal(1, catalog.SchemaVersion);
    }

    [Fact]
    public void DuplicateEquipmentIdFailsWithItemAndField()
    {
        string path = CreateModifiedCatalog(root =>
        {
            JsonArray equipment = root["equipment"]!.AsArray();
            equipment[1]!["id"] = equipment[0]!["id"]!.GetValue<string>();
        });

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("equipment[", StringComparison.Ordinal) && error.Contains(".id: duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownRarityFailsWithItemAndField()
    {
        string path = CreateModifiedCatalog(root =>
            root["equipment"]!.AsArray()[0]!["rarity"] = "Mythic"
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("MinersBlade].rarity", StringComparison.Ordinal) && error.Contains("Mythic", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidWeaponStatsFailWithItemAndField()
    {
        string path = CreateModifiedCatalog(root =>
        {
            JsonNode stats = root["equipment"]!.AsArray()[0]!["stats"]!;
            stats["minDamage"] = 30;
            stats["maxDamage"] = 10;
            stats["critChance"] = 1.1;
        });

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("MinersBlade].stats.minDamage", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("MinersBlade].stats.critChance", StringComparison.Ordinal));
    }

    [Fact]
    public void QualifiedItemIdsUseTheExpectedTypePrefix()
    {
        CatalogIndex index = new(LoadValidCatalog());

        Assert.True(index.TryGetById("romulot.ValleyArmory_MinersBlade", out EquipmentDefinition? weapon));
        Assert.Equal("(W)romulot.ValleyArmory_MinersBlade", EquipmentIdentity.GetQualifiedItemId(weapon!));
        Assert.True(index.TryGetByQualifiedId("(B)romulot.ValleyArmory_MinersBoots", out EquipmentDefinition? boots));
        Assert.Equal(EquipmentType.Boots, boots!.Type);
    }

    [Fact]
    public void CatalogContainsExactlyTenEquipmentDefinitions()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        Assert.Equal(10, catalog.Equipment.Count);
        Assert.Equal(7, catalog.Equipment.Count(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer));
        Assert.Equal(3, catalog.Equipment.Count(item => item.Type is EquipmentType.Boots));
    }

    [Fact]
    public void RarityDistributionMatchesTheApprovedCatalog()
    {
        Dictionary<string, int> distribution = LoadValidCatalog().Equipment
            .GroupBy(item => item.Rarity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(3, distribution["Common"]);
        Assert.Equal(3, distribution["Rare"]);
        Assert.Equal(3, distribution["Epic"]);
        Assert.Equal(1, distribution["Legendary"]);
    }

    [Fact]
    public void TranslationFilesHaveIdenticalKeysAndCoverCatalogReferences()
    {
        ArmoryCatalog catalog = LoadValidCatalog();
        IReadOnlyDictionary<string, string> defaultTranslations = TranslationCatalogValidator.LoadFile(FixturePath("i18n", "default.json"));
        IReadOnlyDictionary<string, string> portugueseTranslations = TranslationCatalogValidator.LoadFile(FixturePath("i18n", "pt-BR.json"));

        IReadOnlyList<string> errors = TranslationCatalogValidator.Validate(catalog, defaultTranslations, portugueseTranslations);

        Assert.Empty(errors);
    }

    [Fact]
    public void MissingCatalogHasAClearError()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-armory-{Guid.NewGuid():N}.json");

        CatalogLoadException exception = Assert.Throws<CatalogLoadException>(() => CreateLoader().Load(path));

        Assert.Contains("file not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidJsonHasLocationInError()
    {
        string path = WriteTemporaryFile("{\n  \"schemaVersion\": nope\n}");

        CatalogLoadException exception = Assert.Throws<CatalogLoadException>(() => CreateLoader().Load(path));

        Assert.Contains("invalid JSON", exception.Message, StringComparison.Ordinal);
        Assert.Contains("line", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ArmoryCatalog LoadValidCatalog()
    {
        return CreateLoader().Load(ValidCatalogPath);
    }

    private static ArmoryCatalogLoader CreateLoader()
    {
        return new ArmoryCatalogLoader(new ArmoryCatalogValidator());
    }

    private static string CreateModifiedCatalog(Action<JsonObject> modify)
    {
        JsonObject root = JsonNode.Parse(File.ReadAllText(ValidCatalogPath))!.AsObject();
        modify(root);
        return WriteTemporaryFile(root.ToJsonString());
    }

    private static string WriteTemporaryFile(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"valley-armory-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    private static string FixturePath(params string[] parts)
    {
        return Path.Combine(new[] { AppContext.BaseDirectory, "Fixtures" }.Concat(parts).ToArray());
    }
}
