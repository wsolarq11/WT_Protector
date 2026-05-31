namespace WTLauncher.Tests;

public sealed partial class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryFeatureRealityNorthStarAudit_CoversActiveFeatureMappings()
    {
        var auditPath = TestRepositoryPaths.Document("feature-reality-north-star-audit.md");
        var readmePath = TestRepositoryPaths.RootFile("README.md");

        Assert.True(File.Exists(auditPath), "The feature reality/north-star audit must stay source-tracked.");

        var audit = File.ReadAllText(auditPath);
        var readme = File.ReadAllText(readmePath);

        Assert.Contains("Purpose: evaluate whether each active WT Launcher feature mapping solves a real user problem", audit);
        Assert.Contains("Reviewed on: 2026-06-01", audit);
        Assert.Contains("It does not evaluate ignored `.snow/` local runtime/index state.", audit);
        Assert.Contains("## Fresh external yardstick spot-check", audit);
        Assert.Contains("## Current GitHub similar-project snapshot", audit);
        Assert.Contains("## Feature mapping audit", audit);
        Assert.Contains("## Objective completion audit", audit);
        Assert.Contains("## Final answer", audit);
        Assert.Equal(21, CountFeatureHeadings(audit));
        AssertFeatureSectionsUseCompleteAuditShape(audit);

        foreach (var expectedMapping in new[]
        {
            "Launch entry object",
            "Existing-directory contract",
            "Search/filter",
            "Direct single launch",
            "Persistent checkbox selection and batch launch",
            "Named launch groups / presets",
            "First-class preset actions",
            "Preset-vs-selection preview",
            "Preset launch plan preview",
            "Add/edit/delete launcher buttons",
            "Custom order and drag reorder",
            "Sort modes",
            "View modes: Cards / Details / Names",
            "Adaptive card sizing and density",
            "Global launch confirmation",
            "Portable settings",
            "JSON persistence, backup, and rollback compensation",
            "Core/App/Tests separation",
            "Self-contained win-x64 publish and native gate",
            "Retired legacy surfaces",
            "Design benchmark and giants-shoulders library",
        })
        {
            Assert.Contains(expectedMapping, audit);
        }

        foreach (var externalYardstick in new[]
        {
            "Windows Terminal",
            "VS Code",
            "Raycast Quicklinks",
            "Docker Compose profiles",
            "WCAG 2.2",
            "Kubernetes",
        })
        {
            Assert.Contains(externalYardstick, audit);
        }

        foreach (var githubYardstick in new[]
        {
            "SyDroX/dev-layout",
            "augustdragon/terminal-manager",
            "joyahmed/devgo-app",
            "Flow-Launcher/Flow.Launcher",
            "Wox-launcher/Wox",
        })
        {
            Assert.Contains(githubYardstick, audit);
        }

        Assert.Contains("\"100% confidence and 0% side effects\"", audit);
        Assert.Contains("preflight everything local before launching", audit);
        Assert.Contains("Live GitHub repository search/API checks were refreshed on 2026-06-01", audit);
        Assert.Contains("updated 2026-05-31T16:46:25Z", audit);
        Assert.Contains("updated 2026-05-31T15:02:11Z", audit);
        AssertObjectiveCompletionAuditPreservesOriginalScope(audit);
        Assert.Contains("not claim broad launcher/platform north-star status", audit);
        Assert.Contains("cannot honestly claim broad launcher supremacy", audit);
        Assert.Contains("Named presets can now launch directly from `config/presets.json` without rewriting checkbox state first", audit);
        Assert.Contains("Current-group preview uses the same preflight output and formatter", audit);
        Assert.Contains("transient `启动回执`", audit);
        Assert.Contains("transient single-launch canceled/request/failure receipts", audit);
        Assert.Contains("honest requested-start wording", audit);
        Assert.Contains("side-effect-free current-group preview", audit);
        Assert.Contains("preserving the preset name in cancellation and unexpected-failure status", audit);
        Assert.Contains("config-save failures during select-all/clear/checkbox mutation", audit);
        Assert.Contains("Applying a preset has a failure contract too", audit);
        Assert.Contains("roll back memory/file state on preset save failures", audit);
        Assert.Contains("discards uncommitted drag-sort preview state first", audit);
        Assert.Contains("ghost order cannot become a hidden source of truth", audit);
        Assert.Contains("missing, unknown, or wrong-case JSON fields now fail fast", audit);
        Assert.Contains("missing `entryIds` rejection", audit);
        Assert.Contains("propagates missing or failed `presets.json`", audit);
        Assert.Contains("instead of synthesizing a second empty preset truth", audit);
        Assert.Contains("fail-fast rejection of missing files plus missing, unknown, or wrong-case JSON fields", audit);
        Assert.Contains("unused `theme` state was removed", audit);
        Assert.Contains("docs/feature-reality-north-star-audit.md", readme);
        Assert.Contains("portable UI preferences such as filter text", readme);
        Assert.DoesNotContain("portable UI preferences such as theme", readme);
        Assert.Contains("must not contain the obsolete per-entry `confirm` key", readme);
        Assert.Contains("must exist, require the documented camelCase keys", readme);
        Assert.Contains("does not replace a failed or missing `presets.json` with an empty launch-group model", readme);
        Assert.DoesNotContain("current app ignores it", readme);
        Assert.Contains("Selected-item and current-group batch launch preflight every target before confirmation or process start", readme);
        Assert.Contains("Single and batch launch write transient `启动回执` text for canceled, requested, or failed launch attempts", readme);
        Assert.Contains("已请求启动", readme);
        Assert.Contains("预演当前组", readme);
        Assert.Contains("without starting processes or rewriting checkbox selection", readme);
        Assert.Contains("launches the selected preset entries directly without rewriting checkbox state first", readme);
        Assert.Contains("Checkbox-selection and preset-apply config save failures", readme);
        Assert.Contains("launch-group save/update/delete writes attempt to roll back", readme);
        Assert.Contains("a ghost reorder is never silently saved by a launch-group action", readme);
        Assert.Contains("resolves against the last committed order", readme);
        Assert.Contains("Cards / Details", readme);
        Assert.Contains("Names", readme);
        Assert.Contains("Current gate state:", readme);
        Assert.Contains("no current native-gate blocker is known after a fresh `tools/verify.ps1` PASS", readme);
        Assert.Contains("not a claim that the launcher is unimprovable", readme);
        Assert.DoesNotContain("none for the current launcher scope", readme);
    }

    private static int CountFeatureHeadings(string text) =>
        text
            .Split('\n')
            .Select(line => line.TrimStart())
            .Count(line => line.StartsWith("### ", StringComparison.Ordinal));

    private static void AssertFeatureSectionsUseCompleteAuditShape(string audit)
    {
        var featureAuditStart = audit.IndexOf("## Feature mapping audit", StringComparison.Ordinal);
        var gapsStart = audit.IndexOf("## Gaps that block a broad-domain north-star claim", StringComparison.Ordinal);
        Assert.True(featureAuditStart >= 0, "Feature mapping audit section is missing.");
        Assert.True(gapsStart > featureAuditStart, "Feature mapping audit section must appear before broad-domain gaps.");

        var featureAudit = audit[featureAuditStart..gapsStart];
        var sections = featureAudit
            .Split("\n### ", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .ToArray();

        Assert.Equal(21, sections.Length);
        foreach (var section in sections)
        {
            Assert.Contains("- Real problem:", section);
            Assert.Contains("- Current data owner:", section);
            Assert.Contains("- Evidence:", section);
            Assert.Contains("- Verdict:", section);
            Assert.Contains("- Remaining gap:", section);
        }
    }

    private static void AssertObjectiveCompletionAuditPreservesOriginalScope(string audit)
    {
        foreach (var requiredLine in new[]
        {
            "- Requirement: evaluate every active feature.",
            "- Requirement: decide whether each feature is battle-practice or market-proven north-star.",
            "- Requirement: current ability is already unimprovable.",
            "- Requirement: stronger than any similar GitHub project.",
            "- Requirement: remain small and beautiful.",
            "- Requirement: 100% confidence and 0% side effects.",
            "- Status: contradicted. There are possible improvements",
            "- Status: false for broad launcher scope",
            "transient single-launch canceled/request/failure receipts plus batch receipts",
            "- Status: impossible as an absolute claim; mitigated where the repository owns the boundary.",
        })
        {
            Assert.Contains(requiredLine, audit);
        }
    }
}
