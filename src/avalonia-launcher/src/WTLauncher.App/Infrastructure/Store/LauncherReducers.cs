using Fluxor;

namespace WTLauncher.App.Infrastructure.Store;

public static class LauncherReducers
{
    [ReducerMethod]
    public static LauncherState OnLoadStore(LauncherState state, LoadStoreAction _)
        => state with { IsLoading = true, ErrorMessage = null };

    [ReducerMethod]
    public static LauncherState OnLoadStoreResult(LauncherState state, LoadStoreResultAction action)
        => state with { IsLoading = false, StoreFile = action.StoreFile };

    [ReducerMethod]
    public static LauncherState OnLoadStoreFailed(LauncherState state, LoadStoreFailedAction action)
        => state with { IsLoading = false, ErrorMessage = action.Error };

    [ReducerMethod]
    public static LauncherState OnSaveStore(LauncherState state, SaveStoreAction _)
        => state with { IsLoading = true, ErrorMessage = null };

    [ReducerMethod]
    public static LauncherState OnSaveStoreResult(LauncherState state, SaveStoreResultAction _)
        => state with { IsLoading = false, IsDirty = false };

    [ReducerMethod]
    public static LauncherState OnSaveStoreFailed(LauncherState state, SaveStoreFailedAction action)
        => state with { IsLoading = false, ErrorMessage = action.Error };

    [ReducerMethod]
    public static LauncherState OnMutateStore(LauncherState state, MutateStoreAction action)
    {
        if (state.StoreFile is null) return state;
        var newStore = action.Transform(state.StoreFile);
        return state with { StoreFile = newStore, IsDirty = true };
    }
}
