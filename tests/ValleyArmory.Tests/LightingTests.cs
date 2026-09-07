using Microsoft.Xna.Framework;
using ValleyArmory.Catalog;
using ValleyArmory.Lighting;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class LightingTests
{
    [Fact]
    public void RareMinersBladeResolvesLightFromCatalog()
    {
        LightAppearanceResolver resolver = CreateResolver();

        bool found = resolver.TryResolve("(W)romulot.ValleyArmory_MinersBlade", out WeaponLightAppearance appearance);

        Assert.True(found);
        Assert.Equal(new Color(0x4A, 0x90, 0xE2), appearance.Color);
        Assert.Equal(1.25f, appearance.Radius);
        Assert.Equal(0.35f, appearance.Intensity);
        Assert.Equal(new Vector2(0f, -32f), appearance.Offset);
    }

    [Theory]
    [InlineData("(W)0")]
    [InlineData("(W)romulot.ValleyArmory_BlackIronSword")]
    [InlineData(null)]
    public void UnknownOrOutOfScopeItemHasNoLight(string? qualifiedItemId)
    {
        bool found = CreateResolver().TryResolve(qualifiedItemId, out _);

        Assert.False(found);
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
}
