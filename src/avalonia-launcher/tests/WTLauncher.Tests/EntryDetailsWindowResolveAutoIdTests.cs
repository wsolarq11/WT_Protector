using WTLauncher.App.Views;

namespace WTLauncher.Tests;

/// <summary>Tests for <see cref="EntryDetailsWindow.ResolveAutoId"/> (R5 friend-handoff:
/// blank-ID add flow derives the entry id from the name with a timestamp fallback).</summary>
public sealed class EntryDetailsWindowResolveAutoIdTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 2, 10, 30, 45);

    [Fact]
    public void LatinName_ReturnsNormalizedName()
    {
        Assert.Equal("my-project", EntryDetailsWindow.ResolveAutoId("my-project", FixedNow));
    }

    [Fact]
    public void NameWithSpaces_KeepsSpaces()
    {
        // EntryId allows Basic Latin including spaces (0x20-0x7E).
        Assert.Equal("My Project", EntryDetailsWindow.ResolveAutoId("My Project", FixedNow));
    }

    [Fact]
    public void NonLatinName_FallsBackToTimestamp()
    {
        Assert.Equal("entry-20260802103045", EntryDetailsWindow.ResolveAutoId("我的项目", FixedNow));
    }

    [Fact]
    public void EmptyName_FallsBackToTimestamp()
    {
        Assert.Equal("entry-20260802103045", EntryDetailsWindow.ResolveAutoId("", FixedNow));
    }

    [Fact]
    public void NullName_FallsBackToTimestamp()
    {
        Assert.Equal("entry-20260802103045", EntryDetailsWindow.ResolveAutoId(null, FixedNow));
    }

    [Fact]
    public void ReservedWindowsName_FallsBackToTimestamp()
    {
        // "CON" is a Windows reserved name — EntryId.Parse rejects it.
        Assert.Equal("entry-20260802103045", EntryDetailsWindow.ResolveAutoId("CON", FixedNow));
    }
}
