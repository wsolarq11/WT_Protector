# Feature reality and north-star audit

Reviewed on: 2026-06-01

Purpose: evaluate whether each active WT Launcher feature mapping solves a real user problem and whether the implementation is already a domain north star. This is an audit document, not marketing copy.

## Scope and standard

This audit covers the active Avalonia mainline and repository-level launcher contract:

- `src/avalonia-launcher/`
- `config/config.json`
- `config/presets.json`
- `config/settings.json`
- `README.md`
- `DESIGN.md`
- `docs/design-benchmark-digest.md`
- `tools/verify.ps1`

It does not evaluate ignored `.snow/` local runtime/index state.

For this repository, "domain north star" means:

1. the feature maps to a real high-frequency local launcher problem;
2. the data structure has one clear source of truth;
3. the user path is explicit, visible, and not a hidden fallback;
4. the behavior has current source/tests/gate evidence;
5. the feature stays inside the narrow Windows Terminal workspace-launcher domain instead of becoming a generic automation platform.

The requested "100% confidence and 0% side effects" standard is useful as pressure, but it is not an honest completion claim for software that starts external processes. This audit therefore treats absolute claims as unproven unless the current source, tests, and gate prove them. For side effects, the correct engineering target is: preflight everything local before launching, make every remaining side effect explicit, and never hide a partial failure behind fallback behavior.

The external yardstick remains the source-tracked benchmark set in `docs/design-benchmark-digest.md` and `docs/giants-shoulders/launcher-first-class-patterns.md`: Windows Terminal profiles/command-line launch shape, VS Code workspaces/settings, Raycast quick links, Alfred workflows, Kubernetes spec/status separation, and WCAG/major design-system accessibility rules.

## Fresh external yardstick spot-check

Current external references were spot-checked on 2026-06-01 before making the north-star judgment:

- Microsoft Windows Terminal still treats `wt.exe` as an explicit command line with `-d` / `--startingDirectory` for the starting folder, which matches this repo's `directory` + `command` entry contract.
- VS Code still separates user/workspace settings and multi-root workspace files, which supports portable, human-readable context objects instead of hidden local state.
- Raycast Quicklinks still solve the same frequent-action problem by opening URLs, file paths, and deeplinks instantly; current v2 Quicklinks also include tags, pinning, and dynamic placeholders, which is exactly why this repo must not claim broad launcher/platform north-star status.
- Docker Compose profiles remain a named-subset model for activating only the needed services, which maps cleanly to WT Launcher's named presets.
- WCAG 2.2 includes Dragging Movements as a Level AA criterion; this makes non-drag reorder a real accessibility contract, not cosmetic polish.
- Kubernetes continues to separate desired object data from observed status; WT Launcher has desired config/preset/settings objects but no run-history/status object yet.

## Current GitHub similar-project snapshot

Live GitHub repository search/API checks were refreshed on 2026-06-01. The result is blunt: a universal "stronger than any similar GitHub project" claim is not provable, and in the broad launcher market it is false. Flow Launcher and Wox are mature broad launchers with thousands of stars, plugin ecosystems, package channels, and community proof. WT Launcher should not fight them as a generic launcher. That would be stupid scope creep.

The relevant fight is narrower: portable Windows Terminal workspace launching with inspectable entries, named launch groups, side-effect-free plan preview, explicit launch confirmation, rollback-aware JSON persistence, and a native repo gate.

Representative GitHub yardsticks:

- <https://github.com/SyDroX/dev-layout> — direct Windows Terminal workspace launcher; PowerShell, 0 stars, updated 2026-05-27. Strong for fixed Claude Code multi-repo layout, session resume, and `cd` blocking. Weaker than WT Launcher on editable portable UI entries, named launch groups, repository-level native gate, and rollback-aware config persistence.
- <https://github.com/augustdragon/terminal-manager> — direct Windows Terminal profile/session manager; TypeScript, 0 stars, updated 2026-05-17. Stronger than WT Launcher for WT profile editing, split-pane session groups, import/export, and WT `settings.json` sync. Heavier stack, local web server, more moving parts, and it mutates Windows Terminal settings by design. This is a different tradeoff, not a small-and-beautiful local config launcher.
- <https://github.com/joyahmed/devgo-app> — WSL-first developer launcher; C#, 2 stars, updated 2026-05-24. Strong for WSL + VS Code + tmux workflows and installer ambition. README still lists keyboard shortcuts and rough edges as in-progress, so it is not stronger on the current WT Launcher axis of verified portable launch groups.
- <https://github.com/Flow-Launcher/Flow.Launcher> — broad Windows launcher; C#, 14,842 stars, updated 2026-05-31T16:46:25Z. Stronger in broad market proof, plugins, search, packaging, and community. Not the same narrow product.
- <https://github.com/Wox-launcher/Wox> — broad cross-platform launcher; Go, 26,979 stars, updated 2026-05-31T15:02:11Z. Stronger in broad market proof, cross-platform packaging, plugin/theme stores, and AI/MCP direction. Not the same narrow product.

