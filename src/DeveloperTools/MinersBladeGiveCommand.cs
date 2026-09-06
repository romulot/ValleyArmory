using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using ValleyArmory.Assets;

namespace ValleyArmory.DeveloperTools;

internal sealed class MinersBladeGiveCommand
{
    private const string CommandArgument = "miners-blade";

    private readonly ITranslationHelper translations;
    private readonly IMonitor monitor;

    public MinersBladeGiveCommand(ITranslationHelper translations, IMonitor monitor)
    {
        this.translations = translations;
        this.monitor = monitor;
    }

    public void Register(ICommandHelper commands)
    {
        commands.Add(
            "va_give",
            this.translations.Get("command.va-give.description"),
            this.Handle
        );
        this.monitor.Log("Registered console command: va_give", LogLevel.Trace);
    }

    private void Handle(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.ShowMessage("command.va-give.world-not-ready", isError: true);
            return;
        }

        if (args.Length != 1 || !string.Equals(args[0], CommandArgument, StringComparison.OrdinalIgnoreCase))
        {
            this.ShowMessage("command.va-give.usage", isError: true);
            return;
        }

        Item? item;
        try
        {
            item = ItemRegistry.Create(MinersBladeWeaponDataFactory.QualifiedItemId, 1, 0, allowNull: true);
        }
        catch (Exception exception)
        {
            this.monitor.Log(
                $"Could not create '{MinersBladeWeaponDataFactory.QualifiedItemId}': {exception}",
                LogLevel.Warn
            );
            this.ShowMessage("command.va-give.creation-failed", isError: true);
            return;
        }

        if (item is not MeleeWeapon || !string.Equals(item.QualifiedItemId, MinersBladeWeaponDataFactory.QualifiedItemId, StringComparison.Ordinal))
        {
            this.monitor.Log(
                $"ItemRegistry did not create the expected weapon '{MinersBladeWeaponDataFactory.QualifiedItemId}'.",
                LogLevel.Warn
            );
            this.ShowMessage("command.va-give.creation-failed", isError: true);
            return;
        }

        if (Game1.player.addItemToInventoryBool(item, makeActiveObject: false))
        {
            this.monitor.Log("Miner's Blade created and added to the player inventory.", LogLevel.Debug);
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
        this.monitor.Log("Player inventory was full; Miner's Blade was dropped safely at the player's position.", LogLevel.Debug);
        this.ShowMessage("command.va-give.inventory-full", isError: false);
    }

    private void ShowMessage(string key, bool isError)
    {
        string message = this.translations.Get(key);
        Game1.addHUDMessage(new HUDMessage(message, isError ? HUDMessage.error_type : HUDMessage.newQuest_type));
    }
}
