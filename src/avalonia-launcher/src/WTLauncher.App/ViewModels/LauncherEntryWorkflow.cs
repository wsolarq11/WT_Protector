using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

internal static class LauncherEntryWorkflow
{
    public static LauncherEntryWorkflowResult AddEntry(
        EntryDraft draft,
        ObservableCollection<LauncherEntryViewModel> entries,
        List<string> customEntryOrder,
        Func<LaunchEntry, LauncherEntryViewModel> createViewModel,
        string updatedAtUtc)
    {
        var entry = LauncherEntryDrafts.CreateEntry(draft, previousEntry: null);
        var validation = LauncherEntryDrafts.ValidateEntry(entry, originalId: null, entries.Select(existing => existing.Entry));
        if (validation is not null)
            return LauncherEntryWorkflowResult.Failed(validation);

        entry.UpdatedAtUtc = updatedAtUtc;
        entries.Add(createViewModel(entry));
        customEntryOrder.Add(entry.Id);
        return LauncherEntryWorkflowResult.Success(entry.Name);
    }

    public static LauncherEntryWorkflowResult EditEntry(
        LauncherEntryViewModel item,
        EntryDraft draft,
        IReadOnlyList<LauncherEntryViewModel> entries,
        IList<LauncherPreset> presets,
        List<string> customEntryOrder,
        string updatedAtUtc)
    {
        var oldId = item.Entry.Id;
        var updated = LauncherEntryDrafts.CreateEntry(draft, item.Entry);
        var validation = LauncherEntryDrafts.ValidateEntry(updated, oldId, entries.Select(existing => existing.Entry));
        if (validation is not null)
            return LauncherEntryWorkflowResult.Failed(validation);

        item.Entry.Id = updated.Id;
        item.Entry.Name = updated.Name;
        item.Entry.Directory = updated.Directory;
        item.Entry.Command = updated.Command;
        item.Entry.Confirm = updated.Confirm;
        item.SetSelectionFromOwner(updated.Selected);
        item.Entry.UpdatedAtUtc = updatedAtUtc;
        item.RefreshDisplay();
        if (!string.Equals(oldId, item.Entry.Id, StringComparison.Ordinal))
            LauncherEntryReferences.Rename(oldId, item.Entry.Id, presets, customEntryOrder);

        return LauncherEntryWorkflowResult.Success(item.Name);
    }

    public static string DeleteEntry(
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherEntryViewModel item,
        IList<LauncherPreset> presets,
        List<string> customEntryOrder)
    {
        var entryName = item.Name;
        entries.Remove(item);
        LauncherEntryReferences.Remove(item.Entry.Id, presets, customEntryOrder);
        return entryName;
    }
}

internal readonly record struct LauncherEntryWorkflowResult(bool Succeeded, string? Message, string? EntryName)
{
    public static LauncherEntryWorkflowResult Failed(string message) => new(false, message, null);

    public static LauncherEntryWorkflowResult Success(string entryName) => new(true, null, entryName);
}
