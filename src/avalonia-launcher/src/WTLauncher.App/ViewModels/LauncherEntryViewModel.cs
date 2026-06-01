using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;
    private readonly LaunchEntry _entry;

    public LauncherEntryViewModel(LaunchEntry entry, MainWindowViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
        _owner = owner;
        _isSelected = entry.Selected;
    }

    public LaunchEntry SnapshotEntry() => _entry with { };

    public string Id => _entry.Id;

    public string Name => _entry.Name;

    public string Directory => _entry.Directory;

    public string Command => _entry.Command;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                _owner.UpdateSelection(this, value);
        }
    }

    [ObservableProperty]
    private double _cardWidth = 300;

    [ObservableProperty]
    private double _cardHeight = 164;

    [ObservableProperty]
    private double _nameTileWidth = 180;

    [ObservableProperty]
    private double _nameTileHeight = 24;

    internal void SetSelectionFromOwner(bool selected)
    {
        _entry.Selected = selected;
        SetProperty(ref _isSelected, selected, nameof(IsSelected));
    }

    internal bool ReplaceEntrySelectionFromOwner(bool selected)
    {
        var previousSelected = _entry.Selected;
        _entry.Selected = selected;
        return previousSelected;
    }

    internal void ReplaceEntryFromOwner(LaunchEntry updated, string updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(updated);
        _entry.Id = updated.Id;
        _entry.Name = updated.Name;
        _entry.Directory = updated.Directory;
        _entry.Command = updated.Command;
        _entry.UpdatedAtUtc = updatedAtUtc;
        SetSelectionFromOwner(updated.Selected);
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Directory));
        OnPropertyChanged(nameof(Command));
        RefreshLayoutPresentation();
    }

    public void RefreshLayoutPresentation()
    {
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    [RelayCommand]
    private void DryRun() => _owner.ShowDryRun(this);

    [RelayCommand]
    private Task LaunchAsync() => _owner.LaunchAsync(this);

    [RelayCommand]
    private Task EditAsync() => _owner.EditEntryAsync(this);

    [RelayCommand]
    private Task DeleteAsync() => _owner.DeleteEntryAsync(this);

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => _owner.MoveEntryUp(this);

    private bool CanMoveUp() => _owner.CanMoveEntryUp(this);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => _owner.MoveEntryDown(this);

    private bool CanMoveDown() => _owner.CanMoveEntryDown(this);

    internal void RefreshReorderCommands()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
