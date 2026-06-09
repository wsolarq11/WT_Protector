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
            $"Command template '{key}' is not defined. Available templates: {string.Join(", ", templates.Keys)}.");
    }
}
