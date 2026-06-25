using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherCommandTemplateResolverTests
{
    [Fact]
    public void Resolve_ReturnsOriginalCommand_WhenTemplatesIsNull()
    {
        var resolved = LauncherCommandTemplateResolver.Resolve("$notfound", null);

        Assert.Equal("$notfound", resolved);
    }

    [Fact]
    public void Resolve_ThrowsWhenTemplatesIsEmpty()
    {
        var templates = new Dictionary<string, string>();

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherCommandTemplateResolver.Resolve("$notfound", templates));

        Assert.Equal(
            "Command template 'notfound' is not defined.",
            exception.Message);
    }

    [Fact]
    public void Resolve_ReturnsOriginalCommand_WhenCommandDoesNotStartWithDollar()
    {
        var templates = new Dictionary<string, string>
        {
            ["pwsh"] = "C:\\pwsh.exe",
        };

        var resolved = LauncherCommandTemplateResolver.Resolve("pwsh", templates);

        Assert.Equal("pwsh", resolved);
    }

    [Fact]
    public void Resolve_ReturnsOriginalCommand_WhenDollarIsFollowedByEmptyKey()
    {
        var templates = new Dictionary<string, string>
        {
            ["pwsh"] = "C:\\pwsh.exe",
        };

        var resolved = LauncherCommandTemplateResolver.Resolve("$", templates);

        Assert.Equal("$", resolved);
    }

    [Fact]
    public void Resolve_ReturnsTemplateValue_WhenKeyExists()
    {
        var templates = new Dictionary<string, string>
        {
            ["pwsh"] = "C:\\Program Files\\PowerShell\\7\\pwsh.exe",
            ["codex"] = "C:\\Users\\me\\AppData\\Local\\Programs\\opencode\\opencode.exe",
        };

        var resolved = LauncherCommandTemplateResolver.Resolve("$codex", templates);

        Assert.Equal("C:\\Users\\me\\AppData\\Local\\Programs\\opencode\\opencode.exe", resolved);
    }

    [Fact]
    public void Resolve_ThrowsInvalidOperationException_WhenKeyNotFound()
    {
        var templates = new Dictionary<string, string>
        {
            ["pwsh"] = "C:\\pwsh.exe",
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherCommandTemplateResolver.Resolve("$notfound", templates));

        Assert.Equal(
            "Command template 'notfound' is not defined.",
            exception.Message);
    }

    [Fact]
    public void ValidateTemplateReferences_ReportsMissingKeysWhenTemplatesEmpty()
    {
        var entries = new[]
        {
            new LaunchEntry { Id = "one", Name = "One", Directory = "C:\\tmp", Command = "$pwsh" },
            new LaunchEntry { Id = "two", Name = "Two", Directory = "C:\\tmp", Command = "cmd.exe" },
            new LaunchEntry { Id = "three", Name = "Three", Directory = "C:\\tmp", Command = "$codex" },
        };
        var templates = new Dictionary<string, string>(); // empty, not null

        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(entries, templates);

        Assert.Equal(2, missingKeys.Count);
        Assert.Contains("pwsh", missingKeys);
        Assert.Contains("codex", missingKeys);
    }

    [Fact]
    public void ValidateTemplateReferences_ReportsMissingKeysWhenTemplatesNull()
    {
        var entries = new[]
        {
            new LaunchEntry { Id = "one", Name = "One", Directory = "C:\\tmp", Command = "$pwsh" },
        };

        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(entries, null);

        Assert.Single(missingKeys);
        Assert.Contains("pwsh", missingKeys);
    }

    [Fact]
    public void ValidateTemplateReferences_ReturnsEmptyWhenAllKeysDefined()
    {
        var entries = new[]
        {
            new LaunchEntry { Id = "one", Name = "One", Directory = "C:\\tmp", Command = "$pwsh" },
            new LaunchEntry { Id = "two", Name = "Two", Directory = "C:\\tmp", Command = "$codex" },
        };
        var templates = new Dictionary<string, string>
        {
            ["pwsh"] = "C:\\pwsh.exe",
            ["codex"] = "C:\\codex.exe",
        };

        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(entries, templates);

        Assert.Empty(missingKeys);
    }
}
