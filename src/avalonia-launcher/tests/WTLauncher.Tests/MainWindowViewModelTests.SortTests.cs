using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void SortModesAndCustomDragOrderPersistPortableSettings()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "Zulu",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = true,
                UpdatedAtUtc = "2026-05-17T00:00:00Z",
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Alpha",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
                UpdatedAtUtc = "2026-05-18T00:00:00Z",
            },
            new LaunchEntry
            {
                Id = "three",
                Name = "Mike",
                Directory = "D:\\three",
                Command = "pwsh",
                Confirm = false,
                UpdatedAtUtc = "",
            });

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["three", "one", "two"] });

        Assert.Equal(["custom", "updated", "alphabetical"], viewModel.SortModes.Select(mode => mode.Id).ToArray());
        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("two");
        Assert.True(viewModel.PreviewMoveEntry("two", "three", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("custom", savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("alphabetical", savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "updated");

        Assert.Equal(["two", "one", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("updated", savedSettings.SortMode);
    }
}
