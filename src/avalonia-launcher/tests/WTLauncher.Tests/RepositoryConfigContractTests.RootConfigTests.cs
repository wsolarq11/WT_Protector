using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryRootConfig_LoadsAndBuildsDryRunPlans()
    {
        var configPath = TestRepositoryPaths.Config("config.json");
        var presetsPath = TestRepositoryPaths.Config("presets.json");
        var settingsPath = TestRepositoryPaths.Config("settings.json");

        AssertEveryEntryDeclaresPersistentSelection(configPath);
        var entries = LauncherConfigStore.Load(configPath);
        AssertPresetsReferenceKnownEntries(presetsPath, entries);
        AssertPortableSettingsContract(settingsPath, entries);

        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false);
            Assert.Equal("wt.exe", plan.Executable);
            Assert.StartsWith("-d ", plan.Arguments, StringComparison.Ordinal);
            Assert.Contains(" pwsh -NoExit -Command ", plan.Arguments, StringComparison.Ordinal);
            Assert.False(plan.RequiresConfirmation);
        }
    }

    private static void AssertEveryEntryDeclaresPersistentSelection(string configPath)
    {
        var json = File.ReadAllText(configPath);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var entries = document.RootElement.EnumerateArray().ToList();
        Assert.All(entries, entry =>
        {
            Assert.True(entry.TryGetProperty("selected", out var selected), "Every entry must declare the portable selected checkbox state.");
            Assert.True(
                selected.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "The selected field must be a JSON boolean.");
        });
    }

    private static void AssertPresetsReferenceKnownEntries(string presetsPath, IReadOnlyList<LaunchEntry> entries)
    {
        var knownEntryIds = entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        var json = File.ReadAllText(presetsPath);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        var defaultPresetId = root.GetProperty("defaultPresetId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(defaultPresetId));
        var presetIds = new HashSet<string>(StringComparer.Ordinal);
        var nonEmptyPresetCount = 0;

        foreach (var preset in root.GetProperty("presets").EnumerateArray())
        {
            var id = preset.GetProperty("id").GetString();
            var name = preset.GetProperty("name").GetString();
            Assert.False(string.IsNullOrWhiteSpace(id));
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.True(presetIds.Add(id!), $"Duplicate preset id: {id}");
            if (preset.TryGetProperty("locked", out var locked))
            {
                Assert.True(
                    locked.ValueKind is JsonValueKind.True or JsonValueKind.False,
                    $"Preset {id} locked field must be boolean.");
            }
            if (id == "blank-manual")
            {
                Assert.True(
                    preset.TryGetProperty("locked", out locked) && locked.GetBoolean(),
                    "blank-manual preset should stay locked so manual mode cannot be deleted in the UI.");
            }

            var entryIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entryIdElement in preset.GetProperty("entryIds").EnumerateArray())
            {
                var entryId = entryIdElement.GetString();
                Assert.False(string.IsNullOrWhiteSpace(entryId));
                Assert.True(entryIds.Add(entryId!), $"Duplicate entry id in preset {id}: {entryId}");
                Assert.Contains(entryId!, knownEntryIds);
            }

            if (entryIds.Count > 0)
                nonEmptyPresetCount++;
        }

        Assert.Contains(defaultPresetId!, presetIds);
        Assert.True(nonEmptyPresetCount > 0, "At least one preset should name launch entries.");
    }

    private static void AssertPortableSettingsContract(string settingsPath, IReadOnlyList<LaunchEntry> entries)
    {
        var knownEntryIds = entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        var json = File.ReadAllText(settingsPath);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var root = document.RootElement;
        Assert.Equal(LauncherSettings.DefaultTheme, root.GetProperty("theme").GetString());
        Assert.Equal("", root.GetProperty("lastFilterText").GetString());
        Assert.Equal("custom", root.GetProperty("sortMode").GetString());
        Assert.Equal("cards", root.GetProperty("entryViewMode").GetString());
        Assert.True(root.GetProperty("requireConfirmationForLaunches").GetBoolean());

        var entryOrder = root.GetProperty("customEntryOrder").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Equal(knownEntryIds.Count, entryOrder.Count);
        Assert.Equal(knownEntryIds.Count, entryOrder.Distinct(StringComparer.Ordinal).Count());
        Assert.All(entryOrder, entryId =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entryId));
            Assert.Contains(entryId!, knownEntryIds);
        });

        var window = root.GetProperty("window");
        Assert.Equal(LauncherWindowSettings.DefaultWidth, window.GetProperty("width").GetDouble());
        Assert.Equal(LauncherWindowSettings.DefaultHeight, window.GetProperty("height").GetDouble());
    }
}
