using StardewValley.GameData.Shirts;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

/// <summary>
/// Converts a Shirt <see cref="EquipmentDefinition"/> into a <see cref="ShirtData"/> entry for
/// Data/Shirts. Unlike Data/Boots, Data/Shirts in Stardew Valley 1.6.15 is already a strongly
/// typed <c>Dictionary&lt;string, ShirtData&gt;</c> (confirmed by loading the real vanilla asset
/// and inspecting <c>StardewValley.GameData.Shirts.ShirtData</c> in the installed assembly) with
/// an explicit <see cref="ShirtData.Texture"/> field, so no legacy raw-string workaround is
/// needed here — this mirrors <see cref="WeaponDataFactory"/> almost directly.
/// </summary>
internal sealed class ArmorDataFactory
{
    public ShirtData Create(EquipmentDefinition definition, Func<string, string> translate)
    {
        if (definition.Type is not EquipmentType.Shirt)
        {
            throw new ArgumentException(
                $"Equipment '{definition.Id}' is not an armor (shirt) definition.",
                nameof(definition)
            );
        }

        EquipmentStats stats = definition.Stats
            ?? throw new ArgumentException($"Armor '{definition.Id}' stats are missing.", nameof(definition));
        SpriteReference sprite = definition.Sprite
            ?? throw new ArgumentException($"Armor '{definition.Id}' sprite reference is missing.", nameof(definition));

        return new ShirtData
        {
            Name = definition.Id,
            DisplayName = translate(definition.DisplayNameKey),
            Description = translate(definition.DescriptionKey),
            Price = stats.Price,
            Texture = sprite.AssetName,
            SpriteIndex = sprite.SpriteIndex,
            DefaultColor = null,
            CanBeDyed = false,
            IsPrismatic = false,
            HasSleeves = false,
            CanChooseDuringCharacterCustomization = false,
            CustomFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["romulot.ValleyArmory/Rarity"] = definition.Rarity
            }
        };
    }
}
