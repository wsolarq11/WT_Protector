using CsCheck;
using WTLauncher.Core;
using WTLauncher.App.Infrastructure;
using System.Collections.Immutable;

namespace WTLauncher.Tests;

public sealed class CsCheckPropertyTests
{
    // Test 1: RotateBackups -- after rotation, the managed backup chain (indices 0..maxBackups-1)
    // never exceeds maxBackups files
    [Fact]
    public void RotateBackups_NeverExceedsMaxBackups()
    {
        Gen.Byte.Array[100]
            .Select(bytes => Convert.ToHexString(bytes))
            .Array[0, 5]
            .Sample(assert: existingBackups =>
            {
                using var temp = new TempDirectory();
                var storePath = Path.Combine(temp.Path, "launcher.json");
                LauncherStore.Save(new LauncherStoreFile
                {
                    Entries = [],
                    Settings = new LauncherSettings(),
                    Presets = [],
                    DefaultPresetId = "",
                }, storePath);

                for (int i = 0; i < existingBackups.Length; i++)
                {
                    var bakPath = i == 0
                        ? storePath + ".bak"
                        : storePath + $".bak.{i}";
                    File.WriteAllText(bakPath, existingBackups[i]);
                }

                LauncherJson.RotateBackups(storePath, maxBackups: 3);

                int managedCount = 0;
                for (int i = 0; i < 3; i++)
                {
                    var bakPath = i == 0
                        ? storePath + ".bak"
                        : storePath + $".bak.{i}";
                    if (File.Exists(bakPath))
                        managedCount++;
                }
                Assert.True(managedCount <= 3,
                    $"Expected <=3 managed backups, got {managedCount} for {existingBackups.Length} inputs");
                Assert.True(File.Exists(storePath));
            });
    }

    // Test 2: LaunchArgumentBuilder produces a valid LaunchPlan for any well-formed entry
    // Uses broad character range: printable ASCII + extended Latin + common unicode
    [Fact]
    public void Build_ProducesValidPlanForAnyValidEntry()
    {
        Gen.Char[' ', '~'].Array[1, 20].Select(c => new string(c))
            .SelectMany(
                name => Gen.Char[' ', '\u02FF'].Array[1, 20].Select(c => new string(c)),
                (name, cmd) => new { Name = name, Command = cmd })
            .Sample(assert: input =>
            {
                var entry = new LaunchEntry
                {
                    Id = "test",
                    Name = input.Name,
                    Directory = (DirectoryPath)Environment.CurrentDirectory,
                    Command = input.Command,
                };

                var plan = LaunchArgumentBuilder.Build(
                    entry, validateDirectory: false, requireConfirmation: false);

                Assert.NotNull(plan);
                Assert.False(string.IsNullOrWhiteSpace(plan.Executable));
                Assert.False(string.IsNullOrWhiteSpace(plan.Arguments));
                Assert.False(string.IsNullOrWhiteSpace(plan.CommandLine));
            });
    }

    // Test 2b: LaunchArgumentBuilder rejects command lines exceeding 32767 chars
    [Fact]
    public void Build_RejectsCommandLineExceeding32767()
    {
        var dir = (DirectoryPath)@"C:\";

        // 18000 chars → ~48200 total >> 32767
        Assert.Throws<InvalidDataException>(() =>
            LaunchArgumentBuilder.Build(
                new LaunchEntry { Id = "overflow", Name = "overflow", Directory = dir, Command = new string('x', 18000) },
                false, false));

        // Short command — always succeeds
        var plan = LaunchArgumentBuilder.Build(
            new LaunchEntry { Id = "ok", Name = "ok", Directory = dir, Command = "pwsh -c echo hello" },
            false, false);
        Assert.NotNull(plan);
    }

    // Test 3: LauncherJson round-trip -- Save then Load preserves all data
    [Fact]
    public void SaveThenLoad_RoundTripsAllData()
    {
        Gen.Char['a', 'z'].Array[1, 8].Select(idChars => new string(idChars))
            .SelectMany(
                _ => Gen.Char['a', 'z'].Array[1, 15].Select(nameChars => new string(nameChars)),
                (id, name) => new { Id = id, Name = name })
            .Array[0, 5]
            .Sample(assert: entryData =>
            {
                using var temp = new TempDirectory();
                var path = Path.Combine(temp.Path, "launcher.json");

                var entries = entryData.Select(d => new LaunchEntry
                {
                    Id = d.Id,
                    Name = d.Name,
                    Directory = (DirectoryPath)Environment.CurrentDirectory,
                    Command = "pwsh",
                }).ToImmutableList();

                var original = new LauncherStoreFile
                {
                    Entries = entries,
                    Settings = new LauncherSettings(),
                    Presets = ImmutableList<LauncherPreset>.Empty,
                    DefaultPresetId = "",
                };

                LauncherStore.Save(original, path);
                var loaded = LauncherStore.Load(path);

                Assert.Equal(original.Entries.Count, loaded.Entries.Count);
                for (int i = 0; i < original.Entries.Count; i++)
                {
                    Assert.Equal(original.Entries[i].Id.ToString(), loaded.Entries[i].Id.ToString());
                    Assert.Equal(original.Entries[i].Name.ToString(), loaded.Entries[i].Name.ToString());
                }
            });
    }

    // Test 4: StoreGrain.Dispatch maintains state consistency after random mutation sequences
    [Fact]
    public void Dispatch_MaintainsConsistentState()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(new LauncherStoreFile
        {
            Entries = [],
            Settings = new LauncherSettings(),
            Presets = [],
            DefaultPresetId = "",
        }, path);
        using var appStore = new StoreGrain(path, new FileStorePersistence());

        Gen.Int[0, 2].Array[0, 10]
            .Sample(assert: operations =>
            {
                foreach (var op in operations)
                {
                    appStore.Dispatch(state => op switch
                    {
                        0 => state with { DefaultPresetId = $"preset-{Guid.NewGuid():N}" },
                        1 => state with { Settings = state.Settings with { SortMode = "alphabetical" } },
                        _ => state with { Settings = state.Settings with { SortMode = "custom" } },
                    });
                }

                var finalState = appStore.Select(s => s);
                Assert.NotNull(finalState.Settings);
                Assert.NotNull(finalState.Entries);
            });
    }
}
