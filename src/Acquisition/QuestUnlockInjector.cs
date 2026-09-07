using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>
/// Declares when an equipment's Special Order becomes active via the vanilla Data/TriggerActions
/// mechanism (Trigger=DayStarted, Condition=GSQ, Action=AddSpecialOrder). No polling, no Harmony —
/// FarmerTeam.AddSpecialOrder is itself idempotent (no-ops if already active or already completed
/// and non-repeatable), so firing this once a day is always safe.
/// </summary>
internal sealed class QuestUnlockInjector
{
    private readonly IReadOnlyList<EquipmentDefinition> questEquipment;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public QuestUnlockInjector(CatalogIndex catalog, IMonitor monitor)
    {
        this.questEquipment = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Quest is not null)
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

        foreach (EquipmentDefinition definition in this.questEquipment)
        {
            try
            {
                TriggerActionData action = BuildTriggerAction(definition);
                if (!existingIds.Add(action.Id))
                {
                    if (this.collisionLogged.Add(definition.Id))
                    {
                        this.monitor.Log(
                            $"Skipped quest unlock trigger because Data/TriggerActions already contains ID '{action.Id}'. Existing entry was preserved.",
                            LogLevel.Warn
                        );
                    }

                    continue;
                }

                actions.Add(action);
                this.monitor.Log($"Injected quest unlock trigger '{action.Id}' for '{definition.Id}'.", LogLevel.Trace);
            }
            catch (Exception exception)
            {
                this.monitor.Log($"Failed to register quest for '{definition.Id}': {exception.Message}", LogLevel.Error);
            }
        }
    }

    internal static TriggerActionData BuildTriggerAction(EquipmentDefinition definition)
    {
        QuestAcquisition quest = definition.Acquisition?.Quest
            ?? throw new InvalidOperationException($"Equipment '{definition.Id}' has no quest acquisition data.");

        return new TriggerActionData
        {
            Id = $"{definition.Id}_QuestUnlock",
            Trigger = "DayStarted",
            Condition = string.IsNullOrWhiteSpace(quest.UnlockCondition) ? null : quest.UnlockCondition,
            Action = $"AddSpecialOrder {quest.QuestId}"
        };
    }
}
