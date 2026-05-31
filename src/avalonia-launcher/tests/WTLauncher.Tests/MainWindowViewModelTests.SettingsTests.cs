using WTLauncher.App.ViewModels;
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
            Entry(
                "one",
                "Zulu",
                command: "codex",
                confirm: true,
                updatedAtUtc: "2026-05-17T00:00:00Z"),
            Entry("two", "Alpha", command: "opencode", updatedAtUtc: "2026-05-18T00:00:00Z"),
            Entry("three", "Mike"));

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings
            {
                SortMode = LauncherEntryVisibility.CustomSortModeId,
                CustomEntryOrder = ["three", "one", "two"],
            });

        Assert.Equal(
            [
                LauncherEntryVisibility.CustomSortModeId,
                LauncherEntryVisibility.UpdatedSortModeId,
                LauncherEntryVisibility.AlphabeticalSortModeId,
            ],
            viewModel.SortModes.Select(mode => mode.Id).ToArray());
        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("two");
        Assert.True(viewModel.PreviewMoveEntry("two", "three", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryVisibility.AlphabeticalSortModeId);

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(LauncherEntryVisibility.AlphabeticalSortModeId, savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryVisibility.UpdatedSortModeId);

        Assert.Equal(["two", "one", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(LauncherEntryVisibility.UpdatedSortModeId, savedSettings.SortMode);
    }

    [Fact]
    public void VisibleSettingsRollBackWhenSettingsSaveFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex"),
            Entry("two", "Two", command: "opencode"));

        var launcherSettings = new LauncherSettings
        {
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            EntryViewMode = LauncherEntryViewModes.CardsId,
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["one", "two"],
        };
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        Directory.CreateDirectory(settings);

        viewModel.FilterText = "two";
        Assert.Equal("", viewModel.FilterText);
        Assert.Equal("", launcherSettings.LastFilterText);
        Assert.Equal(2, viewModel.VisibleCount);
        Assert.Contains("设置保存失败", viewModel.StatusText);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryVisibility.AlphabeticalSortModeId);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, viewModel.SelectedSortMode?.Id);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, launcherSettings.SortMode);
        Assert.Equal(["one", "two"], launcherSettings.CustomEntryOrder);
        Assert.Contains("设置保存失败", viewModel.StatusText);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.DetailsId);
        Assert.Equal(LauncherEntryViewModes.CardsId, viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(LauncherEntryViewModes.CardsId, launcherSettings.EntryViewMode);
        Assert.Contains("设置保存失败", viewModel.StatusText);

        viewModel.RequireConfirmationForLaunches = false;
        Assert.True(viewModel.RequireConfirmationForLaunches);
        Assert.True(launcherSettings.RequireConfirmationForLaunches);
        Assert.Contains("设置保存失败", viewModel.StatusText);
    }

    [Fact]
    public void WindowBoundsSaveDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        Assert.True(viewModel.TrySaveWindowBounds(width: 1320, height: 840));

        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), savedSettings.CustomEntryOrder);
        Assert.Equal(1320d, savedSettings.Window.Width);
        Assert.Equal(840d, savedSettings.Window.Height);
        AssertNoDragCues(viewModel);
    }
}
