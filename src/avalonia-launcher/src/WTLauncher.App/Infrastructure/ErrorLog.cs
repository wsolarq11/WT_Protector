using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("WTLauncher.Tests")]

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
    private const long MaxLogFileSize = 1_000_000; // 1 MB

    private const string OldLogSuffix = ".old";

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
        Write(exception, context, LogDirectory, LogPath);
    }

    /// <summary>
    /// Test seam: allows injecting a custom log path so tests can
    /// verify rotation without touching the real error log.
    /// </summary>
    internal static void Write(Exception exception, string context, string logDirectory, string logPath)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Concurrent writes are intentionally dropped rather than queued.
        // For a desktop launcher, concurrent error bursts are extremely rare,
        // and queuing would risk unbounded memory growth during failure cascades.
        if (Interlocked.Exchange(ref _isWriting, 1) == 1)
            return;

        try
        {
            Directory.CreateDirectory(logDirectory);
            RotateIfNeeded(logPath);
            var entry = $"{DateTimeOffset.UtcNow:O} [{context}] {exception}{Environment.NewLine}";
            File.AppendAllText(logPath, entry);
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

    /// <summary>
    /// Best-effort log rotation: if <paramref name="logPath"/> exceeds
    /// <see cref="MaxLogFileSize"/>, move it to *.old before the next write.
    /// Failures are silently ignored — rotation is a convenience, not a guarantee.
    /// </summary>
    private static void RotateIfNeeded(string logPath)
    {
        try
        {
            var fileInfo = new FileInfo(logPath);
            if (fileInfo.Exists && fileInfo.Length > MaxLogFileSize)
            {
                var oldPath = logPath + OldLogSuffix;
                File.Move(logPath, oldPath, overwrite: true);
            }
        }
        catch
        {
            // Best-effort — if rotation fails we simply append to the
            // existing (possibly over-limit) file. Logging must never
            // cause a secondary crash.
        }
    }
}
