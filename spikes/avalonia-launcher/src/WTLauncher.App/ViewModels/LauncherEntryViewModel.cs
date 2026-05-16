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
    }

    public LaunchEntry Entry { get; }

    public string Name => Entry.Name;

    public string Directory => Entry.Directory;

    public string Command => Entry.Command;

    public string ConfirmationLabel => Entry.Confirm || LaunchArgumentBuilder.IsDangerousCommand(Entry.Command)
        ? "确认后启动"
        : "直接启动";

    public bool IsDangerous => LaunchArgumentBuilder.IsDangerousCommand(Entry.Command);

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
}
