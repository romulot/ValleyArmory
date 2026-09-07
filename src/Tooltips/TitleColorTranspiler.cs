using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ValleyArmory.Tooltips;

internal static class TitleColorTranspiler
{
    private static readonly MethodInfo DrawStringMethod = AccessTools.Method(
        typeof(SpriteBatch),
        nameof(SpriteBatch.DrawString),
        new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color) }
    );

    private static readonly MethodInfo NullableColorValueGetter = AccessTools.PropertyGetter(typeof(Color?), nameof(Nullable<Color>.Value));
    private static readonly MethodInfo ResolveTitleColorMethod = AccessTools.Method(typeof(TooltipPatchContext), nameof(TooltipPatchContext.ResolveTitleColor));

    /// <summary>
    /// Expected 1.6.15 IL: the main title DrawString loads boldTitleText, then
    /// textColor.Value immediately before the four-argument string DrawString.
    /// Only that unique color load is decorated.
    /// </summary>
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        try
        {
            List<CodeInstruction> result = instructions.ToList();
            TooltipPatchContext.Trace($"title transpiler target located: {original.DeclaringType?.FullName}.{original.Name}");
            TooltipPatchContext.Trace($"title transpiler IL instructions received: {result.Count}");

            ParameterInfo[] parameters = original.GetParameters();
            int hoveredItemArgument = GetArgumentIndex(parameters, "hoveredItem", original.IsStatic);
            int boldTitleArgument = GetArgumentIndex(parameters, "boldTitleText", original.IsStatic);
            int textColorArgument = GetArgumentIndex(parameters, "textColor", original.IsStatic);
            bool instanceMethod = !original.IsStatic;

            List<int> matches = new();

            for (int i = 2; i < result.Count; i++)
            {
                if (!result[i].Calls(DrawStringMethod) || !IsTitleColorLoadSequence(result, i, textColorArgument, instanceMethod))
                {
                    continue;
                }

                int previousDrawString = FindPreviousDrawStringIndex(result, i);
                bool consumesBoldTitleText = ContainsArgumentLoad(result, previousDrawString + 1, i - 1, boldTitleArgument, instanceMethod, includeAddressLoads: false);
                if (consumesBoldTitleText)
                    matches.Add(i);
            }

            TooltipPatchContext.Trace($"title transpiler candidate count: {matches.Count}");
            if (matches.Count == 0)
                throw new InvalidOperationException($"Expected one main title color pattern in {original}, but found 0.");

            if (matches.Count != 1)
                throw new InvalidOperationException($"Expected one main title color pattern in {original}, but found {matches.Count}.");

            int insertionIndex = matches[0];
            result.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldarg, hoveredItemArgument));
            result.Insert(insertionIndex + 1, new CodeInstruction(OpCodes.Call, ResolveTitleColorMethod));
            TooltipPatchContext.Trace("title transpiler matched: candidates=1, bestDistance=n/a, bestCount=1");
            return result;
        }
        catch (Exception exception)
        {
            TooltipPatchContext.Trace($"title transpiler failed: {exception.GetType().FullName}: {exception.Message}");
            TooltipPatchContext.Trace($"title transpiler stack: {exception.StackTrace}");

            int depth = 0;
            for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
            {
                TooltipPatchContext.Trace($"title transpiler inner[{depth}] type: {current.GetType().FullName}");
                TooltipPatchContext.Trace($"title transpiler inner[{depth}] message: {current.Message}");
                TooltipPatchContext.Trace($"title transpiler inner[{depth}] stack: {current.StackTrace}");
                depth++;
            }

            throw;
        }
    }

    private static int GetArgumentIndex(ParameterInfo[] parameters, string name, bool isStatic)
    {
        ParameterInfo parameter = parameters.SingleOrDefault(value => value.Name == name)
            ?? throw new InvalidOperationException($"Required parameter '{name}' was not found.");
        return parameter.Position + (isStatic ? 0 : 1);
    }

    private static bool LoadsArgument(CodeInstruction instruction, int argumentIndex, bool instanceMethod, bool includeAddressLoads)
    {
        bool argByOperand = instruction.opcode == OpCodes.Ldarg || instruction.opcode == OpCodes.Ldarg_S;
        bool argByShortOpcode = argumentIndex == 0 && instruction.opcode == OpCodes.Ldarg_0
            || argumentIndex == 1 && instruction.opcode == OpCodes.Ldarg_1
            || argumentIndex == 2 && instruction.opcode == OpCodes.Ldarg_2
            || argumentIndex == 3 && instruction.opcode == OpCodes.Ldarg_3;
        bool argByAddress = includeAddressLoads && (instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S);

        if (!argByOperand && !argByShortOpcode && !argByAddress)
            return false;

        int? operandIndex = instruction.operand switch
        {
            ParameterInfo parameter => parameter.Position + (instanceMethod ? 1 : 0),
            byte value => value,
            short value => value,
            int value => value,
            _ => null
        };

        return operandIndex == argumentIndex || argByShortOpcode;
    }

    private static bool IsTitleColorLoadSequence(List<CodeInstruction> instructions, int drawStringIndex, int textColorArgument, bool instanceMethod)
    {
        if (drawStringIndex < 2)
            return false;

        return instructions[drawStringIndex - 1].Calls(NullableColorValueGetter)
            && LoadsArgument(instructions[drawStringIndex - 2], textColorArgument, instanceMethod, includeAddressLoads: true);
    }

    private static int FindPreviousDrawStringIndex(List<CodeInstruction> instructions, int currentIndex)
    {
        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (instructions[i].Calls(DrawStringMethod))
                return i;
        }

        return -1;
    }

    private static bool ContainsArgumentLoad(List<CodeInstruction> instructions, int startIndex, int endIndex, int argumentIndex, bool instanceMethod, bool includeAddressLoads)
    {
        int start = Math.Max(0, startIndex);
        int end = Math.Min(instructions.Count - 1, endIndex);
        for (int i = start; i <= end; i++)
        {
            if (LoadsArgument(instructions[i], argumentIndex, instanceMethod, includeAddressLoads))
                return true;
        }

        return false;
    }

}
