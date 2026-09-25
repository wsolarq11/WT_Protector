using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Updates;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>View-layer implementation of IDialogService.
/// Lives in the Views layer because it directly creates Window instances —
/// Features layer should only depend on the interface.</summary>
public sealed class DialogService : IDialogService
{
    private Window? _owner;
    private readonly IEntryDraftValidator _entryDraftValidator = new EntryDraftValidator();

    /// <summary>Must be called by MainWindow after construction to enable ShowDialog.</summary>
    public void Initialize(Window owner) => _owner = owner;

    private Window Owner => _owner ?? throw new InvalidOperationException(
        "DialogService not initialized. Call Initialize(Window) with the owner window first.");

    public Task<bool> ConfirmLaunchAsync(LauncherSingleLaunchConfirmation confirmation) =>
        new ConfirmLaunchWindow(confirmation).ShowDialog<bool>(Owner);

    public Task<bool> ConfirmBatchLaunchAsync(LauncherBatchLaunchConfirmation confirmation) =>
        new ConfirmBatchLaunchWindow(confirmation).ShowDialog<bool>(Owner);

    public async Task<string?> RequestNewPresetAsync(LauncherPresetSaveTarget target, HashSet<string> existingNames) =>
        await new PresetDetailsWindow(target, existingNames, _entryDraftValidator).ShowDialog<string?>(Owner);

    public Task<bool> ConfirmDeletePresetAsync(LauncherPresetDeleteTarget target) =>
        new ConfirmPresetDeleteWindow(target).ShowDialog<bool>(Owner);

    public Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? prefill, int presetReferenceCount, IReadOnlyDictionary<string, string>? commandTemplates)
    {
        var prefillData = prefill is not null
            ? new EntryDetailsWindow.EntryPrefill(prefill.Id, prefill.Name, prefill.Directory.Value, prefill.Command, prefill.Selected)
            : null;
        return new EntryDetailsWindow(prefillData, presetReferenceCount, commandTemplates, _entryDraftValidator).ShowDialog<EntryDraft?>(Owner);
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
        ICommandEditApplier applyEdit)
    {
        var vm = new CommandManagementViewModel(initialCommandRows, initialTemplateRows, applyEdit);
        await new CommandManagementWindow(vm).ShowDialog<object?>(Owner);
    }

    public async Task OpenDirectoryManagementAsync(
        IReadOnlyList<DirectoryRow> initialDirectoryRows,
        IDirectoryEditApplier applyEdit)
    {
        var vm = new DirectoryManagementViewModel(initialDirectoryRows, applyEdit);
        await new DirectoryManagementWindow(vm).ShowDialog<object?>(Owner);
    }

    public async Task<string?> PickExportPathAsync()
    {
        var file = await Owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LauncherLocalization.Format("dialog.exportPickerTitle"),
            SuggestedFileName = "wt-launcher-export.json",
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType(LauncherLocalization.Format("dialog.configFileType")) { Patterns = ["*.json"] },
            ],
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickImportPathAsync()
    {
        var files = await Owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LauncherLocalization.Format("dialog.importPickerTitle"),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(LauncherLocalization.Format("dialog.configFileType")) { Patterns = ["*.json"] },
            ],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public Task<LauncherImportPlan?> RequestImportAsync(LauncherStoreFile current, LauncherStoreFile imported)
    {
        var vm = new ImportConfigViewModel(current, imported);
        return new ImportMigrationWindow(vm).ShowDialog<LauncherImportPlan?>(Owner);
    }

    public async Task OpenRunHistoryAsync(IReadOnlyList<LauncherRunHistoryEntry> history)
    {
        var vm = new RunHistoryViewModel(history);
        await new RunHistoryWindow(vm).ShowDialog<object?>(Owner);
    }

    public Task OpenUpdateCheckAsync(string currentVersion, string? manifestUrl)
    {
        var vm = new UpdateCheckViewModel(
            new UpdateCheckService(),
            currentVersion,
            manifestUrl);
        return new UpdateCheckWindow(vm).ShowDialog<object?>(Owner);
    }

    public Task<AccentColorResult?> OpenAccentColorAsync(string currentColor, ImmutableList<string> presets) =>
        new AccentColorWindow(currentColor, presets).ShowDialog<AccentColorResult?>(Owner);
}
