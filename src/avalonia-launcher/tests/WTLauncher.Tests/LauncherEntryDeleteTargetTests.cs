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
        var entry = new LaunchEntry { Id = EntryId.Empty, Name = "One" };

        var exception = Assert.Throws<ArgumentException>(() => new LauncherEntryDeleteTarget(entry));

        Assert.Contains("不能为空", exception.Message);
        Assert.Equal("Id", exception.ParamName);
    }

    [Fact]
    public void TargetRejectsBlankEntryName()
    {
        var entry = new LaunchEntry { Id = "one", Name = EntryName.Empty };

        var exception = Assert.Throws<InvalidOperationException>(() => new LauncherEntryDeleteTarget(entry));

        Assert.Equal("Entry name is required.", exception.Message);
    }
}
