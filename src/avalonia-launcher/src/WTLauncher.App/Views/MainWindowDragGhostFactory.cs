using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowDragGhostFactory
{
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
            Background = Brush.Parse(LauncherDesignTokens.SurfaceColor),
            BorderBrush = Brush.Parse(LauncherDesignTokens.DragHighlightColor),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(LauncherDesignTokens.CardBorderRadius),
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
                        Foreground = Brush.Parse(LauncherDesignTokens.TextPrimaryColor),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                    new TextBlock
                    {
                        Text = directory,
                        FontFamily = FontFamily.Parse("Cascadia Code, Consolas"),
                        FontSize = 12,
                        Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                },
            },
        };

        return (control, layout.Left, layout.Top);
    }
}
