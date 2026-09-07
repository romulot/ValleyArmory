using ValleyArmory.Catalog;
using ValleyArmory.DeveloperTools;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class DeveloperToolsTests
{
    [Fact]
    public void DeveloperEquipmentCatalogContainsExactlyTenEquipmentInCanonicalOrder()
    {
        DeveloperEquipmentCatalog catalog = CreateCatalog();

        Assert.Equal(10, catalog.Entries.Count);
        Assert.Equal(
            new[]
            {
                "miners-blade", "black-iron-sword", "prismatic-blade", "shadow-fang", "moon-dagger", "stonebreaker", "abyss-hammer",
                "miners-boots", "obsidian-boots", "ethereal-boots"
            },
            catalog.Entries.Select(entry => entry.Alias)
        );
    }

    [Fact]
    public void WeaponsResolveWithWQualifierAndBootsWithBQualifier()
    {
        DeveloperEquipmentCatalog catalog = CreateCatalog();

        DeveloperEquipmentEntry[] weapons = catalog.Entries.Where(entry => entry.Definition.Type != EquipmentType.Boots).ToArray();
        DeveloperEquipmentEntry[] boots = catalog.Entries.Where(entry => entry.Definition.Type == EquipmentType.Boots).ToArray();

        Assert.Equal(7, weapons.Length);
        Assert.Equal(3, boots.Length);
        Assert.All(weapons, entry => Assert.StartsWith("(W)", entry.QualifiedItemId, StringComparison.Ordinal));
        Assert.All(boots, entry => Assert.StartsWith("(B)", entry.QualifiedItemId, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("miners-blade", "(W)romulot.ValleyArmory_MinersBlade")]
    [InlineData("black-iron-sword", "(W)romulot.ValleyArmory_BlackIronSword")]
    [InlineData("prismatic-blade", "(W)romulot.ValleyArmory_PrismaticBlade")]
    [InlineData("shadow-fang", "(W)romulot.ValleyArmory_ShadowFang")]
    [InlineData("moon-dagger", "(W)romulot.ValleyArmory_MoonDagger")]
    [InlineData("stonebreaker", "(W)romulot.ValleyArmory_Stonebreaker")]
    [InlineData("abyss-hammer", "(W)romulot.ValleyArmory_AbyssHammer")]
    [InlineData("miners-boots", "(B)romulot.ValleyArmory_MinersBoots")]
    [InlineData("obsidian-boots", "(B)romulot.ValleyArmory_ObsidianBoots")]
    [InlineData("ethereal-boots", "(B)romulot.ValleyArmory_EtherealBoots")]
    public void AliasResolvesExpectedQualifiedItemId(string alias, string qualifiedItemId)
    {
        Assert.True(CreateCatalog().TryResolve(alias, out DeveloperEquipmentEntry? entry));
        Assert.Equal(qualifiedItemId, entry!.QualifiedItemId);
    }

    [Fact]
    public void AliasResolutionIsCaseInsensitiveButUnknownAliasesFail()
    {
        DeveloperEquipmentCatalog catalog = CreateCatalog();

        Assert.True(catalog.TryResolve("MINERS-BLADE", out _));
        Assert.True(catalog.TryResolve("MINERS-BOOTS", out _));
        Assert.False(catalog.TryResolve("unknown-equipment", out _));
    }

    [Fact]
    public void AllTenAliasesAreUnique()
    {
        DeveloperEquipmentCatalog catalog = CreateCatalog();

        Assert.Equal(catalog.Entries.Count, catalog.Entries.Select(entry => entry.Alias).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void AliasConversionIsCentralizedFromPermanentId()
    {
        Assert.Equal("black-iron-sword", DeveloperEquipmentCatalog.ToAlias("romulot.ValleyArmory_BlackIronSword"));
        Assert.Equal("abyss-hammer", DeveloperEquipmentCatalog.ToAlias("romulot.ValleyArmory_AbyssHammer"));
        Assert.Equal("obsidian-boots", DeveloperEquipmentCatalog.ToAlias("romulot.ValleyArmory_ObsidianBoots"));
    }

    private static DeveloperEquipmentCatalog CreateCatalog()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return new DeveloperEquipmentCatalog(new CatalogIndex(catalog));
    }
}
