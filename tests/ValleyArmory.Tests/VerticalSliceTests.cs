using StardewValley.GameData.Weapons;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class VerticalSliceTests
{
    [Fact]
    public void MinersBladeFactoryGeneratesTheExpectedWeaponData()
    {
        EquipmentDefinition definition = LoadMinersBlade();

        WeaponData data = new MinersBladeWeaponDataFactory().Create(definition, key => $"translated:{key}");

        Assert.Equal("romulot.ValleyArmory_MinersBlade", data.Name);
        Assert.Equal("translated:equipment.miners-blade.name", data.DisplayName);
        Assert.Equal("translated:equipment.miners-blade.description", data.Description);
        Assert.Equal(3, data.Type);
        Assert.Equal("Mods/romulot.ValleyArmory/Weapons", data.Texture);
        Assert.Equal(0, data.SpriteIndex);
        Assert.Equal(14, data.MinDamage);
        Assert.Equal(22, data.MaxDamage);
        Assert.Equal(1, data.Speed);
        Assert.Equal(1, data.Defense);
        Assert.Equal(0.03f, data.CritChance);
        Assert.Equal(3.0f, data.CritMultiplier);
        Assert.Equal(1.0f, data.Knockback);
        Assert.Equal(0, data.Precision);
        Assert.Equal(0, data.AreaOfEffect);
        Assert.True(data.CanBeLostOnDeath);
        Assert.Equal(-1, data.MineBaseLevel);
        Assert.Equal(-1, data.MineMinLevel);
        Assert.Equal("Rare", data.CustomFields["romulot.ValleyArmory/Rarity"]);
    }

    [Fact]
    public void MinersBladeQualifiedItemIdIsPermanent()
    {
        EquipmentDefinition definition = LoadMinersBlade();

        Assert.Equal("(W)romulot.ValleyArmory_MinersBlade", EquipmentIdentity.GetQualifiedItemId(definition));
        Assert.Equal("(W)romulot.ValleyArmory_MinersBlade", MinersBladeWeaponDataFactory.QualifiedItemId);
    }

    [Fact]
    public void AssetMergeDoesNotReplaceAnExistingEntry()
    {
        Dictionary<string, string> data = new(StringComparer.Ordinal)
        {
            [MinersBladeWeaponDataFactory.ItemId] = "existing"
        };

        bool added = NonOverwritingAssetEditor.TryAdd(
            data,
            MinersBladeWeaponDataFactory.ItemId,
            "replacement"
        );

        Assert.False(added);
        Assert.Equal("existing", data[MinersBladeWeaponDataFactory.ItemId]);
        Assert.Single(data);
    }

    private static EquipmentDefinition LoadMinersBlade()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return Assert.Single(catalog.Equipment, item => item.Id == MinersBladeWeaponDataFactory.ItemId);
    }
}
