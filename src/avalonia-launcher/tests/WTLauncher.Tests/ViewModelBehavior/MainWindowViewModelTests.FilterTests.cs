using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void ConstructorAppliesSavedFilterWithoutPersistingSettings()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { LastFilterText = "CODEX" });

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
        Assert.Equal("CODEX", viewModel.FilterText);
        // CFA Step 1c: constructor persists synced CustomEntryOrder (includes full Settings).
        // LastFilterText is "CODEX" on disk because CurrentSettings carries it.
        Assert.Equal("CODEX", LauncherStore.Load(settings).Settings.LastFilterText);
    }

    [Fact]
    public void ConstructorRejectsNullSavedFilterInsteadOfInventingEmptyFilter()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var exception = Assert.Throws<InvalidDataException>(() =>
            CreateViewModel(
                config,
                settings,
                new LauncherSettings { LastFilterText = null! }));

        Assert.Equal("Launcher settings lastFilterText is null.", exception.Message);
        // Store file exists from fixture but original settings are preserved (not overwritten).
        Assert.Equal("", LauncherStore.Load(settings).Settings.LastFilterText);
    }

    [Fact]
    public void FilterTextPersistsPortableSettingsAndFiltersVisibleEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.FilterText = "CODEX";

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
        Assert.Equal("CODEX", LauncherStore.Load(settings).Settings.LastFilterText);

        viewModel.FilterText = "D:\\TWO";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("two", visible.Id);
        Assert.Equal("D:\\TWO", LauncherStore.Load(settings).Settings.LastFilterText);

        viewModel.FilterText = "ONE";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
        Assert.Equal("ONE", LauncherStore.Load(settings).Settings.LastFilterText);

        viewModel.FilterText = "   ";

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal("", LauncherStore.Load(settings).Settings.LastFilterText);

        viewModel.FilterText = "missing";

        Assert.Empty(viewModel.VisibleEntries);
        Assert.Equal(0, viewModel.VisibleCount);
        Assert.Equal("missing", LauncherStore.Load(settings).Settings.LastFilterText);
    }

    [Fact]
    public void FilterTextRejectsNullMutationInsteadOfInventingEmptyFilter()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);

        viewModel.FilterText = "CODEX";

        var exception = Assert.Throws<ArgumentNullException>(() => viewModel.FilterText = null!);

        Assert.Equal("value", exception.ParamName);
        Assert.Equal("CODEX", viewModel.FilterText);
        Assert.Equal("CODEX", LauncherStore.Load(settings).Settings.LastFilterText);
        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
    }

    [Fact]
    public void FilterTextDoesNotStackOverflowWhenSaveFailsRepeatedly()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);
        // Make settings path a directory so LauncherSettingsStore.Save always fails
        File.Delete(settings);
        Directory.CreateDirectory(settings);
        // Rapid filter changes should NOT stack overflow (H1 guard)
        for (var i = 0; i < 10; i++)
            viewModel.FilterText = $"filter-{i}";
        // After all failed saves, filter should be back to the last successful value
        Assert.Equal("", viewModel.FilterText);
    }

    [Fact]
    public void FilterMutation_SyncsVisibleCountGroupsAndEmptyState()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp,
            Entry("one", command: "codex", selected: true),
            Entry("two", command: "opencode", selected: false),
            Entry("three", command: "codex", selected: false));
        var viewModel = CreateViewModel(config, settings);
        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Command;

        viewModel.FilterText = "codex";

        Assert.Equal(2, viewModel.VisibleCount);
        Assert.False(viewModel.IsVisibleEmpty);
        Assert.Equal(2, viewModel.VisibleGroups.Sum(group => group.Count));
        Assert.Equal(1, viewModel.SelectedCount);

        viewModel.FilterText = "missing";

        Assert.Equal(0, viewModel.VisibleCount);
        Assert.True(viewModel.IsVisibleEmpty);
        Assert.Empty(viewModel.VisibleGroups);
        Assert.Equal(1, viewModel.SelectedCount);
    }

    [Fact]
    public void StoreReload_SyncsVisibleProjectionsAndGroupsInOnePass()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp,
            Entry("one", command: "codex", selected: true),
            Entry("two", command: "opencode", selected: false));
        var viewModel = CreateViewModel(config, settings);
        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Command;

        var store = LauncherStore.Load(config) with
        {
            Entries =
            [
                Entry("one", command: "codex", selected: true),
                Entry("two", command: "codex", selected: false),
            ],
            Settings = LauncherStore.Load(config).Settings with { LastFilterText = "codex" },
        };
        LauncherStore.Save(store, config, backup: false);
        viewModel.RefreshCommand.Execute(null);

        Assert.Equal(2, viewModel.VisibleCount);
        Assert.False(viewModel.IsVisibleEmpty);
        var group = Assert.Single(viewModel.VisibleGroups);
        Assert.Equal(2, group.Count);
        Assert.Equal(1, viewModel.SelectedCount);
    }

    [Fact]
    public void ClearVisibleSelectionOnlyClearsFilteredEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode", selected: true),
            Entry("three", "Three", command: "someexe", selected: true));

        var viewModel = CreateViewModel(config, settings);

        // All 3 entries are selected initially
        Assert.Equal(3, viewModel.SelectedCount);

        // Filter to show only 2 of 3 entries
        viewModel.FilterText = "code";

        Assert.Equal(2, viewModel.VisibleCount);

        // Clear selection only on visible entries
        viewModel.ClearVisibleSelection();

        // Visible entries are deselected; hidden entry stays selected
        Assert.False(viewModel.Entries.Single(entry => entry.Id == "one").IsSelected);
        Assert.False(viewModel.Entries.Single(entry => entry.Id == "two").IsSelected);
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "three").IsSelected);
        Assert.Equal(1, viewModel.SelectedCount);
    }
}
