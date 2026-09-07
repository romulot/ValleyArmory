using ValleyArmory.Catalog;

namespace ValleyArmory;

internal static class EquipmentIdentity
{
    public static string GetQualifiedItemId(EquipmentDefinition equipment)
    {
        string typePrefix = equipment.Type switch
        {
            EquipmentType.Boots => "(B)",
            EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer => "(W)",
            _ => throw new ArgumentException($"Equipment '{equipment.Id}' has no valid type.", nameof(equipment))
        };
        return typePrefix + equipment.Id;
    }
}
