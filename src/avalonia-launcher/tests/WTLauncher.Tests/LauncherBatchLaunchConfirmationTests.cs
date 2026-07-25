using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherBatchLaunchConfirmationTests
{
    [Fact]
    public void ConfirmationSnapshotsItemsAndTargetLabel()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var preflight = workflow.PreflightBatch([Entry("one", temp.Path)], requireConfirmation: true);
        var items = preflight.Items.ToList();

        var confirmation = new LauncherBatchLaunchConfirmation(items, " 已勾选项 ");
        items.Clear();

        Assert.Equal("已勾选项", confirmation.TargetLabel);
        Assert.Equal("one", Assert.Single(confirmation.Items).EntryName);
    }

    [Fact]
    public void ConfirmationItemsDoNotExposeMutableStorage()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var preflight = workflow.PreflightBatch([Entry("one", temp.Path)], requireConfirmation: true);
        var confirmation = new LauncherBatchLaunchConfirmation(preflight.Items, "已勾选项");

        var items = Assert.IsAssignableFrom<IList<LauncherBatchLaunchItem>>(confirmation.Items);
        Assert.True(items.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => items.Add(Assert.Single(confirmation.Items)));
        Assert.Single(confirmation.Items);
    }

    [Fact]
    public void ConfirmationRejectsBlankTargetLabel()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var preflight = workflow.PreflightBatch([Entry("one", temp.Path)], requireConfirmation: true);

        var exception = Assert.Throws<ArgumentException>(() =>
            new LauncherBatchLaunchConfirmation(preflight.Items, " "));

        Assert.Equal("targetLabel", exception.ParamName);
    }

    private static LaunchEntry Entry(string id, string directory) =>
        new()
        {
            Id = id,
            Name = id,
            Directory = (WTLauncher.Core.DirectoryPath)directory,
            Command = "pwsh",
        };
}

