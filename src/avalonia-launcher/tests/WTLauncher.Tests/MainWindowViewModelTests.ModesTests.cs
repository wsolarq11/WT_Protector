using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void UnknownModeIdsFailBeforeViewModelStateIsBuilt()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var exception = Assert.Throws<InvalidDataException>(() =>
            CreateViewModel(
                config,
                settings,
                new LauncherSettings { SortMode = "missing", EntryViewMode = "cards" }));

        Assert.Equal("Unknown launcher sort mode: missing.", exception.Message);
    }

    [Fact]
    public void SavedModeIdsUseCoreNormalizationRules()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var launcherSettings = new LauncherSettings
        {
            SortMode = $" {LauncherEntryVisibility.UpdatedSortModeId} ",
            EntryViewMode = $" {LauncherEntryViewModes.DetailsId} ",
        };

        var viewModel = CreateViewModel(config, settings, launcherSettings);

        Assert.Equal(LauncherEntryVisibility.UpdatedSortModeId, viewModel.SelectedSortMode?.Id);
        Assert.Equal(LauncherEntryVisibility.UpdatedSortModeId, viewModel.Settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.DetailsId, viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(LauncherEntryViewModes.DetailsId, viewModel.Settings.EntryViewMode);
    }

    [Fact]
    public void EntryViewModePersistsAndOverridesCardDensityForDetailedScanning()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateSingleEntryFixture(temp);

        var viewModel = CreateViewModel(config, settings);
        var entry = Assert.Single(viewModel.Entries);

        // L36: 5px hysteresis — must be below threshold by 5px to trigger denser layout.
        entry.CardWidth = LauncherEntryViewModel.MicroLayoutMaxWidth - 5;
        entry.CardHeight = LauncherEntryViewModel.MicroLayoutMaxHeight - 5;
        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.MicroTitleFontSize, entry.TitleFontSize);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.DetailsId);

        Assert.True(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.ScanningTitleFontSize, entry.TitleFontSize);
        Assert.Equal(LauncherEntryViewModes.DetailsId, LauncherStore.Load(settings).Settings.EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.NamesId);

        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.ScanningTitleFontSize, entry.TitleFontSize);
        Assert.Equal(LauncherEntryViewModes.NamesId, LauncherStore.Load(settings).Settings.EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.CardsId);

        Assert.False(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.MicroTitleFontSize, entry.TitleFontSize);
        Assert.Equal(LauncherEntryViewModes.CardsId, LauncherStore.Load(settings).Settings.EntryViewMode);
    }

    [Fact]
    public void SelectedModesRejectNullAssignmentsInsteadOfFallingBackToDefaults()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateSingleEntryFixture(temp);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = LauncherEntryVisibility.CustomSortModeId, EntryViewMode = LauncherEntryViewModes.CardsId });

        var sortMode = viewModel.SelectedSortMode;
        var displayMode = viewModel.SelectedDisplayMode;

        var sortException = Assert.Throws<InvalidOperationException>(() => viewModel.SelectedSortMode = null);
        Assert.Equal("Launcher sort mode selection is required.", sortException.Message);
        Assert.Same(sortMode, viewModel.SelectedSortMode);
        Assert.True(viewModel.IsCustomSortMode);

        var displayException = Assert.Throws<InvalidOperationException>(() => viewModel.SelectedDisplayMode = null);
        Assert.Equal("Launcher entry view mode selection is required.", displayException.Message);
        Assert.Same(displayMode, viewModel.SelectedDisplayMode);
        Assert.True(viewModel.IsCardsViewMode);
    }
}
