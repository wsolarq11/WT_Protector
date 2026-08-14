using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherDirectoryHealthTests
{
    [Fact]
    public void IsMissing_ExistingDirectory_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var entry = Entry(temp.Path);
        Assert.False(LauncherDirectoryHealth.IsMissing(entry));
    }

    [Fact]
    public void IsMissing_MissingDirectory_ReturnsTrue()
    {
        using var temp = new TempDirectory();
        var missing = System.IO.Path.Combine(temp.Path, "missing");
        Assert.True(LauncherDirectoryHealth.IsMissing(Entry(missing)));
    }

    [Fact]
    public void CountMissing_CountsOnlyMissingEntries()
    {
        using var temp = new TempDirectory();
        var existing = Entry(temp.Path);
        var missing = Entry(System.IO.Path.Combine(temp.Path, "missing"));
        Assert.Equal(1, LauncherDirectoryHealth.CountMissing([existing, missing]));
    }

    private static LaunchEntry Entry(string directory) => new()
    {
        Id = "e",
        Name = "E",
        Directory = (DirectoryPath)directory,
        Command = "pwsh",
    };
}