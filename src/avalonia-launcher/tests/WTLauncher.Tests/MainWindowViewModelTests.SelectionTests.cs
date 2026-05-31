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
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.True(entry.Selected));
        Assert.All(LauncherConfigStore.Load(config + ".bak"), entry => Assert.False(entry.Selected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.False(entry.Selected));
        Assert.All(LauncherConfigStore.Load(config + ".bak"), entry => Assert.True(entry.Selected));
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

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void SelectionRollbackUsesTypedSnapshot()
    {
        Assert.Null(typeof(MainWindowViewModel).GetMethod("CaptureSelectionSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
        Assert.Null(typeof(MainWindowViewModel).GetMethod("RestoreSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
        Assert.Contains(
            typeof(LauncherSelectionMutationSnapshot).GetMethods(),
            method => method.Name == nameof(LauncherSelectionMutationSnapshot.Capture));
    }
}
