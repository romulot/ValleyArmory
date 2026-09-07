using ValleyArmory.Catalog;
using ValleyArmory.DeveloperTools;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class DeveloperToolsTests
{
    [Fact]
    public void DeveloperEquipmentCatalogContainsExactlyThirteenEquipmentInCanonicalOrder()
    {
        DeveloperEquipmentCatalog catalog = CreateCatalog();

        Assert.Equal(13, catalog.Entries.Count);
        Assert.Equal(
            new[]
            {
                "miners-blade", "black-iron-sword", "prismatic-blade", "shadow-fang", "moon-dagger", "stonebreaker", "abyss-hammer",
                "miners-boots", "obsidian-boots", "ethereal-boots",
                "miners-armor", "obsidian-armor", "ethereal-armor"
            },
            catalog.Entries.Select(entry => entry.Alias)
        );
    }

    [Fact]
    public void WeaponsBootsAndArmorResolveWithExpectedQualifiers()
    {
        DeveloperEquipmentCatalog catalog = CreateCatalog();

        DeveloperEquipmentEntry[] weapons = catalog.Entries.Where(entry => entry.Definition.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer).ToArray();
        DeveloperEquipmentEntry[] boots = catalog.Entries.Where(entry => entry.Definition.Type == EquipmentType.Boots).ToArray();
        DeveloperEquipmentEntry[] armor = catalog.Entries.Where(entry => entry.Definition.Type == EquipmentType.Shirt).ToArray();

        Assert.Equal(7, weapons.Length);
        Assert.Equal(3, boots.Length);
        Assert.Equal(3, armor.Length);
        Assert.All(weapons, entry => Assert.StartsWith("(W)", entry.QualifiedItemId, StringComparison.Ordinal));
        Assert.All(boots, entry => Assert.StartsWith("(B)", entry.QualifiedItemId, StringComparison.Ordinal));
        Assert.All(armor, entry => Assert.StartsWith("(S)", entry.QualifiedItemId, StringComparison.Ordinal));
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
    [InlineData("miners-armor", "(S)romulot.ValleyArmory_MinersArmor")]
    [InlineData("obsidian-armor", "(S)romulot.ValleyArmory_ObsidianArmor")]
    [InlineData("ethereal-armor", "(S)romulot.ValleyArmory_EtherealArmor")]
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
        Assert.True(catalog.TryResolve("MINERS-ARMOR", out _));
        Assert.False(catalog.TryResolve("unknown-equipment", out _));
    }

    [Fact]
    public void AllThirteenAliasesAreUnique()
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
        Assert.Equal("ethereal-armor", DeveloperEquipmentCatalog.ToAlias("romulot.ValleyArmory_EtherealArmor"));
    }

    private static DeveloperEquipmentCatalog CreateCatalog()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return new DeveloperEquipmentCatalog(new CatalogIndex(catalog));
    }
}
