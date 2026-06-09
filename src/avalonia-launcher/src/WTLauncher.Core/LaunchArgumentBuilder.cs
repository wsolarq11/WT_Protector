using System.Text;

namespace WTLauncher.Core;

public static class LaunchArgumentBuilder
{
    public const string TerminalExecutable = "wt.exe";
    public const string WorkingDirectoryOption = "-d";
    public const string ShellCommandPrefix = "pwsh -NoExit -Command";

    public static LaunchPlan Build(LaunchEntry entry, bool validateDirectory, bool requireConfirmation, IReadOnlyDictionary<string, string>? commandTemplates = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var name = LauncherEntryText.Normalize(entry.Name);
        var directory = LauncherEntryText.Normalize(entry.Directory);
        directory = Environment.ExpandEnvironmentVariables(directory);
        var command = LauncherEntryText.Normalize(entry.Command);
        command = LauncherCommandTemplateResolver.Resolve(command, commandTemplates);

        if (name.Length == 0)
            throw new InvalidOperationException("Launch entry name is empty.");
        if (directory.Length == 0)
            throw new InvalidOperationException($"Launch entry '{name}' has an empty directory.");
        if (validateDirectory && !System.IO.Directory.Exists(directory))
            throw new InvalidOperationException($"Launch entry '{name}' directory does not exist: {directory}");
        if (command.Length == 0)
            throw new InvalidOperationException($"Launch entry '{name}' has an empty command.");

        var arguments = $"{WorkingDirectoryOption} {QuoteArgument(directory)} --title {QuoteArgument(name)} {ShellCommandPrefix} {QuoteArgument(command)}";
        return new LaunchPlan(
            TerminalExecutable,
            arguments,
            directory,
            command,
            requireConfirmation);
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
