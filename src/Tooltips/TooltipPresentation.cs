using Microsoft.Xna.Framework;
using ValleyArmory.Catalog;

namespace ValleyArmory.Tooltips;

internal sealed record TooltipPresentation(Color NameColor, string RarityText);

internal sealed class TooltipPresentationResolver
{
    private readonly CatalogIndex catalog;

    public TooltipPresentationResolver(CatalogIndex catalog)
    {
        this.catalog = catalog;
    }

    public bool TryResolve(
        string? qualifiedItemId,
        Func<string, string> translate,
        out TooltipPresentation? presentation
    )
    {
        presentation = null;
        if (!string.Equals(qualifiedItemId, Assets.MinersBladeWeaponDataFactory.QualifiedItemId, StringComparison.Ordinal)
            || !this.catalog.TryGetByQualifiedId(qualifiedItemId!, out EquipmentDefinition? equipment)
            || equipment is null
            || !this.catalog.TryGetRarity(equipment.Rarity, out RarityDefinition? rarity)
            || rarity is null
            || !TryParseRgb(rarity.NameColor, out Color color))
        {
            return false;
        }

        string rarityText = translate($"tooltip.rarity.{rarity.Id.ToLowerInvariant()}");
        presentation = new TooltipPresentation(color, rarityText);
        return true;
    }

    internal static bool TryParseRgb(string value, out Color color)
    {
        color = default;
        if (value.Length != 7 || value[0] != '#')
            return false;

        if (!byte.TryParse(value.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out byte red)
            || !byte.TryParse(value.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out byte green)
            || !byte.TryParse(value.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out byte blue))
        {
            return false;
        }

        color = new Color(red, green, blue);
        return true;
    }
}
