using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public readonly record struct EntryDirectory
{
    public string Value { get; }

    private EntryDirectory(string value) => Value = value;

    public static EntryDirectory Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new FormatException("目录不能为空。");

        var normalized = raw.Trim().Normalize(NormalizationForm.FormKC);

        if (normalized.Contains('%') && !raw.Contains('%', StringComparison.Ordinal))
            throw new FormatException("目录包含规范化后引入 % 的字符，不允许。");

        var expanded = Environment.ExpandEnvironmentVariables(normalized);
        var fullPath = Path.GetFullPath(expanded);

        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal) || fullPath.StartsWith("//", StringComparison.Ordinal))
            throw new FormatException("目录不允许使用网络路径 (UNC)。");

        if (fullPath.StartsWith(@"\\.\", StringComparison.Ordinal) || fullPath.StartsWith(@"\??\", StringComparison.Ordinal))
            throw new FormatException("目录不允许使用设备路径。");

        return new EntryDirectory(fullPath);
    }

    public static EntryDirectory ParseAndValidateExists(string? raw)
    {
        var dir = Parse(raw);
        if (!Directory.Exists(dir.Value))
            throw new DirectoryNotFoundException($"目录不存在: {dir.Value}");
        return dir;
    }

    public static implicit operator string(EntryDirectory dir) => dir.Value;
    public override string ToString() => Value;
}

public sealed class EntryDirectoryJsonConverter : JsonConverter<EntryDirectory>
{
    public override EntryDirectory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return EntryDirectory.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, EntryDirectory value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
