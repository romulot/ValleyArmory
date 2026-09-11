namespace ValleyArmory.Acquisition;

/// <summary>Pure eligibility rules for the Prismatic Trial's additive mine spawn.</summary>
internal static class PrismaticTrialSpawnPolicy
{
    public const int MinimumMineLevel = 81;

    public const int MaximumMineLevel = 119;

    public static bool IsEligible(bool isMainPlayer, bool isQuestActive, int mineLevel, bool hasEvent, bool alreadyHasTarget)
    {
        return isMainPlayer
            && isQuestActive
            && mineLevel is >= MinimumMineLevel and <= MaximumMineLevel
            && !hasEvent
            && !alreadyHasTarget;
    }
}

/// <summary>Tracks floors already supplied with a trial golem, preventing same-day re-entry farming.</summary>
internal sealed class PrismaticTrialSpawnTracker
{
    private readonly HashSet<(int Day, int MineLevel)> processedFloors = new();

    public bool TryReserve(int day, int mineLevel)
    {
        return this.processedFloors.Add((day, mineLevel));
    }

    public void Clear()
    {
        this.processedFloors.Clear();
    }
}
