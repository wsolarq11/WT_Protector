namespace WTLauncher.Tests;

public sealed partial class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryFeatureRealityNorthStarAudit_CoversActiveFeatureMappings()
    {
        var auditPath = TestRepositoryPaths.Document("feature-reality-north-star-audit.md");
        var githubSnapshotPath = TestRepositoryPaths.Document(
            Path.Combine("reference", "github-similar-projects-snapshot.json"));
        var readmePath = TestRepositoryPaths.RootFile("README.md");

        Assert.True(File.Exists(auditPath), "The feature reality/north-star audit must stay source-tracked.");
        Assert.True(File.Exists(githubSnapshotPath), "The similar-project GitHub snapshot must stay source-tracked.");

        var audit = File.ReadAllText(auditPath);
        var githubSnapshot = File.ReadAllText(githubSnapshotPath);
        var readme = File.ReadAllText(readmePath);

        Assert.Contains("Purpose: evaluate whether each active WT Launcher feature mapping solves a real user problem", audit);
        Assert.Contains("Reviewed on: 2026-06-01", audit);
        Assert.Contains("It does not evaluate ignored `.snow/` local runtime/index state.", audit);
        Assert.Contains("## Fresh external yardstick spot-check", audit);
        Assert.Contains("## Current GitHub similar-project snapshot", audit);
        Assert.Contains("docs/reference/github-similar-projects-snapshot.json", audit);
        Assert.Contains("gh repo view <owner>/<repo>", audit);
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
            "noambrand/Launchpad-CLI",
            "kevinkicho/win-screen-setup",
            "ewdieckman/DevWorkspace",
            "jsoulet42/claude-launcher-app",
            "A-ema755/claude-dashboard",
            "adityacodepublic/wtsm-cli",
            "pramin/AgentSpace",
            "joyahmed/devgo-app",
            "Flow-Launcher/Flow.Launcher",
            "Wox-launcher/Wox",
        })
        {
            Assert.Contains(githubYardstick, audit);
            Assert.Contains(githubYardstick, githubSnapshot);
        }

        Assert.Contains("\"schemaVersion\": 1", githubSnapshot);
        Assert.Contains("\"capturedAtUtc\": \"2026-06-01T", githubSnapshot);
        Assert.Contains("\"evidenceCommand\": \"gh repo view <owner>/<repo>", githubSnapshot);
        Assert.Contains("\"classification\": \"broad Windows launcher\"", githubSnapshot);
        Assert.Contains("\"classification\": \"broad cross-platform launcher\"", githubSnapshot);
        Assert.Contains("\"100% confidence and 0% side effects\"", audit);
        Assert.Contains("preflight everything local before launching", audit);
        Assert.Contains("Live GitHub repository search/API checks were refreshed on 2026-06-01", audit);
        Assert.Contains("\"Windows Terminal\" workspace launcher", audit);
        Assert.Contains("\"Claude Code\" launcher", audit);
        Assert.Contains("updated 2026-05-31T14:38:02Z", audit);
        Assert.Contains("embedded PTY/SSH scope", audit);
        Assert.Contains("updated 2026-04-08T23:38:30Z", audit);
        Assert.Contains("named-session CLI scope", audit);
        Assert.Contains("14,844 stars, 607 forks, pushed 2026-06-01T09:41:17Z, updated 2026-06-01T08:06:11Z", audit);
        Assert.Contains("26,982 stars, 2,411 forks, pushed 2026-06-01T03:46:06Z, updated 2026-06-01T08:40:12Z", audit);
        Assert.Contains("\"stars\": 14844", githubSnapshot);
        Assert.Contains("\"stars\": 26982", githubSnapshot);
        AssertObjectiveCompletionAuditPreservesOriginalScope(audit);
        Assert.Contains("not claim broad launcher/platform north-star status", audit);
        Assert.Contains("cannot honestly claim broad launcher supremacy", audit);
        Assert.Contains("Named presets can now launch directly from `config/presets.json` without rewriting checkbox state first", audit);
        Assert.Contains("Current-group preview uses the same preflight output and formatter", audit);
        Assert.Contains("transient `启动回执`", audit);
        Assert.Contains("transient single-launch canceled/request/failure receipts", audit);
        Assert.Contains("receipt rejection of blank entry names instead of rendering fake `(unnamed)` launches", audit);
        Assert.Contains("single launch snapshotting entry data before confirmation", audit);
        Assert.Contains("single launch confirmation target carries entry id/name plus the generated plan", audit);
        Assert.Contains("unexpected single-launch exceptions replacing stale dry-run text with a failure receipt", audit);
        Assert.Contains("unexpected dry-run exceptions replacing stale preview text with a preview failure receipt", audit);
        Assert.Contains("Each preflight item snapshots the entry id and name", audit);
        Assert.Contains("Batch preflight exposes its prepared item list as read-only state instead of leaking the mutable staging list", audit);
        Assert.Contains("Batch launch reports are immutable snapshots", audit);
        Assert.Contains("starting a preflight returns a separate launch report instead of mutating the preflight report", audit);
        Assert.Contains("Batch confirmation snapshots its item list as read-only state plus a required target label", audit);
        Assert.Contains("honest requested-start wording", audit);
        Assert.Contains("side-effect-free current-group preview", audit);
        Assert.Contains("rejects blank failure entry names instead of rendering fake `(unnamed)` rows", audit);
        Assert.Contains("exposes batch failure messages as read-only report state instead of handing formatters the mutable failure list", audit);
        Assert.Contains("reports blank-name batch preflight/start failures without inventing fallback names", audit);
        Assert.Contains("unexpected batch exceptions now replace stale dry-run text with a failure receipt", audit);
        Assert.Contains("launch plans themselves reject blank executable/argument/working-directory/command fields before process handoff", audit);
        Assert.Contains("runtime filter mutations and core filter projection rejecting null instead of inventing an empty filter", audit);
        Assert.Contains("projection rejection of null entry text/timestamps or blank ids", audit);
        Assert.Contains("filter/sort projection returning a read-only collection snapshot instead of a mutable projection list", audit);
        Assert.Contains("preserving the preset name in cancellation and unexpected-failure status", audit);
        Assert.Contains("blank-name preview preflight failures replacing stale preview text with a preview failure receipt", audit);
        Assert.Contains("dialog-boundary handler assignments rejecting null instead of storing a broken runtime callback", audit);
        Assert.Contains("config-save failures during select-all/clear/checkbox mutation", audit);
        Assert.Contains("blank/duplicate reference rejection before reference rename/remove mutation", audit);
        Assert.Contains("entry delete confirmation snapshotting target id/name before dialog mutation", audit);
        Assert.Contains("preset delete confirmation snapshotting target id/name/count before dialog mutation", audit);
        Assert.Contains("entry view models exposing snapshot copies instead of public live `LaunchEntry` rows", audit);
        Assert.Contains("entry edit dialogs receiving a snapshot entry instead of the live mutable row", audit);
        Assert.Contains("entry edit dialogs rejecting null initial/submitted text instead of converting it into empty required-field input", audit);
        Assert.Contains("Applying a preset has a failure contract too", audit);
        Assert.Contains("roll back memory/file state on preset save failures", audit);
        Assert.Contains("save-new-preset dialogs receiving a read-only snapshot target", audit);
        Assert.Contains("reject blank/duplicate preset entry ids before add/update mutation", audit);
        Assert.Contains("reject invalid fallback preset references, blank fallback preset ids, and missing default-preset state before delete mutation", audit);
        Assert.Contains("reject missing selected-preset ids during preset state restore instead of silently selecting the first group", audit);
        Assert.Contains("selected fallback view model after deletion instead of hiding fallback selection or rendering `(none)`", audit);
        Assert.Contains("discards uncommitted drag-sort preview state first", audit);
        Assert.Contains("ghost order cannot become a hidden source of truth", audit);
        Assert.Contains("sessions not publicly exposing mutable source view models or settings snapshots", audit);
        Assert.Contains("drag sessions deriving the dragged id from the source entry instead of accepting a second mismatched id", audit);
        Assert.Contains("required drag workflow dependencies failing fast at construction", audit);
        Assert.Contains("preview collection outputs that cannot be cast back to mutable selection/preset/missing-id storage", audit);
        Assert.Contains("custom-order index returning a read-only dictionary snapshot instead of a mutable index map", audit);
        Assert.Contains("reorder bounds capture and animation scheduling exposing read-only cloned snapshots instead of caller-owned mutable dictionaries", audit);
        Assert.Contains("invalid move/custom-order rejection before mutation or sort projection", audit);
        Assert.Contains("missing or stale custom-order ids rejected before sort projection instead of buried at the end", audit);
        Assert.Contains("custom-order sync rejection of invalid order or known ids before mutation", audit);
        Assert.Contains("missing, unknown, or wrong-case JSON fields now fail fast", audit);
        Assert.Contains("missing `entryIds` rejection", audit);
        Assert.Contains("invalid preset references rejected before label/preview/launch projection", audit);
        Assert.Contains("propagates missing or failed `presets.json`", audit);
        Assert.Contains("preset view models snapshot preset facts plus read-only entry ids instead of exposing the live mutable preset object", audit);
        Assert.Contains("instead of synthesizing a second empty preset truth", audit);
        Assert.Contains("reject missing or unknown mode ids", audit);
        Assert.Contains("null settings fields, null saved filter state at ViewModel construction, null runtime filter mutations", audit);
        Assert.Contains("null runtime sort/display mode assignments", audit);
        Assert.Contains("missing/blank mode ids, invalid in-memory settings during close-time window save", audit);
        Assert.Contains("invalid in-memory settings during close-time window save", audit);
        Assert.Contains("non-positive persisted window dimensions now fail fast", audit);
        Assert.Contains("missing files plus missing, null, blank mode, non-positive window geometry, unknown, or wrong-case JSON fields", audit);
        Assert.Contains("config load returning a read-only entry collection snapshot instead of a mutable load list", audit);
        Assert.Contains("invalid in-memory mutation data and invalid rollback snapshots propagating instead of being reported as ordinary save/rollback failure", audit);
        Assert.Contains("rollback snapshot accessors cloning or read-only wrapping captured state instead of exposing mutable rollback storage", audit);
        Assert.Contains("unused `theme` state was removed", audit);
        Assert.Contains("startup arguments reject null argument lists plus unknown or duplicate `--config` inputs instead of silently choosing one", audit);
        Assert.Contains("config path resolution rejects missing search roots instead of silently falling back to `Environment.CurrentDirectory`", audit);
        Assert.Contains("ViewModel-owned collections expose read-only projections instead of public mutable collection owners", audit);
        Assert.Contains("accepts only one optional `--config <path>` pair", readme);
        Assert.Contains("docs/feature-reality-north-star-audit.md", readme);
        Assert.Contains("docs/reference/github-similar-projects-snapshot.json", readme);
        Assert.Contains("portable UI preferences such as filter text", readme);
        Assert.DoesNotContain("portable UI preferences such as theme", readme);
        Assert.Contains("must not contain the obsolete per-entry `confirm` key", readme);
        Assert.Contains("must exist, require the documented camelCase keys", readme);
        Assert.Contains("reject missing, null, blank mode, or unknown keys", readme);
        Assert.Contains("window `width` and `height` must be positive numbers", readme);
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
