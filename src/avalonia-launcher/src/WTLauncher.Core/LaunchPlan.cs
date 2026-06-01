namespace WTLauncher.Core;

public sealed record LaunchPlan(
    string Executable,
    string Arguments,
    string WorkingDirectory,
    string Command,
    bool RequiresConfirmation)
{
    private string _executable = RequireText(Executable, nameof(Executable));
    private string _arguments = RequireText(Arguments, nameof(Arguments));
    private string _workingDirectory = RequireText(WorkingDirectory, nameof(WorkingDirectory));
    private string _command = RequireText(Command, nameof(Command));

    public string Executable
    {
        get => _executable;
        init => _executable = RequireText(value, nameof(Executable));
    }

    public string Arguments
    {
        get => _arguments;
        init => _arguments = RequireText(value, nameof(Arguments));
    }

    public string WorkingDirectory
    {
        get => _workingDirectory;
        init => _workingDirectory = RequireText(value, nameof(WorkingDirectory));
    }

    public string Command
    {
        get => _command;
        init => _command = RequireText(value, nameof(Command));
    }

    public string CommandLine => $"{Executable} {Arguments}";

    private static string RequireText(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);

        return value.Trim();
    }
}
