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
        entry.Id = "broken";
        entry.Name = "";

        Assert.Equal("one", confirmation.EntryId);
        Assert.Equal("One", confirmation.EntryName);
        Assert.Same(plan, confirmation.Plan);
    }

    [Theory]
    [InlineData(" ", "One", "Launcher single launch confirmation entry id is required.")]
    [InlineData("one", " ", "Launcher single launch confirmation entry name is required.")]
    public void ConfirmationRejectsBlankEntryFacts(string id, string name, string expected)
    {
        var entry = new LaunchEntry
        {
            Id = id,
            Name = name,
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new LauncherSingleLaunchConfirmation(entry, Plan("codex --version")));

        Assert.Equal(expected, exception.Message);
    }

    private static LaunchPlan Plan(string command) =>
        new(
            LaunchArgumentBuilder.TerminalExecutable,
            $"-d . {command}",
            ".",
            command,
            RequiresConfirmation: true);
}
