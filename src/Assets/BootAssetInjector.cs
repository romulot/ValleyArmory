using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

internal sealed class BootAssetInjector
{
    internal const string BootTextureAssetName = "Mods/romulot.ValleyArmory/Boots";

    private readonly IReadOnlyList<EquipmentDefinition> boots;
    private readonly BootDataFactory factory;
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public BootAssetInjector(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        this.boots = catalog.GetAllEquipment()
            .Where(item => item.Type is EquipmentType.Boots)
            .ToArray();
        this.factory = new BootDataFactory();
        this.translations = translations;
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Boots"))
        {
            e.Edit(this.EditBoots, AssetEditPriority.Default);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(BootTextureAssetName))
        {
            e.LoadFromModFile<Texture2D>("assets/boots.png", AssetLoadPriority.Medium);
        }
    }

    private void EditBoots(IAssetData asset)
    {
        IDictionary<string, string> boots = asset.AsDictionary<string, string>().Data;
        foreach (EquipmentDefinition definition in this.boots)
        {
            string data = this.factory.Create(
                definition,
                key => this.translations.Get(key).ToString()
            );

            if (!NonOverwritingAssetEditor.TryAdd(boots, definition.Id, data))
            {
                if (this.collisionLogged.Add(definition.Id))
                {
                    this.monitor.Log(
                        $"Skipped boots injection because Data/Boots already contains ID '{definition.Id}'. Existing data was preserved.",
                        LogLevel.Warn
                    );
                }

                continue;
            }

            this.monitor.Log($"Injected Data/Boots entry '{definition.Id}'.", LogLevel.Trace);
        }
    }
}
