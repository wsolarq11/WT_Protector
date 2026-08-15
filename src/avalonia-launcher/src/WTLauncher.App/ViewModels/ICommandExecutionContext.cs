using WTLauncher.App.Infrastructure;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Narrow execution context for command workflows: store, projection sync,
/// status output and timestamp. It must not become a MainWindowViewModel callback bag.
/// </summary>
public interface ICommandExecutionContext : IProjectionSynchronizer
{
    public StoreGrain Store { get; }
    public IStatusSink Status { get; }
    public string GetTimestampUtc();
}
