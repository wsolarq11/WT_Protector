using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherProcessTests
{
    [Fact]
    public void Start_LaunchesARealProcessThatCanWriteASentinelFile()
    {
        using var temp = new TempDirectory("wt-launcher-process-test");
        const string sentinelFile = "started.txt";
        var sentinelPath = Path.Combine(temp.Path, sentinelFile);
        var plan = new LaunchPlan(
            "cmd.exe",
            $"/c echo started > {sentinelFile}",
            temp.Path,
            "echo started",
            false,
            false);

        new LauncherProcess().Start(plan);

        Assert.True(SpinWait.SpinUntil(() => File.Exists(sentinelPath) && File.ReadAllText(sentinelPath).Trim() == "started", TimeSpan.FromSeconds(5)));
    }
}
