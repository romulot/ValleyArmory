namespace ValleyArmory.Acquisition;

/// <summary>Centralizes the IDs used by Valley Armory's Special Order / mail reward pipeline.</summary>
internal static class QuestIdentifiers
{
    /// <summary>Data/SpecialOrders key for the Prismatic Blade's endgame quest.</summary>
    public const string PrismaticTrial = "romulot.ValleyArmory_PrismaticTrial";

    /// <summary>Data/mail key for the Prismatic Trial's reward letter.</summary>
    public const string PrismaticTrialMail = "romulot.ValleyArmory_PrismaticTrialReward";

    /// <summary>Real vanilla Data/Monsters key used as the Prismatic Trial's slay target (deep-mines elite, already used for Abyss Hammer's Fase 7B drop).</summary>
    public const string PrismaticTrialTargetMonster = "Iridium Golem";

    public const int PrismaticTrialRequiredKills = 15;
}
