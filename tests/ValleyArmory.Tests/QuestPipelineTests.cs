using System.Text.Json.Nodes;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;
using StardewValley.GameData;
using StardewValley.GameData.SpecialOrders;
using ValleyArmory.Acquisition;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class QuestPipelineTests
{
    // ---- Definition-level validation ----

    [Fact]
    public void MissingQuestIdFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_PrismaticBlade")["acquisition"]!["quest"]!["questId"] = ""
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.quest.questId: value is required", StringComparison.Ordinal));
    }

    [Fact]
    public void BlankUnlockConditionFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_PrismaticBlade")["acquisition"]!["quest"]!["unlockCondition"] = "   "
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.quest.unlockCondition", StringComparison.Ordinal));
    }

    [Fact]
    public void EquipmentWithoutQuestAcquisitionIsAllowed()
    {
        EquipmentDefinition blackIronSword = GetDefinition("romulot.ValleyArmory_BlackIronSword");

        Assert.Null(blackIronSword.Acquisition!.Quest);
    }

    [Fact]
    public void QuestCanCoexistWithOtherAcquisitionFieldsInPrinciple()
    {
        // The model does not forbid Quest alongside Shop/Drop/Crafting; Prismatic Blade simply
        // chooses not to combine them, by design (see PrismaticBladeHasOnlyQuestAcquisition).
        ArmoryCatalog catalog = LoadValidCatalog();

        EquipmentDefinition prismaticBlade = Assert.Single(catalog.Equipment, item => item.Id == "romulot.ValleyArmory_PrismaticBlade");
        Assert.NotNull(prismaticBlade.Acquisition!.Quest);
    }

    // ---- Prismatic Blade's special role ----

    [Fact]
    public void PrismaticBladeHasOnlyQuestAcquisition()
    {
        EquipmentDefinition prismaticBlade = GetDefinition("romulot.ValleyArmory_PrismaticBlade");

        Assert.Null(prismaticBlade.Acquisition!.Shop);
        Assert.Null(prismaticBlade.Acquisition.Drop);
        Assert.Null(prismaticBlade.Acquisition.Crafting);
        Assert.NotNull(prismaticBlade.Acquisition.Quest);
        Assert.Equal(QuestIdentifiers.PrismaticTrial, prismaticBlade.Acquisition.Quest!.QuestId);
    }

    [Fact]
    public void OnlyOneEquipmentUsesQuestAcquisition()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        int questCount = catalog.Equipment.Count(item => item.Acquisition?.Quest is not null);

        Assert.Equal(1, questCount);
    }

    // ---- Unlock trigger ----

    [Fact]
    public void BuildTriggerActionUsesDayStartedAndAddSpecialOrder()
    {
        EquipmentDefinition definition = GetDefinition("romulot.ValleyArmory_PrismaticBlade");

        TriggerActionData action = QuestUnlockInjector.BuildTriggerAction(definition);

        Assert.Equal("DayStarted", action.Trigger);
        Assert.Equal("AddSpecialOrder romulot.ValleyArmory_PrismaticTrial", action.Action);
        Assert.Equal("MINE_LOWEST_LEVEL_REACHED 120", action.Condition);
        Assert.Equal("romulot.ValleyArmory_PrismaticBlade_QuestUnlock", action.Id);
    }

    [Fact]
    public void ApplyToTriggerActionsPreservesVanillaEntriesAndIsIdempotent()
    {
        List<TriggerActionData> actions = new()
        {
            new TriggerActionData { Id = "Mail_Abigail_8heart", Trigger = "DayEnding", Action = "AddMail Current abbySpiritBoard" }
        };

        QuestUnlockInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(actions);
        injector.ApplyTo(actions); // simulate re-edit

        Assert.Contains(actions, a => a.Id == "Mail_Abigail_8heart");
        Assert.Equal(1 + 1, actions.Count);
    }

    // ---- Special Order ----

    [Fact]
    public void BuildSpecialOrderUsesSlayObjectiveAndMailReward()
    {
        EquipmentDefinition prismaticBlade = GetDefinition("romulot.ValleyArmory_PrismaticBlade");
        QuestAcquisition quest = prismaticBlade.Acquisition!.Quest!;

        SpecialOrderData order = SpecialOrderInjector.BuildSpecialOrder(quest, key => $"translated:{key}");

        Assert.Equal("Marlon", order.Requester);
        Assert.Equal(QuestDuration.Month, order.Duration);
        Assert.False(order.Repeatable);
        Assert.Equal("MINE_LOWEST_LEVEL_REACHED 120", order.Condition);

        SpecialOrderObjectiveData objective = Assert.Single(order.Objectives);
        Assert.Equal("Slay", objective.Type);
        Assert.Equal(QuestIdentifiers.PrismaticTrialRequiredKills.ToString(), objective.RequiredCount);
        Assert.Equal(QuestIdentifiers.PrismaticTrialTargetMonster, objective.Data["TargetName"]);

        SpecialOrderRewardData reward = Assert.Single(order.Rewards);
        Assert.Equal("Mail", reward.Type);
        Assert.Equal(QuestIdentifiers.PrismaticTrialMail, reward.Data["MailReceived"]);
    }

    [Theory]
    [InlineData(false, true, 100, false, false)]
    [InlineData(true, false, 100, false, false)]
    [InlineData(true, true, 80, false, false)]
    [InlineData(true, true, 120, false, false)]
    [InlineData(true, true, 100, true, false)]
    [InlineData(true, true, 100, false, true)]
    public void PrismaticTrialSpawnRequiresHostActiveQuestEligibleFloorAndSafeState(
        bool isMainPlayer,
        bool isQuestActive,
        int mineLevel,
        bool hasEvent,
        bool alreadyHasTarget
    )
    {
        Assert.False(PrismaticTrialSpawnPolicy.IsEligible(isMainPlayer, isQuestActive, mineLevel, hasEvent, alreadyHasTarget));
    }

    [Theory]
    [InlineData(PrismaticTrialSpawnPolicy.MinimumMineLevel)]
    [InlineData(100)]
    [InlineData(PrismaticTrialSpawnPolicy.MaximumMineLevel)]
    public void PrismaticTrialCanSpawnExactlyOncePerEligibleFloorAndDay(int mineLevel)
    {
        PrismaticTrialSpawnTracker tracker = new();

        Assert.True(PrismaticTrialSpawnPolicy.IsEligible(true, true, mineLevel, false, false));
        Assert.True(tracker.TryReserve(42, mineLevel));
        Assert.False(tracker.TryReserve(42, mineLevel));
        Assert.True(tracker.TryReserve(43, mineLevel));
    }

    [Fact]
    public void PrismaticTrialObjectiveOnlyTargetsIridiumGolemAndStopsAtFifteen()
    {
        EquipmentDefinition definition = GetDefinition("romulot.ValleyArmory_PrismaticBlade");
        SpecialOrderData order = SpecialOrderInjector.BuildSpecialOrder(definition.Acquisition!.Quest!, key => key);
        SpecialOrderObjectiveData objective = Assert.Single(order.Objectives);

        Assert.Equal(MonsterIdentifiers.IridiumGolem, objective.Data["TargetName"]);
        Assert.NotEqual(MonsterIdentifiers.GreenSlime, objective.Data["TargetName"]);
        Assert.Equal("15", objective.RequiredCount);
    }

    [Fact]
    public void ApplyToSpecialOrdersPreservesExistingEntry()
    {
        Dictionary<string, SpecialOrderData> orders = new(StringComparer.Ordinal)
        {
            ["Wizard2"] = new SpecialOrderData { Requester = "Wizard" }
        };

        SpecialOrderInjector injector = new(new CatalogIndex(LoadValidCatalog()), null!, new FakeMonitor());
        injector.ApplyTo(orders, key => $"translated:{key}");

        Assert.Equal("Wizard", orders["Wizard2"].Requester);
        Assert.Equal(2, orders.Count);
    }

    // ---- Mail ----

    [Fact]
    public void BuildLetterAttachesTheLinkedEquipmentQualifiedId()
    {
        EquipmentDefinition prismaticBlade = GetDefinition("romulot.ValleyArmory_PrismaticBlade");
        QuestBlueprint blueprint = QuestBlueprints.Resolve(prismaticBlade.Acquisition!.Quest!.QuestId);

        string letter = QuestMailInjector.BuildLetter(prismaticBlade, blueprint, key => $"translated:{key}");
        string expectedId = EquipmentIdentity.GetQualifiedItemId(prismaticBlade);

        Assert.Contains($"%item id {expectedId} 1 %%", letter, StringComparison.Ordinal);
        Assert.EndsWith("[#]translated:quest.prismatic-trial.mail-title", letter, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyToMailPreservesExistingEntry()
    {
        Dictionary<string, string> mail = new(StringComparer.Ordinal)
        {
            ["Robin"] = "Hey there! %item id (O)388 50 %%[#]A Gift From Robin"
        };

        QuestMailInjector injector = new(new CatalogIndex(LoadValidCatalog()), null!, new FakeMonitor());
        injector.ApplyTo(mail, key => $"translated:{key}");

        Assert.StartsWith("Hey there!", mail["Robin"], StringComparison.Ordinal);
        Assert.Equal(2, mail.Count);
    }

    private static EquipmentDefinition GetDefinition(string itemId)
    {
        return Assert.Single(LoadValidCatalog().Equipment, item => item.Id == itemId);
    }

    private static ArmoryCatalog LoadValidCatalog()
    {
        return CreateLoader().Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json"));
    }

    private static ArmoryCatalogLoader CreateLoader()
    {
        return new ArmoryCatalogLoader(new ArmoryCatalogValidator());
    }

    private static string CreateModifiedCatalog(Action<JsonObject> modify)
    {
        string sourcePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        JsonObject root = JsonNode.Parse(File.ReadAllText(sourcePath))!.AsObject();
        modify(root);
        string path = Path.Combine(Path.GetTempPath(), $"valley-armory-quest-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, root.ToJsonString());
        return path;
    }

    private static JsonObject FindEquipment(JsonObject root, string id)
    {
        return root["equipment"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<string>() == id);
    }

    private sealed class FakeMonitor : IMonitor
    {
        public bool IsVerbose => false;

        public void Log(string message, LogLevel level = LogLevel.Trace)
        {
        }

        public void LogOnce(string message, LogLevel level = LogLevel.Trace)
        {
        }

        public void VerboseLog(string message)
        {
        }

        public void VerboseLog(ref VerboseLogStringHandler message)
        {
        }
    }
}
