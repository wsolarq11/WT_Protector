using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Dialogs;

public interface ICommandEditApplier
{
    public CommandManagementResult Apply(CommandManagementEdit edit);
}

public interface IDirectoryEditApplier
{
    public DirectoryManagementResult Apply(DirectoryManagementEdit edit);
}

public interface IDialogService
{
    public Task<bool> ConfirmLaunchAsync(LauncherSingleLaunchConfirmation confirmation);
    public Task<bool> ConfirmBatchLaunchAsync(LauncherBatchLaunchConfirmation confirmation);
    public Task<string?> RequestNewPresetAsync(LauncherPresetSaveTarget target, HashSet<string> existingNames);
    public Task<bool> ConfirmDeletePresetAsync(LauncherPresetDeleteTarget target);
    public Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? prefill, int presetReferenceCount, IReadOnlyDictionary<string, string>? commandTemplates);
    public Task<bool> ConfirmDeleteEntryAsync(LauncherEntryDeleteTarget target);
    public Task<DiscoveryDialogResult?> DiscoverAgentsAsync(
        HashSet<string> alreadyAdded,
        IReadOnlyList<DirectoryRow> existingDirectories,
        IReadOnlyDictionary<string, string> commandTemplates);
    public Task OpenCommandManagementAsync(IReadOnlyList<CommandRow> initialCommandRows, IReadOnlyList<TemplateRow> initialTemplateRows, ICommandEditApplier applyEdit);
    public Task OpenDirectoryManagementAsync(IReadOnlyList<DirectoryRow> initialDirectoryRows, IDirectoryEditApplier applyEdit);
    public Task<string?> PickExportPathAsync();
    public Task<string?> PickImportPathAsync();
    public Task<LauncherImportPlan?> RequestImportAsync(LauncherStoreFile current, LauncherStoreFile imported);
    public Task OpenRunHistoryAsync(IReadOnlyList<LauncherRunHistoryEntry> history);
    public Task OpenUpdateCheckAsync(string currentVersion, string? manifestUrl);
    public Task<AccentColorResult?> OpenAccentColorAsync(string currentColor, ImmutableList<string> presets);
}
