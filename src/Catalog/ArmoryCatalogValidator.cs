using System.Text.RegularExpressions;

namespace ValleyArmory.Catalog;

internal sealed class ArmoryCatalogValidator
{
    internal const string ModIdPrefix = "romulot.ValleyArmory_";

    private static readonly HashSet<string> RequiredRarities = new(StringComparer.Ordinal)
    {
        "Common",
        "Rare",
        "Epic",
        "Legendary"
    };

    private static readonly IReadOnlyDictionary<string, EquipmentType> PermanentEquipment =
        new Dictionary<string, EquipmentType>(StringComparer.Ordinal)
    {
        ["romulot.ValleyArmory_MinersBlade"] = EquipmentType.Sword,
        ["romulot.ValleyArmory_BlackIronSword"] = EquipmentType.Sword,
        ["romulot.ValleyArmory_PrismaticBlade"] = EquipmentType.Sword,
        ["romulot.ValleyArmory_ShadowFang"] = EquipmentType.Dagger,
        ["romulot.ValleyArmory_MoonDagger"] = EquipmentType.Dagger,
        ["romulot.ValleyArmory_Stonebreaker"] = EquipmentType.Hammer,
        ["romulot.ValleyArmory_AbyssHammer"] = EquipmentType.Hammer,
        ["romulot.ValleyArmory_MinersBoots"] = EquipmentType.Boots,
        ["romulot.ValleyArmory_ObsidianBoots"] = EquipmentType.Boots,
        ["romulot.ValleyArmory_EtherealBoots"] = EquipmentType.Boots,
        ["romulot.ValleyArmory_MinersArmor"] = EquipmentType.Shirt,
        ["romulot.ValleyArmory_ObsidianArmor"] = EquipmentType.Shirt,
        ["romulot.ValleyArmory_EtherealArmor"] = EquipmentType.Shirt
    };

    private static readonly Regex PermanentIdPattern = new(
        "^romulot\\.ValleyArmory_[A-Z][A-Za-z0-9]*$",
        RegexOptions.CultureInvariant
    );

    private static readonly Regex ColorPattern = new(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.CultureInvariant
    );

    private static readonly Regex TranslationKeyPattern = new(
        "^[a-z0-9]+(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant
    );

    public void ValidateAndThrow(ArmoryCatalog catalog)
    {
        IReadOnlyList<string> errors = this.Validate(catalog);
        if (errors.Count > 0)
        {
            throw new CatalogValidationException(errors);
        }
    }

    public IReadOnlyList<string> Validate(ArmoryCatalog catalog)
    {
        List<string> errors = new();

        if (catalog.SchemaVersion != 1)
        {
            errors.Add($"catalog.schemaVersion: expected 1, found {catalog.SchemaVersion}.");
        }

        this.ValidateRarities(catalog.Rarities ?? Array.Empty<RarityDefinition>(), errors);
        this.ValidateEquipment(
            catalog.Equipment ?? Array.Empty<EquipmentDefinition>(),
            catalog.Rarities ?? Array.Empty<RarityDefinition>(),
            errors
        );
        return errors;
    }

    private void ValidateRarities(IReadOnlyList<RarityDefinition> rarities, List<string> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (RarityDefinition rarity in rarities)
        {
            string field = $"rarity[{rarity.Id ?? "<missing>"}]";
            if (string.IsNullOrWhiteSpace(rarity.Id))
            {
                errors.Add($"{field}.id: value is required.");
                continue;
            }

            if (!seen.Add(rarity.Id))
            {
                errors.Add($"{field}.id: duplicate rarity ID '{rarity.Id}'.");
            }

            RequireTranslationKey(rarity.DisplayNameKey, $"{field}.displayNameKey", errors);
            ValidateColor(rarity.NameColor, $"{field}.nameColor", errors);
            ValidateColor(rarity.LightColor, $"{field}.lightColor", errors);
            ValidateLight(rarity.LightEnabled, rarity.LightRadius, rarity.LightIntensity, rarity.LightOffset, field, errors);
        }

        foreach (string required in RequiredRarities.Except(seen, StringComparer.Ordinal))
        {
            errors.Add($"catalog.rarities: required rarity '{required}' is missing.");
        }

        foreach (string unexpected in seen.Except(RequiredRarities, StringComparer.Ordinal))
        {
            errors.Add($"rarity[{unexpected}].id: unsupported rarity.");
        }
    }

