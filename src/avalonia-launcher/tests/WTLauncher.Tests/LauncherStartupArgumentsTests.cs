using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherStartupArgumentsTests
{
    [Fact]
    public void ReadConfigPath_ReturnsValueAfterConfigFlag()
    {
        var config = LauncherStartupArguments.ReadConfigPath(["--config", "D:\\portable\\config.json"]);

        Assert.Equal("D:\\portable\\config.json", config);
    }

    [Fact]
    public void ReadConfigPath_TrimsConfigPathValue()
    {
        var config = LauncherStartupArguments.ReadConfigPath(["--config", " config/custom.json "]);

        Assert.Equal("config/custom.json", config);
    }

    [Fact]
    public void ReadConfigPath_IsCaseInsensitive()
    {
        Assert.Equal("config/custom.json", LauncherStartupArguments.ReadConfigPath(["--CONFIG", "config/custom.json"]));
    }

    [Fact]
    public void ReadConfigPath_ReturnsNullWhenNoArgumentsAreProvided()
    {
        Assert.Null(LauncherStartupArguments.ReadConfigPath([]));
    }

    [Fact]
    public void ReadConfigPath_RejectsNullArgumentList()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            LauncherStartupArguments.ReadConfigPath(null));

        Assert.Equal("args", exception.ParamName);
    }

    [Fact]
    public void ReadConfigPath_RejectsMissingConfigValue()
    {
        AssertMissingConfigValue(["--config"]);
        AssertMissingConfigValue(["--config", "  "]);
        AssertMissingConfigValue(["--config", "--ignored-option"]);
    }

    [Fact]
    public void ReadConfigPath_RejectsDuplicateConfigFlags()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherStartupArguments.ReadConfigPath(
                ["--config", "config/first.json", "--CONFIG", "config/second.json"]));

        Assert.Equal("args", exception.ParamName);
        Assert.StartsWith("Config path flag must appear at most once.", exception.Message);
    }

    [Fact]
    public void ReadConfigPath_RejectsUnknownStartupArguments()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherStartupArguments.ReadConfigPath(["--ignored-option", "value"]));

        Assert.Equal("args", exception.ParamName);
        Assert.StartsWith("Unknown startup argument: --ignored-option.", exception.Message);
    }

    private static void AssertMissingConfigValue(string[] args)
    {
        var exception = Assert.Throws<ArgumentException>(() => LauncherStartupArguments.ReadConfigPath(args));

        Assert.Equal("args", exception.ParamName);
        Assert.StartsWith("Config path value is required after --config.", exception.Message);
    }
}
