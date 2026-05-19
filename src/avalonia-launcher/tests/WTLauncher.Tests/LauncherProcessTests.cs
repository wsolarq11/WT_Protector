using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherProcessTests
{
    [Fact]
    public void Start_LaunchesARealProcessThatCanWriteASentinelFile()
    {
        using var temp = new TempDirectory("wt-launcher-process-test");
        const string sentinelFile = "started.txt";
        const string tempSentinelFile = "started.tmp";
        var plan = new LaunchPlan(
            "cmd.exe",
            $"/c echo started > {tempSentinelFile} && move /y {tempSentinelFile} {sentinelFile} > nul",
            temp.Path,
            "echo started",
            false);

        new LauncherProcess().Start(plan);

        Assert.True(SpinWait.SpinUntil(() => File.Exists(Path.Combine(temp.Path, sentinelFile)), TimeSpan.FromSeconds(5)));
        Assert.Equal("started", File.ReadAllText(Path.Combine(temp.Path, sentinelFile)).Trim());
    }
}
