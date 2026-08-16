using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("WT Launcher uninstaller is Windows-only");
}

var commandLine = Environment.GetCommandLineArgs();
if (commandLine.Length > 1 && commandLine[1] == "--create-shortcut")
{
    CreateShortcut(commandLine[2], commandLine[3], commandLine[4]);
    return;
}

var childMode = commandLine.Any(arg => arg == "--child");
var installDir = Path.GetDirectoryName(Environment.ProcessPath!) ?? Environment.CurrentDirectory;
if (childMode && commandLine.Length > 2)
{
    installDir = Path.GetFullPath(commandLine[2]);
}

var backupRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "WTLauncher-config-backup");

BackupConfig(installDir, backupRoot);
RemoveShortcuts();

if (childMode)
{
    await Task.Delay(1500);
    DeleteDirectory(installDir);
    DeleteSelf();
    Console.WriteLine(JsonSerializer.Serialize(new { status = "uninstalled", installDir }));
    return;
}

var tempExe = Path.Combine(
    Path.GetTempPath(),
    $"WTLauncher-uninstaller-{Guid.NewGuid():N}.exe");
File.Copy(Environment.ProcessPath!, tempExe, overwrite: true);

var startInfo = new ProcessStartInfo
{
    FileName = tempExe,
    UseShellExecute = false,
    CreateNoWindow = true,
    WorkingDirectory = Path.GetTempPath(),
};
startInfo.ArgumentList.Add("--child");
startInfo.ArgumentList.Add(installDir);
Process.Start(startInfo);

static void CreateShortcut(string target, string workingDirectory, string location)
{
    var startMenuDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft",
        "Windows",
        "Start Menu",
        "Programs",
        "WT Launcher");
    var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    var shortcutPath = location == "desktop"
        ? Path.Combine(desktopDir, "WT Launcher.lnk")
        : Path.Combine(startMenuDir, "WT Launcher.lnk");
    Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

    var shellType = Type.GetTypeFromProgID("WScript.Shell")
        ?? throw new InvalidOperationException("WScript.Shell COM type is unavailable");
    dynamic shell = Activator.CreateInstance(shellType)!;
    dynamic shortcut = shell.CreateShortcut(shortcutPath);
    shortcut.TargetPath = Path.GetFullPath(target);
    shortcut.WorkingDirectory = Path.GetFullPath(workingDirectory);
    shortcut.Save();
    Marshal.FinalReleaseComObject(shortcut);
    Marshal.FinalReleaseComObject(shell);
}

static void BackupConfig(string installDir, string backupRoot)
{
    var configSource = Path.Combine(installDir, "config");
    if (!Directory.Exists(configSource))
    {
        return;
    }
    var backup = Path.Combine(backupRoot, $"config-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
    Directory.CreateDirectory(backup);
    foreach (var entry in Directory.EnumerateFileSystemEntries(configSource, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(configSource, entry);
        var destination = Path.Combine(backup, relative);
        if (Directory.Exists(entry))
        {
            Directory.CreateDirectory(destination);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(entry, destination, overwrite: true);
        }
    }
    Console.WriteLine($"User config preserved at: {backup}");
}

static void RemoveShortcuts()
{
    var startMenuDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft",
        "Windows",
        "Start Menu",
        "Programs",
        "WT Launcher");
    DeleteFile(Path.Combine(startMenuDir, "WT Launcher.lnk"));
    DeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "WT Launcher.lnk"));
}

static void DeleteFile(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

static void DeleteDirectory(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}

static void DeleteSelf()
{
    var self = Environment.ProcessPath!;
    try
    {
        File.Delete(self);
        return;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        // Fall through to a detached cleanup process; a running exe may be locked for deletion.
    }

    var moveTarget = self + ".deleting";
    try
    {
        File.Move(self, moveTarget);
        File.Delete(moveTarget);
        return;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        // The final fallback waits for this process to exit and then removes the temp copy.
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = "cmd.exe",
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = Path.GetTempPath(),
    };
    startInfo.ArgumentList.Add("/c");
    startInfo.ArgumentList.Add($"timeout /t 1 /nobreak >nul & del /q \"{self}\"");
    Process.Start(startInfo);
}
