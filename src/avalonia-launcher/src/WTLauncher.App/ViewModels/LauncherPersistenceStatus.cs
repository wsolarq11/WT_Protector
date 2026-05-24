using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPersistenceStatus
{
    public static LauncherPersistenceStatusResult SavePresets(LauncherPresetFile presetFile, string presetPath)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(presetPath);

        return LauncherPresetPersistence.Save(presetFile, presetPath, out var errorMessage)
            ? LauncherPersistenceStatusResult.Saved()
            : LauncherPersistenceStatusResult.Failed(errorMessage ?? "启动组保存失败");
    }

    public static LauncherPersistenceStatusResult SaveConfig(IEnumerable<LaunchEntry> entries, string configPath)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(configPath);

        return LauncherConfigPersistence.Save(entries, configPath, out var errorMessage)
            ? LauncherPersistenceStatusResult.Saved()
            : LauncherPersistenceStatusResult.Failed(errorMessage ?? "配置保存失败");
    }
}

public readonly record struct LauncherPersistenceStatusResult(bool Succeeded, string? StatusText)
{
    public static LauncherPersistenceStatusResult Saved()
    {
        return new LauncherPersistenceStatusResult(true, null);
    }

    public static LauncherPersistenceStatusResult Failed(string statusText)
    {
        return new LauncherPersistenceStatusResult(false, statusText);
    }
}
