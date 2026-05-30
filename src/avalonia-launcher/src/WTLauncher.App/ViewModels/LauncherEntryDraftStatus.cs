namespace WTLauncher.App.ViewModels;

public static class LauncherEntryDraftStatus
{
    public static string IdRequired => "按钮 ID 不能为空";

    public static string NameRequired => "按钮名称不能为空";

    public static string DirectoryRequired => "按钮目录不能为空";

    public static string CommandRequired => "按钮命令不能为空";

    public static string DuplicateId(string entryId) =>
        $"按钮 ID 已存在: {entryId}";
}
