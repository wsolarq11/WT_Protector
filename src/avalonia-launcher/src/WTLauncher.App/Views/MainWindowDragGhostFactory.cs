using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowDragGhostFactory
{
    private static readonly IBrush SurfaceBrush = SafeParseBrush(LauncherDesignTokens.SurfaceColor, Brushes.White);
    private static readonly IBrush DragHighlightBrush = SafeParseBrush(LauncherDesignTokens.DragHighlightColor, Brushes.CornflowerBlue);
    private static readonly IBrush TextPrimaryBrush = SafeParseBrush(LauncherDesignTokens.TextPrimaryColor, Brushes.Black);
    private static readonly IBrush TextMutedBrush = SafeParseBrush(LauncherDesignTokens.TextMutedColor, Brushes.Gray);

    /// <summary>
    /// Parses a color string into an <see cref="IBrush"/>. If the design token is malformed
    /// (e.g. a typo in LauncherDesignTokens), falls back to a safe default instead of
    /// crashing the static initializer with <see cref="TypeInitializationException"/>.
    /// </summary>
    private static IBrush SafeParseBrush(string color, IBrush fallback)
    {
        try
        {
            return Brush.Parse(color);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            return fallback;
        }
    }

    // NOTE: Every drag allocates a fresh Border + StackPanel + 2 TextBlocks visual tree.
    // There is no pooling — the ghost is short-lived (visible only during a drag gesture)
    // and drag frequency is inherently low, so the allocation cost is negligible.
    public static (Border Control, double Left, double Top) Create(
        string name,
        string directory,
        double titleFontSize,
        double sourceWidth,
        double sourceHeight,
        double pointerX,
        double pointerY)
    {
        var layout = LauncherDragGhostLayoutCalculator.Calculate(sourceWidth, sourceHeight, pointerX, pointerY);
        var control = new Border
        {
            Width = layout.Width,
            Height = layout.Height,
            Classes = { MainWindowEntryClasses.Card },
            Background = SurfaceBrush,
            BorderBrush = DragHighlightBrush,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(LauncherDesignTokens.CardBorderRadius),
            // Drag ghost opacity (0.86) is an intentional visual design token
            // that provides sufficient transparency to see underlying content
            // while maintaining readability of the dragged entry text.
            Opacity = 0.86,
            IsHitTestVisible = false,
            // Drag ghost shadow uses alpha 22 (0x22 ≈ 13 %) vs CardBoxShadow alpha 12 (0x12 ≈ 7 %).
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 10,
                Blur = 28,
                Spread = 0,
                Color = Color.Parse("#220F172A"),
            }),
            Child = new StackPanel
            {
                Margin = new Thickness(LauncherDesignTokens.CardPadding),
                Spacing = 8,
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock
                    {
                        Text = name,
                        FontSize = titleFontSize,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = TextPrimaryBrush,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                    new TextBlock
                    {
                        Text = directory,
                        FontFamily = FontFamily.Parse("Cascadia Code, Consolas"),
                        FontSize = 12,
                        Foreground = TextMutedBrush,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                },
            },
        };

        return (control, layout.Left, layout.Top);
    }
}
