using StardewValley.GameData.Weapons;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

internal sealed class MinersBladeWeaponDataFactory
{
    internal const string ItemId = "romulot.ValleyArmory_MinersBlade";
    internal const string QualifiedItemId = "(W)romulot.ValleyArmory_MinersBlade";

    public WeaponData Create(EquipmentDefinition definition, Func<string, string> translate)
    {
        if (!string.Equals(definition.Id, ItemId, StringComparison.Ordinal) ||
            definition.Type is not EquipmentType.Sword)
        {
            throw new ArgumentException(
                $"Expected the Miner's Blade definition '{ItemId}' with type Sword.",
                nameof(definition)
            );
        }

        EquipmentStats stats = definition.Stats
            ?? throw new ArgumentException("Miner's Blade stats are missing.", nameof(definition));
        SpriteReference sprite = definition.Sprite
            ?? throw new ArgumentException("Miner's Blade sprite reference is missing.", nameof(definition));

        return new WeaponData
        {
            Name = definition.Id,
            DisplayName = translate(definition.DisplayNameKey),
            Description = translate(definition.DescriptionKey),
            Type = 3,
            Texture = sprite.AssetName,
            SpriteIndex = sprite.SpriteIndex,
            MinDamage = stats.MinDamage!.Value,
            MaxDamage = stats.MaxDamage!.Value,
            Knockback = stats.Knockback!.Value,
            Speed = stats.Speed!.Value,
            Precision = 0,
            Defense = stats.Defense,
            AreaOfEffect = 0,
            CritChance = stats.CritChance!.Value,
            CritMultiplier = stats.CritMultiplier!.Value,
            CanBeLostOnDeath = true,
            MineBaseLevel = -1,
            MineMinLevel = -1,
            CustomFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["romulot.ValleyArmory/Rarity"] = definition.Rarity
            }
        };
    }
}
