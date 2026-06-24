using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

internal static class LauncherDialogButtons
{
    private static readonly Thickness StandardPadding = new(16, 8);
    private static readonly IBrush PrimaryBrush = Brush.Parse(LauncherDesignTokens.PrimaryColor);
    private static readonly IBrush DestructiveBrush = Brush.Parse(LauncherDesignTokens.DestructiveColor);

    public static Button Cancel(double? minWidth = null, bool isDefault = false)
    {
        return Create(
            "取消",
            isDefault: isDefault,
            isCancel: true,
            minWidth: minWidth);
    }

    public static Button SafeDefaultCancel(double? minWidth = null) =>
        Cancel(minWidth, isDefault: true);

    public static Button Primary(string content, double? minWidth = null)
    {
        return Create(
            content,
            isDefault: true,
            minWidth: minWidth,
            background: PrimaryBrush,
            foreground: Brush.Parse(LauncherDesignTokens.SurfaceColor),
            fontWeight: FontWeight.SemiBold);
    }

    public static Button Destructive(string content, double? minWidth = null)
    {
        return Create(
            content,
            minWidth: minWidth,
            background: DestructiveBrush,
            foreground: Brush.Parse(LauncherDesignTokens.SurfaceColor),
            fontWeight: FontWeight.SemiBold);
    }

    public static StackPanel Footer(double topMargin, params Control[] buttons)
    {
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = LauncherDesignTokens.DialogFooterSpacing,
            Margin = new Thickness(0, topMargin, 0, 0),
        };

        foreach (var button in buttons)
        {
            footer.Children.Add(button);
        }

        return footer;
    }

    private static Button Create(
        string content,
        bool isDefault = false,
        bool isCancel = false,
        double? minWidth = null,
        IBrush? background = null,
        IBrush? foreground = null,
        FontWeight? fontWeight = null)
    {
        var button = new Button
        {
            Content = content,
            IsDefault = isDefault,
            IsCancel = isCancel,
        };

        if (minWidth is null)
        {
            button.Padding = StandardPadding;
        }
        else
        {
            button.MinWidth = minWidth.Value;
        }

        if (background is not null)
            button.Background = background;
        if (foreground is not null)
            button.Foreground = foreground;
        if (fontWeight is not null)
            button.FontWeight = fontWeight.Value;

        return button;
    }
}
