using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSingleLaunchConfirmationTests
{
    [Fact]
    public void ConfirmationSnapshotsNormalizedEntryIdNameAndPlan()
    {
        var entry = new LaunchEntry
        {
            Id = " one ",
            Name = " One ",
        };
        var plan = Plan("codex --version");

        var confirmation = new LauncherSingleLaunchConfirmation(entry, plan);
        entry = entry with { Id = "broken", Name = "" };

        Assert.Equal("one", confirmation.EntryId);
        Assert.Equal("One", confirmation.EntryName);
        Assert.Same(plan, confirmation.Plan);
    }

    [Fact]
    public void ConfirmationRejectsBlankEntryId()
    {
        var entry = new LaunchEntry { Id = " ", Name = "One" };

        var exception = Assert.Throws<ArgumentException>(() =>
            new LauncherSingleLaunchConfirmation(entry, Plan("codex --version")));

        Assert.Contains("不能为空", exception.Message);
        Assert.Equal("Id", exception.ParamName);
    }

    [Fact]
    public void ConfirmationRejectsBlankEntryName()
    {
        var entry = new LaunchEntry { Id = "one", Name = " " };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new LauncherSingleLaunchConfirmation(entry, Plan("codex --version")));

        Assert.Equal("Entry name is required.", exception.Message);
    }

    private static LaunchPlan Plan(string command) =>
        new(
            LaunchArgumentBuilder.TerminalExecutable,
            $"-d . {command}",
            ".",
            command,
            RequiresConfirmation: true);
}
