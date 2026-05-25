using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class MainWindowClosingWorkflowTests
{
    [Fact]
    public void TrySaveWindowBoundsPersistsAndDoesNotReportStatusOnSuccess()
    {
        using var temp = new TempDirectory();
        var settingsPath = Path.Combine(temp.Path, "config", "settings.json");
        var settings = new LauncherSettings
        {
            Window = new LauncherWindowSettings { Width = 1000, Height = 700 },
        };

        string? reported = null;
        var saved = MainWindowClosingWorkflow.TrySaveWindowBounds(
            settings,
            settingsPath,
            1320,
            840,
            status => reported = status);

        Assert.True(saved);
        Assert.Null(reported);
        Assert.Equal(1320d, settings.Window.Width);
        Assert.Equal(840d, settings.Window.Height);
        var loaded = LauncherSettingsStore.Load(settingsPath);
        Assert.Equal(1320d, loaded.Window.Width);
        Assert.Equal(840d, loaded.Window.Height);
    }

    [Fact]
    public void TrySaveWindowBoundsRollsBackAndReportsFailureOnSaveError()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var settingsPath = Path.Combine(configDir, "settings.json");
        var settings = new LauncherSettings
        {
            Window = new LauncherWindowSettings { Width = 1000, Height = 700 },
        };
        Directory.CreateDirectory(settingsPath);

        string? reported = null;
        var saved = MainWindowClosingWorkflow.TrySaveWindowBounds(
            settings,
            settingsPath,
            1320,
            840,
            status => reported = status);

        Assert.False(saved);
        Assert.Equal("窗口尺寸保存失败；下次将使用上次保存的尺寸", reported);
        Assert.Equal(1000d, settings.Window.Width);
        Assert.Equal(700d, settings.Window.Height);
        Assert.Empty(Directory.EnumerateFiles(configDir, "settings.json.tmp-*"));
    }

    [Fact]
    public void TrySaveWindowBoundsIgnoresMissingSettings()
    {
        string? reported = null;

        var saved = MainWindowClosingWorkflow.TrySaveWindowBounds(
            null,
            null,
            1320,
            840,
            status => reported = status);

        Assert.True(saved);
        Assert.Null(reported);
    }
}
