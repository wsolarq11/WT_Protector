using System.Diagnostics;

namespace WTLauncher.Core;

public sealed class LauncherProcess
{
    public Process Start(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return Process.Start(new ProcessStartInfo(plan.Executable)
        {
            Arguments = plan.Arguments,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException($"Failed to start process: {plan.Executable}");
    }
}
