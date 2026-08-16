using System.Diagnostics;
using System.Text.Json;

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine(JsonSerializer.Serialize(new { smoke = "critical-journey", checks = Array.Empty<object>() }));
    return;
}

var exe = GetOption("--exe");
var configPath = GetOption("--config");
var workingDirectory = GetOption("--working-directory");
if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
{
    throw new InvalidOperationException($"Published exe missing: {exe}");
}
if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
{
    throw new InvalidOperationException($"Smoke config missing: {configPath}");
}
if (string.IsNullOrWhiteSpace(workingDirectory))
{
    workingDirectory = Path.GetDirectoryName(Path.GetFullPath(exe)) ?? Directory.GetCurrentDirectory();
}

var startInfo = new ProcessStartInfo
{
    FileName = Path.GetFullPath(exe),
    UseShellExecute = false,
    CreateNoWindow = true,
    WindowStyle = ProcessWindowStyle.Hidden,
    WorkingDirectory = Path.GetFullPath(workingDirectory),
};
startInfo.ArgumentList.Add("--config");
startInfo.ArgumentList.Add(Path.GetFullPath(configPath));

using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {exe}");
var checks = new List<object>();
var exitedClean = true;

try
{
    var booted = false;
    for (var i = 0; i < 100; i++)
    {
        await Task.Delay(100);
        process.Refresh();
        if (process.HasExited)
        {
            throw new InvalidOperationException($"Published app exited with code {process.ExitCode} before main window appeared");
        }
        if (process.MainWindowHandle != 0)
        {
            booted = true;
            break;
        }
    }
    if (!booted)
    {
        throw new InvalidOperationException("Main window handle did not appear within 10 seconds");
    }
    checks.Add(new { name = "launch", pass = true, detail = "process-booted-main-window" });

    var title = string.Empty;
    for (var i = 0; i < 30; i++)
    {
        process.Refresh();
        title = process.MainWindowTitle;
        if (!string.IsNullOrWhiteSpace(title))
        {
            break;
        }
        await Task.Delay(100);
    }
    if (!title.Contains("WT Launcher", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Main window title did not match WT Launcher: '{title}'");
    }
    checks.Add(new { name = "title", pass = true, detail = $"window-title: {title}" });
}
finally
{
    if (!process.HasExited)
    {
        var closed = process.CloseMainWindow();
        if (!closed || !process.WaitForExit(5000))
        {
            exitedClean = false;
            process.Kill(entireProcessTree: true);
            process.WaitForExit(2000);
        }
    }
}

if (!exitedClean)
{
    throw new InvalidOperationException("Published app did not exit gracefully after CloseMainWindow");
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    smoke = "critical-journey",
    exe = Path.GetFullPath(exe),
    config = Path.GetFullPath(configPath),
    checks,
}));

static string? GetOption(string name)
{
    var args = Environment.GetCommandLineArgs();
    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] == name && i + 1 < args.Length)
        {
            return args[i + 1];
        }
    }
    return null;
}
