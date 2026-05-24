using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntriesTests
{
    [Fact]
    public void CloneCopiesEntryValuesWithoutSharingEntryInstance()
    {
        var original = new LaunchEntry
        {
            Id = "one",
            Name = "One",
            Directory = "D:\\one",
            Command = "pwsh",
            Confirm = true,
            Selected = true,
            UpdatedAtUtc = "2026-05-20T00:00:00Z",
        };

        var clone = LauncherEntries.Clone(original);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Directory, clone.Directory);
        Assert.Equal(original.Command, clone.Command);
        Assert.Equal(original.Confirm, clone.Confirm);
        Assert.Equal(original.Selected, clone.Selected);
        Assert.Equal(original.UpdatedAtUtc, clone.UpdatedAtUtc);
    }
}
