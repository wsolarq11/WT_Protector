using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;

    public LauncherEntryViewModel(LaunchEntry entry, MainWindowViewModel owner)
    {
        Entry = entry;
        _owner = owner;
        _isSelected = entry.Selected;
    }

    public LaunchEntry Entry { get; }

    public string Id => Entry.Id;

    public string Name => Entry.Name;

    public string Directory => Entry.Directory;

    public string Command => Entry.Command;

    public string UpdatedLabel => string.IsNullOrWhiteSpace(Entry.UpdatedAtUtc)
        ? "未记录更新时间"
        : $"更新 {Entry.UpdatedAtUtc}";

    public string ConfirmationLabel => Entry.Confirm || LaunchArgumentBuilder.IsDangerousCommand(Entry.Command)
        ? "确认后启动"
        : "直接启动";

    public bool IsDangerous => LaunchArgumentBuilder.IsDangerousCommand(Entry.Command);

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _cardWidth = 300;

    [ObservableProperty]
    private double _cardHeight = 164;

    partial void OnIsSelectedChanged(bool value)
    {
        _owner.UpdateSelection(this, value);
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Directory));
        OnPropertyChanged(nameof(Command));
        OnPropertyChanged(nameof(ConfirmationLabel));
        OnPropertyChanged(nameof(IsDangerous));
        OnPropertyChanged(nameof(UpdatedLabel));
    }

    [RelayCommand]
    private void DryRun()
    {
        _owner.ShowDryRun(this);
    }

    [RelayCommand]
    private Task LaunchAsync()
    {
        return _owner.LaunchAsync(this);
    }

    [RelayCommand]
    private Task EditAsync()
    {
        return _owner.EditEntryAsync(this);
    }

    [RelayCommand]
    private Task DeleteAsync()
    {
        return _owner.DeleteEntryAsync(this);
    }
}
