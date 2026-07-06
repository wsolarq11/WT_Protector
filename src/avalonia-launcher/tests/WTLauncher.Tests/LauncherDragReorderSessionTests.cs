using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherDragReorderSessionTests
{
    [Fact]
    public void TryRecordPreviewIgnoresConsecutiveDuplicateTargetAndSide()
    {
        using var temp = new TempDirectory();
        var session = StartSession(temp);

        Assert.True(session.TryRecordPreview("one", insertAfter: false));
        Assert.False(session.TryRecordPreview(" one ", insertAfter: false));
        Assert.True(session.TryRecordPreview("one", insertAfter: true));
        Assert.True(session.TryRecordPreview("two", insertAfter: true));
    }

    [Fact]
    public void ClearHoverAllowsSamePreviewAgain()
    {
        using var temp = new TempDirectory();
        var session = StartSession(temp);

        Assert.True(session.TryRecordPreview("one", insertAfter: false));

        session.ClearHover();

        Assert.True(session.TryRecordPreview("one", insertAfter: false));
    }

    [Fact]
    public void IsForNormalizesEntryIdBeforeOrdinalMatch()
    {
        using var temp = new TempDirectory();
        var session = StartSession(temp, " three ");

        Assert.Equal("three", session.DraggedEntryId);
        Assert.True(session.IsFor("three"));
        Assert.False(session.IsFor("Three"));
    }

    [Fact]
    public void StartRejectsMissingDependenciesAndBlankSourceIds()
    {
        using var temp = new TempDirectory();
        var source = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "one");
        var snapshot = new LauncherSettings();

        Assert.Throws<ArgumentNullException>(() =>
            LauncherDragReorderSession.Start(null!, snapshot));
        Assert.Throws<ArgumentNullException>(() =>
            LauncherDragReorderSession.Start(source, null!));

        var blankSource = new LauncherEntryViewModel(
            new LaunchEntry
            {
                Id = " ",
                Name = "Blank",
                Directory = temp.Path,
                Command = "pwsh",
            },
            MainWindowEntryCardTestFactory.CreateOwner(temp.Path));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LauncherDragReorderSession.Start(blankSource, snapshot));

        Assert.Equal("Drag source entry id is required.", exception.Message);
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

    /// <summary>
    /// Creates a drag-reorder session for testing. The <see cref="TempDirectory"/>
    /// outlives the returned session so that path references inside the ViewModel
    /// remain valid. The temp directory is not explicitly disposed; the OS will
    /// reclaim it on process exit or temp cleanup.
    /// </summary>
    private static LauncherDragReorderSession StartSession(TempDirectory temp, string draggedEntryId = "three")
    {
        return LauncherDragReorderSession.Start(
            MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, draggedEntryId),
            new LauncherSettings());
    }
}
