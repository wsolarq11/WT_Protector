using WTLauncher.App.ViewModels;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherPresetSaveTargetTests
{
    [Fact]
    public void TargetSnapshotsSelectedEntryIds()
    {
        var entryIds = new List<string> { " one ", "two" };

        var target = new LauncherPresetSaveTarget(entryIds);
        entryIds[0] = "broken";
        entryIds.Add("three");

        Assert.Equal(["one", "two"], target.EntryIds);
        Assert.Equal(2, target.EntryCount);
    }

    [Fact]
    public void TargetRejectsEmptySelection()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new LauncherPresetSaveTarget([]));

        Assert.Equal("Launcher preset save target entry ids are required.", exception.Message);
    }
}
