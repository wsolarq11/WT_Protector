using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherDragReorderStatusTests
{
    [Fact]
    public void StatusTextDescribesDragReorderOutcomes()
    {
        Assert.Equal("已保存自定义拖拽排序", LauncherDragReorderStatus.Saved);
        Assert.Equal("已取消: 拖拽排序", LauncherDragReorderStatus.Canceled);
        Assert.Equal("已保存自定义排序: one", LauncherDragReorderStatus.Moved("one"));
        Assert.Equal("当前条目不能继续移动", LauncherDragReorderStatus.MoveUnavailable);
    }
}
