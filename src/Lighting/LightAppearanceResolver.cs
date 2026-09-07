using Microsoft.Xna.Framework;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;

namespace ValleyArmory.Lighting;

internal readonly record struct WeaponLightAppearance(Color Color, float Radius, float Intensity, Vector2 Offset)
{
    public Color ToRuntimeColor()
    {
        float factor = Math.Clamp(this.Intensity, 0f, 1f);
        return new Color(
            (byte)Math.Clamp((int)Math.Round(this.Color.R * factor), 0, 255),
            (byte)Math.Clamp((int)Math.Round(this.Color.G * factor), 0, 255),
            (byte)Math.Clamp((int)Math.Round(this.Color.B * factor), 0, 255)
        );
    }
}

internal sealed class LightAppearanceResolver
{
    private readonly IReadOnlyDictionary<string, WeaponLightAppearance> appearances;

    public LightAppearanceResolver(CatalogIndex catalog)
    {
        this.appearances = catalog.GetAllEquipment()
            .Where(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer)
            .Select(item => (QualifiedItemId: EquipmentIdentity.GetQualifiedItemId(item), Item: item))
            .Select(value => (value.QualifiedItemId, Appearance: TryBuildFromCatalog(catalog, value.Item)))
            .Where(value => value.Appearance is not null)
            .ToDictionary(value => value.QualifiedItemId, value => value.Appearance!.Value, StringComparer.Ordinal);
    }

    public bool TryResolve(string? qualifiedItemId, out WeaponLightAppearance appearance)
    {
        appearance = default;
        return qualifiedItemId is not null && this.appearances.TryGetValue(qualifiedItemId, out appearance);
    }

    private static WeaponLightAppearance? TryBuildFromCatalog(CatalogIndex catalog, EquipmentDefinition equipment)
    {
        if (!catalog.TryGetRarity(equipment.Rarity, out RarityDefinition? rarity) || rarity is null)
            return null;

        LightOverride? lightOverride = equipment.OptionalVisualOverrides?.Light;
        bool enabled = lightOverride?.Enabled ?? rarity.LightEnabled;
        if (!enabled)
            return null;

        string colorHex = lightOverride?.Color ?? rarity.LightColor;
        float radius = lightOverride?.Radius ?? rarity.LightRadius;
        float intensity = lightOverride?.Intensity ?? rarity.LightIntensity;
        VectorOffset? offset = lightOverride?.Offset ?? rarity.LightOffset;

        if (radius <= 0 || intensity is < 0 or > 1 || !TryParseRgb(colorHex, out Color color))
            return null;

        return new WeaponLightAppearance(
            color,
            radius,
            intensity,
            offset is null ? Vector2.Zero : new Vector2(offset.X, offset.Y)
        );
    }

    private static bool TryParseRgb(string value, out Color color)
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