Conclusion from the GitHub snapshot: WT Launcher can honestly claim a north-star shape only for the narrow, local, portable Windows Terminal workspace-launcher slice. It cannot honestly claim broad launcher supremacy.

## Core judgement

WT Launcher is already a north-star implementation for a narrow problem:

```text
open known local workspaces quickly
  + keep launch entries portable and reviewable
  + save repeated subsets as named launch groups
  + preserve one explicit launch boundary
  + preview generated group commands without process start
  + keep UI preferences local and inspectable
  + prove the app through one native gate
```

It is not a general launcher/automation north star. That broader domain includes plugin marketplaces, workflow graphs, cloud sync, import/export ecosystems, package/update channels, and run history/status receipts. Those are intentionally outside current scope unless they beat the existing local gate with less complexity.

## Feature mapping audit

### 1. Launch entry object

- Real problem: repeated local workspaces need stable names, working directories, and shell commands instead of ad-hoc manual terminal setup.
- Current data owner: `config/config.json` entry rows with `id`, `name`, `directory`, `command`, `selected`, optional `updatedAtUtc`.
- Evidence: `README.md` documents the schema; `LaunchArgumentBuilder.Build` turns a row into the `wt.exe` launch plan; repository config tests build dry-run plans for active config rows.
- Verdict: north-star for this narrow Windows Terminal launcher.
- Remaining gap: no import/export format for external launch definitions; keep that out of scope until real user need appears.

### 2. Existing-directory contract

- Real problem: a stale launcher button should fail before click time, not after the user has trusted the UI.
- Current data owner: `config/config.json` `directory`; current local truth is the filesystem.
- Evidence: `tools/verify.ps1` rejects active config rows whose `directory` is not an existing container.
- Verdict: north-star. This is the right boundary: fail fast at the repo gate instead of adding UI guesswork.
- Remaining gap: command executability is intentionally not probed because commands are arbitrary trusted local shell text.

### 3. Search/filter

- Real problem: a launcher with many entries is only useful if the target can be found by name, path, or command.
- Current data owner: in-memory `FilterText` derived from portable `settings.json` when saved.
- Evidence: XAML placeholder says it searches name/directory/command; tests cover filtering across all three fields and persisted filter state.
- Verdict: north-star for current entry count and local use.
- Remaining gap: fuzzy ranking is unnecessary until exact substring search stops solving the real workload.

### 4. Direct single launch

- Real problem: one known workspace should open with one explicit action.
- Current data owner: `LaunchPlan` built from the selected entry and global confirmation setting.
- Evidence: launch workflow tests cover confirm/cancel paths, dry-run text, process handoff, honest `已请求启动` status wording, and transient single-launch canceled/request/failure receipts.
- Verdict: north-star for explicit local launching.
- Remaining gap: launch request success still cannot prove terminal-session health after `wt.exe` accepts the process start request.

### 5. Persistent checkbox selection and batch launch

