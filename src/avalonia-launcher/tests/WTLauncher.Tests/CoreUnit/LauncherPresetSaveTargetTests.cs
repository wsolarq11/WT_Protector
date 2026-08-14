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
    public void TargetExposesReadOnlyEntryIds()
    {
        var target = new LauncherPresetSaveTarget(["one"]);
        var mutable = Assert.IsAssignableFrom<IList<string>>(target.EntryIds);

        Assert.True(mutable.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutable.Add("two"));
        Assert.Equal(["one"], target.EntryIds);
    }

    [Fact]
    public void TargetRejectsEmptySelection()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new LauncherPresetSaveTarget([]));

        Assert.Equal("Launcher preset save target entry ids are required.", exception.Message);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("one", "one")]
    public void TargetRejectsInvalidEntryIds(params string[] entryIds)
    {
        var exception = Assert.Throws<ArgumentException>(() => new LauncherPresetSaveTarget(entryIds));

        Assert.Equal("entryIds", exception.ParamName);
    }
}
