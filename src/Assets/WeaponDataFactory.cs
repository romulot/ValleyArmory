using StardewValley.GameData.Weapons;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

internal sealed class WeaponDataFactory
{
    public WeaponData Create(EquipmentDefinition definition, Func<string, string> translate)
    {
        if (definition.Type is not (EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer))
        {
            throw new ArgumentException(
                $"Equipment '{definition.Id}' is not a weapon definition.",
                nameof(definition)
            );
        }

        if (definition.WeaponBehavior is null)
        {
            throw new ArgumentException(
                $"Weapon '{definition.Id}' has no weapon behavior.",
                nameof(definition)
            );
        }

        EquipmentStats stats = definition.Stats
            ?? throw new ArgumentException($"Weapon '{definition.Id}' stats are missing.", nameof(definition));
        SpriteReference sprite = definition.Sprite
            ?? throw new ArgumentException($"Weapon '{definition.Id}' sprite reference is missing.", nameof(definition));

        return new WeaponData
        {
            Name = definition.Id,
            DisplayName = translate(definition.DisplayNameKey),
            Description = translate(definition.DescriptionKey),
            Type = GetVanillaType(definition.WeaponBehavior.Value),
            Texture = sprite.AssetName,
            SpriteIndex = sprite.SpriteIndex,
            MinDamage = stats.MinDamage ?? throw MissingStat(definition, nameof(stats.MinDamage)),
            MaxDamage = stats.MaxDamage ?? throw MissingStat(definition, nameof(stats.MaxDamage)),
            Knockback = stats.Knockback ?? throw MissingStat(definition, nameof(stats.Knockback)),
            Speed = stats.Speed ?? throw MissingStat(definition, nameof(stats.Speed)),
            Precision = stats.Precision,
            Defense = stats.Defense,
            AreaOfEffect = stats.AreaOfEffect,
            CritChance = stats.CritChance ?? throw MissingStat(definition, nameof(stats.CritChance)),
            CritMultiplier = stats.CritMultiplier ?? throw MissingStat(definition, nameof(stats.CritMultiplier)),
            CanBeLostOnDeath = true,
            MineBaseLevel = -1,
            MineMinLevel = -1,
            CustomFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["romulot.ValleyArmory/Rarity"] = definition.Rarity
            }
        };
    }

    private static int GetVanillaType(WeaponBehavior behavior)
    {
        return behavior switch
        {
            WeaponBehavior.StabbingSword => 0,
            WeaponBehavior.Dagger => 1,
            WeaponBehavior.Club => 2,
            WeaponBehavior.DefenseSword => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unsupported weapon behavior.")
        };
    }

    private static ArgumentException MissingStat(EquipmentDefinition definition, string statName)
    {
        return new ArgumentException($"Weapon '{definition.Id}' is missing required stat '{statName}'.", nameof(definition));
    }
}
