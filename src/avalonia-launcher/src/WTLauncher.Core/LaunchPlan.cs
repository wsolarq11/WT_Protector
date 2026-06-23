namespace WTLauncher.Core;

public sealed record LaunchPlan(
    string Executable,
    string Arguments,
    string WorkingDirectory,
    string Command,
    bool RequiresConfirmation)
{
    public string Executable { get; init; } = RequireText(Executable, nameof(Executable));

    public string Arguments { get; init; } = RequireText(Arguments, nameof(Arguments));

    public string WorkingDirectory { get; init; } = RequireText(WorkingDirectory, nameof(WorkingDirectory));

    public string Command { get; init; } = RequireText(Command, nameof(Command));

    public string CommandLine => $"{Executable} {Arguments}";

    private static string RequireText(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);

        return value.Trim();
    }
}
