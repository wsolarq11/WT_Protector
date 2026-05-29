using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public static class MainWindowReorderFlipAnimation
{
    public static IEnumerable<MainWindowReorderFlipAnimationTarget> FindTargets(
        Control root,
        IReadOnlyDictionary<string, Rect> before,
        IReadOnlyDictionary<string, Rect> after)
    {
        ArgumentNullException.ThrowIfNull(root);
        return FindTargets(root.GetVisualDescendants().OfType<Border>(), before, after);
    }

    public static IEnumerable<MainWindowReorderFlipAnimationTarget> FindTargets(
        IEnumerable<Border> cards,
        IReadOnlyDictionary<string, Rect> before,
        IReadOnlyDictionary<string, Rect> after)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        foreach (var card in cards)
        {
            var entry = MainWindowEntryCardResolver.ResolveCard(card);
            if (entry is null)
                continue;
            if (!MainWindowReorderFlip.TryCalculateOffset(entry.Id, before, after, out var offset))
                continue;

            yield return new MainWindowReorderFlipAnimationTarget(card, entry.Id, offset);
        }
    }

    public static void ApplyStart(Border card, MainWindowReorderFlipOffset offset)
    {
        ArgumentNullException.ThrowIfNull(card);

        card.Transitions = new Transitions();
        card.RenderTransform = TransformOperations.Parse(MainWindowReorderFlip.FormatTranslate(offset));
    }

    public static bool TryApplyFinish(Border card, bool isEffectivelyVisible)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (!isEffectivelyVisible)
            return false;

        card.ClearValue(Animatable.TransitionsProperty);
        card.RenderTransform = TransformOperations.Parse("translate(0px,0px)");
        return true;
    }
}

public readonly record struct MainWindowReorderFlipAnimationTarget(
    Border Card,
    string EntryId,
    MainWindowReorderFlipOffset Offset);
