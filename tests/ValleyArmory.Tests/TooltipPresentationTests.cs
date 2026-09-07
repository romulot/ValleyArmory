using Microsoft.Xna.Framework;
using ValleyArmory.Catalog;
using ValleyArmory.Tooltips;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class TooltipPresentationTests
{
    public static IEnumerable<object[]> WeaponRarityCases()
    {
        yield return new object[] { "(W)romulot.ValleyArmory_MinersBlade", "Rare", "Rarity: Rare" };
        yield return new object[] { "(W)romulot.ValleyArmory_BlackIronSword", "Common", "Rarity: Common" };
        yield return new object[] { "(W)romulot.ValleyArmory_PrismaticBlade", "Legendary", "Rarity: Legendary" };
        yield return new object[] { "(W)romulot.ValleyArmory_ShadowFang", "Rare", "Rarity: Rare" };
        yield return new object[] { "(W)romulot.ValleyArmory_MoonDagger", "Epic", "Rarity: Epic" };
        yield return new object[] { "(W)romulot.ValleyArmory_Stonebreaker", "Common", "Rarity: Common" };
        yield return new object[] { "(W)romulot.ValleyArmory_AbyssHammer", "Epic", "Rarity: Epic" };
    }

    [Theory]
    [MemberData(nameof(WeaponRarityCases))]
    public void AllValleyArmoryWeaponsResolvePresentationFromCatalog(string qualifiedItemId, string rarityId, string rarityText)
    {
        bool found = CreateResolver().TryResolve(qualifiedItemId, key => key == $"tooltip.rarity.{rarityId.ToLowerInvariant()}" ? rarityText : key, out TooltipPresentation? presentation);

        Assert.True(found);
        Assert.NotNull(presentation);
        Assert.Equal(rarityText, presentation!.RarityText);
        Assert.Equal(rarityId == "Common" ? null : GetRarityColor(rarityId), presentation.NameColor);
    }

    [Fact]
    public void RareWeaponsShareTheSameCatalogColor()
    {
        TooltipPresentationResolver resolver = CreateResolver();
        resolver.TryResolve("(W)romulot.ValleyArmory_MinersBlade", key => key, out TooltipPresentation? minersBlade);
        resolver.TryResolve("(W)romulot.ValleyArmory_ShadowFang", key => key, out TooltipPresentation? shadowFang);

        Assert.Equal(minersBlade!.NameColor, shadowFang!.NameColor);
    }

    [Fact]
    public void CommonWeaponsKeepVanillaTitleColorButShowRarityLine()
    {
        bool found = CreateResolver().TryResolve(
            "(W)romulot.ValleyArmory_BlackIronSword",
            key => "Rarity: Common",
            out TooltipPresentation? presentation
        );

        Assert.True(found);
        Assert.Null(presentation!.NameColor);
        Assert.Equal("Rarity: Common", presentation.RarityText);
    }

    [Theory]
    [InlineData("(W)0")]
    [InlineData("(B)romulot.ValleyArmory_MinersBoots")]
    [InlineData("(W)other.mod_Sword")]
    [InlineData(null)]
    public void ExternalUnknownAndBootItemsRemainVanilla(string? qualifiedItemId)
    {
        bool found = CreateResolver().TryResolve(qualifiedItemId, key => key, out TooltipPresentation? presentation);

        Assert.False(found);
        Assert.Null(presentation);
    }

    [Fact]
    public void ResolutionUsesQualifiedItemIdNotAliasOrTranslatedName()
    {
        bool found = CreateResolver().TryResolve("miners-blade", key => key, out TooltipPresentation? presentation);

        Assert.False(found);
        Assert.Null(presentation);
    }

    [Fact]
    public void PortugueseRareTextIsLocalized()
    {
        Assert.True(CreateResolver().TryResolve(
            "(W)romulot.ValleyArmory_ShadowFang",
            key => key == "tooltip.rarity.rare" ? "Raridade: Rara" : key,
            out TooltipPresentation? presentation
        ));
        Assert.Equal("Raridade: Rara", presentation!.RarityText);
    }

    private static Color GetRarityColor(string rarityId)
    {
        return rarityId switch
        {
            "Rare" => new Color(0x3B, 0x82, 0xF6),
            "Epic" => new Color(0x9B, 0x59, 0xB6),
            "Legendary" => new Color(0xD4, 0xA7, 0x2C),
            _ => throw new ArgumentOutOfRangeException(nameof(rarityId))
        };
    }

    private static TooltipPresentationResolver CreateResolver()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return new TooltipPresentationResolver(new CatalogIndex(catalog));
    }
}
