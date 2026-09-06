using StardewModdingAPI;

namespace ValleyArmory;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        this.Monitor.Log(
            $"Valley Armory {this.ModManifest.Version} loaded.",
            LogLevel.Info
        );
    }
}
