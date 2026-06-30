namespace WTLauncher.Core;

/// <summary>Abstracts persistence for the launcher store. Phase 2 uses JsonStore
/// (delegating to LauncherStore); Phase 3 replaces it with LiteDbStore.</summary>
public interface IStore
{
    Result<LauncherStoreFile, StoreLoadError> Load(string storePath);
    Result<Unit, StoreSaveError> Save(LauncherStoreFile data, string storePath);
}

/// <summary>JSON-file implementation of <see cref="IStore"/>. Delegates to
/// <see cref="LauncherStore"/> methods. This is the Phase 1-2 implementation.</summary>
public sealed class JsonStore : IStore
{
    public Result<LauncherStoreFile, StoreLoadError> Load(string storePath)
        => LauncherStore.TryLoad(storePath);

    public Result<Unit, StoreSaveError> Save(LauncherStoreFile data, string storePath)
        => LauncherStore.TrySave(data, storePath);
}
