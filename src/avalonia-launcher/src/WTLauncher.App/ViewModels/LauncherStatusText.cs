using WTLauncher.App.Features;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// User-facing status text lives in Localization/Assets/zh-CN.json; these
// containers only resolve keys and shape LauncherStatusContent values.

public sealed record LauncherSortModeViewModel(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record LauncherDisplayModeViewModel(LauncherEntryViewMode Mode, string Label)
{
    public string Id => LauncherEntryViewModes.ToId(Mode);

    public override string ToString() => Label;
}

public static class LauncherMainWindowText
{
    public static LauncherStatusContent Ready => LauncherStatusContent.Neutral(LauncherLocalization.Format("main.ready"));

    public static string DryRunPlaceholder => LauncherLocalization.Format("main.dryRunPlaceholder");

    public static string PresetSelectionPlaceholder => LauncherLocalization.Format("main.presetSelectionPlaceholder");

    public static string StorePathDisplay(string storePath) =>
        LauncherLocalization.Format("main.storePath", storePath);

    public static LauncherStatusContent Loaded(int entryCount, int presetCount) =>
        entryCount == 0
            ? LauncherStatusContent.Neutral(LauncherLocalization.Format("main.loaded.empty"))
            : LauncherStatusContent.Neutral(LauncherLocalization.Format("main.loaded", entryCount, presetCount));

    public static LauncherStatusContent ConfigLoadFailed(string detail) =>
        LauncherStatusContent.Error(LauncherLocalization.Format("main.configLoadFailed", detail));

    public static LauncherStatusContent ConfigValidationFailed(string detail) =>
        LauncherStatusContent.Error(LauncherLocalization.Format("main.configValidationFailed", detail));

    public static LauncherStatusContent UndefinedCommandTemplateKeys(string keys) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("main.undefinedCommandTemplateKeys", keys));
}

public static class LauncherRefreshStatus
{
    public static LauncherStatusContent DialogOpenFailed(string dialogName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("refresh.dialogOpenFailed", dialogName, ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent DialogCanceled(string action) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("refresh.dialogCanceled", action));

    public static LauncherStatusContent SaveGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("refresh.saveGroupFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent AddGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("refresh.addGroupFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent DeleteGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("refresh.deleteGroupFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent GroupAlreadyUpToDate(string groupName) =>
        LauncherStatusContent.Neutral(LauncherLocalization.Format("refresh.groupAlreadyUpToDate", groupName));

    public static LauncherStatusContent SaveFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("refresh.saveFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent DiscoveryScanning =>
        LauncherStatusContent.Neutral(LauncherLocalization.Format("refresh.discoveryScanning"));

    public static LauncherStatusContent DiscoveryFailed =>
        LauncherStatusContent.Error(LauncherLocalization.Format("refresh.discoveryFailed"));

    public static LauncherStatusContent GroupNoEntries =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("refresh.groupNoEntries"));

    public static LauncherStatusContent GroupNoEntriesCannotSave =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("refresh.groupNoEntriesCannotSave"));

    public static LauncherStatusContent CanceledBatch =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("refresh.canceledBatch"));
}

public static class LauncherDiscoveryStatus
{
    public static LauncherStatusContent AgentsAdded(int addedCount, IReadOnlyList<string> skipped)
    {
        ArgumentNullException.ThrowIfNull(skipped);
        return skipped.Count == 0
            ? LauncherStatusContent.Success(LauncherLocalization.Format("discovery.agentsAdded", addedCount))
            : LauncherStatusContent.Warning(
                LauncherLocalization.Format("discovery.agentsAddedSkipped", addedCount, skipped.Count, string.Join(", ", skipped)));
    }
}

public static class LauncherGroupStatus
{
    public static LauncherStatusContent BatchCompleted(LauncherBatchLaunchReport report) =>
        report.FailedCount == 0
            ? LauncherStatusContent.Success(LauncherLocalization.Format("group.batchCompleted", report.RequestedCount))
            : LauncherStatusContent.Warning(
                LauncherLocalization.Format("group.batchCompletedFailed", report.RequestedCount, report.FailedCount));
}

public static class ExceptionMessageFormatter
{
    /// <summary>
    /// Sanitize exception messages for user-facing display. File-system
    /// exceptions leak paths in their Message property; resolve stable
    /// localized strings instead.
    /// </summary>
    public static string ForUser(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            FileNotFoundException => LauncherLocalization.Format("errors.fileNotFound"),
            DirectoryNotFoundException => LauncherLocalization.Format("errors.directoryNotFound"),
            UnauthorizedAccessException => LauncherLocalization.Format("errors.unauthorized"),
            IOException => LauncherLocalization.Format("errors.io"),
            // Application-layer exceptions carry intentionally user-safe messages.
            InvalidDataException or ArgumentException or InvalidOperationException => exception.Message,
            _ => LauncherLocalization.Format("errors.generic"),
        };
    }
}

