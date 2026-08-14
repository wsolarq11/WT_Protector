using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    MaxDepth = 64)]
[JsonSerializable(typeof(LauncherStoreFile))]
[JsonSerializable(typeof(LauncherSettings))]
[JsonSerializable(typeof(LaunchEntry))]
[JsonSerializable(typeof(LauncherPreset))]
[JsonSerializable(typeof(LauncherRunHistoryEntry))]
[JsonSerializable(typeof(LauncherUpdateManifest))]
[JsonSerializable(typeof(List<LaunchEntry>))]
[JsonSerializable(typeof(List<LauncherPreset>))]
[JsonSerializable(typeof(EntryName))]
[JsonSerializable(typeof(EntryId))]
[JsonSerializable(typeof(DirectoryPath))]
[JsonSerializable(typeof(LegacyPresetDocument))]
internal sealed partial class LauncherJsonContext : JsonSerializerContext
{
}
