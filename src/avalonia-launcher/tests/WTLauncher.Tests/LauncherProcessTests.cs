using System.Runtime.InteropServices;
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
        var plan = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new LaunchPlan(
                "cmd.exe",
                $"/c echo started > {tempSentinelFile} && move /y {tempSentinelFile} {sentinelFile} > nul",
                temp.Path,
                "echo started",
                false)
            : new LaunchPlan(
                "/bin/sh",
                $"-c \"printf started > {tempSentinelFile} && mv {tempSentinelFile} {sentinelFile}\"",
                temp.Path,
                "printf started",
                false);

        using var process = new LauncherProcess().Start(plan);

        Assert.True(process.WaitForExit(5000), "expected launched process to exit before temp cleanup");
        Assert.True(File.Exists(Path.Combine(temp.Path, sentinelFile)));
        Assert.Equal("started", File.ReadAllText(Path.Combine(temp.Path, sentinelFile)).Trim());
    }
}
