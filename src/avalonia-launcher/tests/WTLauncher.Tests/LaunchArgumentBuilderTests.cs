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
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes("codex --version"));
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

        Assert.Throws<DirectoryNotFoundException>(() =>
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
        var decoded = Encoding.Unicode.GetString(Convert.FromBase64String(afterPrefix));
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
        var decoded = Encoding.Unicode.GetString(Convert.FromBase64String(afterPrefix));
        Assert.Equal("Write-Host `$(Get-Process)`", decoded);
    }

    [Fact]
    public void Build_RejectsCommandLineExceeding32767Characters()
    {
        using var temp = new TempDirectory();
        // Create a very long command value so that the base64-encoded result
        // exceeds the 32767-character Windows CreateProcessW limit.
        var longCommand = new string('x', 25000); // ~33336 chars in base64, plus prefix/suffix
        var entry = Entry(temp.Path, command: longCommand);

        var ex = Assert.Throws<System.IO.InvalidDataException>(() =>
            LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false));
        Assert.Contains("32767", ex.Message);
    }

    [Fact]
    public void Build_RejectsUncNetworkPath()
    {
        var entry = Entry(@"\\server\share\folder", name: "network");

        var ex = Assert.Throws<FormatException>(() =>
            LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false));
        Assert.Contains("网络路径", ex.Message);
    }

    [Fact]
    public void Build_DetectsPercentInjectionViaFullwidthNormalization()
    {
        // U+FF05 (fullwidth percent sign) normalizes to U+0025 (%) via FormKC in
        // EntryName.Normalize. The build must detect this and reject it
        // before ExpandEnvironmentVariables sees a literal %.
        var fullwidthPercent = '\uFF05';
        var entry = Entry($"C:\\tmp\\user{fullwidthPercent}data", name: "inject");

        var ex = Assert.Throws<FormatException>(() =>
            LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false));
        Assert.Contains("规范化后引入 %", ex.Message);
    }

    [Fact]
    public void Build_DoesNotRejectLiteralPercentWhenPresentInOriginalDirectory()
    {
        // Literal % in the original directory is allowed — it's only injected
        // fullwidth % that is blocked. A user who intentionally types % is
        // responsible for its behavior.
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "user%data"));
        var entry = Entry(Path.Combine(temp.Path, "user%data"), name: "literal");

        var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: true, requireConfirmation: false);

        Assert.NotNull(plan);
    }

    private static LaunchEntry Entry(
        string directory,
        string name = "demo",
        string command = "pwsh")
    {
        return new LaunchEntry
        {
            Name = name,
            Directory = (WTLauncher.Core.ValueObjects.DirectoryPath)directory,
            Command = command,
        };
    }

}

