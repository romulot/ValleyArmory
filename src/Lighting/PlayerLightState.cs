using Microsoft.Xna.Framework;

namespace ValleyArmory.Lighting;

internal enum LightReconcileAction
{
    None,
    Create,
    Update,
    RebindLocation,
    Remove
}

internal sealed class PlayerLightState
{
    public PlayerLightState(string lightId)
    {
        this.LightId = lightId;
    }

    public string LightId { get; }

    public bool HasLight { get; private set; }

    public string? LocationName { get; private set; }

    public Vector2 Position { get; private set; }

    public WeaponLightAppearance Appearance { get; private set; }

    public LightReconcileAction DetermineAction(bool eligible, string locationName, Vector2 position, WeaponLightAppearance appearance)
    {
        if (!eligible)
            return this.HasLight ? LightReconcileAction.Remove : LightReconcileAction.None;

        if (!this.HasLight)
            return LightReconcileAction.Create;

        if (!string.Equals(this.LocationName, locationName, StringComparison.Ordinal))
            return LightReconcileAction.RebindLocation;

        if (this.Position != position || this.Appearance != appearance)
            return LightReconcileAction.Update;

        return LightReconcileAction.None;
    }

    public void MarkApplied(string locationName, Vector2 position, WeaponLightAppearance appearance)
    {
        this.HasLight = true;
        this.LocationName = locationName;
        this.Position = position;
        this.Appearance = appearance;
    }

    public void MarkRemoved()
    {
        this.HasLight = false;
        this.LocationName = null;
        this.Position = Vector2.Zero;
        this.Appearance = default;
    }
}

internal static class PlayerLightStateSet
{
    public static IReadOnlyList<long> FindStalePlayers(IEnumerable<long> trackedPlayerIds, IEnumerable<long> activePlayerIds)
    {
        HashSet<long> active = activePlayerIds.ToHashSet();
        return trackedPlayerIds.Where(playerId => !active.Contains(playerId)).ToArray();
    }

    public static bool RemovePeerState(IDictionary<long, PlayerLightState> states, long playerId)
    {
        return states.Remove(playerId);
    }
}
