using WTLauncher.Core;
using Xunit;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

/// <summary>
/// EntryName.Normalize 归一化契约：trim、FormKC Unicode 归一化、null→empty。
/// 数据契约级漂移（归一化错 → 匹配/排序悄悄错），静默且昂贵，永不删。
/// 展示文案测试已按红线 3 移除（文案本体在 zh-CN.json，由 localization 生成门禁覆盖）。
/// </summary>
[Trait("Category", TestCategories.UiContract)]
public sealed class StatusTextContractTests
{
    // ── EntryName (Core normalization) ──────────────

    [Theory]
    [InlineData(" One ", "One")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void NormalizeTrimsTextAndConvertsNullToEmpty(string? value, string expected)
    {
        Assert.Equal(expected, EntryName.Normalize(value));
    }

    [Fact]
    public void Normalize_AppliesFormKCUnicodeNormalization()
    {
        // Fullwidth latin letters normalize to halfwidth ASCII under FormKC.
        // U+FF23=Ｃ, U+FF41=ａ, U+FF46=ｆ, U+FF45=ｅ
        Assert.Equal("Cafe", EntryName.Normalize("Ｃａｆｅ"));

        // Combining acute accent (U+0301) composes to precomposed é (U+00E9).
        Assert.Equal("Café", EntryName.Normalize("Café"));
    }
}
