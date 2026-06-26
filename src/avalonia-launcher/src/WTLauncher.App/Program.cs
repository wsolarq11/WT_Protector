using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            var message = $"[WTLauncher] Unhandled domain exception: {ex}";
            Debug.WriteLine(message);
            Trace.WriteLine(message);
            Console.Error.WriteLine(message);
            if (ex is not null)
                ErrorLog.Write(ex, "AppDomain.UnhandledException");
            Environment.ExitCode = 2;
            Environment.Exit(2);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            var ex = e.Exception.Flatten().InnerException ?? e.Exception;
            var message = $"[WTLauncher] Unobserved task exception: {ex}";
            Debug.WriteLine(message);
            Trace.WriteLine(message);
            Console.Error.WriteLine(message);
            ErrorLog.Write(ex, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };

        LauncherJson.OnRecoveryFromBackup = (ex, ctx) => ErrorLog.Write(ex, ctx);

        DeleteStaleTempFiles();
        RecoverStaleCommitJournals();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
    }

    /// <summary>
    /// Replay any stale commit journal left by a crash during a multi-file save.
    /// Best-effort — failures are logged but never block startup.
    /// </summary>
    private static void RecoverStaleCommitJournals()
    {
        try
        {
            var configPath = LauncherConfigStore.ResolveConfigPath(null, Environment.CurrentDirectory, AppContext.BaseDirectory);
            var configDir = Path.GetDirectoryName(Path.GetFullPath(configPath));
            if (string.IsNullOrWhiteSpace(configDir))
            {
                // No clear resolution path for journal either.
                // The journal is tied to the config directory.
                // If we can't resolve it, the next successful save will clean up.
                return;
            }

            if (!Directory.Exists(configDir))
                return;

            var journalPath = CommitJournal.ResolvePath(configDir);
            if (!File.Exists(journalPath))
                return;

            var replayError = CommitJournal.Replay(journalPath);
            if (replayError is not null)
                ErrorLog.Write(new InvalidOperationException(replayError), "CommitJournal.Recover");
        }
        catch
        {
            // Best-effort at startup — never block app launch.
        }
    }

    /// <summary>Best-effort cleanup of leftover temp files from prior crashes.
    /// If the config directory doesn't exist yet, there's nothing to clean.</summary>
    private static void DeleteStaleTempFiles()
    {
        try
        {
            var configPath = LauncherConfigStore.ResolveConfigPath(null, Environment.CurrentDirectory, AppContext.BaseDirectory);
            var configDir = Path.GetDirectoryName(Path.GetFullPath(configPath));
            if (!string.IsNullOrWhiteSpace(configDir) && Directory.Exists(configDir))
                LauncherJson.CleanupStaleTempFiles(configDir);
        }
        catch
        {
            // Best-effort at startup: no config directory yet, nothing to clean.
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
