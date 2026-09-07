using ValleyArmory.Catalog;

namespace ValleyArmory.DeveloperTools;

internal sealed record DeveloperWeaponEntry(string Alias, EquipmentDefinition Definition)
{
    public string QualifiedItemId => EquipmentIdentity.GetQualifiedItemId(this.Definition);
}

internal sealed class DeveloperWeaponCatalog
{
    private const string ModIdPrefix = "romulot.ValleyArmory_";
    private readonly IReadOnlyList<DeveloperWeaponEntry> entries;
    private readonly IReadOnlyDictionary<string, DeveloperWeaponEntry> byAlias;

    public DeveloperWeaponCatalog(CatalogIndex catalog)
    {
        this.entries = catalog.GetAllEquipment()
            .Where(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer)
            .Select(item => new DeveloperWeaponEntry(ToAlias(item.Id), item))
            .OrderBy(entry => entry.Definition.Sprite!.SpriteIndex)
            .ThenBy(entry => entry.Definition.Id, StringComparer.Ordinal)
            .ToArray();

        this.byAlias = this.entries.ToDictionary(entry => entry.Alias, StringComparer.OrdinalIgnoreCase);
        if (this.byAlias.Count != this.entries.Count)
        {
            throw new InvalidOperationException("Weapon developer aliases must be unique.");
        }
    }

    public IReadOnlyList<DeveloperWeaponEntry> Entries => this.entries;

    public bool TryResolve(string alias, out DeveloperWeaponEntry? entry)
    {
        return this.byAlias.TryGetValue(alias, out entry);
    }

    internal static string ToAlias(string itemId)
    {
        if (!itemId.StartsWith(ModIdPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Item ID '{itemId}' is not a Valley Armory ID.", nameof(itemId));
        }

        string suffix = itemId[ModIdPrefix.Length..];
        List<char> result = new(suffix.Length + 4);
        for (int i = 0; i < suffix.Length; i++)
        {
            char character = suffix[i];
            if (i > 0 && char.IsUpper(character))
                result.Add('-');

            result.Add(char.ToLowerInvariant(character));
        }

        return new string(result.ToArray());
    }
}
