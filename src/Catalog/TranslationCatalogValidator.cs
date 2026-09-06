using System.Text.Json;

namespace ValleyArmory.Catalog;

internal static class TranslationCatalogValidator
{
    public static IReadOnlyList<string> Validate(
        ArmoryCatalog catalog,
        IReadOnlyDictionary<string, string> defaultTranslations,
        IReadOnlyDictionary<string, string> localizedTranslations
    )
    {
        List<string> errors = new();
        HashSet<string> requiredKeys = catalog.Equipment
            .SelectMany(item => new[] { item.DisplayNameKey, item.DescriptionKey })
            .Concat(catalog.Rarities.Select(rarity => rarity.DisplayNameKey))
            .ToHashSet(StringComparer.Ordinal);

        foreach (string missing in requiredKeys.Except(defaultTranslations.Keys, StringComparer.Ordinal))
        {
            errors.Add($"i18n/default.json: missing required key '{missing}'.");
        }

        HashSet<string> defaultKeys = defaultTranslations.Keys.ToHashSet(StringComparer.Ordinal);
        HashSet<string> localizedKeys = localizedTranslations.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (string missing in defaultKeys.Except(localizedKeys, StringComparer.Ordinal))
        {
            errors.Add($"i18n/pt-BR.json: missing key '{missing}'.");
        }

        foreach (string extra in localizedKeys.Except(defaultKeys, StringComparer.Ordinal))
        {
            errors.Add($"i18n/pt-BR.json: unexpected key '{extra}'.");
        }

        return errors;
    }

    public static IReadOnlyDictionary<string, string> LoadFile(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidDataException($"Translation file '{path}' contains no data.");
    }
}
