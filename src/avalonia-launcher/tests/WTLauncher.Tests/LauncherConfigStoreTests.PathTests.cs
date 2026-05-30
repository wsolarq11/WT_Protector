using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void ResolveConfigPath_PrefersExplicitPath()
    {
        var explicitPath = Path.Combine("somewhere", "config.json");

        var resolved = LauncherConfigStore.ResolveConfigPath(explicitPath, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.EndsWith(Path.Combine("somewhere", "config.json"), resolved);
    }

    [Fact]
    public void ResolveConfigPath_FindsConfigDirectoryUnderRepositoryRoot()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "README.md"), "demo");
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

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
        var configDir = Path.Combine(appDir, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

        var resolved = LauncherConfigStore.ResolveConfigPath(null, currentDirectory: null, appBaseDirectory: appDir);

        Assert.Equal(config, resolved);
    }

    [Fact]
    public void ResolveSettingsPath_StaysBesideResolvedConfig()
    {
        var config = Path.Combine("portable-root", "config", "config.json");

        var settings = LauncherConfigStore.ResolveSettingsPath(config);

        Assert.EndsWith(Path.Combine("portable-root", "config", "settings.json"), settings);
    }

    [Fact]
    public void ResolvePresetPath_StaysBesideResolvedConfig()
    {
        var config = Path.Combine("portable-root", "config", "config.json");

        var presets = LauncherPresetStore.ResolvePresetPath(config);

        Assert.EndsWith(Path.Combine("portable-root", "config", "presets.json"), presets);
    }
}
