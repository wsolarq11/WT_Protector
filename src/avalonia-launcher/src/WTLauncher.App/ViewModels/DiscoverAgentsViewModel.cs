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
    internal Task ScanAsync()
    {
        if (IsScanning)
            return Task.CompletedTask;

        IsScanning = true;
        StatusText = "扫描中…";

        return Task.Run(() =>
        {
            try
            {
                var result = _scanFunc();
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
                // ponytail: per-agent failures are already captured in the result.
                // Surface a generic error; do not crash the dialog. Narrowed via
                // ExceptionFilters so unrecoverable faults (OOM/SEH/AccessViolation)
                // crash instead of being hidden behind a status string — matches
                // MainWindowViewModel.Discovery dialog-open catch pattern.
                ErrorLog.Write(ex, "DiscoverAgents.Scan");
                StatusText = "扫描过程中发生错误";
            }
            finally
            {
                IsScanning = false;
            }
        });
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
