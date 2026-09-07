using System.Text.Json.Nodes;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;
using StardewValley.GameData.Shops;
using ValleyArmory.Acquisition;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class AcquisitionPipelineTests
{
    private const string GuildShopId = "AdventureShop";

    // ---- Definition-level validation ----

    [Fact]
    public void ShopAcquisitionWithoutShopIdFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            root["equipment"]!.AsArray()[0]!["acquisition"]!["shop"]!["shopId"] = ""
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.shop.shopId", StringComparison.Ordinal));
    }

    [Fact]
    public void RemovingShopBlockLeavesEquipmentValidWithoutShopAcquisition()
    {
        string path = CreateModifiedCatalog(root =>
        {
            JsonObject acquisition = root["equipment"]!.AsArray()[0]!["acquisition"]!.AsObject();
            acquisition.Remove("shop");
        });

        ArmoryCatalog catalog = CreateLoader().Load(path);

        EquipmentDefinition item = Assert.Single(catalog.Equipment, i => i.Id == "romulot.ValleyArmory_MinersBlade");
        Assert.Null(item.Acquisition!.Shop);
    }

    [Fact]
    public void MissingAcquisitionObjectFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            root["equipment"]!.AsArray()[0]!.AsObject().Remove("acquisition")
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition: value is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ZeroPriceWithShopAcquisitionFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            root["equipment"]!.AsArray()[0]!["stats"]!["price"] = 0
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.shop: requires stats.price greater than zero", StringComparison.Ordinal));
    }

    [Fact]
    public void EquipmentWithoutShopAcquisitionIsAllowed()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        EquipmentDefinition prismaticBlade = Assert.Single(catalog.Equipment, item => item.Id == "romulot.ValleyArmory_PrismaticBlade");

        Assert.Null(prismaticBlade.Acquisition!.Shop);
        Assert.Null(prismaticBlade.Acquisition.Drop);
    }

    // ---- Catalog-level ----

    [Fact]
    public void TwelveOfThirteenEquipmentAreConfiguredForShop()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        int shopCount = catalog.Equipment.Count(item => item.Acquisition?.Shop is not null);

        Assert.Equal(12, shopCount);
    }

    [Fact]
    public void AllShopEquipmentHaveExplicitPositivePricesAndNonEmptyShopIds()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        foreach (EquipmentDefinition item in catalog.Equipment.Where(i => i.Acquisition?.Shop is not null))
        {
            Assert.True(item.Stats!.Price > 0, $"{item.Id} must have a positive shop price.");
            Assert.False(string.IsNullOrWhiteSpace(item.Acquisition!.Shop!.ShopId), $"{item.Id} must declare a shop ID.");
        }
    }

    [Fact]
    public void AllShopEquipmentReferenceTheCentralizedAdventureGuildId()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        Assert.All(
            catalog.Equipment.Where(item => item.Acquisition?.Shop is not null),
            item => Assert.Equal(ShopIdentifiers.AdventureGuild, item.Acquisition!.Shop!.ShopId)
        );
    }

    [Fact]
    public void PrismaticBladeIsNotSoldInAnyShop()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        EquipmentDefinition prismaticBlade = Assert.Single(catalog.Equipment, item => item.Id == "romulot.ValleyArmory_PrismaticBlade");

        Assert.Null(prismaticBlade.Acquisition!.Shop);
    }

    [Fact]
    public void PhaseSevenAShopConfigurationIsPreservedExactlyAfterTheDropMigration()
    {
        ArmoryCatalog catalog = LoadValidCatalog();
        (string Id, string Condition)[] expected =
        {
            ("romulot.ValleyArmory_MinersBlade", "MINE_LOWEST_LEVEL_REACHED 40"),
            ("romulot.ValleyArmory_BlackIronSword", "MINE_LOWEST_LEVEL_REACHED 10"),
            ("romulot.ValleyArmory_ShadowFang", "MINE_LOWEST_LEVEL_REACHED 40"),
            ("romulot.ValleyArmory_MoonDagger", "MINE_LOWEST_LEVEL_REACHED 80"),
            ("romulot.ValleyArmory_Stonebreaker", "MINE_LOWEST_LEVEL_REACHED 10"),
            ("romulot.ValleyArmory_AbyssHammer", "MINE_LOWEST_LEVEL_REACHED 80"),
            ("romulot.ValleyArmory_MinersBoots", "MINE_LOWEST_LEVEL_REACHED 10"),
            ("romulot.ValleyArmory_ObsidianBoots", "MINE_LOWEST_LEVEL_REACHED 40"),
            ("romulot.ValleyArmory_EtherealBoots", "MINE_LOWEST_LEVEL_REACHED 80"),
            ("romulot.ValleyArmory_MinersArmor", "MINE_LOWEST_LEVEL_REACHED 10"),
            ("romulot.ValleyArmory_ObsidianArmor", "MINE_LOWEST_LEVEL_REACHED 40"),
            ("romulot.ValleyArmory_EtherealArmor", "MINE_LOWEST_LEVEL_REACHED 80")
        };

        foreach ((string id, string condition) in expected)
        {
            EquipmentDefinition item = GetDefinition(id);
            Assert.Equal(GuildShopId, item.Acquisition!.Shop!.ShopId);
            Assert.Equal(condition, item.Acquisition.Shop!.Condition);
        }
    }

    // ---- Pipeline / BuildEntry ----

    public static IEnumerable<object[]> ShopEquipmentIds()
    {
        yield return new object[] { "romulot.ValleyArmory_MinersBlade", "(W)", "MINE_LOWEST_LEVEL_REACHED 40" };
        yield return new object[] { "romulot.ValleyArmory_MinersBoots", "(B)", "MINE_LOWEST_LEVEL_REACHED 10" };
        yield return new object[] { "romulot.ValleyArmory_EtherealArmor", "(S)", "MINE_LOWEST_LEVEL_REACHED 80" };
    }

    [Theory]
    [MemberData(nameof(ShopEquipmentIds))]
    public void BuildEntryProducesQualifiedIdPriceStockAndCondition(string equipmentId, string expectedPrefix, string expectedCondition)
    {
        EquipmentDefinition definition = GetDefinition(equipmentId);

        ShopItemData entry = ShopAcquisitionInjector.BuildEntry(definition);

        Assert.Equal(equipmentId, entry.Id);
        Assert.StartsWith(expectedPrefix, entry.ItemId, StringComparison.Ordinal);
        Assert.Equal(EquipmentIdentity.GetQualifiedItemId(definition), entry.ItemId);
        Assert.Equal(definition.Stats!.Price, entry.Price);
        Assert.Equal(-1, entry.AvailableStock);
        Assert.Equal(expectedCondition, entry.Condition);
    }

    [Fact]
    public void BuildEntryDoesNotDependOnEquipmentName()
    {
        // The pipeline reads Id/Stats/Sprite/Acquisition generically; no branch keys off a specific equipment name.
        ArmoryCatalog catalog = LoadValidCatalog();

        foreach (EquipmentDefinition item in catalog.Equipment.Where(i => i.Acquisition?.Shop is not null))
        {
            ShopItemData entry = ShopAcquisitionInjector.BuildEntry(item);
            Assert.Equal(item.Id, entry.Id);
        }
    }

    [Fact]
    public void AbsentConditionMeansAlwaysAvailable()
    {
        EquipmentDefinition source = GetDefinition("romulot.ValleyArmory_MinersBoots");
        EquipmentDefinition withoutCondition = new()
        {
            Id = source.Id,
            Type = source.Type,
            Rarity = source.Rarity,
            DisplayNameKey = source.DisplayNameKey,
            DescriptionKey = source.DescriptionKey,
            Stats = source.Stats,
            Sprite = source.Sprite,
            ColorIndex = source.ColorIndex,
            Acquisition = new AcquisitionMetadata
            {
                Shop = new ShopAcquisition { ShopId = GuildShopId, Condition = null }
            }
        };

        ShopItemData entry = ShopAcquisitionInjector.BuildEntry(withoutCondition);

        Assert.Null(entry.Condition);
    }

    // ---- Compatibility / isolation ----

    [Fact]
    public void ApplyToAddsEntriesWithoutRemovingVanillaItems()
    {
        ShopData shop = new()
        {
            Items = new List<ShopItemData>
            {
                new() { Id = "RustySword", ItemId = "(W)0", Price = 250, AvailableStock = -1 }
            }
        };
        Dictionary<string, ShopData> shops = new(StringComparer.Ordinal) { [GuildShopId] = shop };

        ShopAcquisitionInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(shops);

        Assert.Contains(shop.Items, entry => entry.Id == "RustySword");
        Assert.Equal(1 + 12, shop.Items.Count);
    }

    [Fact]
    public void ApplyToSkipsUnknownShopIdWithoutThrowing()
    {
        Dictionary<string, ShopData> shops = new(StringComparer.Ordinal);

        ShopAcquisitionInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        Exception? exception = Record.Exception(() => injector.ApplyTo(shops));

        Assert.Null(exception);
    }

    [Fact]
    public void ApplyToIsolatesACollisionAndStillAddsOtherEntries()
    {
        ShopData shop = new()
        {
            Items = new List<ShopItemData>
            {
                new() { Id = "romulot.ValleyArmory_MinersBlade", ItemId = "(W)0", Price = 1, AvailableStock = -1 }
            }
        };
        Dictionary<string, ShopData> shops = new(StringComparer.Ordinal) { [GuildShopId] = shop };

        ShopAcquisitionInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(shops);

        Assert.Equal(1, shop.Items.Count(entry => entry.Id == "romulot.ValleyArmory_MinersBlade"));
        Assert.Contains(shop.Items, entry => entry.Id == "romulot.ValleyArmory_MinersBoots");
        Assert.Equal(1 + 11, shop.Items.Count);
    }

    [Fact]
    public void ApplyToDoesNotTouchOtherShops()
    {
        ShopData guild = new() { Items = new List<ShopItemData>() };
        ShopData otherShop = new() { Items = new List<ShopItemData> { new() { Id = "SomeOtherItem", ItemId = "(O)1", Price = 10, AvailableStock = -1 } } };
        Dictionary<string, ShopData> shops = new(StringComparer.Ordinal)
        {
            [GuildShopId] = guild,
            ["SeedShop"] = otherShop
        };

        ShopAcquisitionInjector injector = new(new CatalogIndex(LoadValidCatalog()), new FakeMonitor());
        injector.ApplyTo(shops);

        Assert.Single(otherShop.Items);
        Assert.Equal("SomeOtherItem", otherShop.Items[0].Id);
    }

    // ---- Types coverage ----

    [Fact]
    public void SameBuildEntryPipelineWorksForWeaponBootsAndArmor()
    {
        ShopItemData weapon = ShopAcquisitionInjector.BuildEntry(GetDefinition("romulot.ValleyArmory_BlackIronSword"));
        ShopItemData boots = ShopAcquisitionInjector.BuildEntry(GetDefinition("romulot.ValleyArmory_ObsidianBoots"));
        ShopItemData armor = ShopAcquisitionInjector.BuildEntry(GetDefinition("romulot.ValleyArmory_MinersArmor"));

        Assert.StartsWith("(W)", weapon.ItemId, StringComparison.Ordinal);
        Assert.StartsWith("(B)", boots.ItemId, StringComparison.Ordinal);
        Assert.StartsWith("(S)", armor.ItemId, StringComparison.Ordinal);
        Assert.All(new[] { weapon, boots, armor }, entry => Assert.Equal(-1, entry.AvailableStock));
    }

    // ---- Drop: definition-level validation ----

    [Fact]
    public void DropAcquisitionRequiresSourceTypeFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
        {
            JsonObject drop = FindEquipment(root, "romulot.ValleyArmory_ShadowFang")["acquisition"]!["drop"]!.AsObject();
            drop.Remove("sourceType");
        });

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.drop.sourceType: value is required", StringComparison.Ordinal));
    }

    [Fact]
    public void DropAcquisitionRequiresSourceIdFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_ShadowFang")["acquisition"]!["drop"]!["sourceId"] = ""
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.drop.sourceId: value is required", StringComparison.Ordinal));
    }

    [Fact]
    public void DropChanceMustBeGreaterThanZero()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_ShadowFang")["acquisition"]!["drop"]!["chance"] = 0
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.drop.chance", StringComparison.Ordinal));
    }

    [Fact]
    public void DropChanceAboveTheAllowedCeilingFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_ShadowFang")["acquisition"]!["drop"]!["chance"] = 0.9
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.drop.chance", StringComparison.Ordinal));
    }

    [Fact]
    public void BlankDropConditionFailsValidation()
    {
        string path = CreateModifiedCatalog(root =>
            FindEquipment(root, "romulot.ValleyArmory_ShadowFang")["acquisition"]!["drop"]!["condition"] = "   "
        );

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(() => CreateLoader().Load(path));

        Assert.Contains(exception.Errors, error => error.Contains("acquisition.drop.condition", StringComparison.Ordinal));
    }

    // ---- Drop: catalog-level ----

    [Fact]
    public void EightOfThirteenEquipmentAreConfiguredForMonsterDrops()
    {
        ArmoryCatalog catalog = LoadValidCatalog();

        int dropCount = catalog.Equipment.Count(item => item.Acquisition?.Drop is not null);

        Assert.Equal(8, dropCount);
    }

    [Fact]
    public void AllDropEquipmentUseTheMonsterSourceTypeAndAKnownIdentifier()
    {
        HashSet<string> knownMonsters = new(StringComparer.Ordinal)
        {
            MonsterIdentifiers.GreenSlime,
            MonsterIdentifiers.ShadowBrute,
            MonsterIdentifiers.LavaCrab,
            MonsterIdentifiers.HotHead,
            MonsterIdentifiers.SkeletonMage,
            MonsterIdentifiers.IridiumGolem,
            MonsterIdentifiers.CarbonGhost,
            MonsterIdentifiers.PutridGhost
        };

        ArmoryCatalog catalog = LoadValidCatalog();

        foreach (EquipmentDefinition item in catalog.Equipment.Where(i => i.Acquisition?.Drop is not null))
        {
            DropAcquisition drop = item.Acquisition!.Drop!;
            Assert.Equal(DropSourceType.Monster, drop.SourceType);
            Assert.Contains(drop.SourceId, knownMonsters);
            Assert.True(drop.Chance is > 0 and <= 0.5, $"{item.Id} drop chance {drop.Chance} looks unjustified.");
        }
    }

    [Fact]
    public void ShopAndDropCanCoexistOnTheSameEquipment()
    {
        EquipmentDefinition shadowFang = GetDefinition("romulot.ValleyArmory_ShadowFang");

        Assert.NotNull(shadowFang.Acquisition!.Shop);
        Assert.NotNull(shadowFang.Acquisition.Drop);
        Assert.Equal(MonsterIdentifiers.ShadowBrute, shadowFang.Acquisition.Drop!.SourceId);
    }

    [Fact]
    public void PrismaticBladeHasNeitherShopNorDropAcquisition()
    {
        EquipmentDefinition prismaticBlade = GetDefinition("romulot.ValleyArmory_PrismaticBlade");

        Assert.Null(prismaticBlade.Acquisition!.Shop);
        Assert.Null(prismaticBlade.Acquisition.Drop);
    }

    // ---- Drop: pipeline / resolver ----

    [Fact]
    public void DropRuleResolverReturnsRulesForItsConfiguredMonsterAndNothingForOthers()
    {
        DropRuleResolver resolver = new(new CatalogIndex(LoadValidCatalog()));

        IReadOnlyList<DropRule> shadowBruteRules = resolver.GetRulesFor(MonsterIdentifiers.ShadowBrute);
        IReadOnlyList<DropRule> unrelatedMonsterRules = resolver.GetRulesFor("Green Serpent Frog Slime Rock");

        Assert.Single(shadowBruteRules);
        Assert.Equal("romulot.ValleyArmory_ShadowFang", shadowBruteRules[0].Equipment.Id);
        Assert.Empty(unrelatedMonsterRules);
    }

    [Fact]
    public void DropRuleResolverDoesNotDependOnEquipmentName()
    {
        DropRuleResolver resolver = new(new CatalogIndex(LoadValidCatalog()));

        foreach ((string monsterName, string expectedEquipmentId) in new[]
        {
            (MonsterIdentifiers.GreenSlime, "romulot.ValleyArmory_MinersBoots"),
            (MonsterIdentifiers.LavaCrab, "romulot.ValleyArmory_ObsidianBoots"),
            (MonsterIdentifiers.HotHead, "romulot.ValleyArmory_ObsidianArmor"),
            (MonsterIdentifiers.SkeletonMage, "romulot.ValleyArmory_MoonDagger"),
            (MonsterIdentifiers.IridiumGolem, "romulot.ValleyArmory_AbyssHammer"),
            (MonsterIdentifiers.CarbonGhost, "romulot.ValleyArmory_EtherealBoots"),
            (MonsterIdentifiers.PutridGhost, "romulot.ValleyArmory_EtherealArmor")
        })
        {
            IReadOnlyList<DropRule> rules = resolver.GetRulesFor(monsterName);
            Assert.Single(rules);
            Assert.Equal(expectedEquipmentId, rules[0].Equipment.Id);
        }
    }

    [Fact]
    public void DropRuleResolverSupportsMultipleRulesOnTheSameMonsterWithoutDuplication()
    {
        ArmoryCatalog syntheticCatalog = new()
        {
            SchemaVersion = 1,
            Rarities = LoadValidCatalog().Rarities,
            Equipment = new[]
            {
                MakeSyntheticDropDefinition("romulot.ValleyArmory_MinersBoots", "Green Slime", 0.05),
                MakeSyntheticDropDefinition("romulot.ValleyArmory_MinersArmor", "Green Slime", 0.05)
            }
        };

        DropRuleResolver resolver = new(new CatalogIndex(syntheticCatalog));
        IReadOnlyList<DropRule> rules = resolver.GetRulesFor("Green Slime");

        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, rule => rule.Equipment.Id == "romulot.ValleyArmory_MinersBoots");
        Assert.Contains(rules, rule => rule.Equipment.Id == "romulot.ValleyArmory_MinersArmor");
    }

    [Theory]
    [InlineData(0.0, 0.05, true)]
    [InlineData(0.049, 0.05, true)]
    [InlineData(0.05, 0.05, false)]
    [InlineData(0.5, 0.05, false)]
    public void RolledSuccessComparesRngValueAgainstChanceDeterministically(double rngValue, double chance, bool expected)
    {
        Assert.Equal(expected, DropRuleResolver.RolledSuccess(chance, rngValue));
    }

    [Fact]
    public void DropConfiguredEquipmentResolvesToTheSameQualifiedIdAcrossTypes()
    {
        EquipmentDefinition weaponDrop = GetDefinition("romulot.ValleyArmory_ShadowFang");
        EquipmentDefinition bootsDrop = GetDefinition("romulot.ValleyArmory_ObsidianBoots");
        EquipmentDefinition armorDrop = GetDefinition("romulot.ValleyArmory_ObsidianArmor");

        Assert.Equal("(W)romulot.ValleyArmory_ShadowFang", EquipmentIdentity.GetQualifiedItemId(weaponDrop));
        Assert.Equal("(B)romulot.ValleyArmory_ObsidianBoots", EquipmentIdentity.GetQualifiedItemId(bootsDrop));
        Assert.Equal("(S)romulot.ValleyArmory_ObsidianArmor", EquipmentIdentity.GetQualifiedItemId(armorDrop));
    }

    private static EquipmentDefinition MakeSyntheticDropDefinition(string id, string monsterName, double chance)
    {
        EquipmentDefinition source = GetDefinition(id);
        return new EquipmentDefinition
        {
            Id = source.Id,
            Type = source.Type,
            WeaponBehavior = source.WeaponBehavior,
            Rarity = source.Rarity,
            DisplayNameKey = source.DisplayNameKey,
            DescriptionKey = source.DescriptionKey,
            Stats = source.Stats,
            Sprite = source.Sprite,
            ColorIndex = source.ColorIndex,
            Acquisition = new AcquisitionMetadata
            {
                Drop = new DropAcquisition
                {
                    SourceType = DropSourceType.Monster,
                    SourceId = monsterName,
                    Chance = chance
                }
            }
        };
    }

    private static JsonObject FindEquipment(JsonObject root, string id)
    {
        return root["equipment"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<string>() == id);
    }

    private static ArmoryCatalog LoadValidCatalog()
    {
        return CreateLoader().Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json"));
    }

    private static ArmoryCatalogLoader CreateLoader()
    {
        return new ArmoryCatalogLoader(new ArmoryCatalogValidator());
    }

    private static EquipmentDefinition GetDefinition(string itemId)
    {
        return Assert.Single(LoadValidCatalog().Equipment, item => item.Id == itemId);
    }

    private static string CreateModifiedCatalog(Action<JsonObject> modify)
    {
        string sourcePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        JsonObject root = JsonNode.Parse(File.ReadAllText(sourcePath))!.AsObject();
        modify(root);
        string path = Path.Combine(Path.GetTempPath(), $"valley-armory-acquisition-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, root.ToJsonString());
        return path;
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
