using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WTLauncher.App.Views;

internal static class LauncherDialogText
{
    /// <summary>
    /// Parses a color string into an <see cref="IBrush"/>. If the design token is malformed,
    /// falls back to a safe default instead of crashing the static initializer.
    /// </summary>
    private static IBrush SafeParseBrush(string color, IBrush fallback)
    {
        try
        {
            return Brush.Parse(color);
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static readonly IBrush TitleBrush = SafeParseBrush("#0F172A", Brushes.Black);
    private static readonly IBrush BodyBrush = SafeParseBrush("#475569", Brushes.Gray);
    private static readonly IBrush LabelBrush = SafeParseBrush("#334155", Brushes.DimGray);
    private static readonly IBrush SubtleBrush = SafeParseBrush("#64748B", Brushes.Gray);

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

    public static TextBox Input(string text)
    {
        return new TextBox
        {
            Text = text,
            FontSize = 14,
            Padding = new Thickness(10, 8),
        };
    }
}
