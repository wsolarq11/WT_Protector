namespace WTLauncher.Core;

public static class LauncherCommandTemplateResolver
{
    public const string TemplatePrefix = "$";

    /// <summary>
    /// Resolves a command string against the configured templates dictionary.
    /// </summary>
    /// <remarks>
    /// <para>Commands starting with <c>$</c> are treated as template lookups:
    /// <c>$key</c> is replaced with the value defined for <c>key</c>.</para>
    /// <para>To emit a literal <c>$</c> prefix without template lookup, use
    /// a double-dollar escape: <c>$$env:USERPROFILE</c> becomes
    /// <c>$env:USERPROFILE</c>.</para>
    /// </remarks>
    public static string Resolve(string command, IReadOnlyDictionary<string, string>? templates)
    {
        // $$ escape sequence: treat as literal $ prefix — strip one $ and return the rest.
        if (command.StartsWith("$$", StringComparison.Ordinal))
            return command[1..].Trim();

        if (templates is null)
            return command;

        if (!command.StartsWith(TemplatePrefix, StringComparison.Ordinal))
            return command;

        var key = command[TemplatePrefix.Length..];
        if (key.Length == 0)
            return command;

        if (templates.TryGetValue(key, out var resolved))
            return resolved;

        throw new InvalidDataException(
            $"Command template '{key}' is not defined.");
    }

    /// <summary>
    /// Validates that every <c>$key</c> reference in entry commands has a
    /// matching template definition. Returns the list of missing keys (empty if
    /// all references are satisfied). An empty templates dictionary is treated as
    /// "no templates configured" — all <c>$key</c> references are missing.
    /// </summary>
    public static IReadOnlyList<string> ValidateTemplateReferences(
        IEnumerable<LaunchEntry> entries,
        IReadOnlyDictionary<string, string>? templates)
    {
        ArgumentNullException.ThrowIfNull(entries);

        List<string> missingKeys = [];
        // CommandTemplates uses OrdinalIgnoreCase (see LauncherSettings.cs:35),
        // so the dedup set must also be case-insensitive — otherwise "$Profile"
        // and "$profile" would be reported as two separate missing keys when
        // they actually refer to the same template lookup.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var command = LauncherEntryText.Normalize(entry.Command);
            if (command.StartsWith("$$", StringComparison.Ordinal))
                continue;
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
