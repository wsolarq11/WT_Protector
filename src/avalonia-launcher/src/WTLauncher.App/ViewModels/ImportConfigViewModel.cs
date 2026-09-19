using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class ImportConfigViewModel : ObservableObject
{
    private readonly LauncherStoreFile _imported;

    public ObservableCollection<ImportEntryRowViewModel> Rows { get; } = [];

    public string Summary { get; }

    [ObservableProperty]
    private bool _addNewMode = true;

    [ObservableProperty]
    private bool _replaceMode;

    public ImportConfigViewModel(LauncherStoreFile current, LauncherStoreFile imported)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(imported);
        _imported = imported;

        var preview = LauncherConfigExchange.AnalyzeImport(current, imported);
        foreach (var row in preview.Rows)
        {
            Rows.Add(new ImportEntryRowViewModel(row));
        }

        Summary = BuildSummary(preview);
    }

    partial void OnAddNewModeChanged(bool value)
    {
        if (value)
            ReplaceMode = false;
    }

    partial void OnReplaceModeChanged(bool value)
    {
        if (value)
            AddNewMode = false;
    }

    public LauncherImportPlan BuildPlan()
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows)
        {
            if (row.IsSkipped)
            {
                skipped.Add(row.Id);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.DirectoryOverride))
                overrides[row.Id] = row.DirectoryOverride.Trim();
        }

        return new LauncherImportPlan
        {
            Mode = ReplaceMode ? LauncherImportMode.Replace : LauncherImportMode.AddNew,
            DirectoryOverrides = overrides,
            SkippedEntryIds = skipped,
        };
    }

    private static string BuildSummary(LauncherImportPreview preview)
    {
        var duplicateCount = preview.Rows.Count(row => row.IsDuplicate);
        var missingCount = preview.Rows.Count(row => row.IsMissing);
        var warnings = new List<string>();
        if (duplicateCount > 0)
            warnings.Add(LauncherLocalization.Format("import.duplicateIds", duplicateCount));
        if (missingCount > 0)
            warnings.Add(LauncherLocalization.Format("import.missingDirs", missingCount));
        if (preview.InvalidPresetReferences.Count > 0)
            warnings.Add(LauncherLocalization.Format("import.invalidPresetRefs", preview.InvalidPresetReferences.Count));
        if (preview.DuplicatePresetIds.Count > 0)
            warnings.Add(LauncherLocalization.Format("import.duplicatePresetIds", preview.DuplicatePresetIds.Count));
        return warnings.Count == 0
            ? LauncherLocalization.Format("import.valid")
            : LauncherLocalization.Format("import.needHandling", string.Join("；", warnings));
    }
}

public sealed partial class ImportEntryRowViewModel : ObservableObject
{
    public string Id { get; }

    public string Name { get; }

    public string OriginalDirectory { get; }

    public bool IsDuplicate { get; }

    public bool IsMissing { get; }

    [ObservableProperty]
    private string _directoryOverride = "";

    [ObservableProperty]
    private bool _isSkipped;

    public ImportEntryRowViewModel(LauncherImportRow row)
    {
        Id = row.Id;
        Name = row.Name;
        OriginalDirectory = row.Directory;
        IsDuplicate = row.IsDuplicate;
        IsMissing = row.IsMissing;
        DirectoryOverride = row.IsMissing ? row.Directory : "";
    }
}
