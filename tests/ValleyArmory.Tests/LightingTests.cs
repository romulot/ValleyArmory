using Microsoft.Xna.Framework;
using ValleyArmory.Catalog;
using ValleyArmory.Lighting;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class LightingTests
{
    public static IEnumerable<object[]> WeaponLightCases => new[]
    {
        new object[] { "(W)romulot.ValleyArmory_MinersBlade", new Color(0x4A, 0x90, 0xE2), 1.25f, 0.35f },
        new object[] { "(W)romulot.ValleyArmory_ShadowFang", new Color(0x4A, 0x90, 0xE2), 1.25f, 0.35f },
        new object[] { "(W)romulot.ValleyArmory_MoonDagger", new Color(0x8E, 0x5A, 0xC7), 1.75f, 0.6f },
        new object[] { "(W)romulot.ValleyArmory_AbyssHammer", new Color(0x8E, 0x5A, 0xC7), 1.75f, 0.6f },
        new object[] { "(W)romulot.ValleyArmory_PrismaticBlade", new Color(0xD6, 0x33, 0x84), 3.00f, 0.75f }
    };

    [Theory]
    [MemberData(nameof(WeaponLightCases))]
    public void IlluminatedWeaponsResolveTheirCatalogAppearance(string qualifiedItemId, Color color, float radius, float intensity)
    {
        LightAppearanceResolver resolver = CreateResolver();

        bool found = resolver.TryResolve(qualifiedItemId, out WeaponLightAppearance appearance);

        Assert.True(found);
        Assert.Equal(color, appearance.Color);
        Assert.Equal(radius, appearance.Radius);
        Assert.Equal(intensity, appearance.Intensity);
        Assert.Equal(new Vector2(0f, -32f), appearance.Offset);
    }

    [Theory]
    [InlineData("(W)romulot.ValleyArmory_BlackIronSword")]
    [InlineData("(W)romulot.ValleyArmory_Stonebreaker")]
    [InlineData("(W)0")]
    [InlineData("(W)other.mod_Sword")]
    [InlineData(null)]
    public void CommonWeaponUnknownAndExternalItemsHaveNoLight(string? qualifiedItemId)
    {
        Assert.False(CreateResolver().TryResolve(qualifiedItemId, out _));
    }

    [Theory]
    [InlineData("(B)romulot.ValleyArmory_MinersBoots")]
    [InlineData("(B)romulot.ValleyArmory_ObsidianBoots")]
    [InlineData("(B)romulot.ValleyArmory_EtherealBoots")]
    public void NoBootEverResolvesLightRegardlessOfRarity(string qualifiedItemId)
    {
        bool found = CreateResolver().TryResolve(qualifiedItemId, out WeaponLightAppearance appearance);

        Assert.False(found);
        Assert.Equal(default, appearance);
    }

    [Fact]
    public void RuntimeLightColorsIncreaseInLuminanceWithRarityWithoutApproachingWhite()
    {
        LightAppearanceResolver resolver = CreateResolver();

        Assert.True(resolver.TryResolve("(W)romulot.ValleyArmory_MinersBlade", out WeaponLightAppearance rare));
        Assert.True(resolver.TryResolve("(W)romulot.ValleyArmory_MoonDagger", out WeaponLightAppearance epic));
        Assert.True(resolver.TryResolve("(W)romulot.ValleyArmory_PrismaticBlade", out WeaponLightAppearance legendary));

        Color rareColor = rare.ToRuntimeColor();
        Color epicColor = epic.ToRuntimeColor();
        Color legendaryColor = legendary.ToRuntimeColor();

        // Regression guard: Rare and Epic runtime colors must not change while fixing Legendary.
        Assert.Equal(new Color(26, 50, 79), rareColor);
        Assert.Equal(new Color(85, 54, 119), epicColor);

        // Legendary must be visibly brighter than both, without saturating toward white
        // (a near-white runtime color renders as an invisible light in-game).
        static int Luminance(Color c) => (int)Math.Round((0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B));

        Assert.True(Luminance(legendaryColor) > Luminance(epicColor));
        Assert.True(Luminance(epicColor) > Luminance(rareColor));
        Assert.True(legendaryColor.R < 200 && legendaryColor.G < 200 && legendaryColor.B < 200);
    }

    [Fact]
    public void EquipmentLightOverrideTakesPrecedenceOverRarity()
    {
        ArmoryCatalog catalog = CreateCatalog(new LightOverride
        {
            Enabled = true,
            Color = "#010203",
            Radius = 2.5f,
            Intensity = 0.8f,
            Offset = new VectorOffset { X = 4f, Y = -6f }
        });

        bool found = new LightAppearanceResolver(new CatalogIndex(catalog))
            .TryResolve("(W)romulot.ValleyArmory_MinersBlade", out WeaponLightAppearance appearance);

        Assert.True(found);
        Assert.Equal(new Color(1, 2, 3), appearance.Color);
        Assert.Equal(2.5f, appearance.Radius);
        Assert.Equal(0.8f, appearance.Intensity);
        Assert.Equal(new Vector2(4f, -6f), appearance.Offset);
    }

    [Fact]
    public void DisabledEquipmentOverrideDisablesRarityLight()
    {
        ArmoryCatalog catalog = CreateCatalog(new LightOverride { Enabled = false });

        bool found = new LightAppearanceResolver(new CatalogIndex(catalog))
            .TryResolve("(W)romulot.ValleyArmory_MinersBlade", out _);

        Assert.False(found);
    }

    [Fact]
    public void RareWeaponsShareTheSameBaseAppearance()
    {
        LightAppearanceResolver resolver = CreateResolver();

        Assert.True(resolver.TryResolve("(W)romulot.ValleyArmory_MinersBlade", out WeaponLightAppearance first));
        Assert.True(resolver.TryResolve("(W)romulot.ValleyArmory_ShadowFang", out WeaponLightAppearance second));
        Assert.Equal(first, second);
    }

    [Fact]
    public void StableAppearanceTransitionIsUpdateAndCommonTransitionIsRemove()
    {
        PlayerLightState state = new("romulot.ValleyArmory/weapon-light/42");
        WeaponLightAppearance rare = new(new Color(1, 2, 3), 1.25f, 0.35f, Vector2.Zero);
        WeaponLightAppearance epic = new(new Color(4, 5, 6), 1.75f, 0.6f, Vector2.Zero);

        Assert.Equal(LightReconcileAction.Create, state.DetermineAction(true, "Farm", Vector2.Zero, rare));
        state.MarkApplied("Farm", Vector2.Zero, rare);
        Assert.Equal(LightReconcileAction.Update, state.DetermineAction(true, "Farm", Vector2.Zero, epic));
        state.MarkApplied("Farm", Vector2.Zero, epic);
        Assert.Equal(LightReconcileAction.Remove, state.DetermineAction(false, "Farm", Vector2.Zero, default));
    }

    [Fact]
    public void LightIdIsDeterministicAndNamespaced()
    {
        LightIdAllocator allocator = new("romulot.ValleyArmory");

        string first = allocator.GetLocalPlayerLightId(123456);
        string second = allocator.GetLocalPlayerLightId(123456);
        string other = allocator.GetLocalPlayerLightId(987654);

        Assert.Equal("romulot.ValleyArmory/weapon-light/123456", first);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.True(allocator.IsOwned(first));
        Assert.False(allocator.IsOwned("other.mod/weapon-light/123456"));
    }

    [Fact]
    public void CleanupFilterKeepsOnlyValleyArmoryIds()
    {
        LightIdAllocator allocator = new("romulot.ValleyArmory");

        IReadOnlyList<string> owned = allocator.FilterOwned(new[]
        {
            "romulot.ValleyArmory/weapon-light/1",
            "other.mod/weapon-light/2",
            "romulot.ValleyArmory/weapon-light/1"
        });

        Assert.Single(owned);
        Assert.Equal("romulot.ValleyArmory/weapon-light/1", owned[0]);
    }

    [Fact]
    public void DifferentPlayersReceiveDifferentLightIds()
    {
        LightIdAllocator allocator = new("romulot.ValleyArmory");

        string one = allocator.GetLocalPlayerLightId(111);
        string two = allocator.GetLocalPlayerLightId(222);

        Assert.NotEqual(one, two);
    }

    [Fact]
    public void PlayerLightStateTransitionsAreIdempotent()
    {
        PlayerLightState state = new("romulot.ValleyArmory/weapon-light/42");
        WeaponLightAppearance appearance = new(new Color(1, 2, 3), 1.25f, 0.35f, new Vector2(0f, -32f));

        Assert.Equal(LightReconcileAction.None, state.DetermineAction(eligible: false, "Farm", Vector2.Zero, appearance));
        Assert.Equal(LightReconcileAction.Create, state.DetermineAction(eligible: true, "Farm", new Vector2(10f, 20f), appearance));

        state.MarkApplied("Farm", new Vector2(10f, 20f), appearance);

        Assert.Equal(LightReconcileAction.None, state.DetermineAction(eligible: true, "Farm", new Vector2(10f, 20f), appearance));
        Assert.Equal(LightReconcileAction.Update, state.DetermineAction(eligible: true, "Farm", new Vector2(11f, 20f), appearance));
        Assert.Equal(LightReconcileAction.RebindLocation, state.DetermineAction(eligible: true, "Mine", new Vector2(11f, 20f), appearance));
        Assert.Equal(LightReconcileAction.Remove, state.DetermineAction(eligible: false, "Mine", Vector2.Zero, appearance));
    }

    [Fact]
    public void PlayerStatesStayIndependentAcrossPlayers()
    {
        WeaponLightAppearance appearance = new(new Color(1, 2, 3), 1.25f, 0.35f, new Vector2(0f, -32f));
        PlayerLightState first = new("romulot.ValleyArmory/weapon-light/1");
        PlayerLightState second = new("romulot.ValleyArmory/weapon-light/2");

        first.MarkApplied("Farm", new Vector2(10f, 20f), appearance);

        Assert.True(first.HasLight);
        Assert.False(second.HasLight);
        Assert.Equal(LightReconcileAction.Create, second.DetermineAction(true, "Farm", new Vector2(5f, 6f), appearance));
    }

    [Fact]
    public void DisconnectCleanupRemovesOnlyTargetPeerState()
    {
        Dictionary<long, PlayerLightState> states = new()
        {
            [10] = new PlayerLightState("romulot.ValleyArmory/weapon-light/10"),
            [20] = new PlayerLightState("romulot.ValleyArmory/weapon-light/20")
        };

        bool removed = PlayerLightStateSet.RemovePeerState(states, 20);

        Assert.True(removed);
        Assert.True(states.ContainsKey(10));
        Assert.False(states.ContainsKey(20));
        Assert.Single(states);
    }

    [Fact]
    public void FindStalePlayersReturnsOnlyMissingEntries()
    {
        long[] stale = PlayerLightStateSet.FindStalePlayers(new long[] { 1, 2, 3 }, new long[] { 2, 3, 4 }).ToArray();

        Assert.Single(stale);
        Assert.Equal(1, stale[0]);
    }

    [Fact]
    public void RepeatedStableReconcileEndsInNoAction()
    {
        PlayerLightState state = new("romulot.ValleyArmory/weapon-light/42");
        WeaponLightAppearance appearance = new(new Color(1, 2, 3), 1.25f, 0.35f, new Vector2(0f, -32f));
        Vector2 position = new(30f, 40f);

        Assert.Equal(LightReconcileAction.Create, state.DetermineAction(true, "Farm", position, appearance));
        state.MarkApplied("Farm", position, appearance);

        Assert.Equal(LightReconcileAction.None, state.DetermineAction(true, "Farm", position, appearance));
        Assert.Equal(LightReconcileAction.None, state.DetermineAction(true, "Farm", position, appearance));
    }

    [Fact]
    public void LightingFailSafeWarnsOnlyOnce()
    {
        LightingFailSafe failSafe = new();

        Assert.True(failSafe.Enabled);
        Assert.True(failSafe.DisableAndShouldWarn());
        Assert.False(failSafe.Enabled);
        Assert.False(failSafe.DisableAndShouldWarn());
    }

    private static LightAppearanceResolver CreateResolver()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        return new LightAppearanceResolver(new CatalogIndex(catalog));
    }

    private static ArmoryCatalog CreateCatalog(LightOverride lightOverride)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        EquipmentDefinition minersBlade = catalog.Equipment.Single(item => item.Id == "romulot.ValleyArmory_MinersBlade");
        EquipmentDefinition replacement = new()
        {
            Id = minersBlade.Id,
            Type = minersBlade.Type,
            Rarity = minersBlade.Rarity,
            DisplayNameKey = minersBlade.DisplayNameKey,
            DescriptionKey = minersBlade.DescriptionKey,
            Stats = minersBlade.Stats,
            Acquisition = minersBlade.Acquisition,
            Sprite = minersBlade.Sprite,
            OptionalVisualOverrides = new OptionalVisualOverrides { Light = lightOverride }
        };

        return new ArmoryCatalog
        {
            SchemaVersion = catalog.SchemaVersion,
            Rarities = catalog.Rarities,
            Equipment = catalog.Equipment.Select(item => item.Id == replacement.Id ? replacement : item).ToArray()
        };
    }
}
