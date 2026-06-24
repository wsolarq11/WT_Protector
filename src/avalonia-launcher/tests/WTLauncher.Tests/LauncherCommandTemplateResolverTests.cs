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
    public void Resolve_ReturnsOriginalCommand_WhenTemplatesIsEmpty()
    {
        var templates = new Dictionary<string, string>();

        var resolved = LauncherCommandTemplateResolver.Resolve("$notfound", templates);

        Assert.Equal("$notfound", resolved);
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
}
