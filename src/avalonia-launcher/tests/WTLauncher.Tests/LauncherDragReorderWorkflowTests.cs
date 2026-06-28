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

        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(null!, [entry], static _ => null));
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(new LauncherSettings(), [entry], null!));
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(new LauncherSettings(), null!, static _ => null));
    }

    [Fact]
    public void BeginCreatesSessionWhenCustomSortModeAndEntryExists()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);

        var result = workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        Assert.True(workflow.HasActiveSession);
        Assert.False(result); // refreshEntries is false when no prior session existed
    }

    [Fact]
    public void BeginDoesNotCreateSessionWhenNotCustomSortMode()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);

        var result = workflow.Begin(isCustomSortMode: false, draggedEntryId: "three");

        Assert.False(workflow.HasActiveSession);
        Assert.False(result);
    }

    [Fact]
    public void BeginReturnsFalseWhenEntryNotFound()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);

        var result = workflow.Begin(isCustomSortMode: true, draggedEntryId: "missing");

        Assert.False(result);
        Assert.False(workflow.HasActiveSession);
    }

    [Fact]
    public void PreviewAndCommitPersistsCustomOrder()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);
        var storePath = Path.Combine(temp.Path, "store.json");
        var capturedSettings = settings;

        var workflow = new LauncherDragReorderWorkflow(settings, entries, s =>
        {
            capturedSettings = s;
            return null; // simulate successful save
        });
        var beginResult = workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");
        Assert.False(beginResult);

        var (accepted, refreshEntries) = workflow.Preview(
            isCustomSortMode: true,
            draggedEntryId: "three",
            targetEntryId: "one",
            insertAfter: false);
        Assert.True(accepted);
        Assert.True(refreshEntries);
        Assert.Equal(["three", "one", "two"], workflow.Settings.CustomEntryOrder!);

        var (status, commitRefresh) = workflow.Commit(isCustomSortMode: true);
        Assert.Equal(LauncherDragReorderStatus.Saved, status);
        Assert.False(commitRefresh);
        Assert.False(workflow.HasActiveSession);

        Assert.Equal(["three", "one", "two"], capturedSettings.CustomEntryOrder);
    }

    [Fact]
    public void PreviewAndCancelRestoresOriginalCustomOrder()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);

        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);
        workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        var (accepted, refreshEntries) = workflow.Preview(
            isCustomSortMode: true,
            draggedEntryId: "three",
            targetEntryId: "one",
            insertAfter: false);
        Assert.True(accepted);
        Assert.True(refreshEntries);
        Assert.Equal(["three", "one", "two"], workflow.Settings.CustomEntryOrder!);

        var (status, cancelRefresh) = workflow.Cancel();
        Assert.Equal(LauncherDragReorderStatus.Canceled, status);
        Assert.True(cancelRefresh);
        Assert.False(workflow.HasActiveSession);
        Assert.Equal(["one", "two", "three"], workflow.Settings.CustomEntryOrder);
    }

    [Fact]
    public void CancelWithoutSessionReturnsNullStatus()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);

        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);

        var (status, refreshEntries) = workflow.Cancel();

        Assert.Null(status);
        Assert.False(refreshEntries);
    }

    [Fact]
    public void CommitWithoutSessionReturnsNullStatus()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);

        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);

        var (status, refreshEntries) = workflow.Commit(isCustomSortMode: true);

        Assert.Null(status);
        Assert.False(refreshEntries);
    }

    [Fact]
    public void CommitInNonCustomSortModeReturnsNullStatus()
    {
        using var temp = new TempDirectory();
        var (settings, entries) = CreateWorkflowFixture(temp);
        var workflow = new LauncherDragReorderWorkflow(settings, entries, SaveNoop);
        workflow.Begin(isCustomSortMode: true, draggedEntryId: "three");

        var (status, refreshEntries) = workflow.Commit(isCustomSortMode: false);

        Assert.True(workflow.HasActiveSession);
        Assert.Null(status);
        Assert.False(refreshEntries);
    }

    private static string? SaveNoop(LauncherSettings settings) => null;

    private static (LauncherSettings Settings, IReadOnlyList<LauncherEntryViewModel> Entries) CreateWorkflowFixture(TempDirectory temp)
    {
        var one = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "one");
        var two = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "two");
        var three = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "three");
        var settings = new LauncherSettings
        {
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            CustomEntryOrder = ["one", "two", "three"],
        };
        return (settings, [one, two, three]);
    }
}
