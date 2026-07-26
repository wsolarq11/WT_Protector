using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.App;

public sealed partial class App : Application
{
    private bool _isInitialized;

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
        Resources["ErrorBrush"]       = new SolidColorBrush(Color.Parse(LauncherDesignTokens.ErrorColor));
        Resources["BackgroundBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BackgroundColor));
        Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.TextPrimaryColor));
        Resources["TextMutedBrush"]   = new SolidColorBrush(Color.Parse(LauncherDesignTokens.TextMutedColor));
        Resources["CardBorderBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BorderColor));
        Resources["BorderBrush"]      = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BorderColor));
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

        // ── Per-agent brand badge ──────────────────────────
        Resources["BrandBadgeSize"] = LauncherDesignTokens.BrandBadgeSize;
        Resources["BrandBadgeCornerRadius"] = new CornerRadius(LauncherDesignTokens.BrandBadgeCornerRadius);
        Resources["BrandBadgeForegroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BrandBadgeForegroundColor));

        // ── Dimensions / Spacing ────────────────────────────
        Resources["CardBorderRadius"] = new CornerRadius(LauncherDesignTokens.CardBorderRadius);
        Resources["CardPadding"] = new Thickness(LauncherDesignTokens.CardPadding);
        Resources["FontSizeSmall"] = LauncherDesignTokens.FontSizeSmall;
        Resources["FontSizeBody"] = LauncherDesignTokens.FontSizeBody;
        Resources["FontSizeEmphasis"] = LauncherDesignTokens.FontSizeEmphasis;
        Resources["DialogTitleFontSize"] = LauncherDesignTokens.DialogTitleFontSize;
        Resources["DialogOuterPadding"] = new Thickness(LauncherDesignTokens.DialogOuterPadding);
        Resources["DialogFooterMargin"] = new Thickness(0, LauncherDesignTokens.DialogFooterTopMargin, 0, 0);
        Resources["DetailsNameColumnWidth"] = new GridLength(LauncherDesignTokens.DetailsNameColumnWeight, GridUnitType.Star);
        Resources["DetailsDirectoryColumnWidth"] = new GridLength(LauncherDesignTokens.DetailsDirectoryColumnWeight, GridUnitType.Star);
        Resources["DetailsCommandColumnWidth"] = new GridLength(LauncherDesignTokens.DetailsCommandColumnWeight, GridUnitType.Star);
        Resources["WindowDefaultWidth"] = LauncherDesignTokens.WindowDefaultWidth;
        Resources["WindowDefaultHeight"] = LauncherDesignTokens.WindowDefaultHeight;
        Resources["WindowMinWidth"] = LauncherDesignTokens.WindowMinWidth;
        Resources["WindowMinHeight"] = LauncherDesignTokens.WindowMinHeight;

        // ── BoxShadow ───────────────────────────────────────
        Resources["CardBoxShadow"] = BoxShadows.Parse($"0 8 24 0 {LauncherDesignTokens.BoxShadowColor}");
        Resources["NoBoxShadow"] = BoxShadows.Parse("0 0 0 0 Transparent");

        // ── Acrylic material colors ──────────────────────
        Resources["AcrylicTintColor"] = Color.Parse(LauncherDesignTokens.BackgroundColor);
        Resources["AcrylicFallbackColor"] = Color.Parse(LauncherDesignTokens.BackgroundColor);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_isInitialized)
            return;

        // Prevent a single binding error or UI-thread exception from crashing the process.
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            ErrorLog.Write(e.Exception, "Dispatcher.UIThread");
            Console.Error.WriteLine($"[WTLauncher] UI thread exception: {e.Exception}");
            e.Handled = true;
        };

        IClassicDesktopStyleApplicationLifetime? desktopLifetime = null;
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktopLifetime = desktop;
                var (explicitStorePath, unknownArgs) = LauncherStartupArguments.ReadConfigPath(desktop.Args);
                using var tempPersistence = new FileStorePersistence();
                var storePath = tempPersistence.ResolveStorePath(
                    explicitStorePath,
                    Environment.CurrentDirectory,
                    AppContext.BaseDirectory);

                // Verify config directory is writable before loading the store.
                // A stale file lock or permission denial would otherwise surface as a
                // cryptic crash later during save. Early detection gives a clear warning.
                var configDirectory = Path.GetDirectoryName(storePath);
                if (!string.IsNullOrEmpty(configDirectory) && Directory.Exists(configDirectory))
                {
                    try
                    {
                        var testPath = Path.Combine(configDirectory, Path.GetRandomFileName());
                        File.WriteAllText(testPath, "test");
                        File.Delete(testPath);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                    {
                        Console.Error.WriteLine(
                            $"[WTLauncher] WARNING: Config directory is not writable ({ex.GetType().Name}): {configDirectory}");
                    }
                }

                // CA2000: storePersistence 和 storeGrain 在应用生命周期内保持活跃，
                // 进程退出时由 OS 清理。MainWindowViewModel.Dispose 释放 StoreGrain 订阅。
#pragma warning disable CA2000
                var storePersistence = new FileStorePersistence();
                var storeGrain = new StoreGrain(storePath, storePersistence);
#pragma warning restore CA2000
                var storeFile = storeGrain.Select(s => s);

                var launchWorkflow = LauncherLaunchWorkflow.Create();
                var dialogService = new DialogService();
                var entryService = new EntryService(dialogService);
                var presetService = new PresetService(dialogService);

                var viewModel = new MainWindowViewModel(
                    storePath,
                    storeGrain,
                    storeFile,
                    launchWorkflow,
                    dialogService,
                    entryService,
                    presetService,
                    static () => DateTimeOffset.UtcNow);
                var windowSettings = storeFile.Settings.Window;
                var mainWindow = new MainWindow(viewModel)
                {
                    Width = Math.Max(windowSettings.Width, LauncherDesignTokens.WindowMinWidth),
                    Height = Math.Max(windowSettings.Height, LauncherDesignTokens.WindowMinHeight),
                };
                dialogService.Initialize(mainWindow);
                desktop.MainWindow = mainWindow;
            }
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Startup");
            Console.Error.WriteLine($"[WTLauncher] Fatal startup error: {ex}");
            Environment.ExitCode = 1;
        }
        finally
        {
            // If desktop lifetime was resolved but MainWindow is still null after
            // a startup exception, show a user-facing error before Avalonia starts the
            // message loop with no window (which would hang the process invisibly).
            if (desktopLifetime is not null && desktopLifetime.MainWindow is null)
            {
                var messageBox = new Window
                {
                    Title = "启动失败",
                    Width = 480,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new TextBlock
                    {
                        Text = "启动失败：配置加载异常，请检查日志后重试。",
                        Margin = new Thickness(24),
                        TextWrapping = TextWrapping.Wrap,
                    },
                };
                desktopLifetime.MainWindow = messageBox;
            }

            _isInitialized = true;
            // Must always be called — Avalonia expects this to start the message loop.
            base.OnFrameworkInitializationCompleted();
        }
    }

}
