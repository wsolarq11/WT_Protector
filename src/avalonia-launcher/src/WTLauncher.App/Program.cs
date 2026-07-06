using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App;

internal static class Program
{
    // ponytail: global mutex held for app lifetime. Per-operation locking
    // inside LauncherJson would be finer-grained but the existing temp+atomic
    // replace pattern already protects single-writer correctness. This
    // prevents cross-instance interleaving of the multi-step save routine.
    private static Mutex? _instanceMutex;

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

        if (!TryAcquireInstanceMutex())
            return;

        try
        {
            DeleteStaleTempFiles();
            // MigrateJsonToLiteDb();  // LiteDB Phase 3 deferred

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }
        finally
        {
            ReleaseInstanceMutex();
        }
    }

    /// <summary>
    /// Best-effort named mutex to prevent multiple launcher instances from
    /// interleaving config save operations. If another instance holds it,
    /// a message box is shown and the process exits. If acquisition fails
    /// for any other reason (permissions, Global\ namespace unavailable),
    /// the app continues without protection.
    /// </summary>
    private static bool TryAcquireInstanceMutex()
    {
        try
        {
            _instanceMutex = new Mutex(false, @"Local\WTLauncher_ConfigMutex", out bool createdNew);
            try
            {
                if (!_instanceMutex.WaitOne(0))
                {
                    _instanceMutex.Dispose();
                    _instanceMutex = null;
                    ShowAlreadyRunningMessage();
                    return false;
                }
            }
            catch (AbandonedMutexException)
            {
                // Previous instance crashed without releasing — we now own it.
            }

            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or WaitHandleCannotBeOpenedException)
        {
            // ponytail: best-effort — permissions or namespace restrictions
            // prevent the mutex. Continue without cross-instance protection.
            var msg = $"[WTLauncher] Instance mutex unavailable: {ex.Message}";
            Debug.WriteLine(msg);
            Trace.WriteLine(msg);
            ErrorLog.Write(ex, "InstanceMutex");
            _instanceMutex?.Dispose();
            _instanceMutex = null;
            return true;
        }
    }

    private static void ReleaseInstanceMutex()
    {
        try
        {
            _instanceMutex?.ReleaseMutex();
        }
        catch (Exception ex) when (ex is ApplicationException or ObjectDisposedException)
        {
            // Best-effort — the OS reclaims the mutex on process exit regardless.
        }
        finally
        {
            _instanceMutex?.Dispose();
            _instanceMutex = null;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    private static void ShowAlreadyRunningMessage()
    {
        const uint mbIconStop = 0x00000010;
        const uint mbOk = 0x00000000;
        _ = MessageBoxW(IntPtr.Zero,
            "WT Launcher is already running. Only one instance can be open at a time.",
            "WT Launcher",
            mbIconStop | mbOk);
    }

    /// <summary>
    /// Phase 3: One-time JSON→LiteDB migration. Best-effort — if it fails,
    /// the app continues using JSON. The migration will be retried on next startup.
    /// </summary>
    private static void MigrateJsonToLiteDb()
    {
        try
        {
            var storePath = LauncherStore.ResolveStorePath(null, Environment.CurrentDirectory, AppContext.BaseDirectory);
            var storeDir = Path.GetDirectoryName(Path.GetFullPath(storePath));
            if (string.IsNullOrWhiteSpace(storeDir) || !Directory.Exists(storeDir))
                return;

            try { MigrationTool.MigrateIfNeeded(storeDir); }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WTLauncher] JSON→LiteDB migration skipped: {ex.Message}");
                Trace.WriteLine($"[WTLauncher] JSON→LiteDB migration skipped: {ex.Message}");
            }
        }
        catch
        {
            // Best-effort — migration failure must never block startup.
        }
    }

    /// <summary>Best-effort cleanup of leftover temp files from prior crashes.
    /// If the config directory doesn't exist yet, there's nothing to clean.</summary>
    private static void DeleteStaleTempFiles()
    {
        try
        {
            var storePath = LauncherStore.ResolveStorePath(null, Environment.CurrentDirectory, AppContext.BaseDirectory);
            var storeDir = Path.GetDirectoryName(Path.GetFullPath(storePath));
            if (!string.IsNullOrWhiteSpace(storeDir) && Directory.Exists(storeDir))
                LauncherJson.CleanupStaleTempFiles(storeDir);
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
