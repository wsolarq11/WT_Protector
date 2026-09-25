using WTLauncher.App.Localization;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherLocalizationTests
{
    [Fact]
    public void Format_WithArgs_SubstitutesPositionalValues()
    {
        var text = LauncherLocalization.Format("main.loaded", 2, 3);
        Assert.Contains("已加载 2 个启动项、3 个启动组", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_UnknownKey_ReturnsKeySoTheGapIsVisible()
    {
        Assert.Equal("missing.key", LauncherLocalization.Format("missing.key"));
    }
}
