using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.App;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configPath = LauncherConfigStore.ResolveConfigPath(
                ReadConfigArgument(desktop.Args),
                Environment.CurrentDirectory,
                AppContext.BaseDirectory);
            var settingsPath = LauncherConfigStore.ResolveSettingsPath(configPath);
            var settings = LauncherSettingsStore.Load(settingsPath);
            var viewModel = new MainWindowViewModel(configPath, settingsPath, settings, new LauncherProcess());
            var mainWindow = new MainWindow
            {
                DataContext = viewModel,
                Width = settings.Window.Width,
                Height = settings.Window.Height,
            };
            mainWindow.Attach(viewModel, settingsPath, settings);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string? ReadConfigArgument(IReadOnlyList<string>? args)
    {
        if (args is null)
            return null;

        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
                return args[i + 1];
        }

        return null;
    }
}
