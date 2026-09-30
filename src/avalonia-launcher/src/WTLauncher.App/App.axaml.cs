using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.App.Platform;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.App;

public sealed partial class App : Application
{
    private bool _isInitialized;

    public override void Initialize()
    {
        InjectDesignTokens();
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Replaces XAML-hardcoded resources with values derived from
    /// <see cref="LauncherDesignTokens"/> — the single source of truth for colors,
    /// spacing, sizing, and card shadow.
    /// </summary>
    private void InjectDesignTokens()
    {
        // ── Base colors ────────────────────────────────────
        Resources["ActiveBorderBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.ActiveBorderColor));
        Resources["DestructiveBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.DestructiveColor));
        Resources["ErrorBrush"]       = new SolidColorBrush(Color.Parse(LauncherDesignTokens.ErrorColor));

        // Destructive button interaction ramp — same HSL derivation as the
        // accent ramp, but keyed to the fixed destructive base. Hover/pressed
        // darken instead of fading the whole button (the old opacity hack).
        var destructiveRamp = AccentColorRamp.FromHex(LauncherDesignTokens.DestructiveColor);
        Resources["DestructiveHoverBrush"] = new SolidColorBrush(Color.Parse(destructiveRamp.Hover));
        Resources["DestructivePressedBrush"] = new SolidColorBrush(Color.Parse(destructiveRamp.Pressed));
        Resources["DestructiveDisabledBackgroundBrush"] = new SolidColorBrush(Color.Parse(destructiveRamp.DisabledBackground));
        Resources["DestructiveDisabledForegroundBrush"] = new SolidColorBrush(Color.Parse(destructiveRamp.DisabledForeground));
        Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.TextPrimaryColor));
        Resources["TextMutedBrush"]   = new SolidColorBrush(Color.Parse(LauncherDesignTokens.TextMutedColor));
        Resources["CardBorderBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BorderColor));
        Resources["BorderBrush"]      = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BorderColor));
        Resources["SurfaceBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SurfaceColor));

        // ── Canvas ─────────────────────────────────────────
        // Flat neutral background: cards carry the hierarchy, not a gradient.
        Resources["BackgroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BackgroundColor));
        Resources["FocusBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.FocusGlowColor));

        // ── Semantic state colors ──────────────────────────
        Resources["HoverBackgroundBrush"]            = new SolidColorBrush(Color.Parse(LauncherDesignTokens.HoverBackgroundColor));
        Resources["SelectionBackgroundBrush"]        = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SelectionBackgroundColor));
        Resources["SelectionBorderBrush"]            = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SelectionBorderColor));
        Resources["SelectionHoverBackgroundBrush"]   = new SolidColorBrush(Color.Parse(LauncherDesignTokens.SelectionHoverBackgroundColor));

        // ── Drag indicator ─────────────────────────────────
        Resources["DragHighlightBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.DragHighlightColor));

        // ── Accent button ramp (default; overwritten once settings load) ──
        ApplyAccentRamp(Resources, AccentColorRamp.DefaultAccentColor);

        // ── Control box ────────────────────────────────────
        Resources["ControlBoxLabelBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.ControlBoxLabelColor));

        // ── Status badges ──────────────────────────────────
        Resources["BadgeSuccessBackgroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeSuccessBackgroundColor));
        Resources["BadgeSuccessForegroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeSuccessForegroundColor));
        Resources["BadgeInfoBackgroundBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeInfoBackgroundColor));
        Resources["BadgeInfoForegroundBrush"]     = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeInfoForegroundColor));
        Resources["BadgeWarningBackgroundBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeWarningBackgroundColor));
        Resources["BadgeWarningForegroundBrush"]  = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeWarningForegroundColor));

        // ── Per-agent brand badge ──────────────────────────
        Resources["BrandBadgeSize"] = LauncherDesignTokens.BrandBadgeSize;
        Resources["BrandBadgeCornerRadius"] = new CornerRadius(LauncherDesignTokens.BrandBadgeCornerRadius);
        Resources["BrandBadgeForegroundBrush"] = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BrandBadgeForegroundColor));

