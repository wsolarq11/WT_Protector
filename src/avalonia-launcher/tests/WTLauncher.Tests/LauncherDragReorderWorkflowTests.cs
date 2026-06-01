using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherDragReorderWorkflowTests
{
    [Fact]
    public void WorkflowRejectsMissingConstructorDependencies()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var entry = owner.Entries.Single();
        var settingsPath = Path.Combine(temp.Path, "settings.json");

        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(null!, settingsPath, [entry]));
        var settingsPathException = Assert.Throws<ArgumentException>(() =>
            new LauncherDragReorderWorkflow(new LauncherSettings(), " ", [entry]));
        Assert.Equal("settingsPath", settingsPathException.ParamName);
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherDragReorderWorkflow(new LauncherSettings(), settingsPath, null!));
    }
}
