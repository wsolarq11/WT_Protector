using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WTLauncher.App.Views;

internal static class LauncherDialogButtons
{
    private static readonly Thickness StandardPadding = new(16, 8);

    public static Button Cancel(double? minWidth = null, bool isDefault = false)
    {
        return Create(
            "取消",
            isDefault: isDefault,
            isCancel: true,
            minWidth: minWidth);
    }

    public static Button SafeDefaultCancel(double? minWidth = null)
    {
        return Create(
            "取消",
            isDefault: true,
            isCancel: true,
            minWidth: minWidth);
    }

    public static Button Primary(string content, double? minWidth = null)
    {
        return Create(
            content,
            isDefault: true,
            minWidth: minWidth,
            background: Brush.Parse("#2563EB"),
            foreground: Brushes.White,
            fontWeight: FontWeight.SemiBold);
    }

    public static Button Destructive(string content, double? minWidth = null)
    {
        return Create(
            content,
            minWidth: minWidth,
            background: Brush.Parse("#DC2626"),
            foreground: Brushes.White,
            fontWeight: FontWeight.SemiBold);
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