    private void ValidateEquipment(
        IReadOnlyList<EquipmentDefinition> equipment,
        IReadOnlyList<RarityDefinition> rarities,
        List<string> errors
    )
    {
        int weaponCount = equipment.Count(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer);
        int bootsCount = equipment.Count(item => item.Type is EquipmentType.Boots);
        int shirtCount = equipment.Count(item => item.Type is EquipmentType.Shirt);
        if (weaponCount != 7 || bootsCount != 3 || shirtCount != 3)
        {
            errors.Add($"catalog.equipment: expected exactly 7 weapons, 3 boots and 3 armor shirts, found {weaponCount} weapons, {bootsCount} boots and {shirtCount} armor shirts.");
        }

        HashSet<string> rarityIds = rarities.Select(rarity => rarity.Id).ToHashSet(StringComparer.Ordinal);
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (EquipmentDefinition item in equipment)
        {
            string field = $"equipment[{item.Id ?? "<missing>"}]";
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                errors.Add($"{field}.id: value is required.");
            }
            else
            {
                if (!ids.Add(item.Id))
                {
                    errors.Add($"{field}.id: duplicate equipment ID '{item.Id}'.");
                }

                if (!item.Id.StartsWith(ModIdPrefix, StringComparison.Ordinal) || !PermanentIdPattern.IsMatch(item.Id))
                {
                    errors.Add($"{field}.id: must be a namespaced permanent ID matching '{ModIdPrefix}<PascalCaseName>'.");
                }

                if (!PermanentEquipment.TryGetValue(item.Id, out EquipmentType expectedType))
                {
                    errors.Add($"{field}.id: ID is not one of the thirteen reserved permanent equipment IDs.");
                }
                else if (item.Type is not null && item.Type != expectedType)
                {
                    errors.Add($"{field}.type: permanent ID requires type '{expectedType}', found '{item.Type}'.");
                }
            }

            if (item.Type is null)
            {
                errors.Add($"{field}.type: value is required.");
            }

            if (item.Type is EquipmentType.Boots or EquipmentType.Shirt && item.WeaponBehavior is not null)
            {
                errors.Add($"{field}.weaponBehavior: boots and armor cannot define a weapon behavior.");
            }

            if (item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer
                && item.WeaponBehavior is null)
            {
                errors.Add($"{field}.weaponBehavior: weapons require a weapon behavior.");
            }

            if (item.Type is EquipmentType.Boots && item.ColorIndex is null or < 0 or > 18)
            {
                errors.Add($"{field}.colorIndex: boots require a value between 0 and 18 (vanilla shoe color palette).");
            }

            if (item.Type is not EquipmentType.Boots && item.ColorIndex is not null)
            {
                errors.Add($"{field}.colorIndex: only boots may define a boots color index.");
            }

            if (!rarityIds.Contains(item.Rarity))
            {
                errors.Add($"{field}.rarity: unknown rarity '{item.Rarity}'.");
            }

            RequireTranslationKey(item.DisplayNameKey, $"{field}.displayNameKey", errors);
            RequireTranslationKey(item.DescriptionKey, $"{field}.descriptionKey", errors);
            this.ValidateStats(item, field, errors);
            this.ValidateSprite(item.Sprite, field, errors);
            this.ValidateAcquisition(item, field, errors);
            this.ValidateOverrides(item.OptionalVisualOverrides, field, errors);
        }

