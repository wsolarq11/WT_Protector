using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherStatusPresentationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void StatusContentRejectsMissingText(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new LauncherStatusContent(text!, LauncherStatusTone.Neutral));
    }

    [Fact]
    public void StatusFactoriesKeepTextAndToneTogether()
    {
        Assert.Equal(new LauncherStatusContent("ready", LauncherStatusTone.Neutral), LauncherStatusContent.Neutral("ready"));
        Assert.Equal(new LauncherStatusContent("done", LauncherStatusTone.Success), LauncherStatusContent.Success("done"));
        Assert.Equal(new LauncherStatusContent("canceled", LauncherStatusTone.Warning), LauncherStatusContent.Warning("canceled"));
        Assert.Equal(new LauncherStatusContent("failed", LauncherStatusTone.Error), LauncherStatusContent.Error("failed"));
    }

    [Theory]
    [InlineData(LauncherStatusTone.Neutral, "•")]
    [InlineData(LauncherStatusTone.Success, "✓")]
    [InlineData(LauncherStatusTone.Warning, "!")]
    [InlineData(LauncherStatusTone.Error, "×")]
    public void IndicatorFor_ProvidesNonColorCue(LauncherStatusTone tone, string expected)
    {
        Assert.Equal(expected, LauncherStatusPresentation.IndicatorFor(tone));
    }
}
