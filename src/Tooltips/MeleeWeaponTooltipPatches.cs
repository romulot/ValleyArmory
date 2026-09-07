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
        if (TooltipPatchContext.TryResolve(__instance, out _))
            __result.Y += TooltipPatchContext.GetLineHeight(font);
    }

    public static void DrawPrefix(MeleeWeapon __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
    {
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

        }
        catch (Exception exception)
        {
            TooltipPatchContext.Disable("The rarity line could not be drawn.", exception);
        }
    }
}
