using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherDragReorderWorkflowTests
{
    [Fact]
    public void WorkflowRejectsMissingConstructorDependencies()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var entry = owner.Entries.Single();
        var settingsPath = Path.Combine(temp.Path, "settings.json");

        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(null!, settingsPath, [entry]));
        var settingsPathException = Assert.Throws<ArgumentException>(() =>
            new LauncherDragReorderWorkflow(new LauncherSettings(), " ", [entry]));
        Assert.Equal("settingsPath", settingsPathException.ParamName);
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(new LauncherSettings(), settingsPath, null!));
    }

    [Fact]
    public void BeginCreatesSessionWhenCustomSortModeAndEntryExists()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);

        var result = workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        Assert.True(workflow.HasActiveSession);
        Assert.False(result); // refreshEntries is false when no prior session existed
    }

    [Fact]
    public void BeginDoesNotCreateSessionWhenNotCustomSortMode()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);

        var result = workflow.Begin(isCustomSortMode: false, draggedEntryId: "three");

        Assert.False(workflow.HasActiveSession);
        Assert.False(result);
    }

    [Fact]
    public void BeginReturnsFalseWhenEntryNotFound()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);

        var result = workflow.Begin(isCustomSortMode: true, draggedEntryId: "missing");

        Assert.False(result);
        Assert.False(workflow.HasActiveSession);
    }

    [Fact]
    public void PreviewAndCommitPersistsCustomOrder()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);
        LauncherSettingsStore.Save(settings, settingsPath);

        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);
        workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        var (accepted, refreshEntries) = workflow.Preview(
            isCustomSortMode: true,
            draggedEntryId: "three",
            targetEntryId: "one",
            insertAfter: false);
        Assert.True(accepted);
        Assert.True(refreshEntries);
        Assert.Equal(["three", "one", "two"], settings.CustomEntryOrder);

        var (status, commitRefresh) = workflow.Commit(isCustomSortMode: true);
        Assert.Equal(LauncherDragReorderStatus.Saved, status);
        Assert.False(commitRefresh);
        Assert.False(workflow.HasActiveSession);

        var savedSettings = LauncherSettingsStore.Load(settingsPath);
        Assert.Equal(["three", "one", "two"], savedSettings.CustomEntryOrder);
    }

    [Fact]
    public void PreviewAndCancelRestoresOriginalCustomOrder()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);

        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);
        workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        var (accepted, refreshEntries) = workflow.Preview(
            isCustomSortMode: true,
            draggedEntryId: "three",
            targetEntryId: "one",
            insertAfter: false);
        Assert.True(accepted);
        Assert.True(refreshEntries);
        Assert.Equal(["three", "one", "two"], settings.CustomEntryOrder);

        var (status, cancelRefresh) = workflow.Cancel();
        Assert.Equal(LauncherDragReorderStatus.Canceled, status);
        Assert.True(cancelRefresh);
        Assert.False(workflow.HasActiveSession);
        Assert.Equal(["one", "two", "three"], settings.CustomEntryOrder);
    }

    [Fact]
    public void CancelWithoutSessionReturnsNullStatus()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);

        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);

        var (status, refreshEntries) = workflow.Cancel();

        Assert.Null(status);
        Assert.False(refreshEntries);
    }

    [Fact]
    public void CommitWithoutSessionReturnsNullStatus()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);

        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);

        var (status, refreshEntries) = workflow.Commit(isCustomSortMode: true);

        Assert.Null(status);
        Assert.False(refreshEntries);
    }

    [Fact]
    public void CommitInNonCustomSortModeReturnsNullStatus()
    {
        using var temp = new TempDirectory();
        var (settings, settingsPath, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, settingsPath, entries);
        workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        var (status, refreshEntries) = workflow.Commit(isCustomSortMode: false);

        Assert.Null(status);
        Assert.False(refreshEntries);
    }

    private static (LauncherSettings Settings, string SettingsPath, IReadOnlyList<LauncherEntryViewModel> Entries) CreateWorkflowFixture(TempDirectory temp)
    {
        var one = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "one");
        var two = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "two");
        var three = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "three");
        var settings = new LauncherSettings
        {
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            CustomEntryOrder = ["one", "two", "three"],
        };
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        return (settings, settingsPath, [one, two, three]);
    }
}
