using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherSingleLaunchStatusTests
{
    [Theory]
    [InlineData(false, "预演: one")]
    [InlineData(true, "预演: one（需要确认）")]
    public void PreviewIncludesConfirmationOnlyWhenRequired(bool requiresConfirmation, string expected)
    {
        Assert.Equal(expected, LauncherSingleLaunchStatus.Preview("one", requiresConfirmation));
    }

    [Fact]
    public void StatusTextUsesEntryNameAndFailureMessage()
    {
        var exception = new InvalidOperationException("missing directory");

        Assert.Equal("预演失败: one", LauncherSingleLaunchStatus.PreviewFailed("one"));
        Assert.Equal("已取消: one", LauncherSingleLaunchStatus.Canceled("one"));
        Assert.Equal("已请求启动: one", LauncherSingleLaunchStatus.Requested("one"));
        Assert.Equal(
            "启动失败: one — missing directory",
            LauncherSingleLaunchStatus.Failed("one", exception));
    }
}
