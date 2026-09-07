using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>One equipment definition's monster-drop configuration, resolved from the catalog.</summary>
internal sealed record DropRule(EquipmentDefinition Equipment, DropAcquisition Drop);

/// <summary>Looks up which equipment can drop from a given monster, generically — no per-equipment branching.</summary>
internal sealed class DropRuleResolver
{
    private readonly ILookup<string, DropRule> rulesByMonster;

    public DropRuleResolver(CatalogIndex catalog)
    {
        this.rulesByMonster = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Drop is { SourceType: DropSourceType.Monster })
            .Select(item => new DropRule(item, item.Acquisition!.Drop!))
            .ToLookup(rule => rule.Drop.SourceId, StringComparer.Ordinal);
    }

    public IReadOnlyList<DropRule> GetRulesFor(string monsterName)
    {
        return this.rulesByMonster[monsterName].ToArray();
    }

    /// <summary>Pure chance check, isolated from the game's RNG source so it can be unit tested deterministically.</summary>
    public static bool RolledSuccess(double chance, double rngValue)
    {
        return rngValue < chance;
    }
}
