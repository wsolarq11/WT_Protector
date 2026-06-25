using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetIdsTests
{
    [Fact]
    public void Normalize_TruncatesOver256Chars()
    {
        // Build a string longer than 256 characters.
        var longName = new string('A', 500);

        var result = LauncherPresetIds.Normalize(longName);

        // Must not throw. Must produce at most 256 chars of raw input,
        // further reduced by character filtering and FormKC normalization.
        Assert.True(result.Length <= 256);
        // Since all 'A' chars are letters, the result is 256 lowercase 'a'.
        Assert.Equal(new string('a', 256), result);
    }

    [Fact]
    public void Normalize_ExactMaxLength_ReturnsNormalizedFullString()
    {
        // Exactly 256 chars — no truncation needed, all normalization still applied.
        var name = new string('X', LauncherPresetIds.MaxIdLength);

        var result = LauncherPresetIds.Normalize(name);

        Assert.Equal(new string('x', LauncherPresetIds.MaxIdLength), result);
    }

    [Fact]
    public void Normalize_JustOverMaxLength_Truncates()
    {
        // 257 chars — truncation to 256 should kick in.
        var name = new string('Y', LauncherPresetIds.MaxIdLength + 1);

        var result = LauncherPresetIds.Normalize(name);

        Assert.Equal(new string('y', LauncherPresetIds.MaxIdLength), result);
    }

    [Fact]
    public void Normalize_BothIdClassesTruncateConsistently()
    {
        // Both Normalize methods must truncate input exceeding 256 chars
        // without throwing. Outputs differ due to different normalization rules,
        // but both must produce at most 256 characters.
        var longInput = new string('Z', 500);

        var entryResult = LauncherEntryIds.Normalize(longInput);
        var presetResult = LauncherPresetIds.Normalize(longInput);

        // LauncherEntryIds: trims whitespace then truncates (none here), returns as-is.
        Assert.Equal(new string('Z', 256), entryResult);
        // LauncherPresetIds: truncates then FormKC + ToLowerInvariant.
        Assert.Equal(new string('z', 256), presetResult);

        Assert.True(entryResult.Length <= 256);
        Assert.True(presetResult.Length <= 256);
    }

    [Fact]
    public void Normalize_MaxIdLengthConstant_Is256()
    {
        // The design decision is 256 — this test catches accidental changes.
        Assert.Equal(256, LauncherPresetIds.MaxIdLength);
    }
}
