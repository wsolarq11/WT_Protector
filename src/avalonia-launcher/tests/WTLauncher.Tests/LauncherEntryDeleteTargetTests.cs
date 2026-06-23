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
        entry = entry with { Id = "changed", Name = "Changed" };

        Assert.Equal("one", target.EntryId);
        Assert.Equal("One", target.EntryName);
    }

    [Fact]
    public void TargetRejectsBlankEntryId()
    {
        var entry = new LaunchEntry { Id = " ", Name = "One" };

        var exception = Assert.Throws<ArgumentException>(() => new LauncherEntryDeleteTarget(entry));

        Assert.Equal("Entry id is required. (Parameter 'entryId')", exception.Message);
    }

    [Fact]
    public void TargetRejectsBlankEntryName()
    {
        var entry = new LaunchEntry { Id = "one", Name = " " };

        var exception = Assert.Throws<InvalidOperationException>(() => new LauncherEntryDeleteTarget(entry));

        Assert.Equal("Launcher entry delete target name is required.", exception.Message);
    }
}
