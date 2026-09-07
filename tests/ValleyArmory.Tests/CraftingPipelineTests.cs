using System.Text.Json.Nodes;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;
using StardewValley.GameData;
using ValleyArmory.Acquisition;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class CraftingPipelineTests
{
    // ---- Definition-level validation ----

    [Fact]
    public void EmptyIngredientsListFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_BlackIronSword")["acquisition"]!["crafting"]!["ingredients"] = new JsonArray()
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.crafting.ingredients: at least one ingredient", StringComparison.Ordinal));
    }

    [Fact]
    public void IngredientWithZeroQuantityFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_BlackIronSword")["acquisition"]!["crafting"]!["ingredients"]![0]!["quantity"] = 0
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("requires a quantity greater than zero", StringComparison.Ordinal));
    }

    [Fact]
    public void IngredientWithBlankItemIdFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_BlackIronSword")["acquisition"]!["crafting"]!["ingredients"]![0]!["itemId"] = ""
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("ingredient itemId is required", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateIngredientItemIdFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
        {
            JsonObject crafting = FindEquipment(root, "romulot.ValleyArmory_BlackIronSword")["acquisition"]!["crafting"]!.AsObject();
            JsonArray ingredients = crafting["ingredients"]!.AsArray();
            ingredients.Add(new JsonObject { ["itemId"] = "335", ["quantity"] = 1 });
        });

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("duplicate ingredient itemId", StringComparison.Ordinal));
    }

    [Fact]
    public void BlankUnlockConditionFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
        {
            JsonObject crafting = FindEquipment(root, "romulot.ValleyArmory_ObsidianBoots")["acquisition"]!["crafting"]!.AsObject();
            crafting["unlockCondition"] = "   ";
        });

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.crafting.unlockCondition", StringComparison.Ordinal));
    }

    [Fact]
    public void EquipmentWithoutCraftingAcquisitionIsAllowed()
    {
        EquipmentDefinition shadowFang = GetDefinition("romulot.ValleyArmory_ShadowFang");

        Assert.Null(shadowFang.Acquisition!.Crafting);
    }

    // ---- Catalog-level ----

    [Fact]
    public void NineOfThirteenEquipmentAreConfiguredForCrafting()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        int craftingCount = catalog.Equipment.Count(item => item.Acquisition?.Crafting is not null);

        Assert.Equal(9, craftingCount);
    }

    [Fact]
    public void PrismaticBladeHasNoCraftingAcquisition()
    {
        EquipmentDefinition prismaticBlade = GetDefinition("romulot.ValleyArmory_PrismaticBlade");

        Assert.Null(prismaticBlade.Acquisition!.Crafting);
        Assert.Null(prismaticBlade.Acquisition.Shop);
        Assert.Null(prismaticBlade.Acquisition.Drop);
    }

    [Fact]
    public void AtLeastOneEquipmentHasShopAndCraftingWithoutDrop()
    {
        EquipmentDefinition blackIronSword = GetDefinition("romulot.ValleyArmory_BlackIronSword");

        Assert.NotNull(blackIronSword.Acquisition!.Shop);
        Assert.NotNull(blackIronSword.Acquisition.Crafting);
        Assert.Null(blackIronSword.Acquisition.Drop);
    }

    [Fact]
    public void AtLeastOneEquipmentHasShopDropAndCraftingTogether()
    {
        EquipmentDefinition minersBoots = GetDefinition("romulot.ValleyArmory_MinersBoots");

        Assert.NotNull(minersBoots.Acquisition!.Shop);
        Assert.NotNull(minersBoots.Acquisition.Drop);
        Assert.NotNull(minersBoots.Acquisition.Crafting);
    }

    // ---- Pipeline: recipe string ----

    public static IEnumerable<object[]> CraftableEquipmentIds()
    {
        yield return new object[] { "romulot.ValleyArmory_MinersBlade", "(W)" };
        yield return new object[] { "romulot.ValleyArmory_MinersBoots", "(B)" };
        yield return new object[] { "romulot.ValleyArmory_MinersArmor", "(S)" };
    }

    [Theory]
    [MemberData(nameof(CraftableEquipmentIds))]
    public void BuildRecipeStringProducesCorrectFormatAcrossTypes(string equipmentId, string expectedPrefix)
    {
        EquipmentDefinition definition = GetDefinition(equipmentId);

        string recipe = CraftingRecipeInjector.BuildRecipeString(definition);
        string[] fields = recipe.Split('/');

        Assert.Equal(6, fields.Length); // ingredients/category/output/bigCraftable/unlock/ (trailing empty)
        Assert.Equal("Home", fields[1]);
        Assert.StartsWith(expectedPrefix, fields[2], StringComparison.Ordinal);
        Assert.EndsWith(" 1", fields[2]); // numberProducedPerCraft is always 1
        Assert.Equal("false", fields[3]); // never a big craftable
        Assert.Equal("default", fields[4]);
        Assert.Empty(fields[^1]);

        string expectedIngredients = string.Join(" ", definition.Acquisition!.Crafting!.Ingredients.Select(i => $"{i.ItemId} {i.Quantity}"));
        Assert.Equal(expectedIngredients, fields[0]);
    }

    [Fact]
    public void BuildRecipeStringOutputMatchesEquipmentIdentity()
    {
        EquipmentDefinition definition = GetDefinition("romulot.ValleyArmory_ObsidianArmor");

        string recipe = CraftingRecipeInjector.BuildRecipeString(definition);
        string outputField = recipe.Split('/')[2];

        Assert.Equal($"{EquipmentIdentity.GetQualifiedItemId(definition)} 1", outputField);
    }

    [Fact]
    public void BuildRecipeStringRejectsEquipmentWithoutCraftingData()
    {
        EquipmentDefinition shadowFang = GetDefinition("romulot.ValleyArmory_ShadowFang");

        Assert.Throws<InvalidOperationException>(() => CraftingRecipeInjector.BuildRecipeString(shadowFang));
    }

    [Fact]
    public void ApplyToAddsRecipesWithoutRemovingVanillaEntries()
    {
        Dictionary<string, string> recipes = new(StringComparer.Ordinal)
        {
            ["Wood Fence"] = "388 2/Field/322/false/default/"
        };

        CraftingRecipeInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(recipes);

        Assert.Equal("388 2/Field/322/false/default/", recipes["Wood Fence"]);
        Assert.Equal(1 + 9, recipes.Count);
    }

    [Fact]
    public void ApplyToIsolatesACollisionAndStillAddsOtherRecipes()
    {
        Dictionary<string, string> recipes = new(StringComparer.Ordinal)
        {
            ["romulot.ValleyArmory_BlackIronSword"] = "existing raw value"
        };

        CraftingRecipeInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(recipes);

        Assert.Equal("existing raw value", recipes["romulot.ValleyArmory_BlackIronSword"]);
        Assert.True(recipes.ContainsKey("romulot.ValleyArmory_MinersBoots"));
        Assert.Equal(1 + 8, recipes.Count);
    }

    // ---- Unlock: trigger action ----

    [Fact]
    public void BuildTriggerActionUsesDayStartedAndMarkCraftingRecipeKnown()
    {
        EquipmentDefinition definition = GetDefinition("romulot.ValleyArmory_ObsidianBoots");

        TriggerActionData action = CraftingUnlockInjector.BuildTriggerAction(definition);

        Assert.Equal("DayStarted", action.Trigger);
        Assert.Equal("MarkCraftingRecipeKnown Current romulot.ValleyArmory_ObsidianBoots", action.Action);
        Assert.Equal("MINE_LOWEST_LEVEL_REACHED 40", action.Condition);
        Assert.Equal("romulot.ValleyArmory_ObsidianBoots_CraftingUnlock", action.Id);
    }

    [Fact]
    public void BuildTriggerActionHasNullConditionWhenNoneConfigured()
    {
        EquipmentDefinition definition = GetDefinition("romulot.ValleyArmory_BlackIronSword");

        TriggerActionData action = CraftingUnlockInjector.BuildTriggerAction(definition);

        Assert.Null(action.Condition);
        Assert.Equal("MarkCraftingRecipeKnown Current romulot.ValleyArmory_BlackIronSword", action.Action);
    }

    [Fact]
    public void ApplyToAddsTriggersWithoutRemovingVanillaEntries()
    {
        List<TriggerActionData> actions = new()
        {
            new TriggerActionData { Id = "Mail_Abigail_8heart", Trigger = "DayEnding", Action = "AddMail Current abbySpiritBoard" }
        };

        CraftingUnlockInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(actions);

        Assert.Contains(actions, a => a.Id == "Mail_Abigail_8heart");
        Assert.Equal(1 + 9, actions.Count);
    }

    [Fact]
    public void ApplyToTriggersIsIdempotentAcrossRepeatedInvocations()
    {
        List<TriggerActionData> actions = new();
        CraftingUnlockInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());

        injector.ApplyTo(actions);
        injector.ApplyTo(actions); // simulates the asset being re-edited (e.g. Content invalidation)

        Assert.Equal(9, actions.Count);
        Assert.Equal(9, actions.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count());
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
        string path = Path.Combine(Path.GetTempPath(), $"valley-armory-crafting-test-{Guid.NewGuid():N}.json");
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
