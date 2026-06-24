using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

internal static class LauncherJson
{
    private const string RepositoryLineEnding = "\n";
    private const int DefaultMaxBackups = 3;

    internal static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static T? Load<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    public static T LoadRequired<T>(string path, string documentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        return Load<T>(path)
            ?? throw new InvalidDataException($"{documentName} parsed to null.");
    }

    public static void Save<T>(T value, string path, bool backup)
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

            // Backup rotation is best-effort — if it fails, the new data
            // still gets committed. The old backup-on-failure blocking bug
            // is fixed by the try/catch, not by reordering.
            if (backup && File.Exists(fullPath))
            {
                try
                {
                    RotateBackups(fullPath);
                    File.Copy(fullPath, fullPath + ".bak", overwrite: true);
                }
                catch
                {
                    // Non-fatal — the target file has not been modified yet.
                }
            }

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

    private static void RotateBackups(string fullPath)
    {
        for (var i = DefaultMaxBackups - 1; i >= 0; i--)
        {
            var oldPath = i == 0 ? fullPath + ".bak" : fullPath + $".bak.{i}";
            var newPath = fullPath + $".bak.{i + 1}";

            try
            {
                if (i == DefaultMaxBackups - 1)
                    File.Delete(oldPath);
                else
                    File.Move(oldPath, newPath, overwrite: true);
            }
            catch (FileNotFoundException)
            {
                // Best-effort: the backup file doesn't exist — nothing to rotate.
            }
            catch (DirectoryNotFoundException)
            {
                // Best-effort: the backup directory doesn't exist — nothing to rotate.
            }
        }
    }
}
