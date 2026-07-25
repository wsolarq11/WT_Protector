using System.Runtime.CompilerServices;
using System.Text.Json;
using ErrorOr;

[assembly: InternalsVisibleTo("WTLauncher.App")]
[assembly: InternalsVisibleTo("WTLauncher.Tests")]

namespace WTLauncher.Core;

/// <summary>
/// Facade for JSON persistence. Delegates serialization to
/// <see cref="LauncherJsonSerializer"/> (Zone 0 pure function) and I/O to
/// <see cref="ILauncherJsonIO"/> (Zone 1 file operations).
///
/// All public API signatures are preserved for backward compatibility.
/// In tests, replace <see cref="DefaultIO"/> with a stub to avoid file I/O.
/// </summary>
public static class LauncherJson
{
    /// <summary>Source-generated JSON serializer options used by serialization.</summary>
    internal static JsonSerializerOptions Options => LauncherJsonContext.Default.Options;

    /// <summary>The I/O implementation used by all static methods.
    /// Must be set in the App entry point before any Load/Save call.
    /// Override in tests for hermetic file-free execution.</summary>
    internal static ILauncherJsonIO DefaultIO { get; set; } = null!;

    private static ILauncherJsonIO IO => DefaultIO
        ?? throw new InvalidOperationException(
            "LauncherJson.DefaultIO must be set before any Load/Save call.");

    public static T? Load<T>(string path) where T : notnull => IO.Load<T>(path);
    public static T LoadRequired<T>(string path, string documentName) where T : notnull
        => IO.LoadRequired<T>(path, documentName);

    internal static T LoadWithRecovery<T>(
        string path, Func<string, T> loadAndValidate,
        string recoveryContext, T defaultValue,
        Action<Exception, string>? onRecovery = null)
        => IO.LoadWithRecovery(path, loadAndValidate, recoveryContext, defaultValue, onRecovery);

    public static void Save<T>(T value, string path, bool backup) where T : notnull
        => IO.Save(value, path, backup);

    public static ErrorOr<T> LoadErrorOr<T>(string path) where T : notnull
        => IO.LoadErrorOr<T>(path);

    public static ErrorOr<Success> SaveErrorOr<T>(T value, string path, bool backup) where T : notnull
        => IO.SaveErrorOr<T>(value, path, backup);

    public static void CleanupStaleTempFiles(string configDir)
        => IO.CleanupStaleTempFiles(configDir);

    public static void RotateBackups(string fullPath, int maxBackups = 3)
        => IO.RotateBackups(fullPath, maxBackups);
}
