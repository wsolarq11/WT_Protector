using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

internal static class LauncherLaunchWorkflow
{
    public static LauncherLaunchResult Preview(LaunchEntry entry, string entryName, bool requireConfirmation)
    {
        try
        {
            var plan = BuildPlan(entry, requireConfirmation);
            return new LauncherLaunchResult(
                FormatPlan(plan),
                plan.RequiresConfirmation
                    ? $"预演: {entryName}（需要确认）"
                    : $"预演: {entryName}");
        }
        catch (Exception ex)
        {
            return new LauncherLaunchResult(ex.Message, $"预演失败: {entryName}");
        }
    }

    public static async Task<LauncherLaunchResult> LaunchOneAsync(
        LaunchEntry entry,
        string entryName,
        bool requireConfirmation,
        Func<LaunchEntry, LaunchPlan, Task<bool>> confirmLaunchAsync,
        Action<LaunchPlan> start)
    {
        string? dryRunText = null;
        try
        {
            var plan = BuildPlan(entry, requireConfirmation);
            dryRunText = FormatPlan(plan);
            if (plan.RequiresConfirmation)
            {
                var accepted = await confirmLaunchAsync(entry, plan).ConfigureAwait(true);
                if (!accepted)
                    return new LauncherLaunchResult(dryRunText, $"已取消: {entryName}");
            }

            start(plan);
            return new LauncherLaunchResult(dryRunText, $"已启动: {entryName}");
        }
        catch (Exception ex)
        {
            return new LauncherLaunchResult(dryRunText, $"启动失败: {entryName} — {ex.Message}");
        }
    }

    public static async Task<LauncherLaunchResult> LaunchBatchAsync(
        IReadOnlyList<LauncherLaunchItem> selected,
        bool requireConfirmation,
        Func<IReadOnlyList<LaunchEntry>, Task<bool>> confirmBatchLaunchAsync,
        Action<LaunchPlan> start)
    {
        if (selected.Count == 0)
            return new LauncherLaunchResult(null, "未选择任何启动项");

        string? dryRunText = null;
        try
        {
            if (requireConfirmation)
            {
                var accepted = await confirmBatchLaunchAsync(selected.Select(item => item.Entry).ToList()).ConfigureAwait(true);
                if (!accepted)
                    return new LauncherLaunchResult(null, $"已取消: 批量启动 {selected.Count} 项");
            }

            var ok = 0;
            var fail = 0;
            var failures = new List<string>();
            foreach (var item in selected)
            {
                try
                {
                    var plan = BuildPlan(item.Entry, requireConfirmation);
                    dryRunText = FormatPlan(plan);
                    start(plan);
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    failures.Add($"{item.Name}: {ex.Message}");
                }
            }

            return new LauncherLaunchResult(
                dryRunText,
                fail == 0
                    ? $"已启动 {ok} 个勾选项"
                    : $"已启动 {ok} 个勾选项，{fail} 个失败：{FormatLaunchFailures(failures)}");
        }
        catch (Exception ex)
        {
            return new LauncherLaunchResult(dryRunText, $"批量启动失败: {ex.Message}");
        }
    }

    private static LaunchPlan BuildPlan(LaunchEntry entry, bool requireConfirmation)
    {
        return LaunchArgumentBuilder.Build(entry, validateDirectory: true, requireConfirmation);
    }

    private static string FormatPlan(LaunchPlan plan)
    {
        return $"{plan.Executable} {plan.Arguments}";
    }

    private static string FormatLaunchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += $"；…另有 {failures.Count - 3} 项";

        return preview;
    }
}

internal readonly record struct LauncherLaunchItem(string Name, LaunchEntry Entry);

internal readonly record struct LauncherLaunchResult(string? DryRunText, string StatusText);
