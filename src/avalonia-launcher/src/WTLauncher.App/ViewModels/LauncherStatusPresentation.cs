namespace WTLauncher.App.ViewModels;

public enum LauncherStatusTone
{
    Neutral,
    Success,
    Warning,
    Error,
}

public static class LauncherStatusPresentation
{
    private static readonly string[] ErrorMarkers =
    [
        "失败", "错误", "无效", "不存在", "无法", "未找到", "缺少", "冲突",
    ];

    private static readonly string[] WarningMarkers =
    [
        "取消", "跳过", "部分", "警告", "未选择", "无需",
    ];

    private static readonly string[] SuccessMarkers =
    [
        "已新增", "已添加", "已修改", "已更新", "已删除", "已复制", "已保存",
        "已启动", "已请求启动", "已完成", "已应用", "已合并", "启动成功", "保存成功",
    ];

    public static LauncherStatusTone Classify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LauncherStatusTone.Neutral;

        if (ContainsAny(text, ErrorMarkers))
            return LauncherStatusTone.Error;
        if (ContainsAny(text, WarningMarkers))
            return LauncherStatusTone.Warning;
        return ContainsAny(text, SuccessMarkers)
            ? LauncherStatusTone.Success
            : LauncherStatusTone.Neutral;
    }

    public static string IndicatorFor(LauncherStatusTone tone) => tone switch
    {
        LauncherStatusTone.Success => "✓",
        LauncherStatusTone.Warning => "!",
        LauncherStatusTone.Error => "×",
        _ => "•",
    };

    private static bool ContainsAny(string text, IEnumerable<string> markers) =>
        markers.Any(marker => text.Contains(marker, StringComparison.Ordinal));
}
