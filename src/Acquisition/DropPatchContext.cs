using StardewModdingAPI;

namespace ValleyArmory.Acquisition;

/// <summary>Static bridge from the Harmony postfix (which must be static) to the resolver/monitor instances, mirroring TooltipPatchContext.</summary>
internal static class DropPatchContext
{
    private static DropRuleResolver? resolver;
    private static IMonitor? monitor;
    private static bool warned;

    public static bool Enabled { get; private set; }

    public static void Initialize(DropRuleResolver dropRuleResolver, IMonitor modMonitor)
    {
        resolver = dropRuleResolver;
        monitor = modMonitor;
        warned = false;
        Enabled = true;
    }

    public static void Disable(string reason, Exception? exception = null)
    {
        Enabled = false;
        Trace($"atomic fallback triggered: {reason}{(exception is null ? string.Empty : $" ({exception.GetType().Name}: {exception.Message})")}");
        if (warned)
            return;

        warned = true;
        string details = exception is null ? reason : $"{reason} {exception.Message}";
        monitor?.Log($"Monster drop rules were disabled; vanilla loot remains active. {details}", LogLevel.Warn);
    }

    public static bool TryGetRules(string? monsterName, out IReadOnlyList<DropRule> rules)
    {
        rules = Array.Empty<DropRule>();
        if (!Enabled || resolver is null || string.IsNullOrEmpty(monsterName))
        {
            return false;
        }

        rules = resolver.GetRulesFor(monsterName);
        return rules.Count > 0;
    }

    public static void Trace(string message)
    {
        monitor?.Log(message, LogLevel.Trace);
    }

    public static void LogError(string message)
    {
        monitor?.Log(message, LogLevel.Error);
    }
}
