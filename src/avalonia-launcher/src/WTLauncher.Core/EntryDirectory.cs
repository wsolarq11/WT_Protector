using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// A validated and normalized entry directory path. FormKC normalization, %-injection
/// detection (on raw input BEFORE normalization), environment-variable expansion,
/// Path.GetFullPath resolution, UNC path rejection, and DOS device path rejection
/// all happen at construction time.
/// </summary>
[JsonConverter(typeof(EntryDirectoryJsonConverter))]
public readonly record struct EntryDirectory
{
    public string Value { get; }

    private EntryDirectory(string value) => Value = value;

    /// <summary>
    /// Validates and normalizes a directory path.
    /// KEY FIX (C1): %-injection detection runs on the RAW input before FormKC normalization.
    /// If FormKC introduces a % that was not in the original, the input is rejected.
    /// This prevents fullwidth % (U+FF05) → ASCII % injection into ExpandEnvironmentVariables.
    /// </summary>
    public static Result<EntryDirectory, DirectoryError> TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new DirectoryError.Empty();

        var normalized = raw.Trim().Normalize(NormalizationForm.FormKC);

        // Detect FormKC-introduced % characters BEFORE ExpandEnvironmentVariables.
        // Compare raw (pre-normalized) against normalized to catch fullwidth % (U+FF05 → %).
        if (normalized.Contains('%') && !raw.Contains('%', StringComparison.Ordinal))
            return new DirectoryError.PercentInjection();

        // Expand environment variables, then resolve to absolute path.
        var expanded = Environment.ExpandEnvironmentVariables(normalized);
        var fullPath = Path.GetFullPath(expanded);

        // Reject UNC paths to prevent NTLM credential relay.
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal) || fullPath.StartsWith("//", StringComparison.Ordinal))
            return new DirectoryError.UncPath(fullPath);

        // Reject DOS device paths.
        if (fullPath.StartsWith(@"\\.\", StringComparison.Ordinal) || fullPath.StartsWith(@"\??\", StringComparison.Ordinal))
            return new DirectoryError.DevicePath(fullPath);

        return new EntryDirectory(fullPath);
    }

    /// <summary>
    /// Validates directory AND checks it exists on disk. Used during launch (validateDirectory: true).
    /// </summary>
    public static Result<EntryDirectory, DirectoryError> TryParseAndValidateExists(string? raw)
    {
        var result = TryParse(raw);
        if (result.IsErr) return result;
        var dir = result.Match(ok: d => d, err: _ => default);
        if (!Directory.Exists(dir.Value))
            return new DirectoryError.NotFound(dir.Value);
        return dir;
    }

    public static implicit operator string(EntryDirectory dir) => dir.Value;
    public override string ToString() => Value;
}

public sealed class EntryDirectoryJsonConverter : JsonConverter<EntryDirectory>
{
    public override EntryDirectory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        var result = EntryDirectory.TryParse(raw);
        return result.Match(
            ok: dir => dir,
            err: error => throw new InvalidDataException(error.Message));
    }

    public override void Write(Utf8JsonWriter writer, EntryDirectory value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
