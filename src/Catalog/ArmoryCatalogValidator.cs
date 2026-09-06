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
        ["romulot.ValleyArmory_EtherealBoots"] = EquipmentType.Boots
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
        if (weaponCount != 7 || bootsCount != 3)
        {
            errors.Add($"catalog.equipment: expected exactly 7 weapons and 3 boots, found {weaponCount} weapons and {bootsCount} boots.");
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
                    errors.Add($"{field}.id: ID is not one of the ten reserved permanent equipment IDs.");
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

            if (!rarityIds.Contains(item.Rarity))
            {
                errors.Add($"{field}.rarity: unknown rarity '{item.Rarity}'.");
            }

            RequireTranslationKey(item.DisplayNameKey, $"{field}.displayNameKey", errors);
            RequireTranslationKey(item.DescriptionKey, $"{field}.descriptionKey", errors);
            this.ValidateStats(item, field, errors);
            this.ValidateSprite(item.Sprite, field, errors);
            this.ValidateAcquisition(item.Acquisition, field, errors);
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

        if (item.Type is EquipmentType.Boots)
        {
            if (stats.Immunity is null or < 0)
            {
                errors.Add($"{field}.stats.immunity: boots require a value of zero or greater.");
            }

            if (stats.MinDamage is not null || stats.MaxDamage is not null || stats.Speed is not null ||
                stats.CritChance is not null || stats.CritMultiplier is not null || stats.Knockback is not null)
            {
                errors.Add($"{field}.stats: boots cannot define weapon stats.");
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

    private void ValidateAcquisition(AcquisitionMetadata? acquisition, string field, List<string> errors)
    {
        if (acquisition is null || string.IsNullOrWhiteSpace(acquisition.Method))
        {
            errors.Add($"{field}.acquisition.method: metadata value is required.");
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
