using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>System.Text.Json source-generated serialization context for Native AOT.
/// All types that pass through LauncherJson.Save/Load must be registered here.</summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(LauncherStoreFile))]
[JsonSerializable(typeof(LauncherSettings))]
[JsonSerializable(typeof(LaunchEntry))]
[JsonSerializable(typeof(LauncherPreset))]
[JsonSerializable(typeof(List<LaunchEntry>))]
[JsonSerializable(typeof(List<LauncherPreset>))]
internal sealed partial class LauncherJsonContext : JsonSerializerContext
{
}
