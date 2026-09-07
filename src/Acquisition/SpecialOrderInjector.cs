using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.SpecialOrders;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>Adds Valley Armory Special Orders to Data/SpecialOrders additively, without replacing existing entries.</summary>
internal sealed class SpecialOrderInjector
{
    private readonly IReadOnlyList<EquipmentDefinition> questEquipment;
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public SpecialOrderInjector(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        this.questEquipment = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Quest is not null)
            .ToArray();
        this.translations = translations;
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/SpecialOrders"))
        {
            e.Edit(this.EditSpecialOrders, AssetEditPriority.Default);
        }
    }

    private void EditSpecialOrders(IAssetData asset)
    {
        this.ApplyTo(asset.AsDictionary<string, SpecialOrderData>().Data, key => this.translations.Get(key).ToString());
    }

    internal void ApplyTo(IDictionary<string, SpecialOrderData> orders, Func<string, string> translate)
    {
        foreach (EquipmentDefinition definition in this.questEquipment)
        {
            try
            {
                QuestAcquisition quest = definition.Acquisition!.Quest!;
                SpecialOrderData order = BuildSpecialOrder(quest, translate);

                if (!NonOverwritingAssetEditor.TryAdd(orders, quest.QuestId, order))
                {
                    if (this.collisionLogged.Add(definition.Id))
                    {
                        this.monitor.Log(
                            $"Skipped Special Order injection because Data/SpecialOrders already contains ID '{quest.QuestId}'. Existing entry was preserved.",
                            LogLevel.Warn
                        );
                    }

                    continue;
                }

                this.monitor.Log($"Injected Special Order '{quest.QuestId}' for '{definition.Id}'.", LogLevel.Trace);
            }
            catch (Exception exception)
            {
                this.monitor.Log($"Invalid quest acquisition definition for '{definition.Id}': {exception.Message}", LogLevel.Error);
            }
        }
    }

    internal static SpecialOrderData BuildSpecialOrder(QuestAcquisition quest, Func<string, string> translate)
    {
        QuestBlueprint blueprint = QuestBlueprints.Resolve(quest.QuestId);

        return new SpecialOrderData
        {
            Name = quest.QuestId,
            Requester = blueprint.Requester,
            Duration = blueprint.Duration,
            Repeatable = false,
            Condition = string.IsNullOrWhiteSpace(quest.UnlockCondition) ? null : quest.UnlockCondition,
            Text = translate(blueprint.DescriptionKey),
            RandomizedElements = new List<RandomizedElement>(),
            CustomFields = new Dictionary<string, string>(StringComparer.Ordinal),
            Objectives = new List<SpecialOrderObjectiveData>
            {
                new()
                {
                    Type = "Slay",
                    Text = translate(blueprint.ObjectiveTextKey),
                    RequiredCount = blueprint.ObjectiveRequiredCount.ToString(),
                    Data = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["TargetName"] = blueprint.ObjectiveTargetMonster
                    }
                }
            },
            Rewards = new List<SpecialOrderRewardData>
            {
                new()
                {
                    Type = "Mail",
                    Data = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["MailReceived"] = blueprint.MailId,
                        ["NoLetter"] = "false"
                    }
                }
            }
        };
    }
}
