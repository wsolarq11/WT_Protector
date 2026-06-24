namespace WTLauncher.Core;

public sealed record LaunchPlan(
    string Executable,
    string Arguments,
    string WorkingDirectory,
    string Command,
    bool RequiresConfirmation)
{
    // Field initializers validate during primary construction (init accessor NOT called).
    // Init accessors validate during `with` expressions (field initializer NOT re-run).
    // Each assignment path validates exactly once — no redundancy, no gaps.
    private string _executable = RequireText(Executable, nameof(Executable));
    private string _arguments = RequireText(Arguments, nameof(Arguments));
    private string _workingDirectory = RequireText(WorkingDirectory, nameof(WorkingDirectory));
    private string _command = RequireText(Command, nameof(Command));

    public string Executable { get => _executable; init => _executable = RequireText(value, nameof(Executable)); }
    public string Arguments { get => _arguments; init => _arguments = RequireText(value, nameof(Arguments)); }
    public string WorkingDirectory { get => _workingDirectory; init => _workingDirectory = RequireText(value, nameof(WorkingDirectory)); }
    public string Command { get => _command; init => _command = RequireText(value, nameof(Command)); }

    /// <summary>
    /// Human-readable command-line preview for UI display (DryRun, receipts).
    /// The actual execution transmits the command via Base64-encoded
    /// -EncodedCommand (see <see cref="Arguments"/>).
    /// </summary>
    public string CommandLine => $"{Executable} -d \"{WorkingDirectory}\" pwsh -NoExit -Command \"{Command}\"";

    private static string RequireText(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);

        return value.Trim();
    }
}
