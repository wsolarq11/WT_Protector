namespace WTLauncher.App.ViewModels;

public static class LauncherEntryStatus
{
    public static string AddCanceled => "已取消: 新增按钮";

    public static string EditCanceled(string entryName) =>
        $"已取消: 编辑 {entryName}";

    public static string DeleteCanceled(string entryName) =>
        $"已取消: 删除 {entryName}";

    public static string Added(string entryName) =>
        $"已新增按钮: {entryName}（已保存）";

    public static string Updated(string entryName) =>
        $"已更新按钮: {entryName}（已保存）";

    public static string Deleted(string entryName) =>
        $"已删除按钮: {entryName}（已保存）";
}
