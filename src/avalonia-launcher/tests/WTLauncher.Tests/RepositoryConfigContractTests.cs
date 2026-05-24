using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryRootConfig_LoadsAndBuildsDryRunPlans()
    {
        var root = FindRepositoryRoot();
        var configPath = Path.Combine(root, "config", "config.json");
        var presetsPath = Path.Combine(root, "config", "presets.json");
        var settingsPath = Path.Combine(root, "config", "settings.json");

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

    [Fact]
    public void RepositoryRootSettings_RoundTripPortableStateFields()
    {
        var root = FindRepositoryRoot();
        var settingsPath = Path.Combine(root, "config", "settings.json");

        var settings = LauncherSettingsStore.Load(settingsPath);

        Assert.Equal("light", settings.Theme);
        Assert.Equal("", settings.LastFilterText);
        Assert.Equal("custom", settings.SortMode);
        Assert.Equal("cards", settings.EntryViewMode);
        Assert.True(settings.RequireConfirmationForLaunches);
        Assert.NotEmpty(settings.CustomEntryOrder);
        Assert.Equal(1180d, settings.Window.Width);
        Assert.Equal(760d, settings.Window.Height);
    }

    [Fact]
    public void RepositoryDesignBenchmarkDigest_IsSourceTrackedAndReferenced()
    {
        var root = FindRepositoryRoot();
        var digestPath = Path.Combine(root, "docs", "design-benchmark-digest.md");
        var designPath = Path.Combine(root, "DESIGN.md");

        Assert.True(File.Exists(digestPath), "The 12-benchmark digest must stay source-tracked.");

        var digest = File.ReadAllText(digestPath);
        var design = File.ReadAllText(designPath);

        Assert.Contains("Purpose: convert 12 external first-class design benchmarks", digest);
        Assert.Contains("does not authorize importing a foreign design system", digest);
        Assert.Equal(12, CountNumberedListItems(digest, "## The 12 benchmark inputs", "## WT Launcher acceptance translation"));

        foreach (var expectedBenchmark in new[]
        {
            "Microsoft Windows app design",
            "Microsoft Fluent 2",
            "Apple Human Interface Guidelines: Feedback",
            "Apple Human Interface Guidelines: Accessibility",
            "Material Design adaptive layout",
            "WCAG 2.2",
            "IBM Carbon",
            "Atlassian Design System",
            "GitHub Primer",
            "Shopify Polaris web components",
            "Adobe Spectrum 2",
            "GOV.UK Design System",
        })
        {
            Assert.Contains(expectedBenchmark, digest);
        }

        foreach (var expectedLocalRule in new[]
        {
            "Native-first:",
            "One source per state:",
            "One source per visual token:",
            "Feedback is proportional:",
            "Gesture alternatives:",
            "Adaptive density:",
            "View mode separation:",
            "Minimal chrome:",
            "Token restraint:",
            "Testable design:",
        })
        {
            Assert.Contains(expectedLocalRule, digest);
        }

        Assert.Contains("docs/design-benchmark-digest.md", design);
        Assert.Contains("- Last refreshed: 2026-05-20", design);
        Assert.Contains("Border.drag-insert", design);
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
        Assert.Equal("light", root.GetProperty("theme").GetString());
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
        Assert.Equal(1180d, window.GetProperty("width").GetDouble());
        Assert.Equal(760d, window.GetProperty("height").GetDouble());
    }

    private static int CountNumberedListItems(string text, string sectionStart, string sectionEnd)
    {
        var section = SliceSection(text, sectionStart, sectionEnd);
        return section
            .Split('\n')
            .Select(line => line.TrimStart())
            .Count(IsNumberedListItem);
    }

    private static string SliceSection(string text, string sectionStart, string sectionEnd)
    {
        var start = text.IndexOf(sectionStart, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing section: {sectionStart}");

        var end = text.IndexOf(sectionEnd, start + sectionStart.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"Missing section after {sectionStart}: {sectionEnd}");

        return text[start..end];
    }

    private static bool IsNumberedListItem(string line)
    {
        var dot = line.IndexOf('.', StringComparison.Ordinal);
        return dot > 0
            && dot < line.Length - 1
            && line[..dot].All(char.IsDigit)
            && line[dot + 1] == ' ';
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "config", "config.json"))
                && File.Exists(Path.Combine(dir.FullName, "config", "settings.json"))
                && File.Exists(Path.Combine(dir.FullName, "README.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "src", "avalonia-launcher")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WT_Protector repository root from test output directory.");
    }
}
