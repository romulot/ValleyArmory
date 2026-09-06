using System.Text.Json.Serialization;

namespace ValleyArmory.Catalog;

internal sealed class ArmoryCatalog
{
    public int SchemaVersion { get; init; }

    public IReadOnlyList<RarityDefinition> Rarities { get; init; } = Array.Empty<RarityDefinition>();

    public IReadOnlyList<EquipmentDefinition> Equipment { get; init; } = Array.Empty<EquipmentDefinition>();
}

internal sealed class EquipmentDefinition
{
    public string Id { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EquipmentType? Type { get; init; }

    public string Rarity { get; init; } = string.Empty;

    public string DisplayNameKey { get; init; } = string.Empty;

    public string DescriptionKey { get; init; } = string.Empty;

    public EquipmentStats? Stats { get; init; }

    public AcquisitionMetadata? Acquisition { get; init; }

    public SpriteReference? Sprite { get; init; }

    public OptionalVisualOverrides? OptionalVisualOverrides { get; init; }
}

internal enum EquipmentType
{
    Sword,
    Dagger,
    Hammer,
    Boots
}

internal sealed class EquipmentStats
{
    public int? MinDamage { get; init; }

    public int? MaxDamage { get; init; }

    public int? Speed { get; init; }

    public int Defense { get; init; }

    public float? CritChance { get; init; }

    public float? CritMultiplier { get; init; }

    public float? Knockback { get; init; }

    public int? Immunity { get; init; }

    public int Price { get; init; }
}

internal sealed class AcquisitionMetadata
{
    public string Method { get; init; } = string.Empty;

    public string? Notes { get; init; }
}

internal sealed class SpriteReference
{
    public string AssetName { get; init; } = string.Empty;

    public int SpriteIndex { get; init; }
}

internal sealed class OptionalVisualOverrides
{
    public LightOverride? Light { get; init; }
}

internal sealed class LightOverride
{
    public bool? Enabled { get; init; }

    public string? Color { get; init; }

    public float? Radius { get; init; }

    public float? Intensity { get; init; }

    public VectorOffset? Offset { get; init; }
}

internal sealed class RarityDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayNameKey { get; init; } = string.Empty;

    public string NameColor { get; init; } = string.Empty;

    public bool LightEnabled { get; init; }

    public string LightColor { get; init; } = string.Empty;

    public float LightRadius { get; init; }

    public float LightIntensity { get; init; }

    public VectorOffset? LightOffset { get; init; }
}

internal sealed class VectorOffset
{
    public float X { get; init; }

    public float Y { get; init; }
}
