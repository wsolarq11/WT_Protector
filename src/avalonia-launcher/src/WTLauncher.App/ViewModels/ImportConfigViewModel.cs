using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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
            warnings.Add($"{duplicateCount} 个条目与现有条目 id 重复");
        if (missingCount > 0)
            warnings.Add($"{missingCount} 个条目目录在本机不存在");
        if (preview.InvalidPresetReferences.Count > 0)
            warnings.Add($"{preview.InvalidPresetReferences.Count} 个预设引用无效，相关预设将被跳过");
        if (preview.DuplicatePresetIds.Count > 0)
            warnings.Add($"{preview.DuplicatePresetIds.Count} 个预设 id 与现有预设重复");
        return warnings.Count == 0
            ? "导入文件通过 schema 与引用检查。"
            : "需要处理：" + string.Join("；", warnings);
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
