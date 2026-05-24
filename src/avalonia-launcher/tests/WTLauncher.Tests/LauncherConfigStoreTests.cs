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
    public void ResolveConfigPath_FallsBackToExecutableLocalConfigForPortableCopy()
    {
        using var temp = new TempDirectory();
        var appDir = System.IO.Path.Combine(temp.Path, "publish");
        var configDir = System.IO.Path.Combine(appDir, "config");
        Directory.CreateDirectory(configDir);
        var config = System.IO.Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

        var resolved = LauncherConfigStore.ResolveConfigPath(null, currentDirectory: null, appBaseDirectory: appDir);

        Assert.Equal(config, resolved);
    }

    [Fact]
    public void ResolveSettingsPath_StaysBesideResolvedConfig()
    {
        var config = System.IO.Path.Combine("portable-root", "config", "config.json");

        var settings = LauncherConfigStore.ResolveSettingsPath(config);

        Assert.EndsWith(System.IO.Path.Combine("portable-root", "config", "settings.json"), settings);
    }

    [Fact]
    public void PresetStore_RoundTripsHumanReadableLaunchGroups()
    {
        using var temp = new TempDirectory();
        var presetPath = System.IO.Path.Combine(temp.Path, "config", "presets.json");

        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = "daily",
                Presets =
                [
                    new LauncherPreset
                    {
                        Id = "daily",
                        Name = "日常启动",
                        EntryIds = ["one", "two"],
                    },
                ],
            },
            presetPath);

        var loaded = LauncherPresetStore.Load(presetPath);

        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Equal("daily", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("日常启动", preset.Name);
        Assert.Equal(["one", "two"], preset.EntryIds);
        var savedJson = File.ReadAllText(presetPath);
        Assert.Contains("\"presets\"", savedJson);
        Assert.Contains("日常启动", savedJson);
        Assert.EndsWith(Environment.NewLine, savedJson);
    }

    [Fact]
    public void PresetStore_CanSkipBackupForRollbackWrites()
    {
        using var temp = new TempDirectory();
        var configDir = System.IO.Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var presetPath = System.IO.Path.Combine(configDir, "presets.json");
        File.WriteAllText(presetPath, """{"schemaVersion":1,"defaultPresetId":"","presets":[]}""");

        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = "daily",
                Presets =
                [
                    new LauncherPreset
                    {
                        Id = "daily",
                        Name = "Daily",
                        EntryIds = ["one"],
                    },
                ],
            },
            presetPath,
            backup: false);

        Assert.False(File.Exists(presetPath + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, "presets.json.tmp-*"));
    }

    [Fact]
    public void ResolvePresetPath_StaysBesideResolvedConfig()
    {
        var config = System.IO.Path.Combine("portable-root", "config", "config.json");

        var presets = LauncherPresetStore.ResolvePresetPath(config);

        Assert.EndsWith(System.IO.Path.Combine("portable-root", "config", "presets.json"), presets);
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
                    Name = "演示",
                    Directory = "D:\\demo",
                    Command = "codex",
                    Confirm = true,
                    Selected = true,
                },
            ],
            config);

        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.True(entry.Selected);
        var savedJson = File.ReadAllText(config);
        Assert.Contains("演示", savedJson);
        Assert.EndsWith(Environment.NewLine, savedJson);
        Assert.True(File.Exists(config + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, "config.json.tmp-*"));
    }

    [Fact]
    public void Save_CanSkipBackupForRollbackWrites()
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
                    Name = "演示",
                    Directory = "D:\\demo",
                    Command = "codex",
                },
            ],
            config,
            backup: false);

        Assert.False(File.Exists(config + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, "config.json.tmp-*"));
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
                LastFilterText = "爬虫",
                SortMode = "alphabetical",
                EntryViewMode = "details",
                RequireConfirmationForLaunches = false,
                CustomEntryOrder = ["two", "one"],
                Window = new LauncherWindowSettings { Width = 1200, Height = 800 },
            },
            settings);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("爬虫", loaded.LastFilterText);
        Assert.Equal("alphabetical", loaded.SortMode);
        Assert.Equal("details", loaded.EntryViewMode);
        Assert.False(loaded.RequireConfirmationForLaunches);
        Assert.Equal(["two", "one"], loaded.CustomEntryOrder);
        Assert.Equal(1200d, loaded.Window.Width);
        Assert.Equal(800d, loaded.Window.Height);
        var savedJson = File.ReadAllText(settings);
        Assert.Contains("爬虫", savedJson);
        Assert.Contains("alphabetical", savedJson);
        Assert.Contains("details", savedJson);
        Assert.Contains("requireConfirmationForLaunches", savedJson);
        Assert.EndsWith(Environment.NewLine, savedJson);
    }

}
