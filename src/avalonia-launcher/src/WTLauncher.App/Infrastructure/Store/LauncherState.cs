using Fluxor;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure.Store;

[FeatureState]
public sealed record LauncherState
{
    public LauncherStoreFile? StoreFile { get; init; }
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsDirty { get; init; }
}
