using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryCollectionSaveWorkflowTests
{
    [Fact]
    public void SaveReturnsCountsAndPersistsEntrySettings()
    {
        using var temp = new TempDirectory();
        var (owner, configPath, settingsPath) = CreateOwner(
            temp,
            new LauncherSettings { CustomEntryOrder = ["one", "two"] },
            Entry("one", selected: true),
            Entry("two", selected: false));
        var beforeSaveSettingsCalled = false;

        var result = LauncherEntryCollectionSaveWorkflow.Save(
            owner.Entries,
            configPath,
            new LauncherPresetFile(),
            LauncherPresetStore.ResolvePresetPath(configPath),
            new LauncherSettings { CustomEntryOrder = ["one", "two"] },
            settingsPath,
            savePresets: false,
            rollbackSnapshot: null,
            beforeSaveSettings: () => beforeSaveSettingsCalled = true);

        Assert.True(result.Succeeded);
        Assert.Null(result.StatusText);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.SelectedCount);
        Assert.True(beforeSaveSettingsCalled);
        Assert.True(LauncherConfigStore.Load(configPath).Single(entry => entry.Id == "one").Selected);
        Assert.Equal(["one", "two"], LauncherSettingsStore.Load(settingsPath).CustomEntryOrder);
    }

    [Fact]
    public void SaveReturnsCountsAndFailureStatusWhenConfigSaveFails()
    {
        using var temp = new TempDirectory();
        var (owner, _, settingsPath) = CreateOwner(
            temp,
            new LauncherSettings { CustomEntryOrder = ["one"] },
            Entry("one", selected: true));

        var result = LauncherEntryCollectionSaveWorkflow.Save(
            owner.Entries,
            temp.Path,
            new LauncherPresetFile(),
            Path.Combine(temp.Path, "presets.json"),
            new LauncherSettings { CustomEntryOrder = ["one"] },
            settingsPath,
            savePresets: false,
            rollbackSnapshot: null,
            beforeSaveSettings: () => throw new InvalidOperationException("Settings save should not run."));

        Assert.False(result.Succeeded);
        Assert.Contains("配置保存失败", result.StatusText);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.SelectedCount);
    }

    private static (MainWindowViewModel Owner, string ConfigPath, string SettingsPath) CreateOwner(
        TempDirectory temp,
        LauncherSettings settings,
        params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var configPath = Path.Combine(configDir, "config.json");
        var settingsPath = Path.Combine(configDir, "settings.json");
        LauncherConfigStore.Save(entries, configPath);
        return (
            new MainWindowViewModel(configPath, settingsPath, settings, new LauncherProcess()),
            configPath,
            settingsPath);
    }

    private static LaunchEntry Entry(string id, bool selected)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "codex",
            Confirm = false,
            Selected = selected,
        };
    }
}
