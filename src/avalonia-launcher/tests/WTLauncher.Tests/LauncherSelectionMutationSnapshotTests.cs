using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherSelectionMutationSnapshotTests
{
    [Fact]
    public void RestoreReturnsCapturedSelectionState()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var entry = owner.Entries.Single();
        entry.IsSelected = true;
        var snapshot = LauncherSelectionMutationSnapshot.Capture([entry]);

        entry.IsSelected = false;
        snapshot.Restore();

        Assert.True(entry.IsSelected);
        Assert.True(entry.Entry.Selected);
    }
}
