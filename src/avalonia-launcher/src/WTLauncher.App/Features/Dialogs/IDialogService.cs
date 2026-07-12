using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Dialogs;

public interface IDialogService
{
    public Task<bool> ConfirmLaunchAsync(LauncherSingleLaunchConfirmation confirmation);
    public Task<bool> ConfirmBatchLaunchAsync(LauncherBatchLaunchConfirmation confirmation);
    public Task<string?> RequestNewPresetAsync(LauncherPresetSaveTarget target, HashSet<string> existingNames);
    public Task<bool> ConfirmDeletePresetAsync(LauncherPresetDeleteTarget target);
    public Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? prefill, int presetReferenceCount, IReadOnlyDictionary<string, string>? commandTemplates);
    public Task<bool> ConfirmDeleteEntryAsync(LauncherEntryDeleteTarget target);
    public Task<IReadOnlyList<DiscoveredAgent>?> DiscoverAgentsAsync(HashSet<string> alreadyAdded);
    public Task OpenCommandManagementAsync(IReadOnlyList<CommandRow> initialCommandRows, IReadOnlyList<TemplateRow> initialTemplateRows, Func<CommandManagementEdit, CommandManagementResult> applyEdit);
    public Task OpenDirectoryManagementAsync(IReadOnlyList<DirectoryRow> initialDirectoryRows, Func<DirectoryManagementEdit, DirectoryManagementResult> applyEdit);
}
