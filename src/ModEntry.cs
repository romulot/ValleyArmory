using StardewModdingAPI;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using ValleyArmory.DeveloperTools;

namespace ValleyArmory;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
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

        new MinersBladeGiveCommand(helper.Translation, this.Monitor).Register(helper.ConsoleCommands);

        this.Monitor.Log(
            $"Valley Armory {this.ModManifest.Version} loaded with {catalog.Equipment.Count} validated equipment definitions; only Miner's Blade is enabled for the vertical slice.",
            LogLevel.Info
        );
    }
}
