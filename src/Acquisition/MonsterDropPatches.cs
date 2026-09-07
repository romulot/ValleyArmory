using StardewValley;
using StardewValley.Monsters;

namespace ValleyArmory.Acquisition;

/// <summary>
/// Postfixes the single vanilla extension point for monster loot (<see cref="Monster.getExtraDropItems"/>),
/// called once per kill from GameLocation.monsterDrop on the attacking farmer's own client — the same
/// authority model vanilla already uses for Ghost/Bat/Bug/RockGolem/BigSlime bonus drops, so no additional
/// multiplayer guard is needed here.
/// </summary>
internal static class MonsterDropPatches
{
    public static void Postfix(Monster __instance, ref List<Item> __result)
    {
        if (!DropPatchContext.TryGetRules(__instance.Name, out IReadOnlyList<DropRule> rules))
        {
            return;
        }

        __result ??= new List<Item>();

        foreach (DropRule rule in rules)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(rule.Drop.Condition) &&
                    !GameStateQuery.CheckConditions(rule.Drop.Condition, __instance.currentLocation, Game1.player, null, null, Game1.random, null))
                {
                    continue;
                }

                double roll = Game1.random.NextDouble();
                if (!DropRuleResolver.RolledSuccess(rule.Drop.Chance, roll))
                {
                    continue;
                }

                Item item = ItemRegistry.Create(EquipmentIdentity.GetQualifiedItemId(rule.Equipment));
                __result.Add(item);
                DropPatchContext.Trace(
                    $"Dropped '{rule.Equipment.Id}' from monster '{__instance.Name}' (chance={rule.Drop.Chance:0.###}, roll={roll:0.###})."
                );
            }
            catch (Exception exception)
            {
                DropPatchContext.LogError($"Failed to create dropped item '{rule.Equipment.Id}' from monster '{__instance.Name}': {exception.Message}");
            }
        }
    }
}
