using System.Collections.Immutable;
using System.Globalization;

namespace WTLauncher.Core;

/// <summary>
/// Derives the four interaction-state colors of the accent button from a
/// single user-chosen base color. HSL space: hover/pressed darken (lower
/// lightness), disabled desaturates and lightens to a readable pastel.
/// Pure logic — no UI dependency — so it is unit-testable in Core.
/// </summary>
public static class AccentColorRamp
{
    /// <summary>Default accent: the "11/11" entry-count badge text blue (sky-700).</summary>
    public const string DefaultAccentColor = "#0369A1";

    /// <summary>Selectable base colors offered in the accent picker palette.
    /// Chinese classical colors, hex values from the 中科院《色谱》 standard.</summary>
    public static readonly string[] PresetColors =
    [
        "#FF461F", // 朱砂
        "#9D2933", // 胭脂
        "#CA6924", // 琥珀
        "#845A33", // 赭石
        "#057748", // 松花绿
        "#549688", // 铜绿
        "#177CB0", // 靛青
        "#2E4E7E", // 藏青
        "#425066", // 黛蓝
        "#574266", // 黛紫
        "#801DAE", // 青莲
        "#424C50", // 鸦青
    ];

    public readonly record struct AccentRamp(
        string Normal,
        string Hover,
        string Pressed,
        string DisabledBackground,
        string DisabledForeground);

    /// <summary>
    /// Builds the five color strings (normal, hover, pressed, disabled
    /// background, disabled foreground) from a base hex color such as
    /// <c>"#0369A1"</c>.
    /// </summary>
    public static AccentRamp FromHex(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        var (h, s, l) = RgbToHsl(r, g, b);

        var normal = ToHex(r, g, b);
        var hover = HslToHex(h, s, Clamp01(l - 0.07));
        var pressed = HslToHex(h, s, Clamp01(l - 0.14));
        var disabledBackground = HslToHex(h, Clamp01(s * 0.55), Clamp01(l + 0.42));
        var disabledForeground = HslToHex(h, Clamp01(s * 0.95), Clamp01(l - 0.16));

        return new AccentRamp(normal, hover, pressed, disabledBackground, disabledForeground);
    }

    /// <summary>
    /// Normalizes a hex color input to <c>#RRGGBB</c>, accepting an optional
    /// leading <c>#</c> and surrounding whitespace. Returns false for empty or
    /// non-six-digit input so callers can fall back to the default.
    /// </summary>
    public static bool TryNormalize(string? hex, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        try
        {
            var (r, g, b) = ParseHex(hex);
            normalized = ToHex(r, g, b);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static (byte R, byte G, byte B) ParseHex(string hex)
    {
        var trimmed = hex.Trim().TrimStart('#');
        if (trimmed.Length != 6)
            throw new FormatException($"Invalid hex color: {hex}");

        var span = trimmed.AsSpan();
        var r = byte.Parse(span.Slice(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = byte.Parse(span.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = byte.Parse(span.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (r, g, b);
    }

    private static string ToHex(byte r, byte g, byte b) =>
        $"#{r:X2}{g:X2}{b:X2}";

    private static string HslToHex(double h, double s, double l)
    {
        var (r, g, b) = HslToRgb(h, s, l);
        return ToHex(r, g, b);
    }

    private static (double H, double S, double L) RgbToHsl(byte r, byte g, byte b)
    {
        var rn = r / 255.0;
        var gn = g / 255.0;
        var bn = b / 255.0;
        var max = Math.Max(rn, Math.Max(gn, bn));
        var min = Math.Min(rn, Math.Min(gn, bn));
        var l = (max + min) / 2.0;

        if (max == min)
            return (0, 0, l); // achromatic

        var d = max - min;
        var s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

        double h;
        if (max == rn)
            h = (gn - bn) / d + (gn < bn ? 6 : 0);
        else if (max == gn)
            h = (bn - rn) / d + 2;
        else
            h = (rn - gn) / d + 4;
        h /= 6.0;

        return (h * 360.0, s, l);
    }

    private static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        h = ((h % 360.0) + 360.0) % 360.0 / 360.0;

        if (s == 0)
        {
            var gray = (byte)Math.Round(l * 255.0);
            return (gray, gray, gray);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;

        var r = HueToRgb(p, q, h + 1.0 / 3.0);
        var g = HueToRgb(p, q, h);
        var b = HueToRgb(p, q, h - 1.0 / 3.0);

        return ((byte)Math.Round(r * 255.0), (byte)Math.Round(g * 255.0), (byte)Math.Round(b * 255.0));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0.0, 1.0);
}

/// <summary>Result of the accent picker: the applied accent plus the edited palette.</summary>
public sealed record AccentColorResult(string AccentColor, ImmutableList<string> Presets);
