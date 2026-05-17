using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherConfigStoreTests
{
    [Fact]
    public void Load_ReadsRootConfigShape()
    {
        using var temp = new TempDirectory();
        var configDir = System.IO.Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = System.IO.Path.Combine(configDir, "config.json");
        File.WriteAllText(config, """
        [
          {
            "id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex",
            "confirm": true,
            "selected": true
          }
        ]
        """);

        var entries = LauncherConfigStore.Load(config);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Name);
        Assert.Equal("D:\\demo", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.True(entry.Confirm);
        Assert.True(entry.Selected);
    }

    [Fact]
    public void ResolveConfigPath_PrefersExplicitPath()
    {
        var explicitPath = System.IO.Path.Combine("somewhere", "config.json");

        var resolved = LauncherConfigStore.ResolveConfigPath(explicitPath, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.EndsWith(System.IO.Path.Combine("somewhere", "config.json"), resolved);
    }

    [Fact]
    public void ResolveConfigPath_FindsConfigDirectoryUnderRepositoryRoot()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(System.IO.Path.Combine(temp.Path, "README.md"), "demo");
        var configDir = System.IO.Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = System.IO.Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

        var nested = System.IO.Path.Combine(temp.Path, "src", "avalonia-launcher");
        Directory.CreateDirectory(nested);
        var resolved = LauncherConfigStore.ResolveConfigPath(null, currentDirectory: nested, appBaseDirectory: null);

        Assert.Equal(config, resolved);
    }

    [Fact]
    public void Save_PersistsSelectionAndCreatesBackup()
    {
        using var temp = new TempDirectory();
        var configDir = System.IO.Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = System.IO.Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

        LauncherConfigStore.Save(
            [
                new LaunchEntry
                {
                    Id = "demo",
                    Name = "demo",
                    Directory = "D:\\demo",
                    Command = "codex",
                    Confirm = true,
                    Selected = true,
                },
            ],
            config);

        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.True(entry.Selected);
        Assert.True(File.Exists(config + ".bak"));
    }

    [Fact]
    public void SettingsStore_RoundTripsPortableSettings()
    {
        using var temp = new TempDirectory();
        var settings = System.IO.Path.Combine(temp.Path, "config", "settings.json");

        LauncherSettingsStore.Save(
            new LauncherSettings
            {
                Theme = "dark",
                LastFilterText = "crawler",
                Window = new LauncherWindowSettings { Width = 1200, Height = 800 },
            },
            settings);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("crawler", loaded.LastFilterText);
        Assert.Equal(1200, loaded.Window.Width);
        Assert.Equal(800, loaded.Window.Height);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wt-launcher-config-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
