using ValleyArmory.Catalog;
using ValleyArmory.DeveloperTools;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class DeveloperToolsTests
{
    [Fact]
    public void DeveloperWeaponCatalogContainsExactlySevenWeaponsInSpriteOrder()
    {
        DeveloperWeaponCatalog catalog = CreateCatalog();

        Assert.Equal(7, catalog.Entries.Count);
        Assert.Equal(
            new[] { "miners-blade", "black-iron-sword", "prismatic-blade", "shadow-fang", "moon-dagger", "stonebreaker", "abyss-hammer" },
            catalog.Entries.Select(entry => entry.Alias)
        );
        Assert.All(catalog.Entries, entry => Assert.StartsWith("(W)", entry.QualifiedItemId, StringComparison.Ordinal));
        Assert.DoesNotContain(catalog.Entries, entry => entry.Definition.Type == EquipmentType.Boots);
    }

    [Theory]
    [InlineData("miners-blade", "(W)romulot.ValleyArmory_MinersBlade")]
    [InlineData("black-iron-sword", "(W)romulot.ValleyArmory_BlackIronSword")]
    [InlineData("prismatic-blade", "(W)romulot.ValleyArmory_PrismaticBlade")]
    [InlineData("shadow-fang", "(W)romulot.ValleyArmory_ShadowFang")]
    [InlineData("moon-dagger", "(W)romulot.ValleyArmory_MoonDagger")]
    [InlineData("stonebreaker", "(W)romulot.ValleyArmory_Stonebreaker")]
    [InlineData("abyss-hammer", "(W)romulot.ValleyArmory_AbyssHammer")]
    public void AliasResolvesExpectedQualifiedItemId(string alias, string qualifiedItemId)
    {
        Assert.True(CreateCatalog().TryResolve(alias, out DeveloperWeaponEntry? entry));
        Assert.Equal(qualifiedItemId, entry!.QualifiedItemId);
    }

    [Fact]
    public void AliasResolutionIsCaseInsensitiveButUnknownAliasesFail()
    {
        DeveloperWeaponCatalog catalog = CreateCatalog();

        Assert.True(catalog.TryResolve("MINERS-BLADE", out _));
        Assert.False(catalog.TryResolve("miners-boots", out _));
        Assert.False(catalog.TryResolve("unknown-weapon", out _));
    }

    [Fact]
    public void AliasConversionIsCentralizedFromPermanentId()
    {
        Assert.Equal("black-iron-sword", DeveloperWeaponCatalog.ToAlias("romulot.ValleyArmory_BlackIronSword"));
        Assert.Equal("abyss-hammer", DeveloperWeaponCatalog.ToAlias("romulot.ValleyArmory_AbyssHammer"));
    }

    private static DeveloperWeaponCatalog CreateCatalog()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return new DeveloperWeaponCatalog(new CatalogIndex(catalog));
    }
}
