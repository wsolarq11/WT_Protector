using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherMutationTransactionTests
{
    [Fact]
    public void Commit_WhenPersistSucceeds_DoesNotRestore()
    {
        var restoreCount = 0;

        var failure = LauncherMutationTransaction.Commit(
            "before",
            () => null,
            _ => restoreCount++);

        Assert.Null(failure);
        Assert.Equal(0, restoreCount);
    }

    [Fact]
    public void Commit_WhenPersistFails_RestoresOnceAndReturnsFailure()
    {
        const string expectedFailure = "save failed";
        var restoredSnapshot = string.Empty;
        var restoreCount = 0;

        var failure = LauncherMutationTransaction.Commit(
            "before",
            () => expectedFailure,
            snapshot =>
            {
                restoredSnapshot = snapshot;
                restoreCount++;
            });

        Assert.Equal(expectedFailure, failure);
        Assert.Equal("before", restoredSnapshot);
        Assert.Equal(1, restoreCount);
    }
}