public static class LauncherPersistenceStatus
{
    public static LauncherStatusContent StoreSaveFailed(Exception exception) =>
        Failed("persistence.storeSaveFailed", exception);

    public static LauncherStatusContent ConfigSaveFailed(Exception exception) =>
        Failed("persistence.configSaveFailed", exception);

    public static LauncherStatusContent PresetSaveFailed(Exception exception) =>
        Failed("persistence.presetSaveFailed", exception);

    public static LauncherStatusContent SettingsSaveFailed(Exception exception) =>
        Failed("persistence.settingsSaveFailed", exception);

    private static LauncherStatusContent Failed(string key, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format(key, ExceptionMessageFormatter.ForUser(exception)));
    }
}

public static class LauncherEntryStatus
{
    public static LauncherStatusContent AddCanceled =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("entry.addCanceled"));

    public static LauncherStatusContent AddDialogFailed(Exception exception) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("entry.addDialogFailed", ExceptionMessageFormatter.ForUser(exception)));

    public static LauncherStatusContent EditCanceled(string entryName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("entry.editCanceled", entryName));

    public static LauncherStatusContent EditDialogFailed(string entryName, Exception exception) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("entry.editDialogFailed", entryName, ExceptionMessageFormatter.ForUser(exception)));

    public static LauncherStatusContent DeleteCanceled(string entryName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("entry.deleteCanceled", entryName));

    public static LauncherStatusContent DeleteConfirmationFailed(string entryName, Exception exception) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("entry.deleteConfirmationFailed", entryName, ExceptionMessageFormatter.ForUser(exception)));

    public static LauncherStatusContent Added(string entryName) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("entry.added", entryName));

    public static LauncherStatusContent Updated(string entryName) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("entry.updated", entryName));

    public static LauncherStatusContent Deleted(string entryName) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("entry.deleted", entryName));
}

public static class LauncherPresetStatus
{
    public static LauncherStatusContent NoPresetSelected =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.noSelected"));

    public static LauncherStatusContent SaveCanceled =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.saveCanceled"));

    public static LauncherStatusContent SaveDialogFailed(Exception exception) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.saveDialogFailed", ExceptionMessageFormatter.ForUser(exception)));

    public static LauncherStatusContent NameRequired =>
        LauncherStatusContent.Error(LauncherLocalization.Format("preset.nameRequired"));

    public static LauncherStatusContent CurrentPresetMissing =>
        LauncherStatusContent.Error(LauncherLocalization.Format("preset.currentMissing"));

    public static LauncherStatusContent AddFailedInvalidEntryIds(IReadOnlyList<string> invalidIds) =>
        LauncherStatusContent.Error(LauncherLocalization.Format("preset.invalidEntryIds", string.Join(", ", invalidIds)));

    public static LauncherStatusContent UpdateFailedInvalidEntryIds(IReadOnlyList<string> invalidIds) =>
        LauncherStatusContent.Error(LauncherLocalization.Format("preset.invalidEntryIds", string.Join(", ", invalidIds)));

    public static LauncherStatusContent MissingEntryReferences(IEnumerable<string> entryIds) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.missingEntryReferences", string.Join(", ", entryIds)));

    public static LauncherStatusContent NoPresetEntries(string presetName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.noEntries", presetName));

    public static LauncherStatusContent Applied(string presetName, int entryCount) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("preset.applied", presetName, entryCount));

    public static LauncherStatusContent Previewed(string presetName, int entryCount) =>
        LauncherStatusContent.Neutral(LauncherLocalization.Format("preset.previewed", presetName, entryCount));

    public static LauncherStatusContent PreviewFailed(string presetName, LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.previewFailedReport", presetName, report.FailedCount));
    }

    public static LauncherStatusContent PreviewFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.previewFailed", presetName, ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent Launched(string presetName, LauncherBatchLaunchReport report)
    {
        var batch = LauncherSelectionStatus.BatchLaunchCompleted(report, LauncherLocalization.Format("selection.entryItemLabel"));
        return new LauncherStatusContent(
            LauncherLocalization.Format("preset.launched", presetName, batch.Text),
            batch.Tone);
    }

    public static LauncherStatusContent LaunchCanceled(string presetName, int entryCount) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.launchCanceled", presetName, entryCount));

    public static LauncherStatusContent LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.launchFailed", presetName, ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent Saved(string presetName, int entryCount) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("preset.saved", presetName, entryCount));

