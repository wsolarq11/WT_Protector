using System.Text.Json;

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
        using var githubSnapshotDoc = JsonDocument.Parse(File.ReadAllText(githubSnapshotPath));
        var githubSnapshot = githubSnapshotDoc.RootElement;
        var githubCapturedAtUtc = githubSnapshot.GetProperty("capturedAtUtc").GetString();
        var readme = File.ReadAllText(readmePath);

        Assert.Contains("Purpose: evaluate whether each active WT Launcher feature mapping solves a real user problem", audit);
        Assert.Contains("Reviewed on: 2026-06-02", audit);
        Assert.Contains("It does not evaluate ignored `.snow/` local runtime/index state.", audit);
        Assert.Contains("## Fresh external yardstick spot-check", audit);
        Assert.Contains("## Current GitHub similar-project snapshot", audit);
        Assert.Contains("docs/reference/github-similar-projects-snapshot.json", audit);
        Assert.Contains("tools/update-github-similar-projects-snapshot.ps1", audit);
        Assert.Contains("gh repo view <owner>/<repo>", audit);
        Assert.Contains("root verify validates the snapshot schema", audit);
        Assert.Contains("freshness window", audit);
        Assert.Contains("GitHub yardstick verdict tokens used below:", audit);
        Assert.Contains("direct-narrow-weaker", audit);
        Assert.Contains("adjacent-axis-stronger", audit);
        Assert.Contains("broad-market-stronger", audit);
        Assert.Contains("Proof taxonomy used below:", audit);
        Assert.Contains("repo-battle-tested", audit);
        Assert.Contains("market-informed", audit);
        Assert.Contains("legacy-flow-informed", audit);
        Assert.Contains("accessibility-informed", audit);
        Assert.Contains("source-backed", audit);
        Assert.Contains("Gap classes used below:", audit);
        Assert.Contains("none-proven", audit);
        Assert.Contains("future-workload", audit);
        Assert.Contains("external-process-limit", audit);
        Assert.Contains("visual-proof", audit);
        Assert.Contains("distribution-scope", audit);
        Assert.Contains("source-freshness", audit);
        Assert.Contains("- Feature id:", audit);
        Assert.Contains("- Proof type:", audit);
        Assert.Contains("Verdict levels used below:", audit);
        Assert.Contains("narrow-north-star", audit);
        Assert.Contains("- Verdict level:", audit);
        Assert.Contains("- Gap class:", audit);
        Assert.Contains("## Feature mapping audit", audit);
        Assert.Contains("## Objective completion audit", audit);
        Assert.Contains("Objective verdict classes used below:", audit);
        Assert.Contains("proven-current-scope", audit);
        Assert.Contains("proven-audit-only", audit);
        Assert.Contains("contradicted", audit);
        Assert.Contains("false-broad-scope", audit);
        Assert.Contains("preserved-current-scope", audit);
        Assert.Contains("impossible-absolute", audit);
        Assert.Contains("## Claim boundary ledger", audit);
        Assert.Contains("Allowed current claims:", audit);
        Assert.Contains("Forbidden current claims:", audit);
        Assert.Contains("## Final answer", audit);
        Assert.Equal(21, CountFeatureHeadings(audit));
        AssertFeatureSectionsUseCompleteAuditShape(audit);
        AssertBroadDomainGapsUseControlledLedger(audit);
        AssertClaimBoundaryLedgerConstrainsFinalClaims(audit);
        AssertObjectiveCompletionAuditUsesIndentedEvidenceAndStatus(audit);

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
            Assert.Contains(githubYardstick, githubSnapshot.GetProperty("repositories").EnumerateArray().Select(repo => repo.GetProperty("fullName").GetString()));
        }
        AssertGitHubYardsticksUseControlledVerdicts(audit, githubSnapshot);

        Assert.Equal(3, githubSnapshot.GetProperty("schemaVersion").GetInt32());
        Assert.True(DateTimeOffset.TryParse(githubCapturedAtUtc, out _), "GitHub snapshot capturedAtUtc must be parseable.");
        Assert.Contains($"GitHub evidence capturedAtUtc: `{githubCapturedAtUtc}`", audit);
        var githubSnapshotSummary = githubSnapshot.GetProperty("summary");
        Assert.Equal(12, githubSnapshotSummary.GetProperty("repositories").GetInt32());
        Assert.Equal(2, githubSnapshotSummary.GetProperty("directNarrowWeaker").GetInt32());
        Assert.Equal(8, githubSnapshotSummary.GetProperty("adjacentAxisStronger").GetInt32());
        Assert.Equal(2, githubSnapshotSummary.GetProperty("broadMarketStronger").GetInt32());
        Assert.Contains("gh repo view <owner>/<repo>", githubSnapshot.GetProperty("evidenceCommand").GetString());
        Assert.Contains("tools\\update-github-similar-projects-snapshot.ps1", githubSnapshot.GetProperty("refreshCommand").GetString());
        Assert.Equal("broad Windows launcher", githubSnapshot.GetProperty("repositories").EnumerateArray().Single(repo => repo.GetProperty("fullName").GetString() == "Flow-Launcher/Flow.Launcher").GetProperty("classification").GetString());
        Assert.Equal("broad cross-platform launcher", githubSnapshot.GetProperty("repositories").EnumerateArray().Single(repo => repo.GetProperty("fullName").GetString() == "Wox-launcher/Wox").GetProperty("classification").GetString());
        Assert.Contains("\"100% confidence and 0% side effects\"", audit);
        Assert.Contains("preflight everything local before launching", audit);
        Assert.Contains("Live GitHub repository search/API checks were refreshed on 2026-06-02 local time", audit);
        Assert.Contains("\"Windows Terminal\" workspace launcher", audit);
        Assert.Contains("\"Claude Code\" launcher", audit);
        Assert.Contains("embedded PTY/SSH scope", audit);
        Assert.Contains("named-session CLI scope", audit);
        Assert.True(
            githubSnapshot.GetProperty("repositories").EnumerateArray().Single(repo => repo.GetProperty("fullName").GetString() == "Flow-Launcher/Flow.Launcher").GetProperty("stars").GetInt32() >= 10_000,
            "Flow Launcher must remain broad-market proof in the snapshot.");
        Assert.Equal(
            githubSnapshot.GetProperty("repositories").EnumerateArray().Single(repo => repo.GetProperty("fullName").GetString() == "Flow-Launcher/Flow.Launcher").GetProperty("stars").GetInt32(),
            githubSnapshotSummary.GetProperty("flowStars").GetInt32());
        Assert.True(
            githubSnapshot.GetProperty("repositories").EnumerateArray().Single(repo => repo.GetProperty("fullName").GetString() == "Wox-launcher/Wox").GetProperty("stars").GetInt32() >= 10_000,
            "Wox must remain broad-market proof in the snapshot.");
        Assert.Equal(
            githubSnapshot.GetProperty("repositories").EnumerateArray().Single(repo => repo.GetProperty("fullName").GetString() == "Wox-launcher/Wox").GetProperty("stars").GetInt32(),
            githubSnapshotSummary.GetProperty("woxStars").GetInt32());
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
        Assert.Contains("tools/update-github-similar-projects-snapshot.ps1", readme);
        Assert.Contains("current GitHub similar-project evidence schema", readme);
        Assert.Contains("root gate rejects stale snapshots", readme);
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
        var allowedProofTypes = new[]
        {
            "repo-battle-tested",
            "market-informed",
            "legacy-flow-informed",
            "accessibility-informed",
            "source-backed",
        };
        var allowedVerdictLevels = new[]
        {
            "narrow-north-star",
            "strong",
            "source-backed",
        };
        var allowedGapClasses = new[]
        {
            "none-proven",
            "future-workload",
            "external-process-limit",
            "visual-proof",
            "distribution-scope",
            "source-freshness",
        };
        var expectedFeatureIds = new[]
        {
            "launch-entry-object",
            "existing-directory-contract",
            "search-filter",
            "direct-single-launch",
            "persistent-checkbox-selection-and-batch-launch",
            "named-launch-groups-presets",
            "first-class-preset-actions",
            "preset-vs-selection-preview",
            "preset-launch-plan-preview",
            "add-edit-delete-launcher-buttons",
            "custom-order-and-drag-reorder",
            "sort-modes",
            "view-modes-cards-details-names",
            "adaptive-card-sizing-and-density",
            "global-launch-confirmation",
            "portable-settings",
            "json-persistence-backup-and-rollback-compensation",
            "core-app-tests-separation",
            "self-contained-win-x64-publish-and-native-gate",
            "retired-legacy-surfaces",
            "design-benchmark-and-giants-shoulders-library",
        };
        var sections = featureAudit
            .Split("\n### ", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .ToArray();

        Assert.Equal(21, sections.Length);
        Assert.Equal(expectedFeatureIds.Length, expectedFeatureIds.Distinct(StringComparer.Ordinal).Count());
        for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            var section = sections[sectionIndex];
            Assert.Contains("- Feature id:", section);
            var featureIdLine = section
                .Split('\n')
                .Single(line => line.StartsWith("- Feature id:", StringComparison.Ordinal));
            Assert.Equal(expectedFeatureIds[sectionIndex], ParseControlledValue(featureIdLine, "- Feature id:"));
            Assert.Contains("- Real problem:", section);
            Assert.Contains("- Proof type:", section);
            var proofLine = section
                .Split('\n')
                .Single(line => line.StartsWith("- Proof type:", StringComparison.Ordinal));
            foreach (var proofType in ParseControlledList(proofLine, "- Proof type:"))
            {
                Assert.Contains(proofType, allowedProofTypes);
            }
            Assert.Contains("- Verdict level:", section);
            var verdictLevelLine = section
                .Split('\n')
                .Single(line => line.StartsWith("- Verdict level:", StringComparison.Ordinal));
            Assert.Contains(ParseControlledValue(verdictLevelLine, "- Verdict level:"), allowedVerdictLevels);
            Assert.Contains("- Gap class:", section);
            var gapClassLine = section
                .Split('\n')
                .Single(line => line.StartsWith("- Gap class:", StringComparison.Ordinal));
            foreach (var gapClass in ParseControlledList(gapClassLine, "- Gap class:"))
            {
                Assert.Contains(gapClass, allowedGapClasses);
            }
            Assert.Contains("- Current data owner:", section);
            Assert.Contains("- Evidence:", section);
            Assert.Contains("- Verdict:", section);
            Assert.Contains("- Remaining gap:", section);
        }
    }

    private static void AssertGitHubYardsticksUseControlledVerdicts(string audit, JsonElement githubSnapshot)
    {
        var start = audit.IndexOf("Representative GitHub yardsticks:", StringComparison.Ordinal);
        var end = audit.IndexOf("Conclusion from the GitHub snapshot:", StringComparison.Ordinal);
        Assert.True(start >= 0, "Representative GitHub yardsticks section is missing.");
        Assert.True(end > start, "Representative GitHub yardsticks must appear before its conclusion.");

        var allowedVerdicts = new[]
        {
            "direct-narrow-weaker",
            "adjacent-axis-stronger",
            "broad-market-stronger",
        };
        var expectedYardsticks = new (string FullName, string Verdict)[]
        {
            ("SyDroX/dev-layout", "direct-narrow-weaker"),
            ("augustdragon/terminal-manager", "adjacent-axis-stronger"),
            ("noambrand/Launchpad-CLI", "adjacent-axis-stronger"),
            ("kevinkicho/win-screen-setup", "adjacent-axis-stronger"),
            ("ewdieckman/DevWorkspace", "adjacent-axis-stronger"),
            ("jsoulet42/claude-launcher-app", "direct-narrow-weaker"),
            ("A-ema755/claude-dashboard", "adjacent-axis-stronger"),
            ("adityacodepublic/wtsm-cli", "adjacent-axis-stronger"),
            ("pramin/AgentSpace", "adjacent-axis-stronger"),
            ("joyahmed/devgo-app", "adjacent-axis-stronger"),
            ("Flow-Launcher/Flow.Launcher", "broad-market-stronger"),
            ("Wox-launcher/Wox", "broad-market-stronger"),
        };
        var section = audit[start..end];
        var yardstickLines = section
            .Split('\n')
            .Where(line => line.StartsWith("- <https://github.com/", StringComparison.Ordinal))
            .ToArray();
        var snapshotRepos = githubSnapshot.GetProperty("repositories")
            .EnumerateArray()
            .Select(repo => repo.GetProperty("fullName").GetString())
            .ToArray();

        Assert.Equal(expectedYardsticks.Length, yardstickLines.Length);
        Assert.Equal(2, expectedYardsticks.Count(yardstick => yardstick.Verdict == "broad-market-stronger"));
        foreach (var (fullName, verdict) in expectedYardsticks)
        {
            Assert.Contains(verdict, allowedVerdicts);
            Assert.Contains(fullName, snapshotRepos);
            var repo = githubSnapshot.GetProperty("repositories")
                .EnumerateArray()
                .Single(snapshotRepo => snapshotRepo.GetProperty("fullName").GetString() == fullName);
            Assert.Equal(verdict, repo.GetProperty("yardstickVerdict").GetString());
            var line = yardstickLines.Single(yardstickLine => yardstickLine.Contains($"https://github.com/{fullName}", StringComparison.Ordinal));
            Assert.Contains($"Yardstick verdict: `{verdict}`", line);
        }
    }

    private static void AssertBroadDomainGapsUseControlledLedger(string audit)
    {
        var start = audit.IndexOf("## Gaps that block a broad-domain north-star claim", StringComparison.Ordinal);
        var end = audit.IndexOf("## Objective completion audit", StringComparison.Ordinal);
        Assert.True(start >= 0, "Broad-domain gap section is missing.");
        Assert.True(end > start, "Broad-domain gap section must appear before the objective completion audit.");

        var expectedGaps = new (string GapId, string GapClass, string[] RequirementIds)[]
        {
            ("import-export-ecosystem", "future-workload", new[] { "prove-unimprovable-current-ability", "prove-stronger-than-any-github-similar-project" }),
            ("run-history-terminal-health", "future-workload", new[] { "prove-unimprovable-current-ability", "prove-absolute-confidence-and-zero-side-effects" }),
            ("installer-update-signing-channel", "distribution-scope", new[] { "prove-unimprovable-current-ability", "prove-stronger-than-any-github-similar-project" }),
            ("screenshot-backed-visual-comfort", "visual-proof", new[] { "prove-unimprovable-current-ability" }),
            ("universal-supremacy-zero-side-effects", "external-process-limit", new[] { "prove-stronger-than-any-github-similar-project", "prove-absolute-confidence-and-zero-side-effects" }),
        };
        var section = audit[start..end];
        var gaps = section
            .Split("\n- Broad gap id:", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .ToArray();

        Assert.Equal(expectedGaps.Length, gaps.Length);
        for (var gapIndex = 0; gapIndex < gaps.Length; gapIndex++)
        {
            var lines = gaps[gapIndex]
                .Split('\n')
                .Select(line => line.TrimEnd())
                .ToArray();
            var gapId = lines[0].Trim();
            Assert.Equal(expectedGaps[gapIndex].GapId, gapId);

            var gapClassLine = lines
                .Single(line => line.TrimStart().StartsWith("- Gap class:", StringComparison.Ordinal))
                .TrimStart();
            Assert.Equal(expectedGaps[gapIndex].GapClass, ParseControlledValue(gapClassLine, "- Gap class:"));

            var blockedRequirementLine = lines
                .Single(line => line.TrimStart().StartsWith("- Blocks requirement id:", StringComparison.Ordinal))
                .TrimStart();
            Assert.Equal(
                expectedGaps[gapIndex].RequirementIds,
                ParseControlledList(blockedRequirementLine, "- Blocks requirement id:"));
            Assert.Contains(lines, line => line.TrimStart().StartsWith("- Evidence:", StringComparison.Ordinal));
        }
    }

    private static string ParseControlledValue(string line, string prefix)
    {
        Assert.StartsWith(prefix, line);
        var value = line[prefix.Length..].Trim();
        Assert.False(string.IsNullOrWhiteSpace(value), $"{prefix} must not be blank.");
        Assert.DoesNotContain(" ", value);
        Assert.DoesNotContain(".", value);
        Assert.DoesNotContain(";", value);
        return value;
    }

    private static string[] ParseControlledList(string line, string prefix)
    {
        Assert.StartsWith(prefix, line);
        var values = line[prefix.Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.NotEmpty(values);
        foreach (var value in values)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"{prefix} contains a blank token.");
            Assert.DoesNotContain(" ", value);
            Assert.DoesNotContain(".", value);
            Assert.DoesNotContain(";", value);
        }

        return values;
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
            "- Requirement id: evaluate-every-active-feature",
            "- Requirement id: classify-battle-practice-or-market-proven",
            "- Requirement id: prove-unimprovable-current-ability",
            "- Requirement id: prove-stronger-than-any-github-similar-project",
            "- Requirement id: preserve-small-and-beautiful",
            "- Requirement id: prove-absolute-confidence-and-zero-side-effects",
            "controlled gap class",
            "- Status: contradicted",
            "- Status: false-broad-scope",
            "transient single-launch canceled/request/failure receipts plus batch receipts",
            "- Status: impossible-absolute",
        })
        {
            Assert.Contains(requiredLine, audit);
        }
    }

    private static void AssertClaimBoundaryLedgerConstrainsFinalClaims(string audit)
    {
        var start = audit.IndexOf("## Claim boundary ledger", StringComparison.Ordinal);
        var end = audit.IndexOf("## Final answer", StringComparison.Ordinal);
        Assert.True(start >= 0, "Claim boundary ledger section is missing.");
        Assert.True(end > start, "Claim boundary ledger must appear before the final answer.");

        var section = audit[start..end];
        foreach (var requiredLine in new[]
        {
            "WT Launcher is a narrow-domain north-star for a portable Windows Terminal workspace launcher.",
            "WT Launcher is stronger only on the portable config, named group, side-effect-free preview, rollback-aware persistence, and native-gate slice.",
            "The strongest zero-side-effect claim applies only before external process handoff",
            "Do not claim broad launcher or automation-platform north-star status.",
            "Do not claim the product is unimprovable.",
            "Do not claim stronger-than-every-GitHub-similar-project status.",
            "Do not claim 100% confidence or 0% side effects for real `wt.exe` process launch behavior.",
            "Do not claim terminal-session health after `wt.exe` accepts the start request.",
        })
        {
            Assert.Contains(requiredLine, section);
        }

        var finalAnswer = audit[end..];
        Assert.Contains("north-star implementation only for the narrow domain", finalAnswer);
        Assert.Contains("It is not a broad-domain north star", finalAnswer);
    }

    private static void AssertObjectiveCompletionAuditUsesIndentedEvidenceAndStatus(string audit)
    {
        var start = audit.IndexOf("## Objective completion audit", StringComparison.Ordinal);
        var end = audit.IndexOf("## Final answer", StringComparison.Ordinal);
        Assert.True(start >= 0, "Objective completion audit section is missing.");
        Assert.True(end > start, "Objective completion audit section must appear before the final answer.");

        var section = audit[start..end];
        var requirements = section
            .Split("\n- Requirement:", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .ToArray();
        var allowedObjectiveVerdicts = new[]
        {
            "proven-current-scope",
            "proven-audit-only",
            "contradicted",
            "false-broad-scope",
            "preserved-current-scope",
            "impossible-absolute",
        };
        var expectedRequirementIdsAndVerdicts = new (string RequirementId, string Verdict)[]
        {
            ("evaluate-every-active-feature", "proven-current-scope"),
            ("classify-battle-practice-or-market-proven", "proven-audit-only"),
            ("prove-unimprovable-current-ability", "contradicted"),
            ("prove-stronger-than-any-github-similar-project", "false-broad-scope"),
            ("preserve-small-and-beautiful", "preserved-current-scope"),
            ("prove-absolute-confidence-and-zero-side-effects", "impossible-absolute"),
        };

        Assert.Equal(6, requirements.Length);
        for (var requirementIndex = 0; requirementIndex < requirements.Length; requirementIndex++)
        {
            var requirement = requirements[requirementIndex];
            var lines = requirement
                .Split('\n')
                .Select(line => line.TrimEnd())
                .ToArray();
            Assert.StartsWith(" ", lines[0]);
            var requirementIdLine = lines
                .Single(line => line.TrimStart().StartsWith("- Requirement id:", StringComparison.Ordinal))
                .TrimStart();
            var requirementId = ParseControlledValue(requirementIdLine, "- Requirement id:");
            Assert.Equal(expectedRequirementIdsAndVerdicts[requirementIndex].RequirementId, requirementId);
            Assert.Contains(lines, line => line.TrimStart().StartsWith("- Evidence:", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.TrimStart().StartsWith("- Status:", StringComparison.Ordinal));
            var statusLine = lines
                .Single(line => line.TrimStart().StartsWith("- Status:", StringComparison.Ordinal))
                .TrimStart();
            var objectiveVerdict = statusLine["- Status:".Length..]
                .TrimStart()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .First();
            Assert.Contains(objectiveVerdict, allowedObjectiveVerdicts);
            Assert.Equal(expectedRequirementIdsAndVerdicts[requirementIndex].Verdict, objectiveVerdict);
            Assert.DoesNotContain(lines, line => line == "- Evidence:" || line == "- Status:");
        }
    }
}
