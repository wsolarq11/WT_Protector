using IOPath = System.IO.Path;
using WTLauncher.App.Features.Dialogs;
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

internal sealed class FakeCommandEditApplier : ICommandEditApplier
{
    private readonly Func<CommandManagementEdit, CommandManagementResult>? _apply;

    public FakeCommandEditApplier(Func<CommandManagementEdit, CommandManagementResult>? apply = null)
    {
        _apply = apply;
    }

    public CommandManagementResult Apply(CommandManagementEdit edit) =>
        _apply?.Invoke(edit) ?? throw new InvalidOperationException("FakeCommandEditApplier has no apply delegate.");
}

internal sealed class FakeDirectoryEditApplier : IDirectoryEditApplier
{
    private readonly Func<DirectoryManagementEdit, DirectoryManagementResult>? _apply;

    public FakeDirectoryEditApplier(Func<DirectoryManagementEdit, DirectoryManagementResult>? apply = null)
    {
        _apply = apply;
    }

    public DirectoryManagementResult Apply(DirectoryManagementEdit edit) =>
        _apply?.Invoke(edit) ?? throw new InvalidOperationException("FakeDirectoryEditApplier has no apply delegate.");
}
