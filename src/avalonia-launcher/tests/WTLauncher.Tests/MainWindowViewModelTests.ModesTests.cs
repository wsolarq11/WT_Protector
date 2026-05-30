using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void SavedUnknownModesFallbackToDefaultModes()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "missing", EntryViewMode = "missing" });

        Assert.Equal("custom", viewModel.SelectedSortMode?.Id);
        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, viewModel.DisplayModes.Select(mode => mode.Label));
        Assert.Equal(LauncherEntryViewMode.Cards, viewModel.SelectedDisplayMode?.Mode);
        Assert.Equal("cards", viewModel.SelectedDisplayMode?.Id);
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
            new LauncherSettings { SortMode = "custom" });

        Assert.True(viewModel.IsCustomSortMode);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");

        Assert.False(viewModel.IsCustomSortMode);
    }

    [Fact]
    public void EntryViewModePersistsAndOverridesCardDensityForDetailedScanning()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateSingleEntryFixture(temp);

        var viewModel = CreateViewModel(config, settings);
        var entry = Assert.Single(viewModel.Entries);

        entry.CardWidth = 210;
        entry.CardHeight = 90;
        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "details");

        Assert.True(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(13, entry.TitleFontSize);
        Assert.Equal("details", LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "names");

        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(13, entry.TitleFontSize);
        Assert.Equal("names", LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "cards");

        Assert.False(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);
        Assert.Equal("cards", LauncherSettingsStore.Load(settings).EntryViewMode);
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
            new LauncherSettings { EntryViewMode = "details" });

        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, viewModel.DisplayModes.Select(mode => mode.Label));
        Assert.Equal(LauncherEntryViewMode.Details, viewModel.SelectedDisplayMode?.Mode);
        Assert.Equal("details", viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(
            1,
            new[] { viewModel.IsCardsViewMode, viewModel.IsDetailedListMode, viewModel.IsNamesViewMode }.Count(value => value));
    }
}
