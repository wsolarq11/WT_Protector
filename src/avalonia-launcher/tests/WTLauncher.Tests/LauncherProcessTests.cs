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
        Assert.Equal(0, process.ExitCode);
        Assert.True(File.Exists(Path.Combine(temp.Path, sentinelFile)));
        Assert.Equal("started", File.ReadAllText(Path.Combine(temp.Path, sentinelFile)).Trim());
    }

    [Fact]
    public void Start_ProcessWithNonZeroExitCode_DoesNotThrow()
    {
        var plan = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new LaunchPlan("cmd.exe", "/c exit 1", Environment.CurrentDirectory, "exit 1", false)
            : new LaunchPlan("/bin/sh", "-c \"exit 1\"", Environment.CurrentDirectory, "exit 1", false);

        using var process = new LauncherProcess().Start(plan);

        Assert.True(process.WaitForExit(5000), "expected launched process to exit before timeout");
        Assert.Equal(1, process.ExitCode);
    }

    [Fact]
    public void Start_ProcessTimeout_CanBeKilled()
    {
        var plan = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new LaunchPlan("cmd.exe", "/c ping 127.0.0.1 -n 60 > nul", Environment.CurrentDirectory, "sleep", false)
            : new LaunchPlan("/bin/sh", "-c \"sleep 60\"", Environment.CurrentDirectory, "sleep", false);

        using var process = new LauncherProcess().Start(plan);

        Assert.False(process.WaitForExit(300), "expected process to still be running after short wait");
        Assert.False(process.HasExited);
        process.Kill(entireProcessTree: true);
        Assert.True(process.WaitForExit(3000), "expected process to exit after kill");
    }
}
