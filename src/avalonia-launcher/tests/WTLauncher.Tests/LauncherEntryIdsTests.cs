using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryIdsTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("one", true)]
    public void IsUsableRejectsBlankIds(string? id, bool expected)
    {
        Assert.Equal(expected, LauncherEntryIds.IsUsable(id));
    }

    [Fact]
    public void NormalizeTrimsEntryIds()
    {
        Assert.Equal("one", LauncherEntryIds.Normalize(" one "));
        Assert.Equal(string.Empty, LauncherEntryIds.Normalize(null));
    }

    [Fact]
    public void SameUsesOrdinalEntryIdMatch()
    {
        Assert.True(LauncherEntryIds.Same("one", "one"));
        Assert.True(LauncherEntryIds.Same(null, null));
        Assert.False(LauncherEntryIds.Same("One", "one"));
        Assert.False(LauncherEntryIds.Same(null, "one"));
    }

    [Fact]
    public void SameNormalizedTrimsThenUsesOrdinalMatch()
    {
        Assert.True(LauncherEntryIds.SameNormalized(" one ", "one"));
        Assert.True(LauncherEntryIds.SameNormalized(null, " "));
        Assert.False(LauncherEntryIds.SameNormalized(" One ", "one"));
    }

    [Fact]
    public void ToRequiredUniqueListRejectsBlankIds()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryIds.ToRequiredUniqueList(["one", " "], "ids"));

        Assert.Equal("ids", exception.ParamName);
    }

    [Fact]
    public void ToRequiredUniqueListRejectsDuplicateIds()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryIds.ToRequiredUniqueList(["one", " one "], "ids"));

        Assert.Equal("ids", exception.ParamName);
    }

    [Fact]
    public void ToRequiredUniqueListNormalizesValidIdsInOrder()
    {
        var ids = LauncherEntryIds.ToRequiredUniqueList([" one ", "Two"], "ids");

        Assert.Equal(["one", "Two"], ids);
    }

    [Fact]
    public void Normalize_TruncatesOver256Chars()
    {
        // Build a string longer than 256 characters.
        var longId = new string('X', 500);

        var result = LauncherEntryIds.Normalize(longId);

        // Must not throw. Must produce at most 256 characters.
        Assert.Equal(new string('X', 256), result);
    }

    [Fact]
    public void Normalize_JustOver256Chars_TruncatesTo256()
    {
        // 257 chars — truncation to 256 should kick in.
        var id = new string('Y', 257);

        var result = LauncherEntryIds.Normalize(id);

        Assert.Equal(256, result.Length);
        Assert.Equal(new string('Y', 256), result);
    }

    [Fact]
    public void Normalize_Exactly256Chars_NotTruncated()
    {
        var id = new string('Z', 256);

        var result = LauncherEntryIds.Normalize(id);

        Assert.Equal(256, result.Length);
        Assert.Equal(new string('Z', 256), result);
    }
}