- Real problem: users often launch a repeated slice of workspaces together.
- Current data owner: `selected` in `config/config.json`.
- Evidence: selection changes are saved, batch launch uses selected entries, tests cover selection outcomes and batch confirmation/cancellation, and config-save failures during select-all/clear/checkbox mutation now prove visible `配置保存失败` status plus memory/file rollback instead of a false saved action. `LauncherLaunchWorkflow.PreflightBatch` prepares every selected launch plan before confirmation or external process start. The batch confirmation receives the generated `LaunchPlan` items, and if an external start still fails after earlier starts, the workflow records the failure and stops the remaining starts instead of compounding side effects. Named presets can now launch directly from `config/presets.json` without rewriting checkbox state first, and confirmation-cancel/failure tests prove that direct preset launch receives preflighted generated command lines before any process start while preserving the preset name in cancellation and unexpected-failure status. Current-group preview uses the same preflight output and formatter, then writes only transient dry-run/status text instead of starting processes or persisting checkbox state. Batch launch writes a transient `启动回执` that lists requested, failed, and skipped plans, and UI status says `已请求启动` instead of pretending the terminal session completed healthy.
- Verdict: strong, and locally north-star as the bridge from the retired HTA one-click flow.
- Remaining gap: `selected` remains stateful UI memory for ad-hoc batches; direct preset launch is the cleaner path for named intent. Once an external process has started, the app cannot honestly guarantee zero side effects if a later external start fails; that is a property of launching real processes, not a missing fallback branch.

### 6. Named launch groups / presets

- Real problem: repeated subsets should be named, portable, inspectable objects instead of fragile transient checkbox state.
- Current data owner: `config/presets.json`, referencing stable entry ids.
- Evidence: preset schema is documented, validated by `tools/verify.ps1`, and covered by store/workflow/ViewModel tests, including missing `entryIds` rejection. The ViewModel load path propagates missing or failed `presets.json` instead of replacing it with an empty launch-group model. The selected preset can be launched directly, preserving current checkbox state instead of forcing a selection rewrite before launch, while still sharing the same preflight/confirmation path as selected-item batch launch. Applying, previewing, or launching a preset also discards uncommitted drag-sort preview state first, so a visual ghost order is not silently persisted by a launch-group action. Applying a preset has a failure contract too: config backup/write failures leave the preset selection and config file unchanged and report `配置保存失败` instead of claiming the group was applied.
- Verdict: north-star. This is the best current data-structure move in the project.
- Remaining gap: no external import/export yet; not needed for the current local workflow.

### 7. First-class preset actions

- Real problem: a named group must be created, updated, and deleted inside the app, otherwise users fall back to hand-editing JSON.
- Current data owner: `config/presets.json`.
- Evidence: ViewModel commands save current selection as a new group, update current group, delete editable groups, preserve locked groups, report dialog-boundary failures without mutating portable files, roll back memory/file state on preset save failures, and report the new current launch group after deletion instead of hiding fallback selection.
- Verdict: north-star inside this scope.
- Remaining gap: no conflict UI for simultaneous external JSON edits; current repo-local app does not need multi-writer machinery.

### 8. Preset-vs-selection preview

- Real problem: applying a group blindly is dangerous because it rewrites saved selection.
- Current data owner: preview is derived from current selection and selected preset; it is not a second source of truth.
- Evidence: tests cover apply/preview/no-op states and disabled no-op apply.
- Verdict: north-star. It solves the real "what will change?" problem without a second persistent model.
- Remaining gap: visual screenshot receipts are useful only after UI-heavy changes.

### 9. Preset launch plan preview

- Real problem: launching a saved group is the highest-risk routine action; users need to inspect the exact generated `wt.exe` commands without rewriting selection or starting external processes.
- Current data owner: `config/presets.json` owns desired group membership; `LaunchPlan` owns generated command lines; `DryRunText` is transient UI output, not persisted state.
- Evidence: `PreviewSelectedPresetCommand` calls the same `LauncherLaunchWorkflow.PreflightBatch` path as launch, formats plans through `LauncherBatchLaunchPlanText`, starts no process, and preserves checkbox state; tests cover successful preview and mixed valid/invalid plan-failure preview, including valid generated command lines marked as not started plus the failure list.
- Verdict: north-star for the repo-owned side-effect boundary. It converts the impossible "0% side effects after launch" demand into a true pre-launch no-side-effect inspection path.
- Remaining gap: once the user chooses real launch, arbitrary external commands still have side effects outside this app's control.

### 10. Add/edit/delete launcher buttons

- Real problem: the launcher must stay maintainable without manual JSON edits for common entry changes.
- Current data owner: `config/config.json`, with preset/settings references updated or rolled back together.
- Evidence: entry mutation tests cover add/edit/delete, dialog-boundary failure without file mutation, failed save rollback, preset/settings coordination, and status text.
- Verdict: strong.
- Remaining gap: no bulk import; that would be overbuilding until a real import workload exists.

### 11. Custom order and drag reorder

