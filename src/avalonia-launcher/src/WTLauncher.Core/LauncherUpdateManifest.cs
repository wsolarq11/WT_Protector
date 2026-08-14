using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// Machine-readable update manifest for win-x64 portable releases. The app
/// only reads this metadata; it never lets an updater rewrite launcher.json.
/// </summary>
public sealed record LauncherUpdateManifest
{
    [JsonPropertyName("schemaVersion")]
    [JsonRequired]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("version")]
    [JsonRequired]
    public string Version { get; init; } = "";

    [JsonPropertyName("fileName")]
    [JsonRequired]
    public string FileName { get; init; } = "WTLauncher.App.exe";

    [JsonPropertyName("sha256")]
    [JsonRequired]
    public string Sha256 { get; init; } = "";

    [JsonPropertyName("bytes")]
    [JsonRequired]
    public long Bytes { get; init; }

    [JsonPropertyName("publishedAtUtc")]
    [JsonRequired]
    public string PublishedAtUtc { get; init; } = "";

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = "";

    public bool IsNewerThan(string currentVersion)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        return CompareVersions(Version, currentVersion) > 0;
    }

    private static int CompareVersions(string left, string right)
    {
        var a = Parse(left);
        var b = Parse(right);
        var major = a.Major.CompareTo(b.Major);
        if (major != 0) return major;
        var minor = a.Minor.CompareTo(b.Minor);
        if (minor != 0) return minor;
        var patch = a.Patch.CompareTo(b.Patch);
        if (patch != 0) return patch;
        if (a.PreRelease.Length == 0 && b.PreRelease.Length != 0) return 1;
        if (a.PreRelease.Length != 0 && b.PreRelease.Length == 0) return -1;
        return string.CompareOrdinal(a.PreRelease, b.PreRelease);
    }

    private static VersionParts Parse(string value)
    {
        var cleaned = value.Trim().TrimStart('v');
        var suffixStart = cleaned.IndexOfAny(['-', '+']);
        var versionPart = suffixStart >= 0 ? cleaned[..suffixStart] : cleaned;
        var suffix = suffixStart >= 0 ? cleaned[(suffixStart + 1)..] : "";
        var parts = versionPart.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new InvalidDataException($"Version is not valid: {value}");
        var major = ParsePart(parts, 0);
        var minor = ParsePart(parts, 1);
        var patch = ParsePart(parts, 2);
        return new VersionParts(major, minor, patch, suffix);
    }

    private static int ParsePart(string[] parts, int index) =>
        index < parts.Length && int.TryParse(parts[index], out var parsed) ? parsed : 0;

    private sealed record VersionParts(int Major, int Minor, int Patch, string PreRelease);
}

public static class LauncherUpdateManifestParser
{
    public static LauncherUpdateManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        LauncherUpdateManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize(json, LauncherJsonContext.Default.LauncherUpdateManifest);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Update manifest is not valid JSON.", ex);
        }

        if (manifest is null
            || manifest.SchemaVersion != 1
            || string.IsNullOrWhiteSpace(manifest.Version)
            || string.IsNullOrWhiteSpace(manifest.FileName)
            || string.IsNullOrWhiteSpace(manifest.Sha256)
            || manifest.Bytes <= 0
            || string.IsNullOrWhiteSpace(manifest.PublishedAtUtc))
        {
            throw new InvalidDataException("Update manifest is missing required fields.");
        }

        return manifest;
    }
}
