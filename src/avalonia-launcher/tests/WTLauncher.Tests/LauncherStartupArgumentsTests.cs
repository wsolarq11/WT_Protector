using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherStartupArgumentsTests
{
    [Fact]
    public void ReadConfigPath_ReturnsValueAfterConfigFlag()
    {
        var config = LauncherStartupArguments.ReadConfigPath(["--theme", "light", "--config", "D:\\portable\\config.json"]);

        Assert.Equal("D:\\portable\\config.json", config);
    }

    [Fact]
    public void ReadConfigPath_IsCaseInsensitiveAndIgnoresMissingOrBlankValue()
    {
        Assert.Equal("config/custom.json", LauncherStartupArguments.ReadConfigPath(["--CONFIG", "config/custom.json"]));
        Assert.Null(LauncherStartupArguments.ReadConfigPath(["--config"]));
        Assert.Null(LauncherStartupArguments.ReadConfigPath(["--config", "  "]));
        Assert.Null(LauncherStartupArguments.ReadConfigPath(null));
    }
}
