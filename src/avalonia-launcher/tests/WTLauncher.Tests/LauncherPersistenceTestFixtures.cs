using WTLauncher.Core;

namespace WTLauncher.Tests;

internal static class LauncherPersistenceTestFixtures
{
    private static readonly string RootDrive = Path.GetPathRoot(Environment.CurrentDirectory)
        ?? Path.DirectorySeparatorChar.ToString();

    public static LaunchEntry Entry(string id) =>
        new()
        {
            Id = id,
            Name = id,
            Directory = Path.Combine(RootDrive, id),
            Command = "pwsh",
        };

    public static LauncherPresetFile PresetFile(params LauncherPreset[] presets) =>
        new()
        {
            DefaultPresetId = presets.FirstOrDefault()?.Id ?? string.Empty,
            Presets = [.. presets],
        };

    public static LauncherPresetFile PresetFile(string defaultPresetId, params LauncherPreset[] presets) =>
        new()
        {
            DefaultPresetId = defaultPresetId,
            Presets = [.. presets],
        };

    public static LauncherPreset Preset(string id, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = id,
            EntryIds = [.. entryIds],
        };

    public static LauncherPreset NamedPreset(string id, string name, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = name,
            EntryIds = [.. entryIds],
        };
}
