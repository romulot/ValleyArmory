using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Weapons;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

internal sealed class AssetInjector
{
    internal const string WeaponTextureAssetName = "Mods/romulot.ValleyArmory/Weapons";

    private readonly IReadOnlyList<EquipmentDefinition> weapons;
    private readonly WeaponDataFactory factory;
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public AssetInjector(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        this.weapons = catalog.GetAllEquipment()
            .Where(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer)
            .ToArray();
        this.factory = new WeaponDataFactory();
        this.translations = translations;
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Weapons"))
        {
            e.Edit(this.EditWeapons, AssetEditPriority.Default);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(WeaponTextureAssetName))
        {
            e.LoadFromModFile<Texture2D>("assets/weapons.png", AssetLoadPriority.Medium);
        }
    }

    private void EditWeapons(IAssetData asset)
    {
        IDictionary<string, WeaponData> weapons = asset.AsDictionary<string, WeaponData>().Data;
        foreach (EquipmentDefinition definition in this.weapons)
        {
            WeaponData data = this.factory.Create(
                definition,
                key => this.translations.Get(key).ToString()
            );

            if (!NonOverwritingAssetEditor.TryAdd(weapons, definition.Id, data))
            {
                if (this.collisionLogged.Add(definition.Id))
                {
                    this.monitor.Log(
                        $"Skipped weapon injection because Data/Weapons already contains ID '{definition.Id}'. Existing data was preserved.",
                        LogLevel.Warn
                    );
                }

                continue;
            }

            this.monitor.Log($"Injected Data/Weapons entry '{definition.Id}'.", LogLevel.Trace);
        }
    }
}
