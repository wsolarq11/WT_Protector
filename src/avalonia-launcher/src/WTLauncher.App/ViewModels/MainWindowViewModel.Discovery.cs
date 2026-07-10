using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Discovery Add flow. Split from MainWindowViewModel.cs as part of
// 07-10-agent-cli-discovery. Dialog Func properties are bound by the View
// (MainWindow.axaml.cs) and are owned by this partial — same pattern as
// Entries.AddEntry's RequestEntryDraftAsync.

public sealed partial class MainWindowViewModel
{
    private Func<Task<IReadOnlyList<DiscoveredAgent>?>> _discoverAgentsDialogAsync =
        static () => Task.FromResult<IReadOnlyList<DiscoveredAgent>?>(null);

    /// <summary>
    /// Injected dialog that scans PATH for AI agent CLIs and returns the
    /// user's selection (or null if canceled). Default returns null so unit
    /// tests can construct MainWindowViewModel without spawning a UI.
    /// </summary>
    public Func<Task<IReadOnlyList<DiscoveredAgent>?>> DiscoverAgentsDialogAsync
    {
        get => _discoverAgentsDialogAsync;
        set => _discoverAgentsDialogAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    [RelayCommand]
    private async Task DiscoverAgentsAsync()
    {
        DiscardDragReorderPreview();

        IReadOnlyList<DiscoveredAgent>? selected;
        try
        {
            selected = await DiscoverAgentsDialogAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DiscoverAgents.Dialog");
            StatusText = $"发现对话框打开失败: {ExceptionMessageFormatter.ForUser(ex)}";
            return;
        }

        if (selected is null || selected.Count == 0)
        {
            StatusText = "已取消发现";
            return;
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var snapshot = CaptureEntryMutationSnapshot();

        int added = 0;
        var skipped = new List<string>();
        foreach (var discovered in selected)
        {
            var draft = AgentRecommendationFactory.ToDraft(discovered, userHome);
            var entry = CreateEntryFromDraftOrReportValidation(draft, originalId: null, previousEntry: null);
            if (entry is null)
            {
                skipped.Add(discovered.Catalog.DisplayName);
                continue;
            }

            entry = entry with { UpdatedAtUtc = CurrentTimestampUtc() };
            _entries.Add(new LauncherEntryViewModel(entry, this));
            _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };
            added++;
        }

        if (!SaveEntryCollectionChanges(snapshot))
            return;

        StatusText = skipped.Count == 0
            ? $"已添加 {added} 个 AI Agent 启动项"
            : $"已添加 {added} 个 AI Agent 启动项（{skipped.Count} 个跳过: {string.Join(", ", skipped)}）";
    }
}
