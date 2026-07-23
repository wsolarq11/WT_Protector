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
        // 255 Z chars + Latin Extended-F character U+10780 (via surrogate pair
        // U+D801+U+DF80) = 257 chars. The high surrogate at index 255 falls at the
        // 256-char boundary. Normalize should extend past the boundary to prevent
        // splitting the pair.
        var regular = new string('Z', 255);
        var highSurrogate = '\uD801'; // High surrogate for U+10780 (Latin Extended-F)
        var lowSurrogate = '\uDF80';  // Low surrogate for U+10780 (0xDC00 + 0x380)
        var id = regular + highSurrogate + lowSurrogate;

        var result = EntryId.Normalize(id);

        Assert.Equal(257, result.Length);
        Assert.EndsWith("\uD801\uDF80", result);
    }

    [Fact]
    public void Parse_RejectsCyrillicHomoglyph()
    {
        // Cyrillic 'а' (U+0430) is visually identical to Latin 'a' (U+0061)
        // but belongs to a different script — must be rejected.
        var cyrillicA = "\u0430"; // Cyrillic small letter a

        var exception = Assert.Throws<FormatException>(() => EntryId.Parse(cyrillicA));
        Assert.Contains("非 Latin", exception.Message);
    }

    [Fact]
    public void Parse_AcceptsExtendedLatinCharacters()
    {
        // Accented Latin characters used in European languages must be accepted.
        var id = EntryId.Parse("cafe\u0301"); // café with combining acute accent
        Assert.NotNull(id.Value);
        Assert.Equal("caf\u00e9", id.Value); // FormC composes to U+00E9

        // Also accept pre-composed form directly
        var id2 = EntryId.Parse("Jos\u00e9"); // José with pre-composed é
        Assert.Equal("Jos\u00e9", id2.Value);
    }

    [Fact]
    public void Parse_RejectsGreekScript()
    {
        // Greek letters should be rejected as non-Latin script.
        var greekAlpha = "\u03B1"; // Greek small letter alpha

        var exception = Assert.Throws<FormatException>(() => EntryId.Parse(greekAlpha));
        Assert.Contains("非 Latin", exception.Message);
    }

    [Fact]
    public void Same_DistinguishesLatinFromCyrillicHomoglyph()
    {
        // Latin 'a' (U+0061) and Cyrillic 'а' (U+0430) must NOT be Same.
        Assert.True(EntryId.Same("a", "a")); // Latin vs Latin
        Assert.False(EntryId.Same("a", "\u0430")); // Latin vs Cyrillic

        // SameNormalized should also distinguish them
        Assert.True(EntryId.SameNormalized(" a ", "a"));
        Assert.False(EntryId.SameNormalized("\u0430", "a")); // Cyrillic normalizes but doesn't match Latin
    }
}
