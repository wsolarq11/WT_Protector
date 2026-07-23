using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core.ValueObjects;

/// <summary>
/// JsonConverter for Vogen-generated DirectoryPath value object.
/// Vogen generates a type-level [JsonConverter] but source-gen contexts
/// may not pick it up correctly for properties, causing zero-initialized
/// structs whose .Value access throws ValueObjectValidationException.
/// Applied at the property level on LaunchEntry.Directory.
/// </summary>
public sealed class DirectoryPathJsonConverter : JsonConverter<DirectoryPath>
{
    public override DirectoryPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        // Vogen DirectoryPath.From() bypasses validation (trusted input).
        // Empty string is valid via Instance("Empty", "").
        return raw is null ? DirectoryPath.Empty : DirectoryPath.From(raw);
    }

    public override void Write(Utf8JsonWriter writer, DirectoryPath value, JsonSerializerOptions options)
    {
        // Explicit cast to string to avoid triggering Vogen's uninitialized check.
        writer.WriteStringValue(value.ToString());
    }
}
