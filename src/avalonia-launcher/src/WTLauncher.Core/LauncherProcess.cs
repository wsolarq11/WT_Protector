using System.Diagnostics;

namespace WTLauncher.Core;

public sealed class LauncherProcess
{
    /// <summary>
    /// Starts the Windows Terminal process. This call does not impose a timeout —
    /// callers that require bounded wait times should manage timeout themselves
    /// (e.g. via CancellationToken or Task.WaitAsync).
    /// </summary>
    public Process Start(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return Process.Start(new ProcessStartInfo(plan.Executable)
        {
            Arguments = plan.Arguments,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false,
        }) ?? throw new InvalidOperationException($"Failed to start process: {plan.Executable}");
    }
}
