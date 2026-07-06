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
        Assert.Equal(expected, EntryId.IsUsable(id));
    }

    [Fact]
    public void NormalizeTrimsEntryIds()
    {
        Assert.Equal("one", EntryId.Normalize(" one "));
        Assert.Equal(string.Empty, EntryId.Normalize(null));
    }

    [Fact]
    public void SameUsesOrdinalEntryIdMatch()
    {
        Assert.True(EntryId.Same("one", "one"));
        Assert.True(EntryId.Same(null, null));
        Assert.False(EntryId.Same("One", "one"));
        Assert.False(EntryId.Same(null, "one"));
    }

    [Fact]
    public void SameNormalizedTrimsThenUsesOrdinalMatch()
    {
        Assert.True(EntryId.SameNormalized(" one ", "one"));
        Assert.False(EntryId.SameNormalized(null, " "));
        Assert.False(EntryId.SameNormalized(" One ", "one"));
    }

    [Fact]
    public void ToRequiredUniqueListRejectsBlankIds()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            EntryId.ToRequiredUniqueList(["one", " "], "ids"));

        Assert.Equal("ids", exception.ParamName);
    }

    [Fact]
    public void ToRequiredUniqueListRejectsDuplicateIds()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            EntryId.ToRequiredUniqueList(["one", " one "], "ids"));

        Assert.Equal("ids", exception.ParamName);
    }

    [Fact]
    public void ToRequiredUniqueListNormalizesValidIdsInOrder()
    {
        var ids = EntryId.ToRequiredUniqueList([" one ", "Two"], "ids");

        Assert.Equal(["one", "Two"], ids);
    }

    [Fact]
    public void Normalize_TruncatesOver256Chars()
    {
        // Build a string longer than 256 characters.
        var longId = new string('X', 500);

        var result = EntryId.Normalize(longId);

        // Must not throw. Must produce at most 256 characters.
        Assert.Equal(new string('X', 256), result);
    }

    [Fact]
    public void Normalize_JustOver256Chars_TruncatesTo256()
    {
        // 257 chars — truncation to 256 should kick in.
        var id = new string('Y', 257);

        var result = EntryId.Normalize(id);

        Assert.Equal(256, result.Length);
        Assert.Equal(new string('Y', 256), result);
    }

    [Fact]
    public void Normalize_Exactly256Chars_NotTruncated()
    {
        var id = new string('Z', 256);

        var result = EntryId.Normalize(id);

        Assert.Equal(256, result.Length);
        Assert.Equal(new string('Z', 256), result);
    }

    [Fact]
    public void Normalize_257CharsWithTrailingHighSurrogate_PreservesSurrogatePair()
    {
        // 255 Z chars + high surrogate (U+D800) + low surrogate (U+DC00) = 257 chars.
        // The high surrogate at index 255 falls at the 256-char boundary.
        // Normalize should extend past the boundary to prevent splitting the pair.
        var regular = new string('Z', 255);
        var highSurrogate = '\uD800';
        var lowSurrogate = '\uDC00';
        var id = regular + highSurrogate + lowSurrogate;

        var result = EntryId.Normalize(id);

        Assert.Equal(257, result.Length);
        Assert.EndsWith("\uD800\uDC00", result);
    }
}
