using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherPresetDeleteTargetTests
{
    [Fact]
    public void TargetRejectsBlankPresetId()
    {
        var preset = new LauncherPreset
        {
            Id = " ",
            Name = "Daily",
            EntryIds = ["one"],
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new LauncherPresetDeleteTarget(preset.Id, preset.Name, preset.EntryIds));

        Assert.Equal("Launcher preset delete target id is required.", exception.Message);
    }
}
