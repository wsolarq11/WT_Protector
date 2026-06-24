using System.Text;
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
        var encodedCommand = Convert.ToBase64String(Encoding.UTF8.GetBytes("codex --version"));
        Assert.Equal(
            $"{LaunchArgumentBuilder.WorkingDirectoryOption} {LaunchArgumentBuilder.QuoteArgument(temp.Path)} --title \"demo\" {LaunchArgumentBuilder.ShellCommandPrefix} {encodedCommand}",
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

    [Fact]
    public void Build_EncodesCommandAsBase64ToPreventPowerShellInjection()
    {
        using var temp = new TempDirectory();
        var entry = Entry(temp.Path, command: "$(calc)");
        var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false);
        // PowerShell metacharacters must NOT appear in plaintext in Arguments
        Assert.DoesNotContain("$(calc)", plan.Arguments, StringComparison.Ordinal);
        // -EncodedCommand must be used (not -Command)
        Assert.Contains("-EncodedCommand", plan.Arguments, StringComparison.Ordinal);
        // Base64 payload must be present (non-empty after prefix)
        var prefixIndex = plan.Arguments.IndexOf("-EncodedCommand", StringComparison.Ordinal);
        var afterPrefix = plan.Arguments[(prefixIndex + "-EncodedCommand".Length)..].Trim();
        Assert.NotEmpty(afterPrefix);
        // The decoded Base64 should contain the original command
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(afterPrefix));
        Assert.Equal("$(calc)", decoded);
    }

    [Fact]
    public void Build_EncodesBacktickToPreventPowerShellEscapeInjection()
    {
        using var temp = new TempDirectory();
        var entry = Entry(temp.Path, command: "Write-Host `$(Get-Process)`");
        var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false);
        Assert.Contains("-EncodedCommand", plan.Arguments, StringComparison.Ordinal);
        var prefixIndex = plan.Arguments.IndexOf("-EncodedCommand", StringComparison.Ordinal);
        var afterPrefix = plan.Arguments[(prefixIndex + "-EncodedCommand".Length)..].Trim();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(afterPrefix));
        Assert.Equal("Write-Host `$(Get-Process)`", decoded);
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
