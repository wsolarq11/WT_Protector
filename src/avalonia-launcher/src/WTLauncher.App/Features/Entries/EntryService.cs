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

    public async Task<ServiceResult<LaunchEntry>> AddEntryAsync(
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
            return new ServiceFailure<LaunchEntry>(LauncherEntryStatus.AddDialogFailed(ex));
        }

        if (draft is null)
            return new ServiceCanceled<LaunchEntry>(LauncherEntryStatus.AddCanceled);

        var result = CreateEntryFromDraft(draft, originalId: null, previousEntry: null, entries.Select(e => e.SnapshotEntry()));
        if (result is not ServiceSuccess<LaunchEntry> success)
            return result;

        return new ServiceSuccess<LaunchEntry>(success.Value with { UpdatedAtUtc = utcNow().ToString("O") });
    }

    public async Task<ServiceResult<LaunchEntry>> CloneEntryAsync(
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
            return new ServiceFailure<LaunchEntry>(LauncherEntryStatus.AddDialogFailed(ex));
        }

        if (draft is null)
            return new ServiceCanceled<LaunchEntry>(LauncherEntryStatus.AddCanceled);

        var result = CreateEntryFromDraft(draft, originalId: null, previousEntry: null, entries.Select(e => e.SnapshotEntry()));
        if (result is not ServiceSuccess<LaunchEntry> success)
            return result;

        return new ServiceSuccess<LaunchEntry>(success.Value with { UpdatedAtUtc = utcNow().ToString("O") });
    }

    public async Task<ServiceResult<LaunchEntry>> EditEntryAsync(
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
            return new ServiceFailure<LaunchEntry>(LauncherEntryStatus.EditDialogFailed(item.Name, ex));
        }

        if (draft is null)
            return new ServiceCanceled<LaunchEntry>(LauncherEntryStatus.EditCanceled(item.Name));

        var oldId = item.Id;
        return CreateEntryFromDraft(draft, oldId, item.SnapshotEntry(), entries.Select(e => e.SnapshotEntry()));
    }

    public async Task<ServiceResult<EntryDeleteApproval>> DeleteEntryAsync(
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
            return new ServiceFailure<EntryDeleteApproval>(LauncherEntryStatus.DeleteConfirmationFailed(target.EntryName, ex));
        }

        if (!accepted)
            return new ServiceCanceled<EntryDeleteApproval>(LauncherEntryStatus.DeleteCanceled(target.EntryName));

        var clearFocus = (keyboardFocusedEntryId is not null && EntryId.Same(keyboardFocusedEntryId, target.EntryId))
            ? target.EntryId
            : null;

        return new ServiceSuccess<EntryDeleteApproval>(new EntryDeleteApproval(target, clearFocus));
    }

    public ServiceResult<LaunchEntry> CreateEntryFromDraft(EntryDraft draft, string? originalId, LaunchEntry? previousEntry, IEnumerable<LaunchEntry> existingEntries)
    {
        var entry = LauncherEntryDrafts.CreateEntry(draft, previousEntry);
        var validation = LauncherEntryDrafts.ValidateEntry(entry, originalId, existingEntries);
        if (validation is not null)
            return new ServiceFailure<LaunchEntry>(validation);

        return new ServiceSuccess<LaunchEntry>(entry);
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
