using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryRootConfig_LoadsAndBuildsDryRunPlans()
    {
        var root = FindRepositoryRoot();
        var configPath = Path.Combine(root, "config.json");

        var entries = LauncherConfigStore.Load(configPath);

        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false);
            Assert.Equal("wt.exe", plan.Executable);
            Assert.StartsWith("-d ", plan.Arguments, StringComparison.Ordinal);
            Assert.Contains(" pwsh -NoExit -Command ", plan.Arguments, StringComparison.Ordinal);
            if (LaunchArgumentBuilder.IsDangerousCommand(entry.Command))
                Assert.True(plan.RequiresConfirmation);
        }
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "config.json"))
                && File.Exists(Path.Combine(dir.FullName, "README.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "WT Launcher")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WT_Protector repository root from test output directory.");
    }
}
