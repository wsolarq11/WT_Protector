using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

public sealed class EntryService : IEntryService, IEntryDraftValidator
{
    private readonly IDialogService _dialog;
    private readonly EntryDraftValidator _validator = new();

    public EntryService(IDialogService dialog)
    {
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    public bool TryNormalizeRequiredDraft(EntryDraft rawDraft, out EntryDraft draft, out string? requiredFieldError) =>
        _validator.TryNormalizeRequiredDraft(rawDraft, out draft, out requiredFieldError);

    public string NormalizeId(string? id) => _validator.NormalizeId(id);

    public bool IsUsableId(string id) => _validator.IsUsableId(id);

    public string NormalizeText(string? text) => _validator.NormalizeText(text);

    public string ResolveAutoId(string? name, DateTime now) => _validator.ResolveAutoId(name, now);

    public async Task<ServiceResult<LaunchEntry>> AddEntryAsync(
        IEnumerable<LaunchEntry> existingEntries,
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

        var result = CreateEntryFromDraft(draft, originalId: null, previousEntry: null, existingEntries);
        if (result is not ServiceSuccess<LaunchEntry> success)
            return result;

        return new ServiceSuccess<LaunchEntry>(success.Value with { UpdatedAtUtc = utcNow().ToString("O") });
    }

    public async Task<ServiceResult<LaunchEntry>> CloneEntryAsync(
        LauncherEntryViewModel source,
        IEnumerable<string> existingEntryIds,
        IEnumerable<LaunchEntry> existingEntries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow)
    {
        var sourceEntry = source.SnapshotEntry();
        var cloneSource = new LaunchEntry
        {
            Id = GenerateUniqueCloneId(sourceEntry.Id, existingEntryIds),
            Name = sourceEntry.Name + LauncherLocalization.Format("entry.copySuffix"),
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

        var result = CreateEntryFromDraft(draft, originalId: null, previousEntry: null, existingEntries);
        if (result is not ServiceSuccess<LaunchEntry> success)
            return result;

        return new ServiceSuccess<LaunchEntry>(success.Value with { UpdatedAtUtc = utcNow().ToString("O") });
    }

    public async Task<ServiceResult<LaunchEntry>> EditEntryAsync(
        LauncherEntryViewModel item,
        IEnumerable<LaunchEntry> existingEntries,
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
        return CreateEntryFromDraft(draft, oldId, item.SnapshotEntry(), existingEntries);
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
            return new ServiceFailure<LaunchEntry>(LauncherStatusContent.Error(validation));

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

public sealed class EntryDraftValidator : IEntryDraftValidator
{
    public bool TryNormalizeRequiredDraft(EntryDraft rawDraft, out EntryDraft draft, out string? requiredFieldError) =>
        LauncherEntryDrafts.TryNormalizeRequiredDraft(rawDraft, out draft, out requiredFieldError);

    public string NormalizeId(string? id) => EntryId.Normalize(id);

    public bool IsUsableId(string id) => EntryId.IsUsable(id);

    public string NormalizeText(string? text) => EntryName.Normalize(text);

    public string ResolveAutoId(string? name, DateTime now)
    {
        var derived = NormalizeId(name);
        return derived.Length > 0
            ? derived
            : $"entry-{now:yyyyMMddHHmmss}";
    }
}
