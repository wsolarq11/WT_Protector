using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherCommandNotificationsTests
{
    [Fact]
    public void NotifyPresetManagementRaisesOnlyPresetManagementCommands()
    {
        var save = TrackedCommand();
        var update = TrackedCommand();
        var delete = TrackedCommand();

        LauncherCommandNotifications.NotifyPresetManagement(save.Command, update.Command, delete.Command);

        Assert.Equal(1, save.Notifications);
        Assert.Equal(1, update.Notifications);
        Assert.Equal(1, delete.Notifications);
    }

    [Fact]
    public void NotifySelectionRaisesSelectionAndPresetManagementCommands()
    {
        var launch = TrackedCommand();
        var clear = TrackedCommand();
        var apply = TrackedCommand();
        var save = TrackedCommand();
        var update = TrackedCommand();
        var delete = TrackedCommand();

        LauncherCommandNotifications.NotifySelection(
            launch.Command,
            clear.Command,
            apply.Command,
            save.Command,
            update.Command,
            delete.Command);

        Assert.Equal(1, launch.Notifications);
        Assert.Equal(1, clear.Notifications);
        Assert.Equal(1, apply.Notifications);
        Assert.Equal(1, save.Notifications);
        Assert.Equal(1, update.Notifications);
        Assert.Equal(1, delete.Notifications);
    }

    private static TrackedRelayCommand TrackedCommand()
    {
        var command = new RelayCommand(static () => { });
        var tracked = new TrackedRelayCommand(command);
        command.CanExecuteChanged += (_, _) => tracked.Notifications++;
        return tracked;
    }

    private sealed class TrackedRelayCommand(IRelayCommand command)
    {
        public IRelayCommand Command { get; } = command;

        public int Notifications { get; set; }
    }
}
