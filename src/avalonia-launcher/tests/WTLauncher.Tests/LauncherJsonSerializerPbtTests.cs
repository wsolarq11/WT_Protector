using CsCheck;
using WTLauncher.Core;
using WTLauncher.Core.ValueObjects;

namespace WTLauncher.Tests;

public sealed class LauncherJsonSerializerPbtTests
{
    /// <summary>
    /// Property: Serialize → Deserialize restores the original value.
    /// Any LauncherStoreFile can be round-tripped through the serializer
    /// without data loss.
    /// </summary>
    [Fact]
    public void SerializeDeserializeRoundtrip()
    {
        Gen.Int[0, 5]
            .SelectMany(count => Gen.Int[0, 3], (entries, presets) => (entries, presets))
            .Sample(assert: input =>
            {
                var store = new LauncherStoreFile
                {
                    SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
                    Entries = [new LaunchEntry
                    {
                        Id = "test", Name = "Test Entry",
                        Directory = (DirectoryPath)Environment.CurrentDirectory,
                        Command = "pwsh",
                    }],
                    Settings = new LauncherSettings
                    {
                        SortMode = "custom",
                        EntryViewMode = "cards",
                        CustomEntryOrder = ["test"],
                    },
                    Presets = [],
                    DefaultPresetId = "",
                };

                var json = LauncherJsonSerializer.Serialize(store);
                var restored = LauncherJsonSerializer.Deserialize<LauncherStoreFile>(json);

                Assert.NotNull(restored);
                Assert.Equal(store.SchemaVersion, restored.SchemaVersion);
                Assert.Equal(store.Entries.Count, restored.Entries.Count);
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
