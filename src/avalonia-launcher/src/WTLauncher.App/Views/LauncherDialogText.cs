using Avalonia.Controls;
using Avalonia.Media;

namespace WTLauncher.App.Views;

internal static class LauncherDialogText
{
    private static readonly IBrush TitleBrush = Brush.Parse("#0F172A");
    private static readonly IBrush BodyBrush = Brush.Parse("#475569");
    private static readonly IBrush LabelBrush = Brush.Parse("#334155");
    private static readonly IBrush SubtleBrush = Brush.Parse("#64748B");

    public static TextBlock Title(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 22,
            FontWeight = FontWeight.SemiBold,
            Foreground = TitleBrush,
        };
    }

    public static TextBlock Body(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = BodyBrush,
            TextWrapping = TextWrapping.Wrap,
        };
    }

    public static TextBlock Label(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            Foreground = LabelBrush,
        };
    }

    public static TextBlock Subtle(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = SubtleBrush,
            TextWrapping = TextWrapping.Wrap,
        };
    }

    public static TextBlock Error()
    {
        return new TextBlock
        {
            Foreground = Brushes.IndianRed,
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
        };
    }
}
