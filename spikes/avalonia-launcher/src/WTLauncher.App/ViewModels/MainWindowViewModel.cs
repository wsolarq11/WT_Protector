using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly string _configPath;
    private readonly LauncherProcess _launcherProcess;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _dryRunText = "选择一个启动项查看 dry-run。";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    public MainWindowViewModel(string configPath, LauncherProcess launcherProcess)
    {
        _configPath = configPath;
        _launcherProcess = launcherProcess;
        ConfigPathDisplay = configPath;
        Refresh();
    }

    public ObservableCollection<LauncherEntryViewModel> Entries { get; } = [];

    public ObservableCollection<LauncherEntryViewModel> VisibleEntries { get; } = [];

    public string ConfigPathDisplay { get; }

    public Func<LaunchEntry, LaunchPlan, Task<bool>> ConfirmLaunchAsync { get; set; } =
        static (_, _) => Task.FromResult(false);

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void Refresh()
    {
        Entries.Clear();
        foreach (var entry in LauncherConfigStore.Load(_configPath))
        {
            Entries.Add(new LauncherEntryViewModel(entry, this));
        }

        TotalCount = Entries.Count;
        ApplyFilter();
        StatusText = $"已加载 {TotalCount} 个启动项";
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        try
        {
            var plan = LaunchArgumentBuilder.Build(item.Entry);
            DryRunText = $"{plan.Executable} {plan.Arguments}";
            StatusText = plan.RequiresConfirmation
                ? $"Dry-run: {item.Name}（需要确认）"
                : $"Dry-run: {item.Name}";
        }
        catch (Exception ex)
        {
            DryRunText = ex.Message;
            StatusText = $"Dry-run 失败: {item.Name}";
        }
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        try
        {
            var plan = LaunchArgumentBuilder.Build(item.Entry);
            DryRunText = $"{plan.Executable} {plan.Arguments}";
            if (plan.RequiresConfirmation)
            {
                var accepted = await ConfirmLaunchAsync(item.Entry, plan).ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = $"已取消: {item.Name}";
                    return;
                }
            }

            _launcherProcess.Start(plan);
            StatusText = $"已启动: {item.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"启动失败: {item.Name} — {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        var filter = (FilterText ?? "").Trim();
        VisibleEntries.Clear();
        foreach (var entry in Entries)
        {
            if (filter.Length == 0
                || entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || entry.Directory.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || entry.Command.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                VisibleEntries.Add(entry);
            }
        }

        VisibleCount = VisibleEntries.Count;
    }
}
