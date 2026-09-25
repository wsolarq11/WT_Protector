using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

// Swatch item for the accent palette. Resolves the per-hex brushes and the
// context menu here so the ItemsControl template stays declarative in XAML.
public sealed class AccentSwatch
{
    public AccentSwatch(string hex, bool isSelected, string removeText, Action<string> onRemove)
    {
        Hex = hex;
        Background = new SolidColorBrush(Color.Parse(hex));
        BorderBrush = new SolidColorBrush(Color.Parse(
            isSelected ? LauncherDesignTokens.ActiveBorderColor : LauncherDesignTokens.BorderColor));
        BorderThickness = new Thickness(isSelected ? 2 : 1);

        var remove = new MenuItem { Header = removeText, Tag = hex };
        remove.Click += (_, _) => onRemove(hex);
        var menu = new ContextMenu();
        menu.Items.Add(remove);
        Menu = menu;
    }

    public string Hex { get; }

    public IBrush Background { get; }

    public IBrush BorderBrush { get; }

    public Thickness BorderThickness { get; }

    public ContextMenu Menu { get; }
}
