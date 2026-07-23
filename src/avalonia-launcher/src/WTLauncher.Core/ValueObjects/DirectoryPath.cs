// <copyright file="DirectoryPath.cs" company="WT Launcher">
// Copyright (c) 2024 WT Launcher Contributors. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;
using Vogen;

namespace WTLauncher.Core.ValueObjects;

/// <summary>Strongly-typed filesystem directory path. Validated at construction.</summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
[Instance("Empty", "")]
[JsonConverter(typeof(DirectoryPathJsonConverter))]
public readonly partial struct DirectoryPath
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Directory path must not be empty.");
        if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return Validation.Invalid("Directory path contains invalid characters.");
        return Validation.Ok;
    }
}

/// <summary>Custom JSON converter for DirectoryPath to handle Vogen value object serialization correctly
/// with source-generated JsonSerializerContext (LauncherJsonContext).</summary>
public sealed class DirectoryPathJsonConverter : JsonConverter<DirectoryPath>
{
    public override DirectoryPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (raw is null)
            return DirectoryPath.Empty;
        // Vogen's From factory calls Validate internally.
        try { return DirectoryPath.From(raw); }
        catch { return DirectoryPath.Empty; }
    }

    public override void Write(Utf8JsonWriter writer, DirectoryPath value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
