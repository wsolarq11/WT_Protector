using ErrorOr;

namespace WTLauncher.Core;

/// <summary>
/// Facade for launcher store persistence. Delegates schema validation to
/// <see cref="LauncherStoreValidator"/> (Zone 0 pure function) and I/O to
/// <see cref="IStorePersistence"/> (Zone 1 file operations).
///
/// All public API signatures are preserved for backward compatibility.
/// In tests, replace <see cref="DefaultPersistence"/> with a stub to avoid file I/O.
/// </summary>
public static class LauncherStore
{
    public const string ConfigDirectoryName = "config";
    public const string StoreFileName = "launcher.json";
    public const string RepositoryMarkerFileName = "README.md";

    /// <summary>The persistence implementation used by all static methods.
    /// Must be set in the App entry point before any Load/Save call.</summary>
    internal static IStorePersistence DefaultPersistence { get; set; } = null!;

    private static IStorePersistence Persistence => DefaultPersistence
        ?? throw new InvalidOperationException(
            "LauncherStore.DefaultPersistence must be set before any Load/Save call.");

    public static LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null)
        => Persistence.Load(storePath, onRecovery);

    public static void Save(LauncherStoreFile store, string storePath, bool backup = true)
        => Persistence.Save(store, storePath, backup);

    public static ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath)
        => Persistence.LoadErrorOr(storePath);

    public static string ResolveStorePath(string? explicitPath,
        string? currentDirectory = null, string? appBaseDirectory = null)
        => Persistence.ResolveStorePath(explicitPath, currentDirectory, appBaseDirectory);
}