    public static LauncherStatusContent UpdateFailed(string presetName, Exception exception) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.updateFailed", presetName, ExceptionMessageFormatter.ForUser(exception)));

    public static LauncherStatusContent UpdateBlocked(string presetName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.updateBlocked", presetName));

    public static LauncherStatusContent Updated(string presetName, int entryCount) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("preset.updated", presetName, entryCount));

    public static LauncherStatusContent DeleteBlocked(string presetName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.deleteBlocked", presetName));

    public static LauncherStatusContent DeleteCanceled(string presetName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("preset.deleteCanceled", presetName));

    public static LauncherStatusContent DeleteConfirmationFailed(string presetName, Exception exception) =>
        LauncherStatusContent.Error(
            LauncherLocalization.Format("preset.deleteConfirmationFailed", presetName, ExceptionMessageFormatter.ForUser(exception)));

    public static LauncherStatusContent Deleted(string presetName, string selectedPresetName) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("preset.deleted", presetName, selectedPresetName));
}

public static class LauncherSelectionStatus
{
    public static LauncherStatusContent NoVisibleEntries =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("selection.noVisible"));

    public static LauncherStatusContent VisibleAlreadySelected =>
        LauncherStatusContent.Neutral(LauncherLocalization.Format("selection.visibleAlreadySelected"));

    public static LauncherStatusContent NoSelectedEntries =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("selection.noSelected"));

    public static LauncherStatusContent NoSelectedEntriesToClear =>
        LauncherStatusContent.Neutral(LauncherLocalization.Format("selection.noSelectedToClear"));

    public static LauncherStatusContent SelectionRequired =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("selection.selectionRequired"));

    public static LauncherStatusContent VisibleSelected(int entryCount) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("selection.visibleSelected", entryCount));

    public static LauncherStatusContent Cleared(int entryCount) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("selection.cleared", entryCount));

    public static LauncherStatusContent BatchLaunchCanceled(int entryCount) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("selection.batchLaunchCanceled", entryCount));

    public static LauncherStatusContent BatchLaunchCompleted(LauncherBatchLaunchReport report)
    {
        return BatchLaunchCompleted(report, LauncherLocalization.Format("selection.batchItemLabel"));
    }

    public static LauncherStatusContent BatchLaunchCompleted(LauncherBatchLaunchReport report, string itemLabel)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);
        return report.FailedCount == 0
            ? LauncherStatusContent.Success(
                LauncherLocalization.Format("selection.batchCompleted", report.RequestedCount, itemLabel))
            : LauncherStatusContent.Warning(
                LauncherLocalization.Format(
                    "selection.batchCompletedFailed",
                    report.RequestedCount,
                    itemLabel,
                    report.FailedCount,
                    FormatSkipped(report),
                    FormatBatchFailures(report.FailureMessages)));
    }

    private static string FormatSkipped(LauncherBatchLaunchReport report) =>
        report.SkippedCount == 0 ? "" : LauncherLocalization.Format("selection.skipped", report.SkippedCount);

    public static LauncherStatusContent BatchLaunchFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("selection.batchLaunchFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    private static string FormatBatchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += LauncherLocalization.Format("selection.moreFailures", failures.Count - 3);

        return preview;
    }
}

public static class LauncherSettingsStatus
{
    public static LauncherStatusContent SortModeSaved(string modeLabel) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("settings.sortModeSaved", modeLabel));

    public static LauncherStatusContent DisplayModeSaved(string modeLabel) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("settings.displayModeSaved", modeLabel));

    public static LauncherStatusContent LaunchConfirmationSaved(bool requireConfirmation) =>
        requireConfirmation
            ? LauncherStatusContent.Success(LauncherLocalization.Format("settings.launchConfirmationAll"))
            : LauncherStatusContent.Success(LauncherLocalization.Format("settings.launchConfirmationDirect"));

    public static LauncherStatusContent WindowBoundsSaveFailed =>
        LauncherStatusContent.Error(LauncherLocalization.Format("settings.windowBoundsSaveFailed"));
}

// Launch receipt text classes moved to LauncherLaunchReceiptText.cs as part of
// status-text domain decomposition.

public static class LauncherDragReorderStatus
{
    public static LauncherStatusContent Saved =>
        LauncherStatusContent.Success(LauncherLocalization.Format("drag.saved"));

    public static LauncherStatusContent Canceled =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("drag.canceled"));

    public static LauncherStatusContent Moved(string entryName) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("drag.moved", entryName));

    public static LauncherStatusContent MoveUnavailable =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("drag.moveUnavailable"));
}
