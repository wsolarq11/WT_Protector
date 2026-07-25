using ErrorOr;

namespace WTLauncher.Core;

/// <summary>
/// Boundary port: abstracts launcher store persistence operations.
/// The interface lives in Zone 0 (Core) as a seam; implementations
/// (e.g. <c>FileStorePersistence</c>) live in Zone 1 (App).
/// </summary>
public interface IStorePersistence
{
    public LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null);
    public void Save(LauncherStoreFile store, string storePath, bool backup = true);
    public ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath);
    public string ResolveStorePath(string? explicitPath,
        string? currentDirectory = null, string? appBaseDirectory = null);
}
