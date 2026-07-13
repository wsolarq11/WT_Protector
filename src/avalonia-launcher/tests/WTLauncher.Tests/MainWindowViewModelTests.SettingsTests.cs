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
                updatedAtUtc: "2026-05-17T00:00:00Z"),
            Entry("two", "Alpha", command: "opencode", updatedAtUtc: "2026-05-18T00:00:00Z"),
            Entry("three", "Mike"));

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings
            {
                LastFilterText = "",
                SortMode = LauncherEntryQuery.CustomSortModeId,
                CustomEntryOrder = ["three", "one", "two"],
            });

        Assert.Equal(
            [
                LauncherEntryQuery.CustomSortModeId,
                LauncherEntryQuery.RecentSortModeId,
                LauncherEntryQuery.UpdatedSortModeId,
                LauncherEntryQuery.AlphabeticalSortModeId,
                LauncherEntryQuery.FrequentSortModeId,
            ],
            viewModel.SortModes.Select(mode => mode.Id).ToArray());
        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("two");
        Assert.True(viewModel.PreviewMoveEntry("two", "three", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        var savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(LauncherEntryQuery.CustomSortModeId, savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryQuery.AlphabeticalSortModeId);

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(LauncherEntryQuery.AlphabeticalSortModeId, savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryQuery.UpdatedSortModeId);

        Assert.Equal(["two", "one", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(LauncherEntryQuery.UpdatedSortModeId, savedSettings.SortMode);
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
            LastFilterText = "",
            SortMode = LauncherEntryQuery.CustomSortModeId,
            EntryViewMode = LauncherEntryViewModes.CardsId,
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["one", "two"],
        };
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        File.Delete(settings);
        Directory.CreateDirectory(settings);

        viewModel.FilterText = "two";
        Assert.Equal("", viewModel.FilterText);
        Assert.Equal("", launcherSettings.LastFilterText);
        Assert.Equal(2, viewModel.VisibleCount);
        Assert.Contains("存储保存失败", viewModel.StatusText);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryQuery.AlphabeticalSortModeId);
        Assert.Equal(LauncherEntryQuery.CustomSortModeId, viewModel.SelectedSortMode?.Id);
        Assert.Equal(LauncherEntryQuery.CustomSortModeId, viewModel.Settings.SortMode);
        Assert.Equal(["one", "two"], viewModel.Settings.CustomEntryOrder);
        Assert.Contains("存储保存失败", viewModel.StatusText);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.DetailsId);
        Assert.Equal(LauncherEntryViewModes.CardsId, viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(LauncherEntryViewModes.CardsId, viewModel.Settings.EntryViewMode);
        Assert.Contains("存储保存失败", viewModel.StatusText);

        viewModel.RequireConfirmationForLaunches = false;
        Assert.True(viewModel.RequireConfirmationForLaunches);
        Assert.True(viewModel.Settings.RequireConfirmationForLaunches);
        Assert.Contains("存储保存失败", viewModel.StatusText);
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

        var savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), savedSettings.CustomEntryOrder);
        Assert.Equal(1320d, savedSettings.Window!.Width);
        Assert.Equal(840d, savedSettings.Window!.Height);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void WindowBoundsSaveFailureReportsStatusAndDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        File.Delete(settings);
        Directory.CreateDirectory(settings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        Assert.False(viewModel.TrySaveWindowBounds(width: 1320, height: 840));

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(LauncherSettingsStatus.WindowBoundsSaveFailed.Text, viewModel.StatusText);
        AssertNoDragCues(viewModel);
        AssertNoStoreTempFiles(settings);
    }
}
