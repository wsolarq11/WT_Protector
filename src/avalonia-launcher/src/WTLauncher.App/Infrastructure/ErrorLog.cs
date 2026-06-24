using System.Diagnostics;
using System.Threading;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Minimal persistent error log that survives Release builds.
/// Debug.WriteLine is stripped in Release; Trace requires
/// an explicit listener (not configured). This writes to
/// %LOCALAPPDATA%/WTLauncher/errors.log as a last-resort
/// diagnostic fallback.
/// </summary>
internal static class ErrorLog
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WTLauncher");

    private static readonly string LogPath = Path.Combine(LogDirectory, "errors.log");

    private static int _isWriting;

    /// <summary>
    /// Write an exception with a human-readable context tag.
    /// Never throws — if logging itself fails we fall back to
    /// Debug/Trace and move on.
    /// </summary>
    internal static void Write(Exception exception, string context)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (Interlocked.Exchange(ref _isWriting, 1) == 1)
            return;

        try
        {
            Directory.CreateDirectory(LogDirectory);
            var entry = $"{DateTimeOffset.UtcNow:O} [{context}] {exception}{Environment.NewLine}";
            File.AppendAllText(LogPath, entry);
        }
        catch
        {
            try
            {
                // Last resort — logging must not cause a secondary crash.
                Debug.WriteLine($"[ErrorLog] Failed to write: {exception}");
                Trace.WriteLine($"[ErrorLog] Failed to write: {exception}");
            }
            catch
            {
                // Debug/Trace themselves can fail — swallow everything.
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isWriting, 0);
        }
    }
}
