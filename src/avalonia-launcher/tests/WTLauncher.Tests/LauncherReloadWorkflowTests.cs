using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherReloadWorkflowTests
{
    [Fact]
    public void LoadRefreshesEntriesPresetsCustomOrderAndStatus()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherConfigStore.Save(
            [
                Entry("one", selected: true),
                Entry("two", selected: false),
            ],
            configPath);
        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = "daily",
                Presets =
                [
                    new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                ],
            },
            presetPath);
        var owner = CreateOwner(temp.Path, configPath);
        var entries = new ObservableCollection<LauncherEntryViewModel>
        {
            new(new LaunchEntry { Id = "stale", Name = "Stale" }, owner),
        };
        var presets = new ObservableCollection<LauncherPresetViewModel>
        {
            new(new LauncherPreset { Id = "stale", Name = "Stale" }),
        };
        var customOrder = new List<string> { "stale", "two" };

        var result = LauncherReloadWorkflow.Load(
            configPath,
            presetPath,
            entries,
            presets,
            customOrder,
            entry => new LauncherEntryViewModel(entry, owner));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.SelectedCount);
        Assert.Equal(1, result.PresetCount);
        Assert.Equal("daily", result.SelectedPreset?.Id);
        Assert.Equal(["one", "two"], entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["daily"], presets.Select(preset => preset.Id).ToArray());
        Assert.Equal(["two", "one"], customOrder);
        Assert.Equal("已加载 2 个启动项、1 个启动组；勾选自动保存到 config\\config.json", result.StatusText);
    }

    [Fact]
    public void LoadReturnsPresetLoadErrorAsStatus()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherConfigStore.Save([Entry("one", selected: false)], configPath);
        File.WriteAllText(presetPath, "{ invalid-json");
        var owner = CreateOwner(temp.Path, configPath);
        var entries = new ObservableCollection<LauncherEntryViewModel>();
        var presets = new ObservableCollection<LauncherPresetViewModel>();
        var customOrder = new List<string>();

        var result = LauncherReloadWorkflow.Load(
            configPath,
            presetPath,
            entries,
            presets,
            customOrder,
            entry => new LauncherEntryViewModel(entry, owner));

        Assert.StartsWith("启动组加载失败:", result.StatusText, StringComparison.Ordinal);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(0, result.PresetCount);
        Assert.Null(result.SelectedPreset);
        Assert.Empty(presets);
    }

    private static MainWindowViewModel CreateOwner(string rootPath, string configPath)
    {
        return new MainWindowViewModel(
            configPath,
            Path.Combine(rootPath, "settings.json"),
            new LauncherSettings(),
            new LauncherProcess());
    }

    private static LaunchEntry Entry(string id, bool selected)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
            Selected = selected,
        };
    }
}
