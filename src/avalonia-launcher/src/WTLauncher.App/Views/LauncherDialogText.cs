using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>
/// Shared UI utilities for theme-consistent dialog text.
/// ponytail: retained for SafeParseBrush used by MainWindow.Drag.cs;
/// dialog-specific factory methods migrated to XAML.
/// </summary>
internal static class LauncherDialogText
{
    /// <summary>
    /// Parses a color string into an <see cref="IBrush"/>. If the design token is malformed,
    /// falls back to a safe default instead of crashing the static initializer.
    /// </summary>
    internal static IBrush SafeParseBrush(string color, IBrush fallback)
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

    private static readonly IBrush TitleBrush = SafeParseBrush(LauncherDesignTokens.TextPrimaryColor, Brushes.Black);
    private static readonly IBrush BodyBrush = SafeParseBrush(LauncherDesignTokens.TextMutedColor, Brushes.Gray);
    private static readonly IBrush LabelBrush = SafeParseBrush(LauncherDesignTokens.DragHandleTextColor, Brushes.DimGray);
    private static readonly IBrush SubtleBrush = SafeParseBrush(LauncherDesignTokens.TextMutedColor, Brushes.Gray);

    public static TextBlock Title(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = LauncherDesignTokens.DialogTitleFontSize,
            FontWeight = FontWeight.SemiBold,
            Foreground = TitleBrush,
        };
        AutomationProperties.SetName(textBlock, text);
        return textBlock;
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
            Foreground = SafeParseBrush(LauncherDesignTokens.ErrorColor, Brushes.Red),
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
        };
    }

    public static TextBox Input(string text)
    {
        return new TextBox
        {
            Text = text,
            FontSize = LauncherDesignTokens.FontSizeBody,
            Padding = new Thickness(10, 8),
        };
    }
}