        foreach (string missing in PermanentEquipment.Keys.Except(ids, StringComparer.Ordinal))
        {
            errors.Add($"catalog.equipment: reserved permanent ID '{missing}' is missing.");
        }
    }

    private void ValidateStats(EquipmentDefinition item, string field, List<string> errors)
    {
        EquipmentStats? stats = item.Stats;
        if (stats is null)
        {
            errors.Add($"{field}.stats: value is required.");
            return;
        }

        if (stats.Price < 0)
        {
            errors.Add($"{field}.stats.price: must be zero or greater.");
        }

        if (stats.Defense < 0)
        {
            errors.Add($"{field}.stats.defense: must be zero or greater.");
        }

        if (stats.Precision < 0)
        {
            errors.Add($"{field}.stats.precision: must be zero or greater.");
        }

        if (stats.AreaOfEffect < 0)
        {
            errors.Add($"{field}.stats.areaOfEffect: must be zero or greater.");
        }

        if (item.Type is EquipmentType.Boots)
        {
            if (stats.Immunity is null or < 0)
            {
                errors.Add($"{field}.stats.immunity: boots require a value of zero or greater.");
            }

            if (stats.MinDamage is not null || stats.MaxDamage is not null || stats.Speed is not null ||
                stats.CritChance is not null || stats.CritMultiplier is not null || stats.Knockback is not null ||
                stats.Precision != 0 || stats.AreaOfEffect != 0)
            {
                errors.Add($"{field}.stats: boots cannot define weapon stats.");
            }

            return;
        }

        if (item.Type is EquipmentType.Shirt)
        {
            if (stats.Defense != 0 || stats.Immunity is not null)
            {
                errors.Add($"{field}.stats: armor cannot define Defense or Immunity in this phase.");
            }

            if (stats.MinDamage is not null || stats.MaxDamage is not null || stats.Speed is not null ||
                stats.CritChance is not null || stats.CritMultiplier is not null || stats.Knockback is not null ||
                stats.Precision != 0 || stats.AreaOfEffect != 0)
            {
                errors.Add($"{field}.stats: armor cannot define weapon stats.");
            }

            return;
        }

        if (item.Type is null)
        {
            return;
        }

        if (stats.MinDamage is null)
        {
            errors.Add($"{field}.stats.minDamage: weapons require this field.");
        }

        if (stats.MaxDamage is null)
        {
            errors.Add($"{field}.stats.maxDamage: weapons require this field.");
        }

        if (stats.MinDamage is < 0)
        {
            errors.Add($"{field}.stats.minDamage: must be zero or greater.");
        }

        if (stats.MaxDamage is < 0)
        {
            errors.Add($"{field}.stats.maxDamage: must be zero or greater.");
        }

        if (stats.MinDamage > stats.MaxDamage)
        {
            errors.Add($"{field}.stats.minDamage: must be less than or equal to maxDamage.");
        }

        if (stats.Speed is null)
        {
            errors.Add($"{field}.stats.speed: weapons require this field.");
        }

        if (stats.CritChance is null or < 0 or > 1)
        {
            errors.Add($"{field}.stats.critChance: weapons require a value between 0 and 1.");
        }

        if (stats.CritMultiplier is null or <= 0)
        {
            errors.Add($"{field}.stats.critMultiplier: weapons require a value greater than zero.");
        }

        if (stats.Knockback is null or < 0)
        {
            errors.Add($"{field}.stats.knockback: weapons require a value of zero or greater.");
        }

        if (stats.Immunity is not null)
        {
            errors.Add($"{field}.stats.immunity: weapons cannot define immunity.");
        }
    }

    private void ValidateSprite(SpriteReference? sprite, string field, List<string> errors)
    {
        if (sprite is null)
        {
            errors.Add($"{field}.sprite: value is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sprite.AssetName) || !sprite.AssetName.StartsWith("Mods/romulot.ValleyArmory/", StringComparison.Ordinal))
        {
            errors.Add($"{field}.sprite.assetName: must use the 'Mods/romulot.ValleyArmory/' asset namespace.");
        }

        if (sprite.SpriteIndex < 0)
        {
            errors.Add($"{field}.sprite.spriteIndex: must be zero or greater.");
        }
    }

    /// <summary>Highest chance a single drop rule may declare, so a modder can't accidentally trivialize the shop with an "always drops" rule.</summary>
    private const double MaximumDropChance = 0.5;

    private void ValidateAcquisition(EquipmentDefinition item, string field, List<string> errors)
    {
        AcquisitionMetadata? acquisition = item.Acquisition;
        if (acquisition is null)
        {
            errors.Add($"{field}.acquisition: value is required.");
            return;
        }

        if (acquisition.Shop is not null)
        {
            this.ValidateShopAcquisition(item, acquisition.Shop, field, errors);
        }

        if (acquisition.Drop is not null)
        {
            this.ValidateDropAcquisition(acquisition.Drop, field, errors);
        }

        if (acquisition.Crafting is not null)
        {
            this.ValidateCraftingAcquisition(acquisition.Crafting, field, errors);
        }

        if (acquisition.Quest is not null)
        {
            this.ValidateQuestAcquisition(acquisition.Quest, field, errors);
        }
    }

    private void ValidateQuestAcquisition(QuestAcquisition quest, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(quest.QuestId))
        {
            errors.Add($"{field}.acquisition.quest.questId: value is required.");
        }

        if (quest.UnlockCondition is not null && string.IsNullOrWhiteSpace(quest.UnlockCondition))
        {
            errors.Add($"{field}.acquisition.quest.unlockCondition: must not be blank when present.");
        }
    }

    private void ValidateShopAcquisition(EquipmentDefinition item, ShopAcquisition shop, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(shop.ShopId))
        {
            errors.Add($"{field}.acquisition.shop.shopId: value is required.");
        }

        if (shop.Condition is not null && string.IsNullOrWhiteSpace(shop.Condition))
        {
            errors.Add($"{field}.acquisition.shop.condition: must not be blank when present.");
        }

        if (item.Stats is not null && item.Stats.Price <= 0)
        {
            errors.Add($"{field}.acquisition.shop: requires stats.price greater than zero.");
        }
    }

    private void ValidateDropAcquisition(DropAcquisition drop, string field, List<string> errors)
    {
        if (drop.SourceType is null)
        {
            errors.Add($"{field}.acquisition.drop.sourceType: value is required.");
        }

        if (string.IsNullOrWhiteSpace(drop.SourceId))
        {
            errors.Add($"{field}.acquisition.drop.sourceId: value is required.");
        }

        if (drop.Chance <= 0 || drop.Chance > MaximumDropChance)
        {
            errors.Add($"{field}.acquisition.drop.chance: must be greater than zero and at most {MaximumDropChance:0.##}.");
        }

        if (drop.Condition is not null && string.IsNullOrWhiteSpace(drop.Condition))
        {
            errors.Add($"{field}.acquisition.drop.condition: must not be blank when present.");
        }
    }

    private void ValidateCraftingAcquisition(CraftingAcquisition crafting, string field, List<string> errors)
    {
        if (crafting.Ingredients is null || crafting.Ingredients.Count == 0)
        {
            errors.Add($"{field}.acquisition.crafting.ingredients: at least one ingredient is required.");
        }
        else
        {
            HashSet<string> seenIngredientIds = new(StringComparer.Ordinal);
            foreach (CraftingIngredient ingredient in crafting.Ingredients)
            {
                if (string.IsNullOrWhiteSpace(ingredient.ItemId))
                {
                    errors.Add($"{field}.acquisition.crafting.ingredients: ingredient itemId is required.");
                }
                else if (!seenIngredientIds.Add(ingredient.ItemId))
                {
                    errors.Add($"{field}.acquisition.crafting.ingredients: duplicate ingredient itemId '{ingredient.ItemId}'.");
                }

                if (ingredient.Quantity <= 0)
                {
                    errors.Add($"{field}.acquisition.crafting.ingredients: ingredient '{ingredient.ItemId}' requires a quantity greater than zero.");
                }
            }
        }

        if (crafting.UnlockCondition is not null && string.IsNullOrWhiteSpace(crafting.UnlockCondition))
        {
            errors.Add($"{field}.acquisition.crafting.unlockCondition: must not be blank when present.");
        }
    }

    private void ValidateOverrides(OptionalVisualOverrides? overrides, string field, List<string> errors)
    {
        LightOverride? light = overrides?.Light;
        if (light is null)
        {
            return;
        }

        if (light.Color is not null)
        {
            ValidateColor(light.Color, $"{field}.optionalVisualOverrides.light.color", errors);
        }

        if (light.Radius is <= 0)
        {
            errors.Add($"{field}.optionalVisualOverrides.light.radius: must be greater than zero.");
        }

        if (light.Intensity is < 0 or > 1)
        {
            errors.Add($"{field}.optionalVisualOverrides.light.intensity: must be between 0 and 1.");
        }

        ValidateOffset(light.Offset, $"{field}.optionalVisualOverrides.light.offset", errors);
    }

    private static void ValidateLight(
        bool enabled,
        float radius,
        float intensity,
        VectorOffset? offset,
        string field,
        List<string> errors
    )
    {
        if (enabled && radius <= 0)
        {
            errors.Add($"{field}.lightRadius: enabled lights require a radius greater than zero.");
        }

        if (radius < 0)
        {
            errors.Add($"{field}.lightRadius: must be zero or greater.");
        }

        if (intensity is < 0 or > 1)
        {
            errors.Add($"{field}.lightIntensity: must be between 0 and 1.");
        }

        ValidateOffset(offset, $"{field}.lightOffset", errors);
    }

    private static void ValidateOffset(VectorOffset? offset, string field, List<string> errors)
    {
        if (offset is not null && (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y)))
        {
            errors.Add($"{field}: coordinates must be finite numbers.");
        }
    }

    private static void ValidateColor(string? color, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(color) || !ColorPattern.IsMatch(color))
        {
            errors.Add($"{field}: expected a color in #RRGGBB format.");
        }
    }

    private static void RequireTranslationKey(string? key, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(key) || !TranslationKeyPattern.IsMatch(key))
        {
            errors.Add($"{field}: a lowercase dotted i18n key is required.");
        }
    }

}
