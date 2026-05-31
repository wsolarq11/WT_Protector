namespace WTLauncher.App.ViewModels;

public static class LauncherDragReorderStatus
{
    public static string Saved => "已保存自定义拖拽排序";

    public static string Canceled => "已取消: 拖拽排序";

    public static string Moved(string entryName) => $"已保存自定义排序: {entryName}";

    public static string MoveUnavailable => "当前条目不能继续移动";
}
