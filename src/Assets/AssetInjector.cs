using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Weapons;
using ValleyArmory.Catalog;

namespace ValleyArmory.Assets;

internal sealed class AssetInjector
{
    internal const string WeaponTextureAssetName = "Mods/romulot.ValleyArmory/Weapons";

    private readonly EquipmentDefinition minersBlade;
    private readonly MinersBladeWeaponDataFactory factory;
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private bool collisionLogged;

    public AssetInjector(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        if (!catalog.TryGetById(MinersBladeWeaponDataFactory.ItemId, out EquipmentDefinition? definition) || definition is null)
        {
            throw new InvalidOperationException("The validated catalog does not contain the Miner's Blade.");
        }

        this.minersBlade = definition;
        this.factory = new MinersBladeWeaponDataFactory();
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
        WeaponData data = this.factory.Create(
            this.minersBlade,
            key => this.translations.Get(key).ToString()
        );

        if (!NonOverwritingAssetEditor.TryAdd(weapons, this.minersBlade.Id, data))
        {
            if (!this.collisionLogged)
            {
                this.monitor.Log(
                    $"Skipped Miner's Blade injection because Data/Weapons already contains ID '{this.minersBlade.Id}'. Existing data was preserved.",
                    LogLevel.Warn
                );
                this.collisionLogged = true;
            }

            return;
        }

        this.monitor.Log($"Injected Data/Weapons entry '{this.minersBlade.Id}'.", LogLevel.Trace);
    }
}
