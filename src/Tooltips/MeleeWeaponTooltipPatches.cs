using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using System.Text;

namespace ValleyArmory.Tooltips;

internal static class MeleeWeaponTooltipPatches
{
    public static void MeasurePostfix(MeleeWeapon __instance, SpriteFont font, ref Point __result)
    {
        if (TooltipPatchContext.IsMinersBlade(__instance))
        {
            TooltipPatchContext.Trace("extra-space postfix called");
            TooltipPatchContext.Trace($"hovered item type: {__instance.GetType().FullName}");
            TooltipPatchContext.Trace($"hovered item QualifiedItemId: {__instance.QualifiedItemId}");
        }

        if (TooltipPatchContext.TryResolve(__instance, out _))
            __result.Y += TooltipPatchContext.GetLineHeight(font);
    }

    public static void DrawPrefix(MeleeWeapon __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
    {
        if (TooltipPatchContext.IsMinersBlade(__instance))
        {
            TooltipPatchContext.Trace("drawTooltip prefix called");
            TooltipPatchContext.Trace($"hovered item type: {__instance.GetType().FullName}");
            TooltipPatchContext.Trace($"hovered item QualifiedItemId: {__instance.QualifiedItemId}");
        }

        if (!TooltipPatchContext.TryResolve(__instance, out TooltipPresentation? presentation) || presentation is null)
            return;

        try
        {
            Utility.drawTextWithShadow(
                spriteBatch,
                presentation.RarityText,
                font,
                new Vector2(x + 16, y + 12),
                Game1.textColor * 0.9f * alpha
            );
            y += TooltipPatchContext.GetLineHeight(font);

            if (TooltipPatchContext.IsMinersBlade(__instance))
                TooltipPatchContext.Trace($"rarity line drawn: {presentation.RarityText}");
        }
        catch (Exception exception)
        {
            TooltipPatchContext.Disable("The rarity line could not be drawn.", exception);
        }
    }
}
