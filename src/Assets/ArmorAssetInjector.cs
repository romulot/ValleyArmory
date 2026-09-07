using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Shirts;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

internal sealed class ArmorAssetInjector
{
    internal const string ArmorTextureAssetName = "Mods/romulot.ValleyArmory/Armor";

    private readonly IReadOnlyList<EquipmentDefinition> armor;
    private readonly ArmorDataFactory factory;
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public ArmorAssetInjector(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        this.armor = catalog.GetAllEquipment()
            .Where(item => item.Type is EquipmentType.Shirt)
            .ToArray();
        this.factory = new ArmorDataFactory();
        this.translations = translations;
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Shirts"))
        {
            e.Edit(this.EditShirts, AssetEditPriority.Default);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(ArmorTextureAssetName))
        {
            e.LoadFromModFile<Texture2D>("assets/armor.png", AssetLoadPriority.Medium);
        }
    }

    private void EditShirts(IAssetData asset)
    {
        IDictionary<string, ShirtData> shirts = asset.AsDictionary<string, ShirtData>().Data;
        foreach (EquipmentDefinition definition in this.armor)
        {
            ShirtData data = this.factory.Create(
                definition,
                key => this.translations.Get(key).ToString()
            );

            if (!NonOverwritingAssetEditor.TryAdd(shirts, definition.Id, data))
            {
                if (this.collisionLogged.Add(definition.Id))
                {
                    this.monitor.Log(
                        $"Skipped armor injection because Data/Shirts already contains ID '{definition.Id}'. Existing data was preserved.",
                        LogLevel.Warn
                    );
                }

                continue;
            }

            this.monitor.Log($"Injected Data/Shirts entry '{definition.Id}'.", LogLevel.Trace);
        }
    }
}
