using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LaunchArgumentBuilderTests
{
    [Fact]
    public void Build_ProducesCurrentWtArgumentContract()
    {
        using var temp = new TempDirectory();
        var entry = new LaunchEntry
        {
            Name = "demo",
            Directory = temp.Path,
            Command = "codex --version",
            Confirm = false,
        };

        var plan = LaunchArgumentBuilder.Build(entry, requireConfirmation: false);

        Assert.Equal("wt.exe", plan.Executable);
        Assert.Equal($"-d {LaunchArgumentBuilder.QuoteArgument(temp.Path)} pwsh -NoExit -Command \"codex --version\"", plan.Arguments);
        Assert.False(plan.RequiresConfirmation);
    }

    [Fact]
    public void Build_UsesOnlyLaunchConfirmationOverride()
    {
        using var temp = new TempDirectory();
        var entry = new LaunchEntry
        {
            Name = "demo",
            Directory = temp.Path,
            Command = "codex --dangerously-bypass-approvals-and-sandbox",
            Confirm = true,
        };

        var directPlan = LaunchArgumentBuilder.Build(entry, requireConfirmation: false);
        var confirmedPlan = LaunchArgumentBuilder.Build(entry, requireConfirmation: true);

        Assert.False(directPlan.RequiresConfirmation);
        Assert.True(confirmedPlan.RequiresConfirmation);
    }

    [Fact]
    public void Build_DefaultsToConfirmationWhenNoGlobalOverrideIsProvided()
    {
        using var temp = new TempDirectory();
        var entry = new LaunchEntry
        {
            Name = "plain",
            Directory = temp.Path,
            Command = "pwsh",
            Confirm = true,
        };

        var plan = LaunchArgumentBuilder.Build(entry);

        Assert.True(plan.RequiresConfirmation);
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
    public void Build_RejectsMissingDirectoryWhenValidationEnabled()
    {
        var entry = new LaunchEntry
        {
            Name = "missing",
            Directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            Command = "pwsh",
        };

        Assert.Throws<InvalidOperationException>(() => LaunchArgumentBuilder.Build(entry));
    }

}
