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
    private readonly Dictionary<long, PlayerLightState> states = new();
    private readonly HashSet<long> activeLocalPlayerIds = new();
    private readonly List<long> stalePlayerIds = new();

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
            this.states.Clear();
            this.ReconcileLocalFarmerLights();
        });
    }

    public void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.RunSafely("ReturnedToTitle", () =>
        {
            this.CleanupOwnedLights();
            this.states.Clear();
        });
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.RunSafely("DayStarted", this.ReconcileLocalFarmerLights);
    }

    public void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.RunSafely("DayEnding", this.CleanupOwnedLights);
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.Player.IsLocalPlayer)
            return;

        this.RunSafely("Warped", () => this.ReconcileFarmerLight(e.Player));
    }

    public void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        this.monitor.Log($"peer connected: playerId={e.Peer.PlayerID}, screen={e.Peer.ScreenID?.ToString() ?? "<none>"}", LogLevel.Trace);
    }

    public void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        this.RunSafely("PeerDisconnected", () =>
        {
            this.monitor.Log($"peer disconnected: playerId={e.Peer.PlayerID}, screen={e.Peer.ScreenID?.ToString() ?? "<none>"}", LogLevel.Trace);
            this.CleanupDisconnectedPeer(e.Peer.PlayerID);
        });
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !this.failSafe.Enabled)
            return;

        this.RunSafely("UpdateTicked", this.ReconcileLocalFarmerLights);
    }

    private void ReconcileLocalFarmerLights()
    {
        if (!Context.IsWorldReady)
            return;

        this.activeLocalPlayerIds.Clear();
        foreach (Farmer farmer in Game1.getOnlineFarmers())
        {
            if (!farmer.IsLocalPlayer)
                continue;

            this.activeLocalPlayerIds.Add(farmer.UniqueMultiplayerID);
            this.ReconcileFarmerLight(farmer);
        }

        this.stalePlayerIds.Clear();
        this.stalePlayerIds.AddRange(PlayerLightStateSet.FindStalePlayers(this.states.Keys, this.activeLocalPlayerIds));

        foreach (long stalePlayerId in this.stalePlayerIds)
        {
            this.RemoveLightAndState(stalePlayerId, "stale-local-player");
        }
    }

    private void ReconcileFarmerLight(Farmer player)
    {
        if (!player.IsLocalPlayer)
            return;

        PlayerLightState state = this.GetOrCreateState(player.UniqueMultiplayerID);
        string locationName = player.currentLocation?.NameOrUniqueName ?? string.Empty;
        bool eligible = this.TryGetEligibleAppearance(player, out WeaponLightAppearance appearance, out Vector2 position);
        LightReconcileAction action = state.DetermineAction(eligible, locationName, position, appearance);

        long playerId = player.UniqueMultiplayerID;

        switch (action)
        {
            case LightReconcileAction.None:
                return;
            case LightReconcileAction.Create:
                if (player.currentLocation is not null)
                    this.ApplyOrCreateLight(player, state, player.currentLocation, position, appearance);
                return;
            case LightReconcileAction.Update:
                if (player.currentLocation is not null)
                    this.ApplyOrCreateLight(player, state, player.currentLocation, position, appearance);
                return;
            case LightReconcileAction.RebindLocation:
                this.monitor.Log($"light rebind: playerId={playerId}", LogLevel.Trace);
                this.RemoveLightFromTrackedLocation(state, playerId, "rebind");
                if (player.currentLocation is not null)
                    this.ApplyOrCreateLight(player, state, player.currentLocation, position, appearance);
                return;
            case LightReconcileAction.Remove:
                this.RemoveLightFromTrackedLocation(state, playerId, "not-eligible");
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

    private void ApplyOrCreateLight(Farmer player, PlayerLightState state, GameLocation location, Vector2 position, WeaponLightAppearance appearance)
    {
        string lightId = state.LightId;
        if (!this.idAllocator.IsOwned(lightId))
            return;

        bool created = false;
        if (!location.hasLightSource(lightId))
        {
            LightSource source = new(
                lightId,
                WeaponLightTextureIndex,
                position,
                appearance.Radius,
                appearance.ToRuntimeColor(),
                LightSource.LightContext.None,
                player.UniqueMultiplayerID,
                location.NameOrUniqueName
            );
            location.sharedLights.Add(lightId, source);
            created = true;
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

        state.MarkApplied(location.NameOrUniqueName, position, appearance);
        if (created)
            this.monitor.Log($"light create: playerId={player.UniqueMultiplayerID}", LogLevel.Trace);
    }

    private void RemoveLightFromTrackedLocation(PlayerLightState state, long playerId, string reason)
    {
        if (!state.HasLight || !this.idAllocator.IsOwned(state.LightId))
            return;

        bool removed = false;
        string lightId = state.LightId;
        if (state.LocationName is not null)
        {
            GameLocation? tracked = Game1.locations.FirstOrDefault(location => string.Equals(location.NameOrUniqueName, state.LocationName, StringComparison.Ordinal));
            if (tracked is not null && tracked.hasLightSource(lightId))
            {
                tracked.removeLightSource(lightId);
                removed = true;
            }
        }

        GameLocation? current = Game1.currentLocation;
        if (current is not null && current.hasLightSource(lightId))
        {
            current.removeLightSource(lightId);
            removed = true;
        }

        state.MarkRemoved();
        if (removed)
            this.monitor.Log($"light remove: playerId={playerId}, reason={reason}", LogLevel.Trace);
    }

    private void CleanupOwnedLights()
    {
        foreach ((long playerId, PlayerLightState state) in this.states)
        {
            if (this.idAllocator.FilterOwned(new[] { state.LightId }).Count == 0)
                continue;

            if (RemoveLightEverywhere(state.LightId))
                this.monitor.Log($"light remove: playerId={playerId}, reason=cleanup", LogLevel.Trace);

            state.MarkRemoved();
        }

        this.states.Clear();
    }

    private void CleanupDisconnectedPeer(long disconnectedPlayerId)
    {
        string lightId = this.idAllocator.GetLocalPlayerLightId(disconnectedPlayerId);
        if (!this.idAllocator.IsOwned(lightId))
            return;

        bool removed = RemoveLightEverywhere(lightId);
        if (removed)
            this.monitor.Log($"light remove: playerId={disconnectedPlayerId}, reason=peer-disconnected", LogLevel.Trace);

        if (this.states.TryGetValue(disconnectedPlayerId, out PlayerLightState? state))
        {
            state.MarkRemoved();
            _ = PlayerLightStateSet.RemovePeerState(this.states, disconnectedPlayerId);
        }
    }

    private void RemoveLightAndState(long playerId, string reason)
    {
        if (!this.states.TryGetValue(playerId, out PlayerLightState? state))
            return;

        this.RemoveLightFromTrackedLocation(state, playerId, reason);
        _ = PlayerLightStateSet.RemovePeerState(this.states, playerId);
    }

    private PlayerLightState GetOrCreateState(long playerId)
    {
        if (this.states.TryGetValue(playerId, out PlayerLightState? existing))
            return existing;

        string lightId = this.idAllocator.GetLocalPlayerLightId(playerId);
        PlayerLightState state = new(lightId);
        this.states.Add(playerId, state);
        return state;
    }

    private static bool RemoveLightEverywhere(string lightId)
    {
        bool removed = false;
        foreach (GameLocation location in Game1.locations)
        {
            if (!location.hasLightSource(lightId))
                continue;

            location.removeLightSource(lightId);
            removed = true;
        }

        return removed;
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
