using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Shops;
using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>Adds Valley Armory equipment to vanilla shops (Data/Shops) additively, without replacing existing entries.</summary>
internal sealed class ShopAcquisitionInjector
{
    private readonly IReadOnlyList<EquipmentDefinition> shopEquipment;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public ShopAcquisitionInjector(CatalogIndex catalog, IMonitor monitor)
    {
        this.shopEquipment = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Shop is not null)
            .ToArray();
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
        {
            e.Edit(this.EditShops, AssetEditPriority.Default);
        }
    }

    private void EditShops(IAssetData asset)
    {
        this.ApplyTo(asset.AsDictionary<string, ShopData>().Data);
    }

    internal void ApplyTo(IDictionary<string, ShopData> shops)
    {
        foreach (IGrouping<string, EquipmentDefinition> group in this.shopEquipment.GroupBy(
            item => item.Acquisition!.Shop!.ShopId,
            StringComparer.Ordinal
        ))
        {
            if (!shops.TryGetValue(group.Key, out ShopData? shop))
            {
                this.monitor.Log($"Skipped shop injection: unknown shop ID '{group.Key}'.", LogLevel.Warn);
                continue;
            }

            shop.Items ??= new List<ShopItemData>();
            HashSet<string> existingIds = shop.Items
                .Select(entry => entry.Id)
                .ToHashSet(StringComparer.Ordinal);

            foreach (EquipmentDefinition definition in group)
            {
                try
                {
                    ShopItemData entry = BuildEntry(definition);
                    if (!existingIds.Add(entry.Id))
                    {
                        if (this.collisionLogged.Add(definition.Id))
                        {
                            this.monitor.Log(
                                $"Skipped shop injection because shop '{group.Key}' already contains ID '{entry.Id}'. Existing entry was preserved.",
                                LogLevel.Warn
                            );
                        }

                        continue;
                    }

                    shop.Items.Add(entry);
                    this.monitor.Log($"Injected shop entry '{entry.Id}' into shop '{group.Key}'.", LogLevel.Trace);
                }
                catch (Exception exception)
                {
                    this.monitor.Log($"Invalid acquisition definition for '{definition.Id}': {exception.Message}", LogLevel.Error);
                }
            }
        }
    }

    internal static ShopItemData BuildEntry(EquipmentDefinition definition)
    {
        ShopAcquisition shop = definition.Acquisition?.Shop
            ?? throw new InvalidOperationException($"Equipment '{definition.Id}' has no shop acquisition data.");

        int price = definition.Stats?.Price
            ?? throw new InvalidOperationException($"Equipment '{definition.Id}' has no stats/price.");

        return new ShopItemData
        {
            Id = definition.Id,
            ItemId = EquipmentIdentity.GetQualifiedItemId(definition),
            Price = price,
            AvailableStock = -1,
            Condition = string.IsNullOrWhiteSpace(shop.Condition) ? null : shop.Condition
        };
    }
}
