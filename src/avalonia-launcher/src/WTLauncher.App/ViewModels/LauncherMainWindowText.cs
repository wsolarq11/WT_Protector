namespace WTLauncher.App.ViewModels;

public static class LauncherMainWindowText
{
    public static string Ready => "Ready";

    public static string DryRunPlaceholder => "选择一个启动项查看预演命令。";

    public static string PresetSelectionPlaceholder => "选择启动组后可查看与当前勾选的差异。";

    public static string ConfigPathDisplay(string configPath, string presetPath) =>
        $"配置: {configPath} · 启动组: {presetPath}";

    public static string Loaded(int entryCount, int presetCount) =>
        $"已加载 {entryCount} 个启动项、{presetCount} 个启动组；勾选自动保存到 config\\config.json";
}
