using StardewValley.GameData.SpecialOrders;

namespace ValleyArmory.Acquisition;

/// <summary>The bespoke quest design (requester, objective, reward letter) behind one QuestId. New quest content means a new blueprint entry, not a branch on equipment identity.</summary>
internal sealed record QuestBlueprint(
    string Requester,
    QuestDuration Duration,
    string DescriptionKey,
    string ObjectiveTargetMonster,
    int ObjectiveRequiredCount,
    string ObjectiveTextKey,
    string MailId,
    string MailBodyKey,
    string MailTitleKey
);

internal static class QuestBlueprints
{
    public static readonly IReadOnlyDictionary<string, QuestBlueprint> ByQuestId = new Dictionary<string, QuestBlueprint>(StringComparer.Ordinal)
    {
        [QuestIdentifiers.PrismaticTrial] = new QuestBlueprint(
            Requester: "Marlon",
            Duration: QuestDuration.Month,
            DescriptionKey: "quest.prismatic-trial.description",
            ObjectiveTargetMonster: QuestIdentifiers.PrismaticTrialTargetMonster,
            ObjectiveRequiredCount: QuestIdentifiers.PrismaticTrialRequiredKills,
            ObjectiveTextKey: "quest.prismatic-trial.objective",
            MailId: QuestIdentifiers.PrismaticTrialMail,
            MailBodyKey: "quest.prismatic-trial.mail-body",
            MailTitleKey: "quest.prismatic-trial.mail-title"
        )
    };

    public static QuestBlueprint Resolve(string questId)
    {
        return ByQuestId.TryGetValue(questId, out QuestBlueprint? blueprint)
            ? blueprint
            : throw new InvalidOperationException($"No quest blueprint registered for QuestId '{questId}'.");
    }
}
