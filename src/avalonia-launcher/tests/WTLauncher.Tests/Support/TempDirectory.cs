using IOPath = System.IO.Path;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory(string? prefix = null)
    {
        Path = IOPath.Combine(
            IOPath.GetTempPath(),
            $"{prefix ?? "wt-launcher-test"}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
            return;

        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed class FakeLaunchProcessStarter : ILaunchProcessStarter
{
    private readonly Action<LaunchPlan>? _start;

    public FakeLaunchProcessStarter(Action<LaunchPlan>? start = null)
    {
        _start = start;
    }

    public List<LaunchPlan> StartedPlans { get; } = [];

    public void Start(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (_start is not null)
        {
            _start(plan);
            return;
        }

        StartedPlans.Add(plan);
    }
}
