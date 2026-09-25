using System.Globalization;
using WTLauncher.Core;

namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class AccentColorRampTests
{
    [Fact]
    public void FromHex_Default_ReturnsNormalizedBaseAndDerivedStates()
    {
        var ramp = AccentColorRamp.FromHex("#0369A1");

        Assert.Equal("#0369A1", ramp.Normal);
        AssertHex(ramp.Hover);
        AssertHex(ramp.Pressed);
        AssertHex(ramp.DisabledBackground);
        AssertHex(ramp.DisabledForeground);

        Assert.NotEqual(ramp.Normal, ramp.Hover);
        Assert.NotEqual(ramp.Normal, ramp.Pressed);
        Assert.NotEqual(ramp.Normal, ramp.DisabledBackground);
    }

    [Fact]
    public void FromHex_HoverPressedDarken_DisabledLightens()
    {
        var ramp = AccentColorRamp.FromHex("#0369A1");

        Assert.True(PerceivedLightness(ramp.Hover) < PerceivedLightness(ramp.Normal));
        Assert.True(PerceivedLightness(ramp.Pressed) < PerceivedLightness(ramp.Hover));
        Assert.True(PerceivedLightness(ramp.DisabledBackground) > PerceivedLightness(ramp.Normal));
    }

    [Fact]
    public void FromHex_LightBaseColor_ClampsToValidRange()
    {
        // A near-white base must still yield valid darker hover/pressed states.
        var ramp = AccentColorRamp.FromHex("#F1F5F9");

        AssertHex(ramp.Hover);
        AssertHex(ramp.Pressed);
        Assert.True(PerceivedLightness(ramp.Hover) < PerceivedLightness(ramp.Normal));
    }

    [Fact]
    public void TryNormalize_AcceptsLeadingHashOrBareHex()
    {
        Assert.True(AccentColorRamp.TryNormalize("#0369A1", out var withHash));
        Assert.Equal("#0369A1", withHash);

        Assert.True(AccentColorRamp.TryNormalize("0369A1", out var bare));
        Assert.Equal("#0369A1", bare);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    public void TryNormalize_RejectsInvalid(string input)
    {
        Assert.False(AccentColorRamp.TryNormalize(input, out _));
    }

    private static void AssertHex(string value)
    {
        Assert.Matches("^#[0-9A-F]{6}$", value);
    }

    private static double PerceivedLightness(string hex)
    {
        var span = hex.AsSpan(1);
        var r = byte.Parse(span.Slice(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = byte.Parse(span.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = byte.Parse(span.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}
