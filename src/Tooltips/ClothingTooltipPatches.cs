using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Text;

namespace ValleyArmory.Tooltips;

/// <summary>
/// Unlike <see cref="StardewValley.Tools.MeleeWeapon"/> and <see cref="StardewValley.Objects.Boots"/>,
/// <see cref="StardewValley.Objects.Clothing"/> does not override <c>drawTooltip</c> or
/// <c>getExtraSpaceNeededForTooltipSpecialIcons</c> (confirmed by reflecting over the installed
/// 1.6.15.24356 assembly), so it falls through to the base <see cref="Item"/> implementation.
/// These patches target that base method instead of a Clothing-specific override. This only
/// affects items our own resolver recognizes (everything else exits on the first check), so it
/// stays scoped to Valley Armory equipment in practice.
/// </summary>
internal static class ClothingTooltipPatches
{
    public static void MeasurePostfix(Item __instance, SpriteFont font, ref Point __result)
    {
        if (TooltipPatchContext.TryResolve(__instance, out _))
            __result.Y += TooltipPatchContext.GetLineHeight(font);
    }

    public static void DrawPrefix(Item __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
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
            TooltipPatchContext.Disable("The armor rarity line could not be drawn.", exception);
        }
    }
}
