using System.Text;

namespace WTLauncher.Core;

public static class LaunchArgumentBuilder
{
    public const string WorkingDirectoryOption = "-d";
    public const string ShellCommandPrefix = "pwsh -NoExit -EncodedCommand";

    /// <summary>Resolved full path to Windows Terminal executable.</summary>
    public static string TerminalExecutable => ResolveTerminalExecutable();

    /// <summary>
    /// Resolves wt.exe full path from the Windows Apps alias directory.
    /// Falls back to "wt.exe" bare name only when LOCALAPPDATA is unavailable
    /// (should never happen on Windows desktop).
    /// </summary>
    internal static string ResolveTerminalExecutable()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrEmpty(localAppData))
            return "wt.exe";

        var wtPath = Path.Combine(localAppData, @"Microsoft\WindowsApps\wt.exe");
        return File.Exists(wtPath) ? wtPath : "wt.exe";
    }

    public static LaunchPlan Build(LaunchEntry entry, bool validateDirectory, bool requireConfirmation, IReadOnlyDictionary<string, string>? commandTemplates = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var name = StripControlCharacters(LauncherEntryText.Normalize(entry.Name));
        var directory = StripControlCharacters(Environment.ExpandEnvironmentVariables(LauncherEntryText.Normalize(entry.Directory)));
        var command = StripControlCharacters(LauncherCommandTemplateResolver.Resolve(LauncherEntryText.Normalize(entry.Command), commandTemplates));

        if (name.Length == 0)
            throw new InvalidOperationException("Launch entry name is empty.");
        if (directory.Length == 0)
            throw new InvalidOperationException($"Launch entry '{name}' has an empty directory.");
        if (validateDirectory && !System.IO.Directory.Exists(directory))
            throw new InvalidOperationException($"Launch entry '{name}' directory does not exist: {directory}");
        if (command.Length == 0)
            throw new InvalidOperationException($"Launch entry '{name}' has an empty command.");

        // Encode command as Base64 to eliminate PowerShell metacharacter interpretation.
        // -EncodedCommand treats the payload as opaque bytes — $(), backtick, and other
        // PS expansion syntax are never evaluated.
        var encodedCommand = Convert.ToBase64String(Encoding.UTF8.GetBytes(command));

        var arguments = $"{WorkingDirectoryOption} {QuoteArgument(directory)} --title {QuoteArgument(name)} {ShellCommandPrefix} {encodedCommand}";

        const int MaxCommandLineLength = 32767; // Windows CreateProcessW limit
        if (arguments.Length > MaxCommandLineLength)
            throw new System.IO.InvalidDataException($"Launch command line exceeds Windows limit of {MaxCommandLineLength} characters.");

        return new LaunchPlan(
            ResolveTerminalExecutable(),
            arguments,
            directory,
            command,
            requireConfirmation);
    }

    /// <summary>Strips control characters (\0, \r, \n, etc.) to prevent CreateProcessW boundary issues.</summary>
    private static string StripControlCharacters(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (!char.IsControl(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }

    public static string QuoteArgument(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var quoted = new StringBuilder();
        quoted.Append('"');

        var backslashes = 0;
        foreach (var ch in value)
        {
            if (ch == '\\')
            {
                backslashes++;
                continue;
            }

            if (ch == '"')
            {
                quoted.Append('\\', backslashes * 2 + 1);
                quoted.Append('"');
                backslashes = 0;
                continue;
            }

            if (backslashes > 0)
            {
                quoted.Append('\\', backslashes);
                backslashes = 0;
            }

            quoted.Append(ch);
        }

        if (backslashes > 0)
            quoted.Append('\\', backslashes * 2);
        quoted.Append('"');
        return quoted.ToString();
    }
}
