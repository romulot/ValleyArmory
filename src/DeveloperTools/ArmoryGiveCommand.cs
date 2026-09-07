using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using ValleyArmory.Catalog;

namespace ValleyArmory.DeveloperTools;

internal sealed class ArmoryGiveCommand
{
    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;
    private readonly DeveloperEquipmentCatalog equipment;

    public ArmoryGiveCommand(CatalogIndex catalog, ITranslationHelper translations, IMonitor monitor)
    {
        this.translations = translations;
        this.monitor = monitor;
        this.equipment = new DeveloperEquipmentCatalog(catalog);
    }

    public void Register(ICommandHelper commands)
    {
        commands.Add("va_give", this.translations.Get("command.va-give.description"), this.HandleGive);
        commands.Add("va_list", this.translations.Get("command.va-list.description"), this.HandleList);
        this.monitor.Log("Registered console commands: va_give, va_list", LogLevel.Trace);
    }

    private void HandleGive(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.ShowMessage("command.va-give.world-not-ready", isError: true);
            return;
        }

        if (args.Length != 1)
        {
            this.ShowMessage("command.va-give.usage", isError: true);
            return;
        }

        if (!this.equipment.TryResolve(args[0], out DeveloperEquipmentEntry? entry) || entry is null)
        {
            this.monitor.Log($"Unknown Valley Armory equipment alias '{args[0]}'. Use va_list to see available equipment.", LogLevel.Warn);
            this.ShowMessage("command.va-give.unknown-alias", isError: true);
            return;
        }

        Item? item;
        try
        {
            item = ItemRegistry.Create(entry.QualifiedItemId, 1, 0, allowNull: true);
        }
        catch (Exception exception)
        {
            this.monitor.Log($"Could not create '{entry.QualifiedItemId}': {exception}", LogLevel.Warn);
            this.ShowMessage("command.va-give.creation-failed", isError: true);
            return;
        }

        if (item is not (MeleeWeapon or Boots or Clothing) || !string.Equals(item.QualifiedItemId, entry.QualifiedItemId, StringComparison.Ordinal))
        {
            this.monitor.Log($"ItemRegistry did not create the expected equipment '{entry.QualifiedItemId}'.", LogLevel.Warn);
            this.ShowMessage("command.va-give.creation-failed", isError: true);
            return;
        }

        if (Game1.player.addItemToInventoryBool(item, makeActiveObject: false))
        {
            this.monitor.Log($"Equipment '{entry.Alias}' ({entry.QualifiedItemId}) added to the player inventory.", LogLevel.Debug);
            this.ShowMessage("command.va-give.success", isError: false);
            return;
        }

        Game1.createItemDebris(
            item,
            Game1.player.getStandingPosition(),
            Game1.player.FacingDirection,
            Game1.player.currentLocation,
            groundLevel: -1,
            flopFish: false
        );
        this.monitor.Log($"Player inventory was full; equipment '{entry.Alias}' was dropped safely at the player's position.", LogLevel.Debug);
        this.ShowMessage("command.va-give.inventory-full", isError: false);
    }

    private void HandleList(string command, string[] args)
    {
        this.monitor.Log(this.translations.Get("command.va-list.header"), LogLevel.Info);
        foreach (DeveloperEquipmentEntry entry in this.equipment.Entries)
        {
            this.monitor.Log(
                $"- {entry.Alias} | {entry.Definition.Rarity} | {entry.Definition.Type} | {entry.QualifiedItemId}",
                LogLevel.Info
            );
        }
    }

    private void ShowMessage(string key, bool isError)
    {
        string message = this.translations.Get(key);
        Game1.addHUDMessage(new HUDMessage(message, isError ? HUDMessage.error_type : HUDMessage.newQuest_type));
    }
}
