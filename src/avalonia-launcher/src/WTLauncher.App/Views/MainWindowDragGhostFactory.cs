using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowDragGhostFactory
{
    public static MainWindowDragGhostVisual Create(
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
            Classes = { "card" },
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#38BDF8"),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(13),
            Opacity = 0.86,
            IsHitTestVisible = false,
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
                Margin = new Thickness(13),
                Spacing = 8,
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock
                    {
                        Text = name,
                        FontSize = titleFontSize,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#0F172A"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                    new TextBlock
                    {
                        Text = directory,
                        FontFamily = FontFamily.Parse("Cascadia Code, Consolas"),
                        FontSize = 12,
                        Foreground = Brush.Parse("#64748B"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                },
            },
        };

        return new MainWindowDragGhostVisual(control, layout.Left, layout.Top);
    }
}

public sealed record MainWindowDragGhostVisual(Border Control, double Left, double Top);
