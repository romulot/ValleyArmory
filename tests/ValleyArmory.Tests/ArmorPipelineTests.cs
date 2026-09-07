using StardewValley.GameData.Shirts;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class ArmorPipelineTests
{
    public static IEnumerable<object[]> ArmorCases()
    {
        yield return new object[] { "romulot.ValleyArmory_MinersArmor", 0 };
        yield return new object[] { "romulot.ValleyArmory_ObsidianArmor", 1 };
        yield return new object[] { "romulot.ValleyArmory_EtherealArmor", 2 };
    }

    [Fact]
    public void CatalogHasExactlyThreeArmorDefinitions()
    {
        ArmoryCatalog catalog = LoadCatalog();

        Assert.Equal(3, catalog.Equipment.Count(item => item.Type is EquipmentType.Shirt));
    }

    [Theory]
    [MemberData(nameof(ArmorCases))]
    public void ArmorFactoryProducesValidShirtData(string itemId, int expectedSpriteIndex)
    {
        EquipmentDefinition definition = GetDefinition(itemId);
        EquipmentStats stats = definition.Stats!;

        ShirtData data = new ArmorDataFactory().Create(definition, key => $"t:{key}");

        Assert.Equal(definition.Id, data.Name);
        Assert.Equal($"t:{definition.DisplayNameKey}", data.DisplayName);
        Assert.Equal($"t:{definition.DescriptionKey}", data.Description);
        Assert.Equal(stats.Price, data.Price);
        Assert.Equal(ArmorAssetInjector.ArmorTextureAssetName, data.Texture);
        Assert.Equal(expectedSpriteIndex, data.SpriteIndex);
        Assert.Equal(definition.Rarity, data.CustomFields["romulot.ValleyArmory/Rarity"]);
        Assert.False(data.CanBeDyed);
        Assert.False(data.IsPrismatic);
        Assert.False(data.HasSleeves);
    }

    [Fact]
    public void ArmorFactoryDoesNotDeriveStatsFromRarity()
    {
        EquipmentDefinition rare = GetDefinition("romulot.ValleyArmory_ObsidianArmor");
        EquipmentDefinition common = CloneWithRarity(rare, "Common");

        ShirtData rareData = new ArmorDataFactory().Create(rare, key => key);
        ShirtData commonData = new ArmorDataFactory().Create(common, key => key);

        Assert.Equal(rareData.Price, commonData.Price);
    }

    [Fact]
    public void ArmorFactoryRejectsNonShirtDefinition()
    {
        EquipmentDefinition weapon = GetDefinition("romulot.ValleyArmory_MinersBlade");

        Assert.Throws<ArgumentException>(() => new ArmorDataFactory().Create(weapon, key => key));
    }

    [Fact]
    public void WeaponFactoryRejectsArmorDefinition()
    {
        EquipmentDefinition armor = GetDefinition("romulot.ValleyArmory_MinersArmor");

        Assert.Throws<ArgumentException>(() => new WeaponDataFactory().Create(armor, key => key));
    }

    [Fact]
    public void BootFactoryRejectsArmorDefinition()
    {
        EquipmentDefinition armor = GetDefinition("romulot.ValleyArmory_MinersArmor");

        Assert.Throws<ArgumentException>(() => new BootDataFactory().Create(armor, key => key));
    }

    [Fact]
    public void QualifiedArmorIdsUseTheShirtPrefixAndDoNotCollideWithOtherEquipment()
    {
        CatalogIndex index = new(LoadCatalog());

        Assert.True(index.TryGetById("romulot.ValleyArmory_MinersArmor", out EquipmentDefinition? armor));
        string qualified = EquipmentIdentity.GetQualifiedItemId(armor!);
        Assert.Equal("(S)romulot.ValleyArmory_MinersArmor", qualified);

        IReadOnlyCollection<string> allQualifiedIds = LoadCatalog().Equipment
            .Select(EquipmentIdentity.GetQualifiedItemId)
            .ToArray();
        Assert.Equal(allQualifiedIds.Count, allQualifiedIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ArmorAssetMergeDoesNotReplaceAnExistingEntry()
    {
        Dictionary<string, ShirtData> data = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_MinersArmor"] = new ShirtData { Name = "existing" }
        };

        bool added = NonOverwritingAssetEditor.TryAdd(data, "romulot.ValleyArmory_MinersArmor", new ShirtData { Name = "replacement" });

        Assert.False(added);
        Assert.Equal("existing", data["romulot.ValleyArmory_MinersArmor"].Name);
        Assert.Single(data);
    }

    [Fact]
    public void OneArmorCollisionDoesNotBlockOtherArmorDefinitions()
    {
        ArmoryCatalog catalog = LoadCatalog();
        ArmorDataFactory factory = new();
        Dictionary<string, ShirtData> data = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_MinersArmor"] = new ShirtData { Name = "existing" }
        };

        foreach (EquipmentDefinition definition in catalog.Equipment.Where(item => item.Type is EquipmentType.Shirt))
        {
            ShirtData shirt = factory.Create(definition, key => key);
            _ = NonOverwritingAssetEditor.TryAdd(data, definition.Id, shirt);
        }

        Assert.Equal(3, data.Count);
        Assert.Equal("existing", data["romulot.ValleyArmory_MinersArmor"].Name);
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
