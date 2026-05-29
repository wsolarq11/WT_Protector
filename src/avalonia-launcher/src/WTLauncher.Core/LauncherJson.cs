using System.Text.Encodings.Web;
using System.Text.Json;

namespace WTLauncher.Core;

internal static class LauncherJson
{
    private const string RepositoryLineEnding = "\n";

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
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = NormalizeLineEndings(JsonSerializer.Serialize(value, Options));
        var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";
        try
        {
            File.WriteAllText(tempPath, json + RepositoryLineEnding);

            if (backup && File.Exists(fullPath))
                File.Copy(fullPath, fullPath + ".bak", overwrite: true);

            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", RepositoryLineEnding)
            .Replace("\r", RepositoryLineEnding);
    }
}
