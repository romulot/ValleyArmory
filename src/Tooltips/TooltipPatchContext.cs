using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace ValleyArmory.Tooltips;

internal static class TooltipPatchContext
{
    private const string MinersBladeQualifiedItemId = Assets.MinersBladeWeaponDataFactory.QualifiedItemId;
    private static TooltipPresentationResolver? resolver;
    private static ITranslationHelper? translations;
    private static IMonitor? monitor;
    private static bool warned;

    public static bool Enabled { get; private set; }

    public static void Initialize(TooltipPresentationResolver presentationResolver, ITranslationHelper translationHelper, IMonitor modMonitor)
    {
        resolver = presentationResolver;
        translations = translationHelper;
        monitor = modMonitor;
        warned = false;
        Enabled = true;
    }

    public static void Disable(string reason, Exception? exception = null)
    {
        Enabled = false;
        Trace($"atomic fallback triggered: {reason}{(exception is null ? string.Empty : $" ({exception.GetType().Name}: {exception.Message})")}");
        if (warned)
            return;

        warned = true;
        string details = exception is null ? reason : $"{reason} {exception.Message}";
        monitor?.Log($"Tooltip rarity decoration was disabled; vanilla tooltips remain active. {details}", LogLevel.Warn);
    }

    public static bool TryResolve(Item? item, out TooltipPresentation? presentation)
    {
        presentation = null;
        bool resolved = Enabled
            && item is not null
            && resolver is not null
            && translations is not null
            && resolver.TryResolve(item.QualifiedItemId, key => translations.Get(key).ToString(), out presentation);

        if (IsMinersBlade(item))
        {
            Trace($"rarity resolved: {resolved}");
        }

        return resolved;
    }

    public static Color ResolveTitleColor(Color vanillaColor, Item? hoveredItem)
    {
        if (IsMinersBlade(hoveredItem))
        {
            Trace("ResolveTitleColor called");
            Trace($"hovered item type: {hoveredItem!.GetType().FullName}");
            Trace($"hovered item QualifiedItemId: {hoveredItem.QualifiedItemId}");
        }

        return TryResolve(hoveredItem, out TooltipPresentation? presentation) && presentation is not null
            ? presentation.NameColor
            : vanillaColor;
    }

    public static bool IsMinersBlade(Item? item)
    {
        return item is not null && string.Equals(item.QualifiedItemId, MinersBladeQualifiedItemId, StringComparison.Ordinal);
    }

    public static void Trace(string message)
    {
        monitor?.Log(message, LogLevel.Trace);
    }

    public static int GetLineHeight(SpriteFont font)
    {
        return Math.Max(48, (int)Math.Ceiling(font.MeasureString("TT").Y));
    }
}
