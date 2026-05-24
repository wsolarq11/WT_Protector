using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetApplyWorkflowTests
{
    [Fact]
    public void ApplyReportsMissingSelectedPreset()
    {
        var result = LauncherPresetApplyWorkflow.Apply(
            selectedPreset: null,
            presetCount: 1,
            entries: [],
            replaceSelection: _ => throw new InvalidOperationException("Selection should not be replaced."));

        Assert.Equal("未选择启动组", result.StatusText);
    }

    [Fact]
    public void ApplyReportsMissingConfigIdsWithoutReplacingSelection()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp, Entry("one", selected: false));
        var preset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "broken",
            Name = "Broken",
            EntryIds = ["one", "missing"],
        });
        var replaceCalled = false;

        var result = LauncherPresetApplyWorkflow.Apply(
            preset,
            presetCount: 1,
            owner.Entries,
            _ =>
            {
                replaceCalled = true;
                return true;
            });

        Assert.Contains("missing", result.StatusText);
        Assert.False(replaceCalled);
    }

    [Fact]
    public void ApplyReplacesSelectionAndReturnsSavedStatus()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp, Entry("one", selected: false), Entry("two", selected: true));
        var preset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one"],
        });
        IReadOnlySet<string>? replacement = null;

        var result = LauncherPresetApplyWorkflow.Apply(
            preset,
            presetCount: 1,
            owner.Entries,
            ids =>
            {
                replacement = ids;
                return true;
            });

        Assert.Equal("已应用启动组: Daily（1 项，已保存）", result.StatusText);
        Assert.NotNull(replacement);
        Assert.Equal(["one"], replacement!.Order(StringComparer.Ordinal).ToArray());
    }

    private static MainWindowViewModel CreateOwner(TempDirectory temp, params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var configPath = Path.Combine(configDir, "config.json");
        LauncherConfigStore.Save(entries, configPath);
        return new MainWindowViewModel(
            configPath,
            Path.Combine(configDir, "settings.json"),
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
            Command = "codex",
            Confirm = false,
            Selected = selected,
        };
    }
}
