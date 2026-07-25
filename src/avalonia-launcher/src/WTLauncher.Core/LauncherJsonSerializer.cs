using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace WTLauncher.Core;

/// <summary>
/// Zone 0: Pure JSON serialization/deserialization. No file I/O, no side effects.
///
/// Example
/// -------
/// var json = LauncherJsonSerializer.Serialize(store);
/// var store = LauncherJsonSerializer.Deserialize<LauncherStoreFile>(json);
/// </summary>
public static class LauncherJsonSerializer
{
    public static JsonSerializerOptions Options => LauncherJsonContext.Default.Options;

    public static string Serialize<T>(T value) where T : notnull
    {
        var typeInfo = (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!;
        return NormalizeLineEndings(JsonSerializer.Serialize(value, typeInfo));
    }

    public static T? Deserialize<T>(string json) where T : notnull
    {
        var typeInfo = (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!;
        return JsonSerializer.Deserialize(json, typeInfo);
    }

    private static string NormalizeLineEndings(string text) =>
        text.ReplaceLineEndings("\n");
}
