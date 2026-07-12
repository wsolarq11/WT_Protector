using System.Collections.ObjectModel;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

public sealed class EntryService : IEntryService
{
    private readonly IDialogService _dialog;

    public EntryService(IDialogService dialog)
    {
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    public async Task<(LaunchEntry? Entry, string? Status)> AddEntryAsync(
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow)
    {
        EntryDraft? draft;
        try
        {
            draft = await _dialog.RequestEntryDraftAsync(null, presetReferenceCount, commandTemplates).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddEntry");
            return (null, LauncherEntryStatus.AddDialogFailed(ex));
        }

        if (draft is null)
            return (null, LauncherEntryStatus.AddCanceled);

        var entry = CreateEntryFromDraft(draft, originalId: null, previousEntry: null, entries.Select(e => e.SnapshotEntry()), out var validation);
        if (entry is null)
            return (null, validation);

        entry = entry with { UpdatedAtUtc = utcNow().ToString("O") };
        return (entry, null);
    }

    public async Task<(LaunchEntry? Entry, string? Status)> CloneEntryAsync(
        LauncherEntryViewModel source,
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow)
    {
        var sourceEntry = source.SnapshotEntry();
        var cloneSource = new LaunchEntry
        {
            Id = GenerateUniqueCloneId(sourceEntry.Id, entries.Select(e => e.Id)),
            Name = sourceEntry.Name + " (副本)",
            Directory = sourceEntry.Directory,
            Command = sourceEntry.Command,
            Selected = false,
        };

        EntryDraft? draft;
        try
        {
            draft = await _dialog.RequestEntryDraftAsync(cloneSource, presetReferenceCount, commandTemplates).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CloneEntry");
            return (null, LauncherEntryStatus.AddDialogFailed(ex));
        }

        if (draft is null)
            return (null, LauncherEntryStatus.AddCanceled);

        var entry = CreateEntryFromDraft(draft, originalId: null, previousEntry: null, entries.Select(e => e.SnapshotEntry()), out var validation);
        if (entry is null)
            return (null, validation);

        entry = entry with { UpdatedAtUtc = utcNow().ToString("O") };
        return (entry, null);
    }

    public async Task<(LaunchEntry? Updated, string? Status)> EditEntryAsync(
        LauncherEntryViewModel item,
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherPresetFile presetFile,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow)
    {
        EntryDraft? draft;
        try
        {
            draft = await _dialog.RequestEntryDraftAsync(item.SnapshotEntry(), presetReferenceCount, commandTemplates).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "EditEntry.Draft");
            return (null, LauncherEntryStatus.EditDialogFailed(item.Name, ex));
        }

        if (draft is null)
            return (null, LauncherEntryStatus.EditCanceled(item.Name));

        var oldId = item.Id;
        var updated = CreateEntryFromDraft(draft, oldId, item.SnapshotEntry(), entries.Select(e => e.SnapshotEntry()), out var validation);
        if (updated is null)
            return (null, validation);

        return (updated, null);
    }

    public async Task<(string? Status, string? ClearKeyboardFocusId)> DeleteEntryAsync(
        LauncherEntryViewModel item,
        string? keyboardFocusedEntryId)
    {
        var target = new LauncherEntryDeleteTarget(item.SnapshotEntry());
        bool accepted;
        try
        {
            accepted = await _dialog.ConfirmDeleteEntryAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeleteConfirmation");
            return (LauncherEntryStatus.DeleteConfirmationFailed(target.EntryName, ex), null);
        }

        if (!accepted)
            return (LauncherEntryStatus.DeleteCanceled(target.EntryName), null);

        var clearFocus = (keyboardFocusedEntryId is not null && EntryId.Same(keyboardFocusedEntryId, target.EntryId))
            ? target.EntryId
            : null;

        return (null, clearFocus);
    }

    public LaunchEntry? CreateEntryFromDraft(EntryDraft draft, string? originalId, LaunchEntry? previousEntry, IEnumerable<LaunchEntry> existingEntries, out string? validationMessage)
    {
        validationMessage = null;
        var entry = LauncherEntryDrafts.CreateEntry(draft, previousEntry);
        var validation = LauncherEntryDrafts.ValidateEntry(entry, originalId, existingEntries);
        if (validation is not null)
        {
            validationMessage = validation;
            return null;
        }
        return entry;
    }

    public string GenerateUniqueCloneId(string sourceId, IEnumerable<string> existingIds)
    {
        var normalizedSourceId = EntryId.Normalize(sourceId);
        var baseId = normalizedSourceId + "-copy";
        var candidate = baseId;
        var index = 2;
        var idSet = existingIds.ToHashSet(EntryId.Comparer);
        while (idSet.Contains(candidate))
        {
            if (index > 10000)
                throw new InvalidOperationException("Cannot generate a unique clone ID — too many copies exist.");
            candidate = $"{baseId}-{index++}";
        }
        return candidate;
    }
}
