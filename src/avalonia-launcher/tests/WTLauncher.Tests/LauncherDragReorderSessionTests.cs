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

        Assert.Equal("three", session.DraggedEntryId);
        Assert.True(session.IsFor("three"));
        Assert.False(session.IsFor("Three"));
    }

    [Fact]
    public void StartRejectsMissingDependenciesAndBlankSourceIds()
    {
        using var temp = new TempDirectory();
        var source = MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, "one");
        var snapshot = LauncherSettingsSnapshot.Capture(new LauncherSettings());

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
    /// <para>
    /// Creates a drag-reorder session for testing. The <see cref="TempDirectory"/>
    /// is disposed immediately after this method returns; the returned session and
    /// its <see cref="LauncherEntryViewModel"/> owner still hold references to
    /// paths inside the temp directory (via <c>MainWindowViewModel._configPath</c>
    /// and <c>_settingsPath</c>). This is intentional and harmless: the tests only
    /// exercise session logic (preview recording, hover clearing, id normalization)
    /// and never touch those file paths after construction.
    /// </para>
    /// <para>
    /// L61: Do not add tests that save config or persist settings using sessions
    /// created by this helper — the underlying temp directory no longer exists.
    /// </para>
    /// </summary>
    private static LauncherDragReorderSession StartSession(string draggedEntryId = "three")
    {
        using var temp = new TempDirectory();
        return LauncherDragReorderSession.Start(
            MainWindowEntryCardTestFactory.CreateEntryViewModel(temp.Path, draggedEntryId),
            LauncherSettingsSnapshot.Capture(new LauncherSettings()));
    }
}
