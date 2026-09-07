using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class BootsPipelineTests
{
    public static IEnumerable<object[]> BootCases()
    {
        yield return new object[] { "romulot.ValleyArmory_MinersBoots", 0 };
        yield return new object[] { "romulot.ValleyArmory_ObsidianBoots", 1 };
        yield return new object[] { "romulot.ValleyArmory_EtherealBoots", 2 };
    }

    [Fact]
    public void CatalogHasExactlyThreeBootsDefinitions()
    {
        ArmoryCatalog catalog = LoadCatalog();

        Assert.Equal(3, catalog.Equipment.Count(item => item.Type is EquipmentType.Boots));
    }

    [Theory]
    [MemberData(nameof(BootCases))]
    public void BootFactoryProducesTenFieldsInVanillaPlusExtensionOrder(string itemId, int expectedSpriteIndex)
    {
        EquipmentDefinition definition = GetDefinition(itemId);
        EquipmentStats stats = definition.Stats!;

        string raw = new BootDataFactory().Create(definition, key => $"t:{key}");
        string[] fields = raw.Split('/');

        Assert.Equal(10, fields.Length);
        Assert.Equal(definition.Id, fields[0]);
        Assert.Equal($"t:{definition.DescriptionKey}", fields[1]);
        Assert.Equal(stats.Price.ToString(), fields[2]);
        Assert.Equal(stats.Defense.ToString(), fields[3]);
        Assert.Equal(stats.Immunity!.Value.ToString(), fields[4]);
        Assert.Equal(definition.ColorIndex!.Value.ToString(), fields[5]);
        Assert.Equal($"t:{definition.DisplayNameKey}", fields[6]);
        Assert.Equal(string.Empty, fields[7]);
        Assert.Equal(expectedSpriteIndex.ToString(), fields[8]);
        Assert.Equal(BootAssetInjector.BootTextureAssetName.Replace('/', '\\'), fields[9]);
        Assert.DoesNotContain('/', fields[9]);
    }

    [Theory]
    [MemberData(nameof(BootCases))]
    public void BootFactoryPreservesStatsWithoutTransformation(string itemId, int _)
    {
        EquipmentDefinition definition = GetDefinition(itemId);
        EquipmentStats stats = definition.Stats!;

        string[] fields = new BootDataFactory().Create(definition, key => key).Split('/');

        Assert.Equal(stats.Price, int.Parse(fields[2]));
        Assert.Equal(stats.Defense, int.Parse(fields[3]));
        Assert.Equal(stats.Immunity, int.Parse(fields[4]));
    }

    [Fact]
    public void BootFactoryDoesNotDeriveStatsFromRarity()
    {
        EquipmentDefinition rare = GetDefinition("romulot.ValleyArmory_ObsidianBoots");
        EquipmentDefinition common = CloneWithRarity(rare, "Common");

        string[] rareFields = new BootDataFactory().Create(rare, key => key).Split('/');
        string[] commonFields = new BootDataFactory().Create(common, key => key).Split('/');

        Assert.Equal(rareFields[2], commonFields[2]);
        Assert.Equal(rareFields[3], commonFields[3]);
        Assert.Equal(rareFields[4], commonFields[4]);
    }

    [Fact]
    public void BootFactoryRejectsNonBootsDefinition()
    {
        EquipmentDefinition weapon = GetDefinition("romulot.ValleyArmory_MinersBlade");

        Assert.Throws<ArgumentException>(() => new BootDataFactory().Create(weapon, key => key));
    }

    [Fact]
    public void WeaponFactoryRejectsBootsDefinition()
    {
        EquipmentDefinition boots = GetDefinition("romulot.ValleyArmory_MinersBoots");

        Assert.Throws<ArgumentException>(() => new WeaponDataFactory().Create(boots, key => key));
    }

    [Fact]
    public void BootFactoryRejectsSlashInTranslatedFields()
    {
        EquipmentDefinition boots = GetDefinition("romulot.ValleyArmory_MinersBoots");

        Assert.Throws<ArgumentException>(() => new BootDataFactory().Create(boots, key => "broken/value"));
    }

    [Fact]
    public void QualifiedBootIdsUseTheBootsPrefixAndDoNotCollideWithWeapons()
    {
        CatalogIndex index = new(LoadCatalog());

        Assert.True(index.TryGetById("romulot.ValleyArmory_MinersBoots", out EquipmentDefinition? boots));
        string qualified = EquipmentIdentity.GetQualifiedItemId(boots!);
        Assert.Equal("(B)romulot.ValleyArmory_MinersBoots", qualified);

        IReadOnlyCollection<string> allQualifiedIds = LoadCatalog().Equipment
            .Select(EquipmentIdentity.GetQualifiedItemId)
            .ToArray();
        Assert.Equal(allQualifiedIds.Count, allQualifiedIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void BootAssetMergeDoesNotReplaceAnExistingEntry()
    {
        Dictionary<string, string> data = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_MinersBoots"] = "existing"
        };

        bool added = NonOverwritingAssetEditor.TryAdd(data, "romulot.ValleyArmory_MinersBoots", "replacement");

        Assert.False(added);
        Assert.Equal("existing", data["romulot.ValleyArmory_MinersBoots"]);
        Assert.Single(data);
    }

    [Fact]
    public void OneBootCollisionDoesNotBlockOtherBootDefinitions()
    {
        ArmoryCatalog catalog = LoadCatalog();
        BootDataFactory factory = new();
        Dictionary<string, string> data = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_MinersBoots"] = "existing"
        };

        foreach (EquipmentDefinition definition in catalog.Equipment.Where(item => item.Type is EquipmentType.Boots))
        {
            string raw = factory.Create(definition, key => key);
            _ = NonOverwritingAssetEditor.TryAdd(data, definition.Id, raw);
        }

        Assert.Equal(3, data.Count);
        Assert.Equal("existing", data["romulot.ValleyArmory_MinersBoots"]);
    }

    private static ArmoryCatalog LoadCatalog()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        return new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
    }

    private static EquipmentDefinition GetDefinition(string itemId)
    {
        return Assert.Single(LoadCatalog().Equipment, item => item.Id == itemId);
    }

    private static EquipmentDefinition CloneWithRarity(EquipmentDefinition source, string rarity)
    {
        return new EquipmentDefinition
        {
            Id = source.Id,
            Type = source.Type,
            Rarity = rarity,
            DisplayNameKey = source.DisplayNameKey,
            DescriptionKey = source.DescriptionKey,
            Stats = source.Stats,
            Acquisition = source.Acquisition,
            Sprite = source.Sprite,
            OptionalVisualOverrides = source.OptionalVisualOverrides,
            ColorIndex = source.ColorIndex
        };
    }
}
