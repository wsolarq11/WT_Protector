using System.Text;

namespace WTLauncher.Core;

public static class LauncherPresetIds
{
    public static StringComparer Comparer => StringComparer.Ordinal;

    public static bool Same(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    public static string GenerateUnique(string name, IEnumerable<string> existingIds)
    {
        ArgumentNullException.ThrowIfNull(existingIds);

        var baseId = Normalize(name);
        var candidate = baseId;
        var index = 2;
        var existing = existingIds
            .Select(Normalize)
            .ToHashSet(Comparer);
        while (existing.Contains(candidate))
        {
            candidate = $"{baseId}-{index++}";
        }

        return candidate;
    }

    public static string Normalize(string? value)
    {
        value ??= string.Empty;

        // H15: Guard against maliciously long preset names that could cause OOM.
        if (value.Length > 256)
            throw new InvalidDataException($"Preset name length {value.Length} exceeds maximum of 256.");

        var builder = new StringBuilder(value.Length);
        var separatorPending = false;
        // FormKC (compatibility decomposition + canonical composition) may change
        // the visual appearance of some Unicode characters (e.g. fullwidth → ASCII,
        // ligature → individual letters, superscript → regular). This is intentional:
        // we produce stable, ASCII-adjacent IDs suitable for preset cross-references.
        foreach (var ch in value.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                separatorPending = false;
            }
            else if (ch is '-' or '_')
            {
                builder.Append(ch);
                separatorPending = false;
            }
            else if (char.IsWhiteSpace(ch) || ch is '/' or '\\' or '.' or ':')
            {
                if (!separatorPending && builder.Length > 0)
                {
                    builder.Append('-');
                    separatorPending = true;
                }
            }
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "preset" : normalized;
    }

    public static string NormalizeReference(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : Normalize(value);
}
