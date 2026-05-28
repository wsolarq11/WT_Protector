namespace WTLauncher.Core;

public sealed record LaunchPlan(
    string Executable,
    string Arguments,
    string WorkingDirectory,
    string Command,
    bool RequiresConfirmation)
{
    public string CommandLine => $"{Executable} {Arguments}";
}
