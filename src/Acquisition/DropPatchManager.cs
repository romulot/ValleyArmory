using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Monsters;

namespace ValleyArmory.Acquisition;

internal sealed class DropPatchManager
{
    private readonly Harmony harmony;
    private readonly IMonitor monitor;

    public DropPatchManager(string harmonyId, IMonitor monitor)
    {
        this.harmony = new Harmony(harmonyId);
        this.monitor = monitor;
    }

    public bool Apply(DropRuleResolver resolver)
    {
        DropPatchContext.Initialize(resolver, this.monitor);
        try
        {
            MethodInfo getExtraDropItems = AccessTools.Method(typeof(Monster), nameof(Monster.getExtraDropItems), Type.EmptyTypes)
                ?? throw new MissingMethodException("Monster.getExtraDropItems() was not found.");

            this.harmony.Patch(
                getExtraDropItems,
                postfix: new HarmonyMethod(typeof(MonsterDropPatches), nameof(MonsterDropPatches.Postfix))
            );

            DropPatchContext.Trace("Monster drop rules enabled.");
            this.monitor.Log("Monster drop rules enabled (Monster.getExtraDropItems postfix).", LogLevel.Debug);
            return true;
        }
        catch (Exception exception)
        {
            this.harmony.UnpatchAll(this.harmony.Id);
            DropPatchContext.Disable("The monster drop Harmony patch could not be applied.", exception);
            return false;
        }
    }
}
