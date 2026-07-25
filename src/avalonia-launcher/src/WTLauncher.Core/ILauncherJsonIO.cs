using ErrorOr;

namespace WTLauncher.Core;

/// <summary>
/// Boundary port: abstracts all JSON file I/O operations.
/// The interface lives in Zone 0 (Core) as a seam; implementations
/// (e.g. <c>FileLauncherJsonIO</c>) live in Zone 1 (App).
/// </summary>
public interface ILauncherJsonIO
{
    public T? Load<T>(string path) where T : notnull;
    public T LoadRequired<T>(string path, string documentName) where T : notnull;
    public T LoadWithRecovery<T>(string path, Func<string, T> loadAndValidate,
        string recoveryContext, T defaultValue,
        Action<Exception, string>? onRecovery = null);
    public void Save<T>(T value, string path, bool backup) where T : notnull;
    public ErrorOr<T> LoadErrorOr<T>(string path) where T : notnull;
    public ErrorOr<Success> SaveErrorOr<T>(T value, string path, bool backup) where T : notnull;
    public void CleanupStaleTempFiles(string configDir);
    public void RotateBackups(string fullPath, int maxBackups = 3);
}
