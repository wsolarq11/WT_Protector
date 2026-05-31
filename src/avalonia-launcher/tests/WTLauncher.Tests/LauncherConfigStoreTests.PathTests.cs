using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void ResolveConfigPath_PrefersExplicitPath()
    {
        var explicitPath = Path.Combine("somewhere", LauncherConfigStore.ConfigFileName);

        var resolved = LauncherConfigStore.ResolveConfigPath(explicitPath, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.EndsWith(Path.Combine("somewhere", LauncherConfigStore.ConfigFileName), resolved);
    }

    [Fact]
    public void ResolveConfigPath_TrimsExplicitPath()
    {
        var explicitPath = $" {Path.Combine("somewhere", LauncherConfigStore.ConfigFileName)} ";

        var resolved = LauncherConfigStore.ResolveConfigPath(explicitPath, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.EndsWith(Path.Combine("somewhere", LauncherConfigStore.ConfigFileName), resolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ResolveConfigPath_RejectsBlankExplicitPath(string explicitPath)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => LauncherConfigStore.ResolveConfigPath(explicitPath, currentDirectory: "ignored", appBaseDirectory: "ignored"));

        Assert.Equal("explicitPath", exception.ParamName);
        Assert.StartsWith("Explicit config path is required.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ConfigPathMethodsRejectBlankPath(string? configPath)
    {
        AssertConfigPathRequired(() => LauncherConfigStore.Load(configPath!));
        AssertConfigPathRequired(() => LauncherConfigStore.Save([], configPath!));
        AssertConfigPathRequired(() => LauncherConfigStore.ResolveSettingsPath(configPath!));
    }

    [Fact]
    public void ResolveConfigPath_FindsConfigDirectoryUnderRepositoryRoot()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, LauncherConfigStore.RepositoryMarkerFileName), "demo");
        var config = WriteEmptyLauncherConfig(temp.Path);

        var nested = Path.Combine(temp.Path, "src", "avalonia-launcher");
        Directory.CreateDirectory(nested);
        var resolved = LauncherConfigStore.ResolveConfigPath(null, currentDirectory: nested, appBaseDirectory: null);

        Assert.Equal(config, resolved);
    }

    [Fact]
    public void ResolveConfigPath_FallsBackToExecutableLocalConfigForPortableCopy()
    {
        using var temp = new TempDirectory();
        var appDir = Path.Combine(temp.Path, "publish");
        var config = WriteEmptyLauncherConfig(appDir);

        var resolved = LauncherConfigStore.ResolveConfigPath(null, currentDirectory: null, appBaseDirectory: appDir);

        Assert.Equal(config, resolved);
    }

    [Fact]
    public void ResolveSettingsPath_StaysBesideResolvedConfig()
    {
        var config = Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.ConfigFileName);

        var settings = LauncherConfigStore.ResolveSettingsPath(config);

        Assert.EndsWith(Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.SettingsFileName), settings);
    }

    [Fact]
    public void ResolveSettingsPath_TrimsConfigPath()
    {
        var config = $" {Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.ConfigFileName)} ";

        var settings = LauncherConfigStore.ResolveSettingsPath(config);

        Assert.EndsWith(Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.SettingsFileName), settings);
    }

    [Fact]
    public void ResolvePresetPath_StaysBesideResolvedConfig()
    {
        var config = Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.ConfigFileName);

        var presets = LauncherPresetStore.ResolvePresetPath(config);

        Assert.EndsWith(Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherPresetStore.PresetsFileName), presets);
    }

    [Fact]
    public void ResolvePresetPath_TrimsConfigPath()
    {
        var config = $" {Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.ConfigFileName)} ";

        var presets = LauncherPresetStore.ResolvePresetPath(config);

        Assert.EndsWith(Path.Combine("portable-root", LauncherConfigStore.ConfigDirectoryName, LauncherPresetStore.PresetsFileName), presets);
    }

    private static void AssertConfigPathRequired(Action action)
    {
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("configPath", exception.ParamName);
    }
}