- Real problem: users build spatial memory; alphabetical order is not always the fastest launch order.
- Current data owner: `settings.json` `customEntryOrder`; drag session is a single transient object.
- Evidence: drag reorder session/workflow tests cover preview, commit, cancel, rollback, and single-session ownership; context-menu `上移排序` / `下移排序` commands persist the same `customEntryOrder` without creating a second reorder model, and when invoked during a drag preview they discard the preview before resolving targets so ghost order cannot become a hidden source of truth.
- Verdict: north-star for mouse-first local use.
- Remaining gap: none proven. Non-drag reorder now uses the same data owner and persistence path as drag reorder.

### 12. Sort modes

- Real problem: users need different scan modes: custom memory order, newest updated, or alphabetic lookup.
- Current data owner: `settings.json` `sortMode`.
- Evidence: tests cover custom/updated/alphabetic sort behavior and reject unknown mode ids.
- Verdict: north-star; three modes are enough and not a framework.
- Remaining gap: none proven.

### 13. View modes: Cards / Details / Names

- Real problem: one layout cannot solve readable actions, dense field scanning, and overview at the same time.
- Current data owner: `settings.json` `entryViewMode`.
- Evidence: code exposes `cards`, `details`, and `names`; XAML has three roots; tests cover mode persistence and root selection.
- Verdict: north-star data model; README needed to name the already-implemented `Names` mode explicitly.
- Remaining gap: screenshot-backed comfort pass only after visual changes.

### 14. Adaptive card sizing and density

- Real problem: fixed cards waste space on large windows and become unusable on small windows.
- Current data owner: `LauncherEntryLayoutDensity`, derived from available size and visible entry count.
- Evidence: layout tests cover Normal/Compact/Micro density and filtered visible-entry sizing.
- Verdict: strong, close to north-star.
- Remaining gap: exact visual comfort is partly perceptual; tests prove structure, not taste.

### 15. Global launch confirmation

- Real problem: launch safety should have one visible rule; per-command "danger" guesses create false confidence and hidden branches.
- Current data owner: `settings.json` `requireConfirmationForLaunches`.
- Evidence: launch confirmation tests cover saved setting, confirmation/cancel, and `LaunchPlan.RequiresConfirmation`; missing, unknown, or wrong-case JSON fields now fail fast, so obsolete per-entry `confirm` cannot silently compete with the global setting.
- Verdict: north-star. One switch is simpler and safer than command-classification theater.
- Remaining gap: none proven.

### 16. Portable settings

- Real problem: UI preferences should move with the repo and stay reviewable, not hide in AppData or registry.
- Current data owner: `config/settings.json`.
- Evidence: settings schema is documented and tests cover sort/view/confirmation/filter/order/window fields; null settings fields and blank mode ids now fail fast instead of being rewritten to default UI state; unused `theme` state was removed instead of kept as a fake preference.
- Verdict: north-star for this local tool.
- Remaining gap: no cross-machine path normalization; this is fine because launch directories are machine-local facts.

### 17. JSON persistence, backup, and rollback compensation

- Real problem: config edits must not leave half-written JSON or half-applied multi-file state.
- Current data owner: config/preset/settings stores plus mutation snapshots.
- Evidence: store tests cover temp writes, `.bak` backups, skip-backup rollback writes, multi-file rollback failure statuses, and fail-fast rejection of missing files plus missing, null, blank mode, unknown, or wrong-case JSON fields. ViewModel refresh now shares that fail-fast preset-load contract instead of synthesizing a second empty preset truth.
- Verdict: north-star for a single-process local app.
- Remaining gap: no file locking or merge resolution for concurrent external edits; that is not a current requirement.

### 18. Core/App/Tests separation

- Real problem: launch argument rules and config behavior must be testable without opening the GUI.
- Current data owner: Core owns pure records/rules; App owns ViewModels/Views; Tests own behavior contracts.
- Evidence: solution layout and root verify enforce Core/App/Tests presence, and tests cover Core helpers independently.
- Verdict: north-star for maintainability.
- Remaining gap: none proven.

### 19. Self-contained win-x64 publish and native gate

- Real problem: a desktop launcher is only real if it can be restored, tested, published, and smoke-started.
- Current data owner: `tools/verify.ps1`, `src/avalonia-launcher/verify.ps1`, and ignored publish artifacts.
- Evidence: root verify checks retired paths, config/presets/settings, docs, tests, publish, and hidden startup smoke.
- Verdict: north-star for repo-local Windows release proof.
- Remaining gap: no installer/update channel; that belongs to distribution scope, not current launcher correctness.

