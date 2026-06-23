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
        Assert.Equal(LauncherEntryViewModes.DetailsId, viewModel.SelectedDisplayMode?.Id);
    }

    [Fact]
    public void SortModeUsesSelectedModeAsOnlyCustomSortState()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsCustomSortMode))?.SetMethod);
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.Name.Contains("CustomSort", StringComparison.OrdinalIgnoreCase));

        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = LauncherEntryVisibility.CustomSortModeId });

        Assert.True(viewModel.IsCustomSortMode);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == LauncherEntryVisibility.AlphabeticalSortModeId);

        Assert.False(viewModel.IsCustomSortMode);
    }

    [Fact]
    public void EntryViewModePersistsAndOverridesCardDensityForDetailedScanning()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateSingleEntryFixture(temp);

        var viewModel = CreateViewModel(config, settings);
        var entry = Assert.Single(viewModel.Entries);

        entry.CardWidth = LauncherEntryViewModel.MicroLayoutMaxWidth;
        entry.CardHeight = LauncherEntryViewModel.MicroLayoutMaxHeight;
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
        Assert.Equal(LauncherEntryViewModes.DetailsId, LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.NamesId);

        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.ScanningTitleFontSize, entry.TitleFontSize);
        Assert.Equal(LauncherEntryViewModes.NamesId, LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == LauncherEntryViewModes.CardsId);

        Assert.False(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.MicroTitleFontSize, entry.TitleFontSize);
        Assert.Equal(LauncherEntryViewModes.CardsId, LauncherSettingsStore.Load(settings).EntryViewMode);
    }

    [Fact]
    public void EntryViewModeUsesOneTypedStateSource()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsCardsViewMode))?.SetMethod);
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsDetailedListMode))?.SetMethod);
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsNamesViewMode))?.SetMethod);
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.Name.Contains("Refreshing", StringComparison.OrdinalIgnoreCase));

        using var temp = new TempDirectory();
        var (config, settings, _) = CreateSingleEntryFixture(temp);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { EntryViewMode = LauncherEntryViewModes.DetailsId });

        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, viewModel.DisplayModes.Select(mode => mode.Label));
        Assert.Equal(LauncherEntryViewMode.Details, viewModel.SelectedDisplayMode?.Mode);
        Assert.Equal(LauncherEntryViewModes.DetailsId, viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(
            1,
            new[] { viewModel.IsCardsViewMode, viewModel.IsDetailedListMode, viewModel.IsNamesViewMode }.Count(value => value));
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