        // ── Dimensions / Spacing ────────────────────────────
        Resources["CardBorderRadius"] = new CornerRadius(LauncherDesignTokens.CardBorderRadius);
        Resources["CardPadding"] = new Thickness(LauncherDesignTokens.CardPadding);
        Resources["ControlCornerRadius"] = new CornerRadius(LauncherDesignTokens.ControlCornerRadius);
        Resources["DropdownCornerRadius"] = new CornerRadius(LauncherDesignTokens.DropdownCornerRadius);
        Resources["DialogCornerRadius"] = new CornerRadius(LauncherDesignTokens.DialogCornerRadius);
        Resources["ButtonCornerRadius"] = new CornerRadius(LauncherDesignTokens.ButtonCornerRadius);
        Resources["BadgeCornerRadius"] = new CornerRadius(LauncherDesignTokens.BadgeCornerRadius);
        Resources["FocusBorderThickness"] = new Thickness(LauncherDesignTokens.FocusBorderThickness);
        Resources["SpacingFrame"] = new Thickness(LauncherDesignTokens.SpacingFrame);
        Resources["FontSizeSmall"] = LauncherDesignTokens.FontSizeSmall;
        Resources["FontSizeCaption"] = LauncherDesignTokens.FontSizeCaption;
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

        // ── Card hover lift ────────────────────────────────
        // 位移与预留空间都由 LauncherDesignTokens.CardHoverLift 单一推导，
        // 保证 XAML 里不再手写 -2px / 4px 两处 magic number。
        Resources["CardSelectedBorderThickness"] = new Thickness(LauncherDesignTokens.CardSelectedBorderThickness);
        Resources["CardHoverLiftMargin"] = new Thickness(0, LauncherDesignTokens.CardHoverLiftMarginTop, 0, 0);
        // ScrollViewer 借位：-P，与 FillGridPanel 的 +P 抵消，第一行卡片静止位置不变。
        Resources["CardHoverLiftScrollMargin"] = new Thickness(0, LauncherDesignTokens.CardHoverLiftScrollMarginTop, 0, 0);
        Resources["CardHoverLiftTransform"] = TransformOperations.Parse($"translateY(-{LauncherDesignTokens.CardHoverLift}px)");
        Resources["CardHoverLiftRestTransform"] = TransformOperations.Parse("translateY(0px)");

        // ── BoxShadow ───────────────────────────────────────
        Resources["CardBoxShadow"] = BoxShadows.Parse($"0 8 24 0 {LauncherDesignTokens.BoxShadowColor}");
        Resources["ControlBoxShadow"] = BoxShadows.Parse($"0 3 10 0 {LauncherDesignTokens.ControlShadowColor}");
        Resources["NoBoxShadow"] = BoxShadows.Parse("0 0 0 0 Transparent");

        // ── Acrylic material colors ──────────────────────
        Resources["AcrylicTintColor"] = Color.Parse(LauncherDesignTokens.BackgroundColor);
        Resources["AcrylicFallbackColor"] = Color.Parse(LauncherDesignTokens.BackgroundColor);

