using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    private static LauncherPresetFile PresetFile(string name, params string[] entryIds) =>
        new()
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = name,
                    EntryIds = [.. entryIds],
                },
            ],
        };

    private static LaunchEntry Entry(bool selected = false) =>
        new()
        {
            Id = "demo",
            Name = "演示",
            Directory = Path.Combine(
                Path.GetPathRoot(Environment.CurrentDirectory)
                    ?? Path.DirectorySeparatorChar.ToString(),
                "demo"),
            Command = "codex",
            Selected = selected,
        };

    private static string ConfigDirectoryPath(string root)
    {
        var configDir = Path.Combine(root, LauncherConfigStore.ConfigDirectoryName);
        Directory.CreateDirectory(configDir);
        return configDir;
    }

    private static string ConfigFilePath(string root, string fileName) =>
        Path.Combine(ConfigDirectoryPath(root), fileName);

    private static string WriteConfigDirectoryFile(string root, string fileName, string content)
    {
        var path = ConfigFilePath(root, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private static string WriteEmptyLauncherConfig(string root) =>
        WriteConfigDirectoryFile(root, LauncherConfigStore.ConfigFileName, "[]");

    private static void AssertUsesRepositoryLineEndings(string text)
    {
        Assert.EndsWith("\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", text);
    }
}
