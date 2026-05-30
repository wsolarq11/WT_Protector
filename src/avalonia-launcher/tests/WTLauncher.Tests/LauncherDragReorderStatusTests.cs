using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherDragReorderStatusTests
{
    [Fact]
    public void StatusTextDescribesDragReorderOutcomes()
    {
        Assert.Equal("已保存自定义拖拽排序", LauncherDragReorderStatus.Saved);
        Assert.Equal("已取消: 拖拽排序", LauncherDragReorderStatus.Canceled);
    }
}