### 20. Retired legacy surfaces

- Real problem: two active UI lines create duplicate truth and slow failure.
- Current data owner: Avalonia mainline; retired HTA/WinForms are archive/reference only.
- Evidence: root verify rejects old WinForms/spike/root executable return and keeps retirement docs readable.
- Verdict: north-star. The old line is retired instead of kept alive by compatibility branches.
- Remaining gap: none proven.

### 21. Design benchmark and giants-shoulders library

- Real problem: UI/product decisions should not be made from vibes or copied frameworks.
- Current data owner: `DESIGN.md`, `docs/design-benchmark-digest.md`, and `docs/giants-shoulders/`.
- Evidence: docs translate external references into local acceptance rules and tests keep the digest referenced.
- Verdict: strong, not automatically north-star.
- Remaining gap: source freshness must be refreshed when a decision depends on a changed external standard.

## Gaps that block a broad-domain north-star claim

These are not defects in the current narrow product. They are the reasons not to claim "best launcher/automation tool in the whole domain":

1. no first-class import/export ecosystem for entries or groups;
2. no persisted run history or terminal-session health model separated from desired config; transient launch receipts exist for requested/failed/skipped plans;
3. no installer/update/signing channel;
4. no screenshot-backed comfort receipt for visual taste after future visual-heavy layout changes.
5. no honest way to prove "stronger than every GitHub similar project" or "0% side effects" for real process launch behavior.

Do not build these preemptively. They become work only when the real user problem appears.

## Objective completion audit

This section checks the user's full objective against current evidence instead of redefining success around the work already done.

- Requirement: evaluate every active feature.
  - Evidence: the 21 feature sections above cover every active feature mapping documented in `README.md` and the current Avalonia mainline scope.
  - Status: proven for the current repository scope.
- Requirement: decide whether each feature is battle-practice or market-proven north-star.
  - Evidence: every feature section has a real problem, data owner, evidence, verdict, and remaining gap; the GitHub snapshot separates narrow direct competitors from broad launchers.
  - Status: proven as an audit, not as a universal supremacy claim.
- Requirement: current ability is already unimprovable.
  - Evidence: the broad-domain gap list still names import/export, persisted terminal-session health/history, distribution/update, visual receipts, and external side-effect limits.
  - Status: contradicted. There are possible improvements, but most are intentionally not current work until real demand appears.
- Requirement: stronger than any similar GitHub project.
  - Evidence: Flow Launcher and Wox have far stronger broad-market proof; Terminal Manager has stronger WT profile/split-pane/import-export scope; WT Launcher is stronger only in the narrower portable config + named group + native gate slice.
  - Status: false for broad launcher scope; supported only for the narrow local Windows Terminal workspace-launcher slice.
- Requirement: remain small and beautiful.
  - Evidence: this audit rejects speculative plugin marketplace, local web server, split-pane editor, installer/update channel, and workflow graph features until they beat the current local gate.
  - Status: currently preserved.
- Requirement: 100% confidence and 0% side effects.
- Evidence: current tests and gate prove batch preflight-before-confirmation, side-effect-free current-group preview with valid plans visible even when another plan fails preflight, exact generated command confirmation, transient single-launch canceled/request/failure receipts plus batch receipts for canceled/requested/failed/skipped plans, abort-after-first-start-failure, rollback-aware JSON persistence, and native publish smoke. They cannot prove that starting arbitrary external processes has zero side effects.
  - Status: impossible as an absolute claim; mitigated where the repository owns the boundary.

## Final answer

The current project is already a north-star implementation for the narrow domain of a portable Windows Terminal workspace launcher. The strongest mappings are launch entries, named presets, side-effect-free current-group preview, honest requested-start wording, transient single and batch launch receipts, global launch confirmation, portable settings, JSON persistence/rollback, batch preflight-before-confirmation, exact generated command confirmation, abort-after-first-start-failure, and the native verify/publish gate.

It is not a broad-domain north star for every launcher or automation product. GitHub market evidence contradicts that broad claim. Building import/export, status history, distribution/update, split panes, or a plugin system now would be speculative unless a real workflow beats the current small local contract.
