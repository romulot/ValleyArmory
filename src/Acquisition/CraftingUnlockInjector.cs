using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>
/// Declares recipe-unlock rules for Valley Armory equipment via the vanilla Data/TriggerActions
/// mechanism (Trigger=DayStarted, Condition=GSQ, Action=MarkCraftingRecipeKnown) instead of any
/// custom runtime polling. Additive: appends to the existing list, never replaces it.
/// </summary>
internal sealed class CraftingUnlockInjector
{
    private readonly IReadOnlyList<EquipmentDefinition> craftableEquipment;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public CraftingUnlockInjector(CatalogIndex catalog, IMonitor monitor)
    {
        this.craftableEquipment = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Crafting is not null)
            .ToArray();
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/TriggerActions"))
        {
            e.Edit(this.EditTriggerActions, AssetEditPriority.Default);
        }
    }

    private void EditTriggerActions(IAssetData asset)
    {
        this.ApplyTo(asset.GetData<List<TriggerActionData>>());
    }

    internal void ApplyTo(List<TriggerActionData> actions)
    {
        HashSet<string> existingIds = actions.Select(action => action.Id).ToHashSet(StringComparer.Ordinal);

        foreach (EquipmentDefinition definition in this.craftableEquipment)
        {
            try
            {
                TriggerActionData action = BuildTriggerAction(definition);
                if (!existingIds.Add(action.Id))
                {
                    if (this.collisionLogged.Add(definition.Id))
                    {
                        this.monitor.Log(
                            $"Skipped crafting unlock trigger because Data/TriggerActions already contains ID '{action.Id}'. Existing entry was preserved.",
                            LogLevel.Warn
                        );
                    }

                    continue;
                }

                actions.Add(action);
                this.monitor.Log($"Injected crafting unlock trigger '{action.Id}' for '{definition.Id}'.", LogLevel.Trace);
            }
            catch (Exception exception)
            {
                this.monitor.Log($"Failed to unlock recipe for '{definition.Id}': {exception.Message}", LogLevel.Error);
            }
        }
    }

    internal static TriggerActionData BuildTriggerAction(EquipmentDefinition definition)
    {
        CraftingAcquisition crafting = definition.Acquisition?.Crafting
            ?? throw new InvalidOperationException($"Equipment '{definition.Id}' has no crafting acquisition data.");

        return new TriggerActionData
        {
            Id = $"{definition.Id}_CraftingUnlock",
            Trigger = "DayStarted",
            Condition = string.IsNullOrWhiteSpace(crafting.UnlockCondition) ? null : crafting.UnlockCondition,
            Action = $"MarkCraftingRecipeKnown Current {definition.Id}"
        };
    }
}
