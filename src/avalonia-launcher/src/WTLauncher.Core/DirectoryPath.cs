using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// 文件系统目录路径。不可变值对象 —— 所有验证在 Parse 中完成。
/// 取代之前的 Vogen DirectoryPath + EntryDirectory 双类型设计。
/// </summary>
[JsonConverter(typeof(DirectoryPathJsonConverter))]
public readonly record struct DirectoryPath
{
    private readonly string _value;

    public string Value => _value;

    private DirectoryPath(string value) => _value = value;

    public static DirectoryPath Empty => new(string.Empty);

    /// <summary>解析并规范化目录路径。验证包括：
    /// FormKC 规范化、% 注入检测、环境变量展开、GetFullPath、UNC 拒绝、DOS 设备拒绝。</summary>
    public static DirectoryPath Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new FormatException("目录不能为空。");

        var normalized = raw.Trim().Normalize(NormalizationForm.FormKC);

        if (normalized.Contains('%') && !raw.Contains('%', StringComparison.Ordinal))
            throw new FormatException("目录包含规范化后引入的 % 字符。");

        var expanded = Environment.ExpandEnvironmentVariables(normalized);

        if (expanded.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
            throw new FormatException("目录包含无效字符。");

        var fullPath = System.IO.Path.GetFullPath(expanded);

        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal) || fullPath.StartsWith("//", StringComparison.Ordinal))
            throw new FormatException("目录不允许使用网络路径 (UNC)。");
        if (fullPath.StartsWith(@"\\.\", StringComparison.Ordinal) || fullPath.StartsWith(@"\??\", StringComparison.Ordinal))
            throw new FormatException("目录不允许使用设备路径。");

        return new DirectoryPath(fullPath);
    }

    /// <summary>解析并验证目录存在。</summary>
    public static DirectoryPath ParseAndValidateExists(string? raw)
    {
        var dir = Parse(raw);
        if (!System.IO.Directory.Exists(dir.Value))
            throw new System.IO.DirectoryNotFoundException($"目录不存在: {dir.Value}");
        return dir;
    }

    public static implicit operator string(DirectoryPath dir) => dir.Value;
    public static implicit operator DirectoryPath(string value) => Parse(value);
    public override string ToString() => Value;
}

public sealed class DirectoryPathJsonConverter : JsonConverter<DirectoryPath>
{
    public override DirectoryPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return raw is not null ? DirectoryPath.Parse(raw) : DirectoryPath.Empty;
    }

    public override void Write(Utf8JsonWriter writer, DirectoryPath value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
