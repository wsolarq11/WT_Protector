using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
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
        entry = entry with { Id = "broken", Name = EntryName.Empty };

        Assert.Equal("one", confirmation.EntryId);
        Assert.Equal("One", confirmation.EntryName);
        Assert.Same(plan, confirmation.Plan);
    }

    [Fact]
    public void ConfirmationRejectsBlankEntryId()
    {
        var entry = new LaunchEntry { Id = EntryId.Empty, Name = "One" };

        var exception = Assert.Throws<ArgumentException>(() =>
            new LauncherSingleLaunchConfirmation(entry, Plan("codex --version")));

        Assert.Contains("不能为空", exception.Message);
        Assert.Equal("Id", exception.ParamName);
    }

    [Fact]
    public void ConfirmationRejectsBlankEntryName()
    {
        var entry = new LaunchEntry { Id = "one", Name = EntryName.Empty };

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
