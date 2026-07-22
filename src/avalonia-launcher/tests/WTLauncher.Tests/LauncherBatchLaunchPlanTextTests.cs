using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherBatchLaunchPlanTextTests
{
    [Fact]
    public void FormatUsesPreflightNameSnapshotWhenEntryNameMutates()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var entry = Entry("one", temp.Path);
        var preflight = workflow.PreflightBatch([entry], requireConfirmation: true);
        entry = entry with { Name = EntryName.Empty };

        var text = LauncherBatchLaunchPlanText.Format(preflight.Items);

        Assert.Contains("• one", text);
        Assert.Contains(LaunchArgumentBuilder.TerminalExecutable, text);
        Assert.DoesNotContain("• " + Environment.NewLine, text);
    }

    private static LaunchEntry Entry(string id, string directory) =>
        new()
        {
            Id = id,
            Name = id,
            Directory = (WTLauncher.Core.ValueObjects.DirectoryPath)directory,
            Command = "pwsh",
        };
}

