
namespace WTLauncher.Tests;

public sealed partial class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryDesignBenchmarkDigest_IsSourceTrackedAndReferenced()
    {
        var digestPath = TestRepositoryPaths.Document("design-benchmark-digest.md");
        var giantsReadmePath = TestRepositoryPaths.Document(Path.Combine("giants-shoulders", "README.md"));
        var launcherPatternsPath = TestRepositoryPaths.Document(Path.Combine("giants-shoulders", "launcher-first-class-patterns.md"));
        var designPath = TestRepositoryPaths.RootFile("DESIGN.md");

        Assert.True(File.Exists(digestPath), "The 12-benchmark digest must stay source-tracked.");
        Assert.True(File.Exists(giantsReadmePath), "The giants-shoulders index must stay source-tracked.");
        Assert.True(File.Exists(launcherPatternsPath), "The launcher patterns reference must stay source-tracked.");

        var digest = File.ReadAllText(digestPath);
        var giantsReadme = File.ReadAllText(giantsReadmePath);
        var launcherPatterns = File.ReadAllText(launcherPatternsPath);
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
        Assert.Contains("- Last refreshed: 2026-06-24", design);
        Assert.Contains("Border.drag-insert", design);

        Assert.Contains("This is a reference library, not an authority layer.", giantsReadme);
        Assert.Equal(5, CountNumberedListItems(giantsReadme, "## Admission rule", "## Current entries"));
        Assert.Contains("Reviewed source", giantsReadme);
        Assert.Contains("Observed stack", giantsReadme);
        Assert.Contains("Lessons worth borrowing", giantsReadme);
        Assert.Contains("What not to copy", giantsReadme);
        Assert.Contains("Next local proof", giantsReadme);
        Assert.Contains("First-class launcher patterns", giantsReadme);

        Assert.Contains("This page is source material for WT Launcher decisions, not a mandate to copy any product.", launcherPatterns);
        Assert.Contains("## What we intentionally did not copy", launcherPatterns);
        Assert.Contains("No plugin marketplace or extension runtime yet.", launcherPatterns);
        Assert.Contains("No hidden status database.", launcherPatterns);
        Assert.Contains("No workflow graph engine.", launcherPatterns);
        Assert.Contains("No new dependency.", launcherPatterns);
        Assert.Contains("Rollback point:", launcherPatterns);
        Assert.Equal(14, CountMarkdownTableRows(launcherPatterns, "## The 12 borrowed design patterns", "Note:"));
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

    private static int CountMarkdownTableRows(string text, string sectionStart, string sectionEnd)
    {
        var section = SliceSection(text, sectionStart, sectionEnd);
        return section
            .Split('\n')
            .Select(line => line.Trim())
            .Count(line => line.StartsWith("|", StringComparison.Ordinal)
                && !line.StartsWith("| ---", StringComparison.Ordinal)
                && !line.StartsWith("| Tool family", StringComparison.Ordinal));
    }
}
