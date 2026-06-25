using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherStartupArgumentsTests
{
    [Fact]
    public void ReadConfigPath_ReturnsValueAfterConfigFlag()
    {
        var (config, _) = LauncherStartupArguments.ReadConfigPath(["--config", "D:\\portable\\config.json"]);

        Assert.Equal("D:\\portable\\config.json", config);
    }

    [Fact]
    public void ReadConfigPath_TrimsConfigPathValue()
    {
        var (config, _) = LauncherStartupArguments.ReadConfigPath(["--config", " config/custom.json "]);

        Assert.Equal("config/custom.json", config);
    }

    [Fact]
    public void ReadConfigPath_IsCaseInsensitive()
    {
        var (config, _) = LauncherStartupArguments.ReadConfigPath(["--CONFIG", "config/custom.json"]);
        Assert.Equal("config/custom.json", config);
    }

    [Fact]
    public void ReadConfigPath_ReturnsNullWhenNoArgumentsAreProvided()
    {
        var (config, unknownArgs) = LauncherStartupArguments.ReadConfigPath([]);
        Assert.Null(config);
        Assert.Empty(unknownArgs);
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
        var (config, unknownArgs) = LauncherStartupArguments.ReadConfigPath(["--ignored-option"]);

        Assert.Null(config);
        Assert.Equal(["--ignored-option"], unknownArgs);
    }

    private static void AssertMissingConfigValue(string[] args)
    {
        var exception = Assert.Throws<ArgumentException>(() => LauncherStartupArguments.ReadConfigPath(args));

        Assert.Equal("args", exception.ParamName);
        Assert.StartsWith("Config path value is required after --config.", exception.Message);
    }
}
