using System.Globalization;
using System.Text;

namespace WTLauncher.Core;

public static class LaunchArgumentBuilder
{
    public const string WorkingDirectoryOption = "-d";
    public const string ShellCommandPrefix = "pwsh -NoExit -EncodedCommand";

    /// <summary>Resolved full path to Windows Terminal executable.</summary>
    public static string TerminalExecutable => ResolveTerminalExecutable();

    /// <summary>
    /// Resolves wt.exe full path from well-known Windows Terminal install locations.
    /// Never falls back to a bare name — PATH-based resolution is a binary-hijack vector.
    /// </summary>
    internal static string ResolveTerminalExecutable()
    {
        // Primary: per-user WindowsApps alias (Microsoft Store / winget installs).
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrEmpty(localAppData))
        {
            var wtPath = Path.GetFullPath(Path.Combine(localAppData, @"Microsoft\WindowsApps\wt.exe"));
            if (File.Exists(wtPath))
                return wtPath;
        }

        // Secondary: system-wide WindowsApps (provisioned packages).
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(programFiles))
        {
            var wtPath = Path.GetFullPath(Path.Combine(programFiles, @"WindowsApps\wt.exe"));
            if (File.Exists(wtPath))
                return wtPath;
        }

        throw new FileNotFoundException(
            "Windows Terminal (wt.exe) was not found in any known install location. " +
            "Install Windows Terminal from the Microsoft Store or via winget.");
    }

    public static LaunchPlan Build(LaunchEntry entry, bool validateDirectory, bool requireConfirmation, IReadOnlyDictionary<string, string>? commandTemplates = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var name = StripUnsafeCharacters(LauncherEntryText.Normalize(entry.Name));
        var normalizedDirectory = LauncherEntryText.Normalize(entry.Directory);
        // FormKC normalization converts fullwidth characters (e.g. U+FF05 ％ → U+0025 %).
        // If % appears after normalization but before ExpandEnvironmentVariables, an
        // attacker can inject environment-variable expansion into the directory path.
        // Detect and reject this before expansion.
        if (normalizedDirectory.Contains('%') && !entry.Directory.Contains('%', StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Launch entry '{name}' directory contains characters that normalize to '%' — " +
                "this is not allowed because it would trigger unintended environment-variable expansion.");
        var directory = StripUnsafeCharacters(Environment.ExpandEnvironmentVariables(normalizedDirectory));
        var command = StripUnsafeCharacters(LauncherCommandTemplateResolver.Resolve(LauncherEntryText.Normalize(entry.Command), commandTemplates));

        if (name.Length == 0)
            throw new InvalidOperationException("Launch entry name is empty.");
        if (directory.Length == 0)
            throw new InvalidOperationException($"Launch entry '{name}' has an empty directory.");
        // Reject UNC paths to prevent NTLM credential relay to attacker-controlled SMB servers.
        if (directory.StartsWith(@"\\", StringComparison.Ordinal))
            throw new InvalidOperationException($"Launch entry '{name}' directory is a network path, which is not allowed.");
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

    /// <summary>Strips control characters (\0, \r, \n, etc.) and Unicode format characters
    /// (BiDi overrides, zero-width spaces, etc.) to prevent CreateProcessW boundary issues
    /// and visual-spoofing attacks via directional formatting codes.</summary>
    private static string StripUnsafeCharacters(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            var cat = char.GetUnicodeCategory(ch);
            if (cat is UnicodeCategory.Control or UnicodeCategory.Format)
                continue;
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
