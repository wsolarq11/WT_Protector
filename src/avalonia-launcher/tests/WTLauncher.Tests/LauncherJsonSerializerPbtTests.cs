using System.Collections.Immutable;
using CsCheck;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherJsonSerializerPbtTests
{
    /// <summary>
    /// Property: Serialize → Deserialize restores the original value.
    /// Any LauncherStoreFile with variable entry/preset counts can be
    /// round-tripped through the serializer without data loss.
    /// </summary>
    [Fact]
    public void SerializeDeserializeRoundtrip()
    {
        Gen.Int[0, 5].SelectMany(
                entryCount => Gen.Int[0, 3],
                (entryCount, presetCount) => (entryCount, presetCount))
            .Sample(assert: input =>
            {
                var (entryCount, presetCount) = input;
                var entries = Enumerable.Range(0, entryCount)
                    .Select(i => new LaunchEntry
                    {
                        Id = $"e{i}", Name = $"Entry {i}",
                        Directory = (DirectoryPath)Environment.CurrentDirectory,
                        Command = "pwsh",
                    })
                    .ToImmutableList();
                var entryIds = entries.Select(e => e.Id.Value).ToImmutableList();
                var presets = Enumerable.Range(0, presetCount)
                    .Select(i => new LauncherPreset
                    {
                        Id = $"p{i}", Name = $"Preset {i}",
                        EntryIds = entryIds,
                    })
                    .ToImmutableList();

                var store = new LauncherStoreFile
                {
                    SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
                    Entries = entries,
                    Settings = new LauncherSettings
                    {
                        SortMode = "custom",
                        EntryViewMode = "cards",
                        CustomEntryOrder = entryIds,
                    },
                    Presets = presets,
                    DefaultPresetId = presetCount > 0 ? "p0" : "",
                };

                var json = LauncherJsonSerializer.Serialize(store);
                var restored = LauncherJsonSerializer.Deserialize<LauncherStoreFile>(json);

                Assert.NotNull(restored);
                Assert.Equal(store.SchemaVersion, restored.SchemaVersion);
                Assert.Equal(entryCount, restored.Entries.Count);
                Assert.Equal(presetCount, restored.Presets.Count);
                Assert.Equal(store.Settings.SortMode, restored.Settings.SortMode);
                Assert.Equal(store.DefaultPresetId, restored.DefaultPresetId);
            });
    }

    /// <summary>
    /// Property: Serialized output uses \n line endings (not \r\n).
    /// </summary>
    [Fact]
    public void NormalizedLineEndings()
    {
        Gen.String[0, 50]
            .Select(s => s.Contains('\r') ? s.Replace("\r\n", "\n").Replace("\r", "\n") : s)
            .Sample(assert: text =>
            {
                var store = new LauncherStoreFile
                {
                    SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
                    Entries = [],
                    Settings = new LauncherSettings(),
                    Presets = [],
                    DefaultPresetId = text.Length > 100 ? text[..100] : text,
                };

                var json = LauncherJsonSerializer.Serialize(store);

                // JSON should not contain \r\n sequences (normalized by serializer)
                Assert.DoesNotContain("\r\n", json);
            });
    }
}
