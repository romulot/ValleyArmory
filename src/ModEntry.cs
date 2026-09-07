using StardewModdingAPI;
using StardewModdingAPI.Events;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using ValleyArmory.DeveloperTools;
using ValleyArmory.Lighting;
using ValleyArmory.Tooltips;

namespace ValleyArmory;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    private EquippedWeaponLightController? weaponLightController;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModConfig config = helper.ReadConfig<ModConfig>();

        ArmoryCatalog catalog;
        try
        {
            string catalogPath = Path.Combine(helper.DirectoryPath, "assets", "armory.json");
            catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(catalogPath);
        }
        catch (Exception exception) when (exception is CatalogLoadException or CatalogValidationException)
        {
            this.Monitor.Log($"Valley Armory could not load its catalog and will remain disabled: {exception.Message}", LogLevel.Error);
            return;
        }

        CatalogIndex catalogIndex = new(catalog);
        AssetInjector assetInjector = new(catalogIndex, helper.Translation, this.Monitor);
        helper.Events.Content.AssetRequested += assetInjector.OnAssetRequested;

        BootAssetInjector bootAssetInjector = new(catalogIndex, helper.Translation, this.Monitor);
        helper.Events.Content.AssetRequested += bootAssetInjector.OnAssetRequested;

        new ArmoryGiveCommand(catalogIndex, helper.Translation, this.Monitor).Register(helper.ConsoleCommands);

        _ = new TooltipPatchManager(this.ModManifest.UniqueID, this.Monitor).Apply(
            new TooltipPresentationResolver(catalogIndex),
            helper.Translation
        );

        if (config.EnableWeaponLights)
        {
            this.weaponLightController = new EquippedWeaponLightController(
                this.Monitor,
                new LightAppearanceResolver(catalogIndex),
                new LightIdAllocator(this.ModManifest.UniqueID)
            );

            helper.Events.GameLoop.SaveLoaded += this.weaponLightController.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.weaponLightController.OnDayStarted;
            helper.Events.GameLoop.DayEnding += this.weaponLightController.OnDayEnding;
            helper.Events.GameLoop.ReturnedToTitle += this.weaponLightController.OnReturnedToTitle;
            helper.Events.GameLoop.UpdateTicked += this.weaponLightController.OnUpdateTicked;
            helper.Events.Player.Warped += this.weaponLightController.OnWarped;
            helper.Events.Multiplayer.PeerConnected += this.weaponLightController.OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected += this.weaponLightController.OnPeerDisconnected;
            this.Monitor.Log("Weapon light subsystem enabled for local lifecycle and multiplayer peer events.", LogLevel.Debug);
        }
        else
        {
            this.Monitor.Log("Weapon lights are disabled by configuration (EnableWeaponLights=false).", LogLevel.Info);
        }

        this.Monitor.Log(
            $"Valley Armory {this.ModManifest.Version} loaded with {catalog.Equipment.Count} validated equipment definitions.",
            LogLevel.Info
        );
    }
}
