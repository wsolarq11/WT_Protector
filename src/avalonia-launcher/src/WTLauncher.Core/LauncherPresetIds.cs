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
        var builder = new StringBuilder(value.Length);
        var separatorPending = false;
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
}
