using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryManagementWorkflow
{
    public static LauncherEntryManagementResult AddEntry(
        EntryDraft draft,
        LauncherEntryMutationSnapshot snapshot,
        ObservableCollection<LauncherEntryViewModel> entries,
        List<string> customEntryOrder,
        Func<LaunchEntry, LauncherEntryViewModel> createViewModel,
        string updatedAtUtc,
        Func<bool, LauncherEntryMutationSnapshot, bool> saveChanges,
        Action<LauncherEntryMutationSnapshot> restoreSnapshot)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(createViewModel);
        ArgumentNullException.ThrowIfNull(saveChanges);
        ArgumentNullException.ThrowIfNull(restoreSnapshot);

        var result = LauncherEntryWorkflow.AddEntry(
            draft,
            entries,
            customEntryOrder,
            createViewModel,
            updatedAtUtc);
        if (!result.Succeeded)
            return LauncherEntryManagementResult.Status(result.Message ?? "新增按钮失败");

        if (!saveChanges(false, snapshot))
        {
            restoreSnapshot(snapshot);
            return LauncherEntryManagementResult.NoChange();
        }

        return LauncherEntryManagementResult.Status($"已新增按钮: {result.EntryName}（已保存）");
    }

    public static LauncherEntryManagementResult EditEntry(
        LauncherEntryViewModel item,
        EntryDraft draft,
        LauncherEntryMutationSnapshot snapshot,
        IReadOnlyList<LauncherEntryViewModel> entries,
        IList<LauncherPreset> presets,
        List<string> customEntryOrder,
        string updatedAtUtc,
        Func<bool, LauncherEntryMutationSnapshot, bool> saveChanges,
        Action<LauncherEntryMutationSnapshot> restoreSnapshot)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(saveChanges);
        ArgumentNullException.ThrowIfNull(restoreSnapshot);

        var result = LauncherEntryWorkflow.EditEntry(
            item,
            draft,
            entries,
            presets,
            customEntryOrder,
            updatedAtUtc);
        if (!result.Succeeded)
            return LauncherEntryManagementResult.Status(result.Message ?? "更新按钮失败");

        if (!saveChanges(true, snapshot))
        {
            restoreSnapshot(snapshot);
            return LauncherEntryManagementResult.NoChange();
        }

        return LauncherEntryManagementResult.Status($"已更新按钮: {result.EntryName}（已保存）");
    }

    public static LauncherEntryManagementResult DeleteEntry(
        LauncherEntryViewModel item,
        LauncherEntryMutationSnapshot snapshot,
        ObservableCollection<LauncherEntryViewModel> entries,
        IList<LauncherPreset> presets,
        List<string> customEntryOrder,
        Func<bool, LauncherEntryMutationSnapshot, bool> saveChanges,
        Action<LauncherEntryMutationSnapshot> restoreSnapshot)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(saveChanges);
        ArgumentNullException.ThrowIfNull(restoreSnapshot);

        var entryName = LauncherEntryWorkflow.DeleteEntry(
            entries,
            item,
            presets,
            customEntryOrder);
        if (!saveChanges(true, snapshot))
        {
            restoreSnapshot(snapshot);
            return LauncherEntryManagementResult.NoChange();
        }

        return LauncherEntryManagementResult.Status($"已删除按钮: {entryName}（已保存）");
    }
}

public readonly record struct LauncherEntryManagementResult(string? StatusText)
{
    public static LauncherEntryManagementResult NoChange()
    {
        return new LauncherEntryManagementResult(null);
    }

    public static LauncherEntryManagementResult Status(string statusText)
    {
        return new LauncherEntryManagementResult(statusText);
    }
}
