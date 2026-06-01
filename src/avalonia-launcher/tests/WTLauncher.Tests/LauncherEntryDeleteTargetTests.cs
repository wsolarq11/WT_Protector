using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryDeleteTargetTests
{
    [Fact]
    public void TargetSnapshotsNormalizedEntryIdAndName()
    {
        var entry = new LaunchEntry
        {
            Id = " one ",
            Name = " One ",
        };

        var target = new LauncherEntryDeleteTarget(entry);
        entry.Id = "changed";
        entry.Name = "Changed";

        Assert.Equal("one", target.EntryId);
        Assert.Equal("One", target.EntryName);
    }

    [Theory]
    [InlineData(" ", "One", "Launcher entry delete target id is required.")]
    [InlineData("one", " ", "Launcher entry delete target name is required.")]
    public void TargetRejectsBlankEntryFacts(string id, string name, string expected)
    {
        var entry = new LaunchEntry
        {
            Id = id,
            Name = name,
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new LauncherEntryDeleteTarget(entry));

        Assert.Equal(expected, exception.Message);
    }
}
