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
                LauncherStartupArguments.ReadConfigPath(desktop.Args),
                Environment.CurrentDirectory,
                AppContext.BaseDirectory);
            var settingsPath = LauncherConfigStore.ResolveSettingsPath(configPath);
            var settings = LauncherSettingsStore.Load(settingsPath);
            var launchWorkflow = new LauncherLaunchWorkflow(new LauncherProcess());
            var viewModel = new MainWindowViewModel(configPath, settingsPath, settings, launchWorkflow);
            var mainWindow = new MainWindow(viewModel, settingsPath, settings)
            {
                Width = settings.Window.Width,
                Height = settings.Window.Height,
            };
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

}
