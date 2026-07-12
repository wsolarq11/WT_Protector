using System.Collections.ObjectModel;
using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// View-model backing the discovery dialog. Owns:
/// - the discovered-agent rows (one per found agent + greyed rows for already-added)
/// - the scan progress state
/// - the selection state (which rows the user wants to import)
///
/// The actual dialog Window (DiscoverAgentsWindow) consumes this VM and
/// returns the selected DiscoveredAgent list to MainWindowViewModel via
/// the injected Func.
/// </summary>
public sealed partial class DiscoverAgentsViewModel : ObservableObject
{
    private readonly Func<AgentDiscoveryResult> _scanFunc;
    private readonly HashSet<string> _alreadyAddedExecNames;

    public ObservableCollection<DiscoveredAgentRow> Rows { get; } = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = "等待扫描";

    [ObservableProperty]
    private int _selectedCount;

    public DiscoverAgentsViewModel(Func<AgentDiscoveryResult> scanFunc, IEnumerable<string> alreadyAddedExecNames)
    {
        _scanFunc = scanFunc ?? throw new ArgumentNullException(nameof(scanFunc));
        _alreadyAddedExecNames = new HashSet<string>(
            alreadyAddedExecNames,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>True if at least one row is selected for import.</summary>
    public bool CanAddSelected => SelectedCount > 0;

    [RelayCommand]
    internal async Task ScanAsync()
    {
        if (IsScanning)
            return;

        IsScanning = true;
        StatusText = LauncherRefreshStatus.DiscoveryScanning;

        AgentDiscoveryResult result;
        try
        {
            // Scanner is slow (spawns 8 CLI processes); offload to thread pool.
            // await resumes on UI thread (Avalonia SynchronizationContext captures it),
            // so Rows/StatusText mutations below are on UI thread — required because
            // CollectionChanged fires synchronously into RenderRows which mutates the
            // visual tree. Pre-fix this was inside Task.Run, threw
            // InvalidOperationException cross-thread, and silently left UI stuck.
            result = await Task.Run(() => _scanFunc());

            Rows.Clear();
            foreach (var discovered in result.Found)
            {
                var alreadyAdded = _alreadyAddedExecNames.Contains(discovered.Catalog.ExecName);
                Rows.Add(new DiscoveredAgentRow(discovered, IsAlreadyAdded: alreadyAdded));
            }

            StatusText = result.Found.Length == 0
                ? "未发现任何已安装的 AI Agent"
                : $"发现 {result.Found.Length} 个 AI Agent";
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DiscoverAgents.Scan");
            StatusText = LauncherRefreshStatus.DiscoveryFailed;
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void ToggleSelect(DiscoveredAgentRow? row)
    {
        if (row is null || row.IsAlreadyAdded)
            return;

        row.IsSelected = !row.IsSelected;
        RecomputeSelectedCount();
    }

    internal void RecomputeSelectedCount()
    {
        SelectedCount = Rows.Count(r => r.IsSelected);
        OnPropertyChanged(nameof(CanAddSelected));
    }

    /// <summary>Returns the DiscoveredAgents the user selected. Empty when user cancels.</summary>
    public IReadOnlyList<DiscoveredAgent> GetSelected()
    {
        return Rows
            .Where(r => r.IsSelected && !r.IsAlreadyAdded)
            .Select(r => r.Discovered)
            .ToImmutableList();
    }
}

/// <summary>One row in the discovery dialog.</summary>
public sealed partial class DiscoveredAgentRow : ObservableObject
{
    public DiscoveredAgent Discovered { get; }
    public bool IsAlreadyAdded { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayName => Discovered.Catalog.DisplayName;
    public string Vendor => Discovered.Catalog.Vendor;
    public string Version => Discovered.Version;
    public string ResolvedPath => Discovered.ResolvedPath;
    public string ExecName => Discovered.Catalog.ExecName;
    public string HomePageUrl => Discovered.Catalog.HomePageUrl;
    public string BrandColorHex => Discovered.Catalog.BrandColorHex;
    public string BrandGlyph => Discovered.Catalog.ExecName.Length >= 2
        ? Discovered.Catalog.ExecName.Substring(0, 2).ToUpperInvariant()
        : Discovered.Catalog.ExecName.ToUpperInvariant();
    public bool HasBrand => !string.IsNullOrEmpty(BrandColorHex);

    public DiscoveredAgentRow(DiscoveredAgent discovered, bool IsAlreadyAdded)
    {
        Discovered = discovered;
        this.IsAlreadyAdded = IsAlreadyAdded;
    }
}
