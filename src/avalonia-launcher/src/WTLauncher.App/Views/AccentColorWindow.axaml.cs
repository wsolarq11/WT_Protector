using System.Collections.Immutable;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class AccentColorWindow : Window
{
    private string _currentHex = AccentColorRamp.DefaultAccentColor;
    private ImmutableList<string> _presets = LauncherSettingsDefaults.AccentPresets;

    public AccentColorWindow() : this(AccentColorRamp.DefaultAccentColor, LauncherSettingsDefaults.AccentPresets)
    {
    }

    public AccentColorWindow(string currentHex, ImmutableList<string> presets)
    {
        InitializeComponent();
        ApplyLocalizedChrome();

        if (presets is { Count: > 0 })
            _presets = presets;
        if (AccentColorRamp.TryNormalize(currentHex, out var normalized))
            _currentHex = normalized;

        HexInput.Text = _currentHex;
        RebuildSwatches();
        UpdatePreview(_currentHex);
    }

    private void ApplyLocalizedChrome()
    {
        Title = LauncherLocalization.Format("accentColor.title");
        TitleText.Text = LauncherLocalization.Format("accentColor.title");
        PaletteLabel.Text = LauncherLocalization.Format("accentColor.palette");
        ResetPaletteButton.Content = LauncherLocalization.Format("accentColor.resetPalette");
        CustomLabel.Text = LauncherLocalization.Format("accentColor.custom");
        HexInput.Watermark = LauncherLocalization.Format("accentColor.hexPlaceholder");
        AddToPaletteButton.Content = LauncherLocalization.Format("accentColor.addToPalette");
        CancelButton.Content = LauncherLocalization.Format("accentColor.cancel");
        ConfirmButton.Content = LauncherLocalization.Format("accentColor.confirm");
    }

    private void RebuildSwatches()
    {
        SwatchList.ItemsSource = _presets
            .Select(hex => new AccentSwatch(
                hex,
                string.Equals(hex, _currentHex, StringComparison.OrdinalIgnoreCase),
                LauncherLocalization.Format("accentColor.remove"),
                RemovePreset))
            .ToList();
    }

    private void RemovePreset(string hex)
    {
        _presets = _presets.Remove(hex);
        RebuildSwatches();
    }

    private void OnSwatchPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { Tag: string hex })
        {
            _currentHex = hex;
            HexInput.Text = hex;
            UpdatePreview(hex);
            RebuildSwatches();
        }
    }

    private void OnResetPaletteClicked(object? sender, RoutedEventArgs e)
    {
        _presets = LauncherSettingsDefaults.AccentPresets;
        RebuildSwatches();
    }

    private void OnAddToPaletteClicked(object? sender, RoutedEventArgs e)
    {
        if (!AccentColorRamp.TryNormalize(HexInput.Text, out var normalized))
            return;

        if (!_presets.Contains(normalized))
            _presets = _presets.Add(normalized);

        _currentHex = normalized;
        UpdatePreview(normalized);
        RebuildSwatches();
    }

    private void OnHexChanged(object? sender, TextChangedEventArgs e)
    {
        if (AccentColorRamp.TryNormalize(HexInput.Text, out var normalized))
        {
            _currentHex = normalized;
            UpdatePreview(normalized);
            RebuildSwatches();
        }
    }

    private void UpdatePreview(string hex) =>
        PreviewSwatch.Background = new SolidColorBrush(Color.Parse(hex));

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (!AccentColorRamp.TryNormalize(HexInput.Text, out var normalized))
            return;
        Close(new AccentColorResult(normalized, _presets));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close((AccentColorResult?)null);
}
