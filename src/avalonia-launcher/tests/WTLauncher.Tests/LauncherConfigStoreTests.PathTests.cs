using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void ResolveConfigPath_PrefersExplicitPath()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);

        var resolved = LauncherConfigStore.ResolveConfigPath(config, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.Equal(config, resolved);
    }

    [Fact]
    public void ResolveConfigPath_TrimsExplicitPath()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);
        var untrimmedPath = $" {config} ";

        var resolved = LauncherConfigStore.ResolveConfigPath(untrimmedPath, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.Equal(config, resolved);
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
        AssertConfigPathRequired(() => LauncherConfigStore.Save([], configPath!, backup: false));
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
    public void ResolveConfigPath_ReturnsCurrentDirectoryConfigPathWhenSearchMisses()
    {
        using var temp = new TempDirectory();
        var currentDir = Path.Combine(temp.Path, "work");
        Directory.CreateDirectory(currentDir);

        var resolved = LauncherConfigStore.ResolveConfigPath(
            null,
            currentDirectory: currentDir,
            appBaseDirectory: null);

        Assert.Equal(Path.Combine(currentDir, LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.ConfigFileName), resolved);
    }

    [Fact]
    public void ResolveConfigPath_ReturnsAppBaseConfigPathWhenCurrentDirectoryIsUnavailable()
    {
        using var temp = new TempDirectory();
        var appDir = Path.Combine(temp.Path, "publish");
        Directory.CreateDirectory(appDir);

        var resolved = LauncherConfigStore.ResolveConfigPath(
            null,
            currentDirectory: null,
            appBaseDirectory: appDir);

        Assert.Equal(Path.Combine(appDir, LauncherConfigStore.ConfigDirectoryName, LauncherConfigStore.ConfigFileName), resolved);
    }

    [Fact]
    public void ResolveConfigPath_RejectsMissingSearchRootsWithoutExplicitPath()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherConfigStore.ResolveConfigPath(null, currentDirectory: null, appBaseDirectory: null));

        Assert.Equal("currentDirectory", exception.ParamName);
        Assert.StartsWith("Cannot resolve config path: no explicit path was p", exception.Message);
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

    // ponytail: ResolvePresetPath tests removed — presets now live in the unified
    // LauncherStoreFile. ResolveStorePath is tested separately in LauncherStorePathTests.

    private static void AssertConfigPathRequired(Action action)
    {
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("configPath", exception.ParamName);
    }
}
