using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryCollectionSaveStateWorkflowTests
{
    [Fact]
    public void SaveAppliesCountsAndRunsSuccessCallback()
    {
        using var temp = new TempDirectory();
        var (owner, configPath, settingsPath) = CreateOwner(
            temp,
            new LauncherSettings { CustomEntryOrder = ["one", "two"] },
            Entry("one", selected: true),
            Entry("two", selected: false));
        LauncherEntryCollectionSaveState? applied = null;
        var beforeSaveSettingsCalled = false;
        var afterSaveCalled = false;

        var succeeded = LauncherEntryCollectionSaveStateWorkflow.Save(
            owner.Entries,
            configPath,
            new LauncherPresetFile(),
            LauncherPresetStore.ResolvePresetPath(configPath),
            new LauncherSettings { CustomEntryOrder = ["one", "two"] },
            settingsPath,
            savePresets: false,
            rollbackSnapshot: null,
            beforeSaveSettings: () => beforeSaveSettingsCalled = true,
            applyState: state => applied = state,
            updateStatus: _ => throw new InvalidOperationException("Status should not update on success."),
            afterSave: () => afterSaveCalled = true);

        Assert.True(succeeded);
        Assert.True(beforeSaveSettingsCalled);
        Assert.True(afterSaveCalled);
        var appliedState = applied ?? throw new InvalidOperationException("Save state was not applied.");
        Assert.Equal(2, appliedState.TotalCount);
        Assert.Equal(1, appliedState.SelectedCount);
    }

    [Fact]
    public void SaveAppliesCountsAndFailureStatusWithoutSuccessCallback()
    {
        using var temp = new TempDirectory();
        var (owner, _, settingsPath) = CreateOwner(
            temp,
            new LauncherSettings { CustomEntryOrder = ["one"] },
            Entry("one", selected: true));
        LauncherEntryCollectionSaveState? applied = null;
        string? status = null;
        var afterSaveCalled = false;

        var succeeded = LauncherEntryCollectionSaveStateWorkflow.Save(
            owner.Entries,
            temp.Path,
            new LauncherPresetFile(),
            Path.Combine(temp.Path, "presets.json"),
            new LauncherSettings { CustomEntryOrder = ["one"] },
            settingsPath,
            savePresets: false,
            rollbackSnapshot: null,
            beforeSaveSettings: () => throw new InvalidOperationException("Settings save should not run."),
            applyState: state => applied = state,
            updateStatus: value => status = value,
            afterSave: () => afterSaveCalled = true);

        Assert.False(succeeded);
        Assert.False(afterSaveCalled);
        Assert.Contains("配置保存失败", status);
        var appliedState = applied ?? throw new InvalidOperationException("Save state was not applied.");
        Assert.Equal(1, appliedState.TotalCount);
        Assert.Equal(1, appliedState.SelectedCount);
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
