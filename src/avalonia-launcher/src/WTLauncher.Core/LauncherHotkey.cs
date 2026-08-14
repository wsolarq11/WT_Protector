namespace WTLauncher.Core;

/// <summary>
/// Parsed global quick-launch hotkey. The numeric values are Win32 constants,
/// but parsing stays in Core so the text contract is testable without UI.
/// </summary>
public sealed record LauncherHotkey(uint Modifiers, uint Key)
{
    public const string DefaultText = "Ctrl+Alt+Space";

    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public static LauncherHotkey Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new FormatException("快速启动热键不能为空。");

        var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            throw new FormatException("热键格式必须是 修饰键+主键，例如 Ctrl+Alt+Space。");

        var modifiers = 0u;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            modifiers |= parts[i].ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => ModControl,
                "ALT" => ModAlt,
                "SHIFT" => ModShift,
                "WIN" or "WINDOWS" or "META" => ModWin,
                _ => throw new FormatException($"未知修饰键: {parts[i]}"),
            };
        }

        if (modifiers == 0)
            throw new FormatException("热键至少需要一个修饰键。");

        return new LauncherHotkey(modifiers, ParseKey(parts[^1]));
    }

    private static uint ParseKey(string text)
    {
        var upper = text.ToUpperInvariant();
        if (upper == "SPACE")
            return 0x20;
        if (upper.Length == 1 && upper[0] is >= 'A' and <= 'Z' or >= '0' and <= '9')
            return upper[0];

        throw new FormatException($"不支持的主键: {text}");
    }
}
