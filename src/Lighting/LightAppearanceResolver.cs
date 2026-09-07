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
    private const string MinersBladeQualifiedItemId = MinersBladeWeaponDataFactory.QualifiedItemId;
    private readonly bool hasMinersBladeAppearance;
    private readonly WeaponLightAppearance minersBladeAppearance;

    public LightAppearanceResolver(CatalogIndex catalog)
    {
        this.hasMinersBladeAppearance = TryBuildFromCatalog(catalog, out this.minersBladeAppearance);
    }

    public bool TryResolve(string? qualifiedItemId, out WeaponLightAppearance appearance)
    {
        appearance = default;
        if (!this.hasMinersBladeAppearance
            || !string.Equals(qualifiedItemId, MinersBladeQualifiedItemId, StringComparison.Ordinal))
        {
            return false;
        }

        appearance = this.minersBladeAppearance;
        return true;
    }

    private static bool TryBuildFromCatalog(CatalogIndex catalog, out WeaponLightAppearance appearance)
    {
        appearance = default;
        if (!catalog.TryGetByQualifiedId(MinersBladeQualifiedItemId, out EquipmentDefinition? equipment)
            || equipment is null
            || !catalog.TryGetRarity(equipment.Rarity, out RarityDefinition? rarity)
            || rarity is null)
        {
            return false;
        }

        LightOverride? lightOverride = equipment.OptionalVisualOverrides?.Light;
        bool enabled = lightOverride?.Enabled ?? rarity.LightEnabled;
        if (!enabled)
            return false;

        string colorHex = lightOverride?.Color ?? rarity.LightColor;
        float radius = lightOverride?.Radius ?? rarity.LightRadius;
        float intensity = lightOverride?.Intensity ?? rarity.LightIntensity;
        VectorOffset? offset = lightOverride?.Offset ?? rarity.LightOffset;

        if (radius <= 0 || intensity is < 0 or > 1 || !TryParseRgb(colorHex, out Color color))
            return false;

        appearance = new WeaponLightAppearance(color, radius, intensity, offset is null
            ? Vector2.Zero
            : new Vector2(offset.X, offset.Y));
        return true;
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
