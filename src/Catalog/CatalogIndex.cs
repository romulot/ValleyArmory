namespace ValleyArmory.Catalog;

internal sealed class CatalogIndex
{
    private readonly IReadOnlyDictionary<string, EquipmentDefinition> byId;
    private readonly IReadOnlyDictionary<string, EquipmentDefinition> byQualifiedId;
    private readonly IReadOnlyDictionary<string, RarityDefinition> rarities;

    public CatalogIndex(ArmoryCatalog catalog)
    {
        this.byId = catalog.Equipment.ToDictionary(item => item.Id, StringComparer.Ordinal);
        this.byQualifiedId = catalog.Equipment.ToDictionary(
            EquipmentIdentity.GetQualifiedItemId,
            StringComparer.Ordinal
        );
        this.rarities = catalog.Rarities.ToDictionary(rarity => rarity.Id, StringComparer.Ordinal);
    }

    public bool TryGetById(string id, out EquipmentDefinition? equipment)
    {
        return this.byId.TryGetValue(id, out equipment);
    }

    public bool TryGetByQualifiedId(string qualifiedItemId, out EquipmentDefinition? equipment)
    {
        return this.byQualifiedId.TryGetValue(qualifiedItemId, out equipment);
    }

    public bool TryGetRarity(string id, out RarityDefinition? rarity)
    {
        return this.rarities.TryGetValue(id, out rarity);
    }
}
