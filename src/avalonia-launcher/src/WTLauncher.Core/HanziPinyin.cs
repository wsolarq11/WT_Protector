using System.Text;

namespace WTLauncher.Core;

/// <summary>
/// 汉字转拼音：全拼 + 首字母缩写。数据来自嵌入资源 HanziPinyinData.txt（Unihan，
/// CJK 基本区 U+4E00-U+9FA5，每个码位一行、去声调、取首选音），按码位直查，
/// 无字典类、无外部依赖。
/// </summary>
public static class HanziPinyin
{
    private static string[]? _pinyins;

    private static string[] Pinyins => _pinyins ??= Load();

    private static string[] Load()
    {
        var assembly = typeof(HanziPinyin).Assembly;
        using var stream = assembly.GetManifestResourceStream("WTLauncher.Core.HanziPinyinData.txt")
            ?? throw new InvalidOperationException("Missing embedded pinyin resource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n');
    }

    /// <summary>单个汉字的首选拼音（去声调）；非汉字返回 null。</summary>
    public static string? GetPinyin(char c)
    {
        var index = c - '\u4E00';
        if ((uint)index >= (uint)Pinyins.Length) return null;
        var pinyin = Pinyins[index];
        return pinyin.Length == 0 ? null : pinyin;
    }

    /// <summary>是否包含 CJK 基本区汉字。</summary>
    public static bool ContainsHan(string text)
    {
        foreach (var c in text)
        {
            if (c is >= '\u4E00' and <= '\u9FA5')
                return true;
        }

        return false;
    }

    /// <summary>
    /// 全拼：汉字转拼音，多字之间用空格分隔；非汉字的 ASCII 字符保留并小写。
    /// 例：<c>"project-上海"</c> → <c>"project- shang hai"</c>。
    /// </summary>
    public static string ToPinyin(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder(text.Length * 4);
        foreach (var c in text)
        {
            var pinyin = GetPinyin(c);
            if (pinyin is not null)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(pinyin);
            }
            else if (!char.IsWhiteSpace(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 首字母缩写：汉字取拼音首字母，非汉字的 ASCII 字符保留并小写，无分隔。
    /// 例：<c>"上海项目"</c> → <c>"shxm"</c>。
    /// </summary>
    public static string ToInitials(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            var pinyin = GetPinyin(c);
            if (pinyin is not null)
                sb.Append(pinyin[0]);
            else if (!char.IsWhiteSpace(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
