using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LaunchArgumentBuilderTests
{
    [Fact]
    public void Build_ProducesCurrentWtArgumentContract()
    {
        using var temp = new TempDirectory();
        var entry = Entry(temp.Path, command: "codex --version");

        var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: true, requireConfirmation: false);

        Assert.Equal(LaunchArgumentBuilder.TerminalExecutable, plan.Executable);
        Assert.Equal(
            $"{LaunchArgumentBuilder.WorkingDirectoryOption} {LaunchArgumentBuilder.QuoteArgument(temp.Path)} --title \"demo\" {LaunchArgumentBuilder.ShellCommandPrefix} \"codex --version\"",
            plan.Arguments);
        Assert.False(plan.RequiresConfirmation);
    }

    [Fact]
    public void Build_UsesLaunchConfirmationParameter()
    {
        using var temp = new TempDirectory();
        var entry = Entry(
            temp.Path,
            command: "codex --dangerously-bypass-approvals-and-sandbox");

        var directPlan = LaunchArgumentBuilder.Build(entry, validateDirectory: true, requireConfirmation: false);
        var confirmedPlan = LaunchArgumentBuilder.Build(entry, validateDirectory: true, requireConfirmation: true);

        Assert.False(directPlan.RequiresConfirmation);
        Assert.True(confirmedPlan.RequiresConfirmation);
    }

    [Fact]
    public void Build_UsesDirectoryValidationParameter()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var entry = Entry(missingDirectory, name: "missing");

        var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false);

        Assert.Equal(missingDirectory, plan.WorkingDirectory);
    }

    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("has space", "\"has space\"")]
    [InlineData("has \"quote\"", "\"has \\\"quote\\\"\"")]
    [InlineData("C:\\path\\", "\"C:\\path\\\\\"")]
    public void QuoteArgument_UsesWindowsCommandLineEscaping(string value, string expected)
    {
        Assert.Equal(expected, LaunchArgumentBuilder.QuoteArgument(value));
    }

    [Fact]
    public void QuoteArgument_RejectsNullArgument()
    {
        Assert.Throws<ArgumentNullException>(() => LaunchArgumentBuilder.QuoteArgument(null!));
    }

    [Fact]
    public void Build_RejectsMissingDirectoryWhenValidationEnabled()
    {
        var entry = Entry(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            name: "missing");

        Assert.Throws<InvalidOperationException>(() =>
            LaunchArgumentBuilder.Build(entry, validateDirectory: true, requireConfirmation: false));
    }

    private static LaunchEntry Entry(
        string directory,
        string name = "demo",
        string command = "pwsh")
    {
        return new LaunchEntry
        {
            Name = name,
            Directory = directory,
            Command = command,
        };
    }

}
