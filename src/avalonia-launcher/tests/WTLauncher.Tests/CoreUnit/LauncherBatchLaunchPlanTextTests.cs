using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
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

    [Fact]
    public void Format_EmptyList_ReturnsEmptyString()
    {
        var text = LauncherBatchLaunchPlanText.Format([]);

        Assert.Equal("", text);
    }

    [Fact]
    public void Format_SingleItem_ContainsEntryNameAndCommandLine()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var entry = Entry("alpha", temp.Path);
        var preflight = workflow.PreflightBatch([entry], requireConfirmation: true);

        var text = LauncherBatchLaunchPlanText.Format(preflight.Items);

        Assert.Contains("• alpha", text);
        Assert.Contains(LaunchArgumentBuilder.TerminalExecutable, text);
        Assert.Contains("（实际:", text);
    }

    [Fact]
    public void Format_MultipleItems_SeparatesEachWithNewline()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var entry1 = Entry("first", temp.Path);
        var entry2 = Entry("second", temp.Path);
        var preflight = workflow.PreflightBatch([entry1, entry2], requireConfirmation: true);

        var text = LauncherBatchLaunchPlanText.Format(preflight.Items);

        Assert.Contains("• first", text);
        Assert.Contains("• second", text);
        Assert.Contains(LaunchArgumentBuilder.TerminalExecutable, text);
        // Each item block ends with （实际: ...）, so two items have two occurrences
        Assert.Equal(2, CountOccurrences(text, "（实际:"));
    }

    private static LaunchEntry Entry(string id, string directory) =>
        new()
        {
            Id = id,
            Name = id,
            Directory = (WTLauncher.Core.DirectoryPath)directory,
            Command = "pwsh",
        };

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}

