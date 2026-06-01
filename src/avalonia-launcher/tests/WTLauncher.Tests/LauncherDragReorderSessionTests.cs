using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherDragReorderSessionTests
{
    [Fact]
    public void TryRecordPreviewIgnoresConsecutiveDuplicateTargetAndSide()
    {
        var session = StartSession();

        Assert.True(session.TryRecordPreview("one", insertAfter: false));
        Assert.False(session.TryRecordPreview(" one ", insertAfter: false));
        Assert.True(session.TryRecordPreview("one", insertAfter: true));
        Assert.True(session.TryRecordPreview("two", insertAfter: true));
    }

    [Fact]
    public void ClearHoverAllowsSamePreviewAgain()
    {
        var session = StartSession();

        Assert.True(session.TryRecordPreview("one", insertAfter: false));

        session.ClearHover();

        Assert.True(session.TryRecordPreview("one", insertAfter: false));
    }

    [Fact]
    public void IsForNormalizesEntryIdBeforeOrdinalMatch()
    {
        var session = StartSession(" three ");

        Assert.True(session.IsFor("three"));
        Assert.False(session.IsFor("Three"));
    }

    [Fact]
    public void SessionDoesNotExposeMutableSourceOrSettingsSnapshotPublicly()
    {
        var properties = typeof(LauncherDragReorderSession)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Source", properties);
        Assert.DoesNotContain("Snapshot", properties);
        Assert.Contains(nameof(LauncherDragReorderSession.DraggedEntryId), properties);
    }

    private static LauncherDragReorderSession StartSession(string draggedEntryId = "three")
    {
        using var temp = new TempDirectory();
        return LauncherDragReorderSession.Start(
            draggedEntryId,
            LauncherSettingsSnapshot.Capture(new LauncherSettings()),
            MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, draggedEntryId));
    }
}