        // ── Lucide icons ─────────────────────────────────
        // Each geometry is uniformly scaled from its 24×24 viewbox to the target
        // pixel size, so the icon centre lands on the Path centre by construction
        // (the old raw-coordinate render left the icon offset and clipped). The
        // matching stroke width is injected alongside so the line weight tracks the
        // same scale instead of staying a 24-viewbox 2-unit pen on a smaller glyph.
        Resources["IconGripVertical"] = LucideIconGeometry.Fit(LucideIconGeometry.GripVertical, 16);
        Resources["IconGripVerticalSmall"] = LucideIconGeometry.Fit(LucideIconGeometry.GripVertical, 12);
        Resources["IconSettings"] = LucideIconGeometry.Fit(LucideIconGeometry.Settings, 16);
        Resources["IconChevronDown"] = LucideIconGeometry.Fit(LucideIconGeometry.ChevronDown, 16);
        Resources["IconChevronRight"] = LucideIconGeometry.Fit(LucideIconGeometry.ChevronRight, 16);
        Resources["IconStatusCheck"] = LucideIconGeometry.Fit(LucideIconGeometry.StatusCheck, 16);
        Resources["IconStatusX"] = LucideIconGeometry.Fit(LucideIconGeometry.StatusX, 16);
        Resources["IconStatusCircle"] = LucideIconGeometry.Fit(LucideIconGeometry.StatusCircle, 14);
        Resources["IconStatusAlert"] = LucideIconGeometry.Fit(LucideIconGeometry.StatusAlert, 16);
        Resources["IconStroke16"] = LucideIconGeometry.StrokeFor(16);
        Resources["IconStroke14"] = LucideIconGeometry.StrokeFor(14);
        Resources["IconStroke12"] = LucideIconGeometry.StrokeFor(12);
    }

    /// <summary>
    /// Injects (or refreshes) the five accent-button brushes derived from a
    /// single base color. DynamicResource bindings in App.axaml pick up the
    /// new values immediately, so re-calling this after a color change
    /// re-skins every accent button live.
    /// </summary>
    public static void ApplyAccentRamp(IResourceDictionary resources, string baseHex)
    {
        if (!AccentColorRamp.TryNormalize(baseHex, out var normalized))
            normalized = AccentColorRamp.DefaultAccentColor;

        var ramp = AccentColorRamp.FromHex(normalized);
        resources["AccentNormalBrush"] = new SolidColorBrush(Color.Parse(ramp.Normal));
        resources["AccentHoverBrush"] = new SolidColorBrush(Color.Parse(ramp.Hover));
        resources["AccentPressedBrush"] = new SolidColorBrush(Color.Parse(ramp.Pressed));
        resources["AccentDisabledBackgroundBrush"] = new SolidColorBrush(Color.Parse(ramp.DisabledBackground));
        resources["AccentDisabledForegroundBrush"] = new SolidColorBrush(Color.Parse(ramp.DisabledForeground));
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
                LauncherStore.DefaultPersistence = storePersistence;
                LauncherJson.DefaultIO = new FileLauncherJsonIO();
                var storeGrain = new StoreGrain(storePath, storePersistence);
#pragma warning restore CA2000
                var storeFile = storeGrain.Select(s => s);

                // Re-skin accent buttons from the persisted user color (falls
                // back to the injected default when the config omits the key).
                ApplyAccentRamp(Resources, storeFile.Settings.AccentColor);

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
                WireQuickLaunchAndTray(mainWindow, viewModel);
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
                    Title = LauncherLocalization.Format("app.startupFailureTitle"),
                    Width = 480,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new TextBlock
                    {
                        Text = LauncherLocalization.Format("app.startupFailureText"),
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

    private void WireQuickLaunchAndTray(MainWindow mainWindow, MainWindowViewModel viewModel)
    {
        var quickLaunchViewModel = new QuickLaunchViewModel(viewModel);
        var quickLaunchWindow = new QuickLaunchWindow(quickLaunchViewModel);
        var trayQuitting = false;

        void ShowMainWindow()
        {
            if (!mainWindow.IsVisible)
                mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }

        void ShowQuickLaunch()
        {
            quickLaunchViewModel.Reset();
            if (!quickLaunchWindow.IsVisible)
                quickLaunchWindow.Show(mainWindow);
            quickLaunchWindow.Activate();
        }

        try
        {
            var hotkey = LauncherHotkey.Parse(viewModel.Settings.QuickLaunchHotkey);
#pragma warning disable CA2000
            var hotkeyService = new Win32HotkeyService(hotkey.Modifiers, hotkey.Key);
#pragma warning restore CA2000
            hotkeyService.HotkeyPressed += () => Dispatcher.UIThread.Post(ShowQuickLaunch);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or System.ComponentModel.Win32Exception or DllNotFoundException or EntryPointNotFoundException)
        {
            ErrorLog.Write(ex, "QuickLaunchHotkey");
            Console.Error.WriteLine($"[WTLauncher] Quick-launch hotkey disabled: {ex.Message}");
        }

        if (viewModel.Settings.RunInTray)
        {
            try
            {
#pragma warning disable CA2000
                var tray = new Win32TrayIcon("WT Launcher");
#pragma warning restore CA2000
                tray.OpenRequested += () => Dispatcher.UIThread.Post(ShowMainWindow);
                tray.QuitRequested += () => Dispatcher.UIThread.Post(() =>
                {
                    trayQuitting = true;
                    mainWindow.Close();
                });
                mainWindow.Closing += (_, e) =>
                {
                    if (!trayQuitting)
                    {
                        e.Cancel = true;
                        mainWindow.Hide();
                    }
                };
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or DllNotFoundException or EntryPointNotFoundException)
            {
                ErrorLog.Write(ex, "Tray");
                Console.Error.WriteLine($"[WTLauncher] Tray disabled: {ex.Message}");
            }
        }
    }
}
