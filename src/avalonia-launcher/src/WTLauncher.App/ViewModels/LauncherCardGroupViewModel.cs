using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Cards 视图分组后的组头 view-model。绑定 XAML 组头 + 嵌套 card ItemsControl。
/// ponytail: 单 VM,无子 VM — 与 DiscoverAgentsViewModel 同模式。
/// 启所有 / 存为启动组 通过 owner 注入的 callbacks 回到 MainWindowViewModel,本 VM 不直接 mutate store。
/// </summary>
public sealed partial class LauncherCardGroupViewModel : ObservableObject
{
    private readonly Func<IReadOnlyList<LauncherEntryViewModel>, Task> _launchAllAsync;
    private readonly Func<IReadOnlyList<string>, Task> _saveAsPresetAsync;

    public string Key { get; }
    public string DisplayName { get; }
    public int Count { get; }
    public bool ShowHeader { get; }
    public ObservableCollection<LauncherEntryViewModel> Entries { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    public string CountLabel => $"{Count} 个";

    public LauncherCardGroupViewModel(
        string key,
        string displayName,
        int count,
        bool showHeader,
        IEnumerable<LauncherEntryViewModel> entries,
        Func<IReadOnlyList<LauncherEntryViewModel>, Task> launchAllAsync,
        Func<IReadOnlyList<string>, Task> saveAsPresetAsync)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _launchAllAsync = launchAllAsync ?? throw new ArgumentNullException(nameof(launchAllAsync));
        _saveAsPresetAsync = saveAsPresetAsync ?? throw new ArgumentNullException(nameof(saveAsPresetAsync));

        Key = key;
        DisplayName = displayName;
        Count = count;
        ShowHeader = showHeader;
        Entries = new ObservableCollection<LauncherEntryViewModel>(entries);
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private async Task LaunchAllAsync()
    {
        await _launchAllAsync(Entries.ToList()).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task SaveAsPresetAsync()
    {
        await _saveAsPresetAsync(Entries.Select(e => e.Id).ToList()).ConfigureAwait(true);
    }
}
