using ValleyArmory.Catalog;

namespace ValleyArmory;

internal static class EquipmentIdentity
{
    internal const string MinersBladeItemId = "romulot.ValleyArmory_MinersBlade";
    internal const string MinersBladeQualifiedItemId = "(W)romulot.ValleyArmory_MinersBlade";

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
