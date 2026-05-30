using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherEntryDraftStatusTests
{
    [Fact]
    public void StatusTextDescribesDraftValidationFailures()
    {
        Assert.Equal("按钮 ID 不能为空", LauncherEntryDraftStatus.IdRequired);
        Assert.Equal("按钮名称不能为空", LauncherEntryDraftStatus.NameRequired);
        Assert.Equal("按钮目录不能为空", LauncherEntryDraftStatus.DirectoryRequired);
        Assert.Equal("按钮命令不能为空", LauncherEntryDraftStatus.CommandRequired);
        Assert.Equal("按钮 ID 已存在: two", LauncherEntryDraftStatus.DuplicateId("two"));
    }
}
