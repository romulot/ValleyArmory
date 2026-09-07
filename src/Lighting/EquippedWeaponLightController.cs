using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace ValleyArmory.Lighting;

internal sealed class EquippedWeaponLightController
{
    private const int WeaponLightTextureIndex = 4;

    private readonly IMonitor monitor;
    private readonly LightAppearanceResolver appearanceResolver;
    private readonly LightIdAllocator idAllocator;
    private readonly LightingFailSafe failSafe = new();
    private PlayerLightState? state;

    public EquippedWeaponLightController(IMonitor monitor, LightAppearanceResolver appearanceResolver, LightIdAllocator idAllocator)
    {
        this.monitor = monitor;
        this.appearanceResolver = appearanceResolver;
        this.idAllocator = idAllocator;
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.RunSafely("SaveLoaded", () =>
        {
            this.EnsureState();
            this.ReconcileLocalPlayerLight();
        });
    }

    public void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.RunSafely("ReturnedToTitle", () =>
        {
            this.CleanupOwnedLights();
            this.state = null;
        });
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.RunSafely("DayStarted", this.ReconcileLocalPlayerLight);
    }

    public void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.RunSafely("DayEnding", this.CleanupOwnedLights);
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;

        this.RunSafely("Warped", this.ReconcileLocalPlayerLight);
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !this.failSafe.Enabled)
            return;

        this.RunSafely("UpdateTicked", this.ReconcileLocalPlayerLight);
    }

    private void ReconcileLocalPlayerLight()
    {
        if (!Context.IsWorldReady || !this.EnsureState())
            return;

        Farmer player = Game1.player;
        if (player.currentLocation is null)
            return;

        string locationName = player.currentLocation.NameOrUniqueName;
        bool eligible = this.TryGetEligibleAppearance(player, out WeaponLightAppearance appearance, out Vector2 position);
        LightReconcileAction action = this.state!.DetermineAction(eligible, locationName, position, appearance);

        switch (action)
        {
            case LightReconcileAction.None:
                return;
            case LightReconcileAction.Create:
                this.ApplyOrCreateLight(player.currentLocation, position, appearance);
                return;
            case LightReconcileAction.Update:
                this.ApplyOrCreateLight(player.currentLocation, position, appearance);
                return;
            case LightReconcileAction.RebindLocation:
                this.RemoveLightFromTrackedLocation();
                this.ApplyOrCreateLight(player.currentLocation, position, appearance);
                return;
            case LightReconcileAction.Remove:
                this.RemoveLightFromTrackedLocation();
                return;
        }
    }

    private bool TryGetEligibleAppearance(Farmer player, out WeaponLightAppearance appearance, out Vector2 position)
    {
        appearance = default;
        position = Vector2.Zero;

        if (player.CurrentTool is not MeleeWeapon weapon
            || !this.appearanceResolver.TryResolve(weapon.QualifiedItemId, out appearance))
        {
            return false;
        }

        position = player.Position + appearance.Offset;
        return true;
    }

    private void ApplyOrCreateLight(GameLocation location, Vector2 position, WeaponLightAppearance appearance)
    {
        string lightId = this.state!.LightId;
        if (!this.idAllocator.IsOwned(lightId))
            return;

        if (!location.hasLightSource(lightId))
        {
            LightSource source = new(
                lightId,
                WeaponLightTextureIndex,
                position,
                appearance.Radius,
                appearance.ToRuntimeColor(),
                LightSource.LightContext.None,
                Game1.player.UniqueMultiplayerID,
                location.NameOrUniqueName
            );
            location.sharedLights.Add(lightId, source);
        }
        else
        {
            location.repositionLightSource(lightId, position);
            LightSource? source = location.getLightSource(lightId);
            if (source is not null)
            {
                source.radius.Value = appearance.Radius;
                source.color.Value = appearance.ToRuntimeColor();
            }
        }

        this.state.MarkApplied(location.NameOrUniqueName, position, appearance);
    }

    private void RemoveLightFromTrackedLocation()
    {
        if (this.state is null || !this.state.HasLight || !this.idAllocator.IsOwned(this.state.LightId))
            return;

        string lightId = this.state.LightId;
        if (this.state.LocationName is not null)
        {
            GameLocation? tracked = Game1.locations.FirstOrDefault(location => string.Equals(location.NameOrUniqueName, this.state.LocationName, StringComparison.Ordinal));
            if (tracked is not null && tracked.hasLightSource(lightId))
                tracked.removeLightSource(lightId);
        }

        GameLocation? current = Game1.currentLocation;
        if (current is not null && current.hasLightSource(lightId))
            current.removeLightSource(lightId);

        this.state.MarkRemoved();
    }

    private void CleanupOwnedLights()
    {
        if (this.state is null || !this.idAllocator.IsOwned(this.state.LightId))
            return;

        string lightId = this.state.LightId;
        foreach (GameLocation location in Game1.locations)
        {
            if (location.hasLightSource(lightId))
                location.removeLightSource(lightId);
        }

        this.state.MarkRemoved();
    }

    private bool EnsureState()
    {
        if (!Context.IsWorldReady)
            return false;

        long playerId = Game1.player.UniqueMultiplayerID;
        string lightId = this.idAllocator.GetLocalPlayerLightId(playerId);
        if (this.state is null || !string.Equals(this.state.LightId, lightId, StringComparison.Ordinal))
            this.state = new PlayerLightState(lightId);

        return true;
    }

    private void RunSafely(string operation, Action action)
    {
        if (!this.failSafe.Enabled)
            return;

        try
        {
            action();
        }
        catch (Exception exception)
        {
            this.CleanupOwnedLights();
            bool shouldWarn = this.failSafe.DisableAndShouldWarn();
            if (shouldWarn)
            {
                this.monitor.Log(
                    $"Weapon light subsystem was disabled; existing Valley Armory lights were removed. {operation}: {exception.Message}",
                    LogLevel.Warn
                );
            }
        }
    }
}
