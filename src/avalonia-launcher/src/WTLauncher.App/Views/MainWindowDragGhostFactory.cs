using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowDragGhostFactory
{
    private static readonly IBrush SurfaceBrush = LauncherDialogText.SafeParseBrush(LauncherDesignTokens.SurfaceColor, Brushes.White);
    private static readonly IBrush DragHighlightBrush = LauncherDialogText.SafeParseBrush(LauncherDesignTokens.DragHighlightColor, Brushes.CornflowerBlue);
    private static readonly IBrush TextPrimaryBrush = LauncherDialogText.SafeParseBrush(LauncherDesignTokens.TextPrimaryColor, Brushes.Black);
    private static readonly IBrush TextMutedBrush = LauncherDialogText.SafeParseBrush(LauncherDesignTokens.TextMutedColor, Brushes.Gray);
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
        var sw = double.IsFinite(sourceWidth) ? sourceWidth : 0;
        var sh = double.IsFinite(sourceHeight) ? sourceHeight : 0;
        var layout = LauncherDragGhostLayoutCalculator.Calculate(sw, sh, pointerX, pointerY);
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
