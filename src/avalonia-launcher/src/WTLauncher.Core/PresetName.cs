using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// A validated and normalized preset name. FormKC normalization and 256-char length
/// enforcement happen at construction time. KEY FIX (C3): the 256-char limit was
/// previously only enforced at LOAD time (LauncherPresetFiles), causing a
/// save-then-crash cycle. Now the limit is enforced at construction — a preset
/// with an over-length name can never be created.
/// </summary>
[JsonConverter(typeof(PresetNameJsonConverter))]
public readonly record struct PresetName
{
    public const int MaxLength = 256;

    public string Value { get; }

    private PresetName(string value) => Value = value;

    public static Result<PresetName, NameError> TryParse(string? raw)
    {
        var entryNameResult = EntryName.TryParse(raw);
        if (entryNameResult.IsErr)
            return entryNameResult.Match<Result<PresetName, NameError>>(
                ok: _ => default!,
                err: e => e); // implicit: E → Result<T,E>

        var value = entryNameResult.Match(ok: n => n.Value, err: _ => "");

        if (value.Length > MaxLength)
            return new NameError.TooLong(value.Length, MaxLength); // implicit: E → Result<T,E>

        return new PresetName(value); // implicit: T → Result<T,E>
    }

    public static implicit operator string(PresetName name) => name.Value;
    public override string ToString() => Value;
}

public sealed class PresetNameJsonConverter : JsonConverter<PresetName>
{
    public override PresetName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        var result = PresetName.TryParse(raw);
        return result.Match(
            ok: name => name,
            err: error => throw new InvalidDataException(error.Message));
    }

    public override void Write(Utf8JsonWriter writer, PresetName value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
