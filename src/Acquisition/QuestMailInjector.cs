using StardewModdingAPI;
using StardewModdingAPI.Events;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>Adds Valley Armory quest-reward letters to Data/mail additively, attaching the linked equipment via the vanilla "%item id &lt;QualifiedItemId&gt; %%" mail syntax.</summary>
internal sealed class QuestMailInjector
{
    private readonly IReadOnlyList<EquipmentDefinition> questEquipment;
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public QuestMailInjector(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        this.questEquipment = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Quest is not null)
            .ToArray();
        this.translations = translations;
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
        {
            e.Edit(this.EditMail, AssetEditPriority.Default);
        }
    }

    private void EditMail(IAssetData asset)
    {
        this.ApplyTo(asset.AsDictionary<string, string>().Data, key => this.translations.Get(key).ToString());
    }

    internal void ApplyTo(IDictionary<string, string> mail, Func<string, string> translate)
    {
        foreach (EquipmentDefinition definition in this.questEquipment)
        {
            try
            {
                QuestAcquisition quest = definition.Acquisition!.Quest!;
                QuestBlueprint blueprint = QuestBlueprints.Resolve(quest.QuestId);
                string letter = BuildLetter(definition, blueprint, translate);

                if (!NonOverwritingAssetEditor.TryAdd(mail, blueprint.MailId, letter))
                {
                    if (this.collisionLogged.Add(definition.Id))
                    {
                        this.monitor.Log(
                            $"Skipped quest reward mail injection because Data/mail already contains ID '{blueprint.MailId}'. Existing entry was preserved.",
                            LogLevel.Warn
                        );
                    }

                    continue;
                }

                this.monitor.Log($"Injected quest reward mail '{blueprint.MailId}' for '{definition.Id}'.", LogLevel.Trace);
            }
            catch (Exception exception)
            {
                this.monitor.Log($"Failed to build quest reward mail for '{definition.Id}': {exception.Message}", LogLevel.Error);
            }
        }
    }

    internal static string BuildLetter(EquipmentDefinition definition, QuestBlueprint blueprint, Func<string, string> translate)
    {
        string body = translate(blueprint.MailBodyKey);
        string title = translate(blueprint.MailTitleKey);
        string qualifiedId = EquipmentIdentity.GetQualifiedItemId(definition);

        return $"{body} %item id {qualifiedId} 1 %%[#]{title}";
    }
}
