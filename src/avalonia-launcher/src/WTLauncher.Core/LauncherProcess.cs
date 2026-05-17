using System.Diagnostics;

namespace WTLauncher.Core;

public sealed class LauncherProcess
{
    public void Start(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        Process.Start(new ProcessStartInfo(plan.Executable)
        {
            Arguments = plan.Arguments,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
        });
    }
}
