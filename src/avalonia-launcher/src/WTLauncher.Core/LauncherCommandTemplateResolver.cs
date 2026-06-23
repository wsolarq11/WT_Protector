namespace WTLauncher.Core;

public static class LauncherCommandTemplateResolver
{
    public const string TemplatePrefix = "$";

    public static string Resolve(string command, IReadOnlyDictionary<string, string>? templates)
    {
        if (templates is null || templates.Count == 0)
            return command;

        if (!command.StartsWith(TemplatePrefix, StringComparison.Ordinal))
            return command;

        var key = command[TemplatePrefix.Length..];
        if (key.Length == 0)
            return command;

        if (templates.TryGetValue(key, out var resolved))
            return resolved;

        throw new InvalidOperationException(
            $"Command template '{key}' is not defined.");
    }

    /// <summary>
    /// E10: Validates that every <c>$key</c> reference in entry commands has a
    /// matching template definition. Returns the list of missing keys (empty if
    /// all references are satisfied). An empty templates dictionary is treated as
    /// "no templates configured" — all <c>$key</c> references are missing.
    /// </summary>
    public static IReadOnlyList<string> ValidateTemplateReferences(
        IEnumerable<LaunchEntry> entries,
        IReadOnlyDictionary<string, string>? templates)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var missingKeys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var command = LauncherEntryText.Normalize(entry.Command);
            if (command.Length == 0 || !command.StartsWith(TemplatePrefix, StringComparison.Ordinal))
                continue;

            var key = command[TemplatePrefix.Length..];
            if (key.Length == 0)
                continue;

            if (templates is null || !templates.ContainsKey(key))
            {
                if (seen.Add(key))
                    missingKeys.Add(key);
            }
        }

        return missingKeys;
    }
}
