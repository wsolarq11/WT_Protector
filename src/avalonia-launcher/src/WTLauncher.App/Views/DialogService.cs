using Avalonia.Controls;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>View-layer implementation of IDialogService.
/// Lives in the Views layer because it directly creates Window instances —
/// Features layer should only depend on the interface.</summary>
public sealed class DialogService : IDialogService
{
    private Window? _owner;

    /// <summary>Must be called by MainWindow after construction to enable ShowDialog.</summary>
    public void Initialize(Window owner) => _owner = owner;

    private Window Owner => _owner ?? throw new InvalidOperationException(
        "DialogService not initialized. Call Initialize(Window) with the owner window first.");

    public Task<bool> ConfirmLaunchAsync(LauncherSingleLaunchConfirmation confirmation) =>
        new ConfirmLaunchWindow(confirmation).ShowDialog<bool>(Owner);

    public Task<bool> ConfirmBatchLaunchAsync(LauncherBatchLaunchConfirmation confirmation) =>
        new ConfirmBatchLaunchWindow(confirmation).ShowDialog<bool>(Owner);

    public async Task<string?> RequestNewPresetAsync(LauncherPresetSaveTarget target, HashSet<string> existingNames) =>
        await new PresetDetailsWindow(target, existingNames).ShowDialog<string?>(Owner);

    public Task<bool> ConfirmDeletePresetAsync(LauncherPresetDeleteTarget target) =>
        new ConfirmPresetDeleteWindow(target).ShowDialog<bool>(Owner);

    public Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? prefill, int presetReferenceCount, IReadOnlyDictionary<string, string>? commandTemplates)
    {
        var prefillData = prefill is not null
            ? new EntryDetailsWindow.EntryPrefill(prefill.Id, prefill.Name, prefill.Directory.Value, prefill.Command, prefill.Selected)
            : null;
        return new EntryDetailsWindow(prefillData, presetReferenceCount, commandTemplates).ShowDialog<EntryDraft?>(Owner);
    }

    public Task<bool> ConfirmDeleteEntryAsync(LauncherEntryDeleteTarget target) =>
        new ConfirmEntryDeleteWindow(target).ShowDialog<bool>(Owner);

    public async Task<DiscoveryDialogResult?> DiscoverAgentsAsync(
        HashSet<string> alreadyAdded,
        IReadOnlyList<DirectoryRow> existingDirectories,
        IReadOnlyDictionary<string, string> commandTemplates)
    {
        var vm = new DiscoverAgentsViewModel(
            ct => AgentDiscoveryScanner.Scan(AgentCatalog.Entries, ct: ct),
            alreadyAdded,
            existingDirectories,
            commandTemplates);
        return await new DiscoverAgentsWindow(vm).ShowDialog<DiscoveryDialogResult?>(Owner);
    }

    public async Task OpenCommandManagementAsync(
        IReadOnlyList<CommandRow> initialCommandRows,
        IReadOnlyList<TemplateRow> initialTemplateRows,
        Func<CommandManagementEdit, CommandManagementResult> applyEdit)
    {
        var vm = new CommandManagementViewModel(initialCommandRows, initialTemplateRows, applyEdit);
        await new CommandManagementWindow(vm).ShowDialog<object?>(Owner);
    }

    public async Task OpenDirectoryManagementAsync(
        IReadOnlyList<DirectoryRow> initialDirectoryRows,
        Func<DirectoryManagementEdit, DirectoryManagementResult> applyEdit)
    {
        var vm = new DirectoryManagementViewModel(initialDirectoryRows, applyEdit);
        await new DirectoryManagementWindow(vm).ShowDialog<object?>(Owner);
    }
}
