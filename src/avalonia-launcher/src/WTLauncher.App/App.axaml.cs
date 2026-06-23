using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.App;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InjectDesignTokens();
    }

    /// <summary>
    /// Replaces XAML-hardcoded resources with values derived from
    /// <see cref="LauncherDesignTokens"/> — the single source of truth for colors,
    /// spacing, sizing, and card shadow.
    /// </summary>
    private void InjectDesignTokens()
    {
        // ── Base colors ────────────────────────────────────
        Resources["PrimaryBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.PrimaryColor));
        Resources["DestructiveBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.DestructiveColor));
        Resources["BackgroundBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BackgroundColor));
        Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.TextPrimaryColor));
        Resources["TextMutedBrush"]   = new SolidColorBrush(Color.Parse(LauncherDesignTokens.TextMutedColor));
        Resources["CardBorderBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BorderColor));
        Resources["SurfaceBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SurfaceColor));

        // ── Semantic state colors ──────────────────────────
        Resources["HoverBackgroundBrush"]            = new SolidColorBrush(Color.Parse(LauncherDesignTokens.HoverBackgroundColor));
        Resources["SelectionBackgroundBrush"]        = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SelectionBackgroundColor));
        Resources["SelectionBorderBrush"]            = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SelectionBorderColor));
        Resources["SelectionHoverBackgroundBrush"]   = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SelectionHoverBackgroundColor));

        // ── Drag indicator ─────────────────────────────────
        Resources["DragHighlightBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.DragHighlightColor));

        // ── Secondary button ───────────────────────────────
        Resources["SecondaryButtonBackgroundBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SecondaryButtonBackgroundColor));
        Resources["SecondaryButtonForegroundBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SecondaryButtonForegroundColor));

        // ── Control box ────────────────────────────────────
        Resources["ControlBoxLabelBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.ControlBoxLabelColor));

        // ── Status badges ──────────────────────────────────
        Resources["BadgeSuccessBackgroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeSuccessBackgroundColor));
        Resources["BadgeSuccessForegroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeSuccessForegroundColor));
        Resources["BadgeInfoBackgroundBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeInfoBackgroundColor));
        Resources["BadgeInfoForegroundBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeInfoForegroundColor));

        // ── Drag handle ────────────────────────────────────
        Resources["DragHandleTextBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.DragHandleTextColor));

        // ── Dimensions / Spacing ────────────────────────────
        Resources["CardBorderRadius"] = new CornerRadius(LauncherDesignTokens.CardBorderRadius);
        Resources["CardPadding"] = new Thickness(LauncherDesignTokens.CardPadding);
        Resources["DetailsNameColumnWidth"] = new GridLength(LauncherDesignTokens.DetailsNameColumnWeight, GridUnitType.Star);
        Resources["DetailsDirectoryColumnWidth"] = new GridLength(LauncherDesignTokens.DetailsDirectoryColumnWeight, GridUnitType.Star);
        Resources["DetailsCommandColumnWidth"] = new GridLength(LauncherDesignTokens.DetailsCommandColumnWeight, GridUnitType.Star);
        Resources["WindowDefaultWidth"] = LauncherDesignTokens.WindowDefaultWidth;
        Resources["WindowDefaultHeight"] = LauncherDesignTokens.WindowDefaultHeight;
        Resources["WindowMinWidth"] = LauncherDesignTokens.WindowMinWidth;
        Resources["WindowMinHeight"] = LauncherDesignTokens.WindowMinHeight;

        // ── BoxShadow ───────────────────────────────────────
        Resources["CardBoxShadow"] = BoxShadows.Parse($"0 8 24 0 {LauncherDesignTokens.BoxShadowColor}");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var configPath = LauncherConfigStore.ResolveConfigPath(
                    LauncherStartupArguments.ReadConfigPath(desktop.Args),
                    Environment.CurrentDirectory,
                    AppContext.BaseDirectory);
                var settingsPath = LauncherConfigStore.ResolveSettingsPath(configPath);
                var settings = LauncherSettingsStore.Load(settingsPath);
                var launchWorkflow = new LauncherLaunchWorkflow(new LauncherProcess());
                var viewModel = new MainWindowViewModel(
                    configPath,
                    settingsPath,
                    settings,
                    launchWorkflow,
                    static () => DateTimeOffset.UtcNow);
                var mainWindow = new MainWindow(viewModel)
                {
                    Width = settings.Window.Width,
                    Height = settings.Window.Height,
                };
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WTLauncher] Fatal startup error: {ex}");
            Environment.ExitCode = 1;
        }
    }

}
