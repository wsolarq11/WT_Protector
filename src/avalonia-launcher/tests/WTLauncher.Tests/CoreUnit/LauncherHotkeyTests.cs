using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherHotkeyTests
{
    [Fact]
    public void Parse_DefaultHotkey_UsesControlAltSpace()
    {
        var hotkey = LauncherHotkey.Parse("Ctrl+Alt+Space");

        Assert.Equal(LauncherHotkey.ModControl | LauncherHotkey.ModAlt, hotkey.Modifiers);
        Assert.Equal(0x20u, hotkey.Key);
    }

    [Theory]
    [InlineData("Shift+Win+Q")]
    [InlineData("shift + win + Q")]
    public void Parse_ModifierOrderAndCase_AreAccepted(string raw)
    {
        var hotkey = LauncherHotkey.Parse(raw);

        Assert.Equal(LauncherHotkey.ModShift | LauncherHotkey.ModWin, hotkey.Modifiers);
        Assert.Equal((uint)'Q', hotkey.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Space")]
    [InlineData("Ctrl+F1")]
    [InlineData("Ctrl+Alt+F1")]
    public void Parse_InvalidText_ThrowsFormatException(string raw)
    {
        Assert.Throws<FormatException>(() => LauncherHotkey.Parse(raw));
    }
}
