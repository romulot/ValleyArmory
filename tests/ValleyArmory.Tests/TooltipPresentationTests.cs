using Microsoft.Xna.Framework;
using ValleyArmory.Catalog;
using ValleyArmory.Tooltips;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class TooltipPresentationTests
{
    [Fact]
    public void MinersBladeResolvesRarePresentationFromCatalog()
    {
        TooltipPresentationResolver resolver = CreateResolver();
        Dictionary<string, string> translations = new(StringComparer.Ordinal)
        {
            ["tooltip.rarity.rare"] = "Rarity: Rare"
        };

        bool found = resolver.TryResolve(
            "(W)romulot.ValleyArmory_MinersBlade",
            key => translations[key],
            out TooltipPresentation? presentation
        );

        Assert.True(found);
        Assert.NotNull(presentation);
        Assert.Equal(new Color(0x3B, 0x82, 0xF6), presentation.NameColor);
        Assert.Equal("Rarity: Rare", presentation.RarityText);
    }

    [Fact]
    public void MinersBladeCanResolvePortugueseText()
    {
        TooltipPresentationResolver resolver = CreateResolver();
        Dictionary<string, string> translations = new(StringComparer.Ordinal)
        {
            ["tooltip.rarity.rare"] = "Raridade: Rara"
        };

        Assert.True(resolver.TryResolve(
            "(W)romulot.ValleyArmory_MinersBlade",
            key => translations[key],
            out TooltipPresentation? presentation
        ));
        Assert.Equal("Raridade: Rara", presentation!.RarityText);
    }

    [Theory]
    [InlineData("(W)romulot.ValleyArmory_BlackIronSword")]
    [InlineData("(W)0")]
    [InlineData(null)]
    public void AnyItemOtherThanMinersBladeUsesVanillaFallback(string? qualifiedItemId)
    {
        bool found = CreateResolver().TryResolve(qualifiedItemId, key => key, out TooltipPresentation? presentation);

        Assert.False(found);
        Assert.Null(presentation);
    }

    private static TooltipPresentationResolver CreateResolver()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return new TooltipPresentationResolver(new CatalogIndex(catalog));
    }
}
