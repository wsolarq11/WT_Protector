using System.Text;

namespace WTLauncher.Core;

public static class LaunchArgumentBuilder
{
    public static LaunchPlan Build(LaunchEntry entry, bool validateDirectory = true, bool requireConfirmation = true)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var name = (entry.Name ?? "").Trim();
        var directory = (entry.Directory ?? "").Trim();
        var command = (entry.Command ?? "").Trim();

        if (name.Length == 0)
            throw new LaunchPlanException("Launch entry name is empty.");
        if (directory.Length == 0)
            throw new LaunchPlanException($"Launch entry '{name}' has an empty directory.");
        if (validateDirectory && !System.IO.Directory.Exists(directory))
            throw new LaunchPlanException($"Launch entry '{name}' directory does not exist: {directory}");
        if (command.Length == 0)
            throw new LaunchPlanException($"Launch entry '{name}' has an empty command.");

        var arguments = $"-d {QuoteArgument(directory)} pwsh -NoExit -Command {QuoteArgument(command)}";
        return new LaunchPlan(
            "wt.exe",
            arguments,
            directory,
            command,
            requireConfirmation);
    }

    public static string QuoteArgument(string? value)
    {
        value ??= "";
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
