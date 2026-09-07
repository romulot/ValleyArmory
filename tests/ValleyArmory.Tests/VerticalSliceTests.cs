using StardewValley.GameData.Weapons;
using ValleyArmory;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class VerticalSliceTests
{
    public static IEnumerable<object[]> WeaponCases()
    {
        yield return new object[] { "romulot.ValleyArmory_MinersBlade", "DefenseSword", 3 };
        yield return new object[] { "romulot.ValleyArmory_BlackIronSword", "DefenseSword", 3 };
        yield return new object[] { "romulot.ValleyArmory_PrismaticBlade", "StabbingSword", 0 };
        yield return new object[] { "romulot.ValleyArmory_ShadowFang", "Dagger", 1 };
        yield return new object[] { "romulot.ValleyArmory_MoonDagger", "Dagger", 1 };
        yield return new object[] { "romulot.ValleyArmory_Stonebreaker", "Club", 2 };
        yield return new object[] { "romulot.ValleyArmory_AbyssHammer", "Club", 2 };
    }

    public static IEnumerable<object[]> WeaponStatCases()
    {
        foreach (object[] item in WeaponCases())
            yield return new[] { item[0] };
    }

    [Fact]
    public void CatalogHasExactlySevenWeaponDefinitions()
    {
        ArmoryCatalog catalog = LoadCatalog();

        Assert.Equal(7, catalog.Equipment.Count(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer));
    }

    [Theory]
    [MemberData(nameof(WeaponCases))]
    public void WeaponFactoryMapsBehaviorAndQualifiedId(string itemId, string behavior, int vanillaType)
    {
        EquipmentDefinition definition = GetDefinition(itemId);
        WeaponData data = new WeaponDataFactory().Create(definition, key => $"translated:{key}");

        Assert.Equal(vanillaType, data.Type);
        Assert.Equal($"(W){itemId}", EquipmentIdentity.GetQualifiedItemId(definition));
        Assert.Equal(definition.Rarity, data.CustomFields["romulot.ValleyArmory/Rarity"]);
        Assert.Equal(behavior, definition.WeaponBehavior!.Value.ToString());
    }

    [Theory]
    [MemberData(nameof(WeaponStatCases))]
    public void WeaponFactoryMapsStatsWithoutTransformation(string itemId)
    {
        EquipmentDefinition definition = GetDefinition(itemId);
        EquipmentStats stats = definition.Stats!;
        WeaponData data = new WeaponDataFactory().Create(definition, key => key);

        Assert.Equal(stats.MinDamage, data.MinDamage);
        Assert.Equal(stats.MaxDamage, data.MaxDamage);
        Assert.Equal(stats.Speed, data.Speed);
        Assert.Equal(stats.Defense, data.Defense);
        Assert.Equal(stats.CritChance, data.CritChance);
        Assert.Equal(stats.CritMultiplier, data.CritMultiplier);
        Assert.Equal(stats.Knockback, data.Knockback);
        Assert.Equal(stats.Precision, data.Precision);
        Assert.Equal(stats.AreaOfEffect, data.AreaOfEffect);
        Assert.True(data.CanBeLostOnDeath);
        Assert.Equal(-1, data.MineBaseLevel);
        Assert.Equal(-1, data.MineMinLevel);
    }

    [Fact]
    public void WeaponFactoryDoesNotDeriveStatsFromRarity()
    {
        EquipmentDefinition rare = GetDefinition("romulot.ValleyArmory_MinersBlade");
        EquipmentDefinition common = CloneWithRarity(rare, "Common");
        WeaponData rareData = new WeaponDataFactory().Create(rare, key => key);
        WeaponData commonData = new WeaponDataFactory().Create(common, key => key);

        Assert.Equal(rareData.MinDamage, commonData.MinDamage);
        Assert.Equal(rareData.MaxDamage, commonData.MaxDamage);
        Assert.Equal(rareData.Speed, commonData.Speed);
        Assert.Equal(rareData.Defense, commonData.Defense);
        Assert.Equal(rareData.CritChance, commonData.CritChance);
        Assert.Equal(rareData.CritMultiplier, commonData.CritMultiplier);
        Assert.Equal(rareData.Knockback, commonData.Knockback);
    }

    [Fact]
    public void WeaponFactoryRejectsNonWeaponDefinition()
    {
        EquipmentDefinition boots = GetDefinition("romulot.ValleyArmory_MinersBoots");

        Assert.Throws<ArgumentException>(() => new WeaponDataFactory().Create(boots, key => key));
    }

    [Fact]
    public void WeaponFactoryRejectsMissingWeaponBehavior()
    {
        EquipmentDefinition source = GetDefinition("romulot.ValleyArmory_MinersBlade");
        EquipmentDefinition invalid = CloneWithBehavior(source, null);

        Assert.Throws<ArgumentException>(() => new WeaponDataFactory().Create(invalid, key => key));
    }

    [Fact]
    public void WeaponAssetMergeDoesNotReplaceAnExistingEntry()
    {
        Dictionary<string, string> data = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_MinersBlade"] = "existing"
        };

        bool added = NonOverwritingAssetEditor.TryAdd(
            data,
            "romulot.ValleyArmory_MinersBlade",
            "replacement"
        );

        Assert.False(added);
        Assert.Equal("existing", data["romulot.ValleyArmory_MinersBlade"]);
        Assert.Single(data);
    }

    [Fact]
    public void OneWeaponCollisionDoesNotBlockOtherWeaponDefinitions()
    {
        ArmoryCatalog catalog = LoadCatalog();
        WeaponDataFactory factory = new();
        Dictionary<string, WeaponData> data = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_MinersBlade"] = new WeaponData { Name = "existing" }
        };

        foreach (EquipmentDefinition definition in catalog.Equipment.Where(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer))
        {
            WeaponData weapon = factory.Create(definition, key => key);
            _ = NonOverwritingAssetEditor.TryAdd(data, definition.Id, weapon);
        }

        Assert.Equal(7, data.Count);
        Assert.Equal("existing", data["romulot.ValleyArmory_MinersBlade"].Name);
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
            WeaponBehavior = source.WeaponBehavior,
            Rarity = rarity,
            DisplayNameKey = source.DisplayNameKey,
            DescriptionKey = source.DescriptionKey,
            Stats = source.Stats,
            Acquisition = source.Acquisition,
            Sprite = source.Sprite,
            OptionalVisualOverrides = source.OptionalVisualOverrides
        };
    }

    private static EquipmentDefinition CloneWithBehavior(EquipmentDefinition source, WeaponBehavior? behavior)
    {
        return new EquipmentDefinition
        {
            Id = source.Id,
            Type = source.Type,
            WeaponBehavior = behavior,
            Rarity = source.Rarity,
            DisplayNameKey = source.DisplayNameKey,
            DescriptionKey = source.DescriptionKey,
            Stats = source.Stats,
            Acquisition = source.Acquisition,
            Sprite = source.Sprite,
            OptionalVisualOverrides = source.OptionalVisualOverrides
        };
    }
}
