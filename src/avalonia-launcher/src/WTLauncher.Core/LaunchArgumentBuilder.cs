using System.Globalization;
using System.Text;

namespace WTLauncher.Core;

public static class LaunchArgumentBuilder
{
    public const string WorkingDirectoryOption = "-d";
    public const string ShellCommandPrefix = "-- pwsh -NoExit -EncodedCommand";

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

    /// <param name="validateDirectory">When true, verifies the entry's directory exists on disk before building the plan.</param>
    /// <param name="requireConfirmation">When true, the resulting LaunchPlan will require user confirmation before execution.</param>
    public static LaunchPlan Build(LaunchEntry entry, bool validateDirectory, bool requireConfirmation, IReadOnlyDictionary<string, string>? commandTemplates = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Directory trust-boundary validation is centralized in DirectoryPath.Parse:
        // FormKC normalization, % injection detection, env var expansion, GetFullPath
        // traversal defense, UNC rejection, DOS device rejection. Single source of truth.
        var directory = validateDirectory
            ? DirectoryPath.ParseAndValidateExists(entry.Directory.Value).Value
            : DirectoryPath.Parse(entry.Directory.Value).Value;

        var name = StripUnsafeCharacters(EntryName.Normalize(entry.Name));
        var command = StripUnsafeCharacters(LauncherCommandTemplateResolver.Resolve(EntryName.Normalize(entry.Command), commandTemplates));

        if (name.Length == 0)
            throw new InvalidOperationException("Launch entry name is empty.");
        if (command.Length == 0)
            throw new InvalidOperationException($"Launch entry '{name}' has an empty command.");
        // Command length is validated against the Windows CreateProcessW 32767-char limit
        // during argument construction below — no need for an earlier soft limit.

        // Encode command as Base64 to eliminate PowerShell metacharacter interpretation.
        // -EncodedCommand treats the payload as opaque bytes — $(), backtick, and other
        // PS expansion syntax are never evaluated.
        // PowerShell -EncodedCommand requires UTF-16LE (Encoding.Unicode), not UTF-8.
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

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
    /// and visual-spoofing attacks via directional formatting codes.
    /// Confirmed: UnicodeCategory.Format covers BiDi override characters (U+202A-U+202E, U+2066-U+2069).</summary>
    private static string StripUnsafeCharacters(string value)
    {
        // Fast path: scan for Control/Format characters first.
        // Most inputs (normalized entry names, resolved directories) are clean.
        var hasUnsafe = false;
        for (int i = 0; i < value.Length; i++)
        {
            var cat = char.GetUnicodeCategory(value[i]);
            if (cat is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                hasUnsafe = true;
                break;
            }
        }

        if (!hasUnsafe)
            return value;

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
