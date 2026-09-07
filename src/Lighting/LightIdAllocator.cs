namespace ValleyArmory.Lighting;

internal sealed class LightIdAllocator
{
    private readonly string prefix;

    public LightIdAllocator(string modUniqueId)
    {
        this.prefix = $"{modUniqueId}/weapon-light/";
    }

    public string Prefix => this.prefix;

    public string GetLocalPlayerLightId(long uniqueMultiplayerId)
    {
        return $"{this.prefix}{uniqueMultiplayerId}";
    }

    public bool IsOwned(string lightId)
    {
        return !string.IsNullOrWhiteSpace(lightId)
            && lightId.StartsWith(this.prefix, StringComparison.Ordinal);
    }
}
