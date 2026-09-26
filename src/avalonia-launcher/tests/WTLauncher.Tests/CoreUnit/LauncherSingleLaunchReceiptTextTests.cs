using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherSingleLaunchReceiptTextTests
{
    [Fact]
    public void RequestedReceiptRejectsEmptyName()
    {
        using var temp = new TempDirectory();
        var plan = Plan(temp.Path, "pwsh");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherSingleLaunchReceiptText.Requested(" ", plan));

        Assert.Equal("entryName", exception.ParamName);
    }

    private static LaunchPlan Plan(string directory, string command) =>
        LaunchArgumentBuilder.Build(
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = (WTLauncher.Core.DirectoryPath)directory,
                Command = command,
            },
            validateDirectory: true,
            requireConfirmation: false);
}
