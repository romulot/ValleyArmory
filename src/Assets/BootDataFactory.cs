using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

/// <summary>
/// Converts a Boots <see cref="EquipmentDefinition"/> into the raw slash-separated
/// value expected by Data/Boots in Stardew Valley 1.6.15. The vanilla format has
/// 7 positional fields (Name/Description/Price/Defense/Immunity/ColorIndex/DisplayName);
/// fields 7-9 are an undocumented extension confirmed by disassembling
/// StardewValley.Objects.Boots.reloadData/GetBootsColorString and
/// StardewValley.ItemTypeDefinitions.BootsDataDefinition.GetData/GetSpriteIndex/GetSourceRect
/// in the installed 1.6.15.24356 assembly: field 8 is an optional explicit sprite index and
/// field 9 an optional explicit icon texture name, letting a non-numeric mod ID resolve its
/// own icon instead of the vanilla "Maps/springobjects" sheet keyed by legacy numeric ID.
/// </summary>
internal sealed class BootDataFactory
{
    public string Create(EquipmentDefinition definition, Func<string, string> translate)
    {
        if (definition.Type is not EquipmentType.Boots)
        {
            throw new ArgumentException(
                $"Equipment '{definition.Id}' is not a boots definition.",
                nameof(definition)
            );
        }

        EquipmentStats stats = definition.Stats
            ?? throw new ArgumentException($"Boots '{definition.Id}' stats are missing.", nameof(definition));
        SpriteReference sprite = definition.Sprite
            ?? throw new ArgumentException($"Boots '{definition.Id}' sprite reference is missing.", nameof(definition));
        int? immunity = stats.Immunity
            ?? throw MissingStat(definition, nameof(stats.Immunity));
        int? colorIndex = definition.ColorIndex
            ?? throw new ArgumentException($"Boots '{definition.Id}' color index is missing.", nameof(definition));

        string displayName = RejectSlash(definition, translate(definition.DisplayNameKey), "displayName");
        string description = RejectSlash(definition, translate(definition.DescriptionKey), "description");
        string name = RejectSlash(definition, definition.Id, "name");

        // The whole raw Data/Boots value is split on '/' by the game, so the asset name
        // (which legitimately contains '/' as its path separator, e.g. "Mods/Author/Boots")
        // must be re-encoded with '\' before being embedded in field 9. SMAPI treats '/' and
        // '\' as equivalent in asset names, and this is exactly the convention the vanilla
        // fallback itself uses internally ("Maps\springobjects", backslash, not forward slash).
        string textureName = sprite.AssetName.Replace('/', '\\');

        string[] fields =
        {
            name,
            description,
            stats.Price.ToString(),
            stats.Defense.ToString(),
            immunity.Value.ToString(),
            colorIndex.Value.ToString(),
            displayName,
            string.Empty,
            sprite.SpriteIndex.ToString(),
            textureName
        };

        return string.Join('/', fields);
    }

    private static string RejectSlash(EquipmentDefinition definition, string value, string fieldName)
    {
        if (value.Contains('/'))
        {
            throw new ArgumentException(
                $"Boots '{definition.Id}' field '{fieldName}' cannot contain '/': it would break the Data/Boots raw format.",
                nameof(definition)
            );
        }

        return value;
    }

    private static ArgumentException MissingStat(EquipmentDefinition definition, string statName)
    {
        return new ArgumentException($"Boots '{definition.Id}' is missing required stat '{statName}'.", nameof(definition));
    }
}
