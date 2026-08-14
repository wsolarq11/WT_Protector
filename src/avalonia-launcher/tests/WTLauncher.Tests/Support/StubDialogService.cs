using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

internal sealed class StubDialogService : IDialogService
{
    public Func<LauncherSingleLaunchConfirmation, Task<bool>>? OnConfirmLaunch { get; set; }
    public Func<LauncherBatchLaunchConfirmation, Task<bool>>? OnConfirmBatchLaunch { get; set; }
    public Func<LauncherPresetSaveTarget, HashSet<string>, Task<string?>>? OnRequestNewPreset { get; set; }
    public Func<LauncherPresetDeleteTarget, Task<bool>>? OnConfirmDeletePreset { get; set; }
    public Func<LaunchEntry?, int, IReadOnlyDictionary<string, string>?, Task<EntryDraft?>>? OnRequestEntryDraft { get; set; }
    public Func<LauncherEntryDeleteTarget, Task<bool>>? OnConfirmDeleteEntry { get; set; }
    public Func<HashSet<string>, IReadOnlyList<DirectoryRow>, IReadOnlyDictionary<string, string>, Task<DiscoveryDialogResult?>>? OnDiscoverAgents { get; set; }
    public Func<IReadOnlyList<CommandRow>, IReadOnlyList<TemplateRow>, Func<CommandManagementEdit, CommandManagementResult>, Task>? OnOpenCommandManagement { get; set; }
    public Func<IReadOnlyList<DirectoryRow>, Func<DirectoryManagementEdit, DirectoryManagementResult>, Task>? OnOpenDirectoryManagement { get; set; }
    public Func<Task<string?>>? OnPickExportPath { get; set; }
    public Func<Task<string?>>? OnPickImportPath { get; set; }
    public Func<LauncherStoreFile, LauncherStoreFile, Task<LauncherImportPlan?>>? OnRequestImport { get; set; }
    public Func<IReadOnlyList<LauncherRunHistoryEntry>, Task>? OnOpenRunHistory { get; set; }
    public Func<string, string?, Task>? OnOpenUpdateCheck { get; set; }

    public Task<bool> ConfirmLaunchAsync(LauncherSingleLaunchConfirmation confirmation) =>
        OnConfirmLaunch?.Invoke(confirmation) ?? Task.FromResult(false);

    public Task<bool> ConfirmBatchLaunchAsync(LauncherBatchLaunchConfirmation confirmation) =>
        OnConfirmBatchLaunch?.Invoke(confirmation) ?? Task.FromResult(false);

    public Task<string?> RequestNewPresetAsync(LauncherPresetSaveTarget target, HashSet<string> existingNames) =>
        OnRequestNewPreset?.Invoke(target, existingNames) ?? Task.FromResult<string?>(null);

    public Task<bool> ConfirmDeletePresetAsync(LauncherPresetDeleteTarget target) =>
        OnConfirmDeletePreset?.Invoke(target) ?? Task.FromResult(false);

    public Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? prefill, int presetReferenceCount, IReadOnlyDictionary<string, string>? commandTemplates) =>
        OnRequestEntryDraft?.Invoke(prefill, presetReferenceCount, commandTemplates) ?? Task.FromResult<EntryDraft?>(null);

    public Task<bool> ConfirmDeleteEntryAsync(LauncherEntryDeleteTarget target) =>
        OnConfirmDeleteEntry?.Invoke(target) ?? Task.FromResult(false);

    public Task<DiscoveryDialogResult?> DiscoverAgentsAsync(
        HashSet<string> alreadyAdded,
        IReadOnlyList<DirectoryRow> existingDirectories,
        IReadOnlyDictionary<string, string> commandTemplates) =>
        OnDiscoverAgents?.Invoke(alreadyAdded, existingDirectories, commandTemplates)
        ?? Task.FromResult<DiscoveryDialogResult?>(null);

    public Task OpenCommandManagementAsync(IReadOnlyList<CommandRow> initialCommandRows, IReadOnlyList<TemplateRow> initialTemplateRows, Func<CommandManagementEdit, CommandManagementResult> applyEdit) =>
        OnOpenCommandManagement?.Invoke(initialCommandRows, initialTemplateRows, applyEdit) ?? Task.CompletedTask;

    public Task OpenDirectoryManagementAsync(IReadOnlyList<DirectoryRow> initialDirectoryRows, Func<DirectoryManagementEdit, DirectoryManagementResult> applyEdit) =>
        OnOpenDirectoryManagement?.Invoke(initialDirectoryRows, applyEdit) ?? Task.CompletedTask;

    public Task<string?> PickExportPathAsync() =>
        OnPickExportPath?.Invoke() ?? Task.FromResult<string?>(null);

    public Task<string?> PickImportPathAsync() =>
        OnPickImportPath?.Invoke() ?? Task.FromResult<string?>(null);

    public Task<LauncherImportPlan?> RequestImportAsync(LauncherStoreFile current, LauncherStoreFile imported) =>
        OnRequestImport?.Invoke(current, imported) ?? Task.FromResult<LauncherImportPlan?>(null);

    public Task OpenRunHistoryAsync(IReadOnlyList<LauncherRunHistoryEntry> history) =>
        OnOpenRunHistory?.Invoke(history) ?? Task.CompletedTask;

    public Task OpenUpdateCheckAsync(string currentVersion, string? manifestUrl) =>
        OnOpenUpdateCheck?.Invoke(currentVersion, manifestUrl) ?? Task.CompletedTask;
}
