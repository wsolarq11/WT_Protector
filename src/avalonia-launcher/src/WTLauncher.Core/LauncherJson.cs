using System.Text.Encodings.Web;
using System.Text.Json;

namespace WTLauncher.Core;

internal static class LauncherJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static T? Load<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    public static void Save<T>(T value, string path, bool backup = false)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (backup && File.Exists(path))
            File.Copy(path, path + ".bak", overwrite: true);

        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(path, json + Environment.NewLine);
    }
}
