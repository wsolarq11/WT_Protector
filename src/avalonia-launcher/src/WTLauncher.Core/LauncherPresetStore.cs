using System.Text.Encodings.Web;
using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherPresetStore
{
    public const string PresetsFileName = "presets.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LauncherPresetFile Load(string presetPath)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));
        if (!File.Exists(presetPath))
            return new LauncherPresetFile();

        using var stream = File.OpenRead(presetPath);
        return JsonSerializer.Deserialize<LauncherPresetFile>(stream, Options)
            ?? throw new InvalidDataException("Launcher presets parsed to null.");
    }

    public static void Save(LauncherPresetFile presets, string presetPath)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(presetPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(presetPath))
            File.Copy(presetPath, presetPath + ".bak", overwrite: true);

        var json = JsonSerializer.Serialize(presets, Options);
        File.WriteAllText(presetPath, json + Environment.NewLine);
    }

    public static string ResolvePresetPath(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath))
            ?? throw new InvalidOperationException("Config path must have a parent directory.");
        return Path.Combine(directory, PresetsFileName);
    }
}
