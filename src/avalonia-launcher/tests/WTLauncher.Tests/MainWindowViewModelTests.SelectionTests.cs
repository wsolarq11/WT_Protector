using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void BatchSelectionCommandsPersistPortableConfig()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(0, viewModel.SelectedCount);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(LauncherStore.Load(config).Entries, entry => Assert.True(entry.Selected));
        Assert.All(LauncherStore.Load(config + ".bak").Entries, entry => Assert.False(entry.Selected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(LauncherStore.Load(config).Entries, entry => Assert.False(entry.Selected));
        Assert.All(LauncherStore.Load(config + ".bak").Entries, entry => Assert.True(entry.Selected));
    }

    [Fact]
    public void SelectVisibleReportsWhenVisibleEntriesAreAlreadySelected()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: true, twoSelected: true);

        var viewModel = CreateViewModel(config, settings);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal("可见项已全部勾选", viewModel.StatusText);
        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(viewModel.Entries, entry => Assert.True(entry.IsSelected));
    }

    [Fact]
    public void SelectionChangesRollBackWhenConfigSaveFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(config, settings);
        File.Delete(config);
        Directory.CreateDirectory(config);

        viewModel.Entries.Single(entry => entry.Id == "one").IsSelected = true;

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(viewModel.Entries, entry => Assert.False(entry.IsSelected));
        Assert.Contains("存储保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public void SelectVisibleRollsBackFileAndStatusWhenConfigBackupFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(config, settings);
        // Force save failure by replacing the config file with a directory.
        // C6 fix wraps backup in try/catch, so we block the main file path.
        File.Delete(config);
        Directory.CreateDirectory(config);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(viewModel.Entries, entry => Assert.False(entry.IsSelected));
        Assert.Contains("存储保存失败", viewModel.StatusText);
    }

    [Fact]
    public void ClearSelectionRollsBackFileAndStatusWhenConfigBackupFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: true, twoSelected: true);

        var viewModel = CreateViewModel(config, settings);
        // Force save failure by replacing the config file with a directory.
        // C6 fix wraps backup in try/catch, so we block the main file path.
        File.Delete(config);
        Directory.CreateDirectory(config);

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(viewModel.Entries, entry => Assert.True(entry.IsSelected));
        Assert.Contains("存储保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public void SelectionChangeDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.Entries.Single(entry => entry.Id == "two").IsSelected = true;

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void SelectVisibleOnlySelectsFilteredEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: false),
            Entry("two", "Two", command: "opencode", selected: false),
            Entry("three", "Three", command: "someexe", selected: false));

        var viewModel = CreateViewModel(config, settings);

        // Set filter to show only 2 of 3 entries
        viewModel.FilterText = "code";

        Assert.Equal(2, viewModel.VisibleCount);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "one").IsSelected);
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "two").IsSelected);
        Assert.False(viewModel.Entries.Single(entry => entry.Id == "three").IsSelected);
    }

    [Fact]
    public async Task BatchLaunchWithDuplicateEquivalentEntryIdsDoesNotCrash()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        // Inject a duplicate-entry-ID scenario that bypasses Save/Load validation.
        // The LauncherConfigStore enforces unique IDs on both paths, so the only way
        // to reproduce the ToDictionary crash is via direct collection manipulation.
        var listField = typeof(MainWindowViewModel).GetField(
            "_list",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(listField);
        var list = listField!.GetValue(viewModel)!;
        var entriesProp = list.GetType().GetProperty(
            "MutableEntries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(entriesProp);
        var entries = (System.Collections.ObjectModel.ObservableCollection<LauncherEntryViewModel>)
            entriesProp!.GetValue(list)!;
        var duplicate = new LauncherEntryViewModel(
            Entry("one", directory: temp.Path, command: "opencode", selected: true),
            viewModel);
        entries.Add(duplicate);
        // Refresh the VisibleEntries projection so the duplicate is visible.
        typeof(MainWindowViewModel).GetMethod(
            "ApplyFilter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(viewModel, null);
        Assert.Equal(2, viewModel.VisibleEntries.Count(entry => entry.Id == "one"));

        // Should not throw ArgumentException from ToDictionary — the TryAdd
        // dictionary build handles duplicates gracefully.
        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.DryRunText);
        Assert.DoesNotContain("ArgumentException", viewModel.DryRunText);
    }
}
