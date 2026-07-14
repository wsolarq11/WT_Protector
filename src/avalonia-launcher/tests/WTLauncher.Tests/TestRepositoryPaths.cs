namespace WTLauncher.Tests;

internal static class TestRepositoryPaths
{
    private static string Root { get; } = FindRepositoryRoot();

    public static string Config(string fileName) => Path.Combine(Root, "config", fileName);

    public static string Document(string fileName) => Path.Combine(Root, "docs", fileName);

    public static string RootFile(string fileName) => Path.Combine(Root, fileName);

    public static string View(string fileName) => Path.Combine(
        Root,
        "src",
        "avalonia-launcher",
        "src",
        "WTLauncher.App",
        "Views",
        fileName);

    public static string ViewModel(string fileName) => Path.Combine(
        Root,
        "src",
        "avalonia-launcher",
        "src",
        "WTLauncher.App",
        "ViewModels",
        fileName);

    public static string MainWindowXaml => View("MainWindow.axaml");

    public static string MainWindowCodeBehind => View("MainWindow.axaml.cs");

    public static string MainWindowDragCodeBehind => View("MainWindow.Drag.cs");

    public static string AppCodeBehind => Path.Combine(
        AppSourceRoot,
        "App.axaml.cs");

    public static string AppSourceRoot => Path.Combine(
        Root,
        "src",
        "avalonia-launcher",
        "src",
        "WTLauncher.App");

    public static string CoreSource(string fileName) => Path.Combine(
        Root,
        "src",
        "avalonia-launcher",
        "src",
        "WTLauncher.Core",
        fileName);

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "config", "launcher.json"))
                && File.Exists(Path.Combine(dir.FullName, "README.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "src", "avalonia-launcher")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WT_Protector repository root from test output directory.");
    }
}
