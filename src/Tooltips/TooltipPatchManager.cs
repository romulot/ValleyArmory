using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System.Text;

namespace ValleyArmory.Tooltips;

internal sealed class TooltipPatchManager
{
    private readonly Harmony harmony;
    private readonly IMonitor monitor;

    public TooltipPatchManager(string harmonyId, IMonitor monitor)
    {
        this.harmony = new Harmony(harmonyId);
        this.monitor = monitor;
    }

    public bool Apply(TooltipPresentationResolver resolver, ITranslationHelper translations)
    {
        TooltipPatchContext.Initialize(resolver, translations, this.monitor);
        try
        {
            MethodInfo hoverText = AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.drawHoverText),
                new[] { typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe), typeof(IList<Item>), typeof(Texture2D), typeof(Rectangle?), typeof(Color?), typeof(Color?), typeof(float), typeof(int), typeof(int) })
                ?? throw new MissingMethodException("IClickableMenu.drawHoverText(StringBuilder, ...) was not found.");
            MethodInfo measure = AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons),
                new[] { typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(StringBuilder), typeof(string), typeof(int) })
                ?? throw new MissingMethodException("MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons was not found.");
            MethodInfo draw = AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.drawTooltip),
                new[] { typeof(SpriteBatch), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(SpriteFont), typeof(float), typeof(StringBuilder) })
                ?? throw new MissingMethodException("MeleeWeapon.drawTooltip was not found.");

            LogTargetMethod("title-color", hoverText);
            LogTargetMethod("extra-space", measure);
            LogTargetMethod("draw-tooltip", draw);

            ApplyPatch("title-color", () => this.harmony.Patch(
                hoverText,
                transpiler: new HarmonyMethod(typeof(TitleColorTranspiler), nameof(TitleColorTranspiler.Transpiler))));

            ApplyPatch("extra-space", () => this.harmony.Patch(
                measure,
                postfix: new HarmonyMethod(typeof(MeleeWeaponTooltipPatches), nameof(MeleeWeaponTooltipPatches.MeasurePostfix))));

            ApplyPatch("draw-tooltip", () => this.harmony.Patch(
                draw,
                prefix: new HarmonyMethod(typeof(MeleeWeaponTooltipPatches), nameof(MeleeWeaponTooltipPatches.DrawPrefix))));

            TooltipPatchContext.Trace("Tooltip decoration manager enabled");
            LogPatchOwners("IClickableMenu.drawHoverText(StringBuilder,...)", hoverText);
            LogPatchOwners("MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons", measure);
            LogPatchOwners("MeleeWeapon.drawTooltip", draw);
            this.monitor.Log("Miner's Blade tooltip rarity decoration enabled.", LogLevel.Debug);
            return true;
        }
        catch (Exception exception)
        {
            LogExceptionDetails("Patch application failed", exception);
            this.harmony.UnpatchAll(this.harmony.Id);
            TooltipPatchContext.Disable("One or more required Harmony patches could not be applied.", exception);
            return false;
        }
    }

    private void ApplyPatch(string patchName, Action patchAction)
    {
        TooltipPatchContext.Trace($"Applying patch: {patchName}");
        try
        {
            patchAction();
            TooltipPatchContext.Trace($"Patch success: {patchName}");
        }
        catch (Exception exception)
        {
            TooltipPatchContext.Trace($"Patch failed: {patchName}: {exception.GetType().FullName}: {exception.Message}");
            LogExceptionDetails($"Patch failed: {patchName}", exception);
            throw;
        }
    }

    private static void LogTargetMethod(string patchName, MethodInfo method)
    {
        string declaringType = method.DeclaringType?.FullName ?? "<unknown>";
        string returnType = method.ReturnType.FullName ?? method.ReturnType.Name;
        string parameters = string.Join(", ", method.GetParameters().Select(parameter =>
        {
            string parameterType = parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
            return $"{parameterType} {parameter.Name}";
        }));

        TooltipPatchContext.Trace($"Patch target [{patchName}] type: {declaringType}");
        TooltipPatchContext.Trace($"Patch target [{patchName}] method: {method.Name}");
        TooltipPatchContext.Trace($"Patch target [{patchName}] signature: {method}");
        TooltipPatchContext.Trace($"Patch target [{patchName}] parameters: ({parameters})");
        TooltipPatchContext.Trace($"Patch target [{patchName}] return type: {returnType}");
    }

    private void LogExceptionDetails(string context, Exception exception)
    {
        this.monitor.Log($"{context}: Type={exception.GetType().FullName}", LogLevel.Trace);
        this.monitor.Log($"{context}: Message={exception.Message}", LogLevel.Trace);
        this.monitor.Log($"{context}: StackTrace={exception.StackTrace}", LogLevel.Trace);

        Exception? inner = exception.InnerException;
        this.monitor.Log($"{context}: InnerException={(inner is null ? "<null>" : inner.GetType().FullName)}", LogLevel.Trace);
        this.monitor.Log($"{context}: InnerException.Message={(inner is null ? "<null>" : inner.Message)}", LogLevel.Trace);
        this.monitor.Log($"{context}: InnerException.StackTrace={(inner is null ? "<null>" : inner.StackTrace)}", LogLevel.Trace);

        int depth = 0;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            this.monitor.Log($"{context}: ExceptionChain[{depth}].Type={current.GetType().FullName}", LogLevel.Trace);
            this.monitor.Log($"{context}: ExceptionChain[{depth}].Message={current.Message}", LogLevel.Trace);
            this.monitor.Log($"{context}: ExceptionChain[{depth}].StackTrace={current.StackTrace}", LogLevel.Trace);
            depth++;
        }
    }

    private static void LogPatchOwners(string methodName, MethodBase method)
    {
        Patches? patchInfo = Harmony.GetPatchInfo(method);
        if (patchInfo is null)
        {
            TooltipPatchContext.Trace($"patch owners for {methodName}: none");
            return;
        }

        string owners = string.Join(",", patchInfo.Owners.Distinct(StringComparer.Ordinal));
        TooltipPatchContext.Trace($"patch owners for {methodName}: {owners}");
    }
}
