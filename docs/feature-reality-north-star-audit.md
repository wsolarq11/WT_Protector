# Feature reality and north-star audit

Reviewed on: 2026-05-31

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

The external yardstick remains the source-tracked benchmark set in `docs/design-benchmark-digest.md` and `docs/giants-shoulders/launcher-first-class-patterns.md`: Windows Terminal profiles/command-line launch shape, VS Code workspaces/settings, Raycast quick links, Alfred workflows, Kubernetes spec/status separation, and WCAG/major design-system accessibility rules.

## Fresh external yardstick spot-check

Current external references were spot-checked on 2026-05-31 before making the north-star judgment:

- Microsoft Windows Terminal still treats `wt.exe` as an explicit command line with `-d` / `--startingDirectory` for the starting folder, which matches this repo's `directory` + `command` entry contract.
- VS Code still separates user/workspace settings and multi-root workspace files, which supports portable, human-readable context objects instead of hidden local state.
- Raycast Quicklinks still solve the same frequent-action problem by opening URLs, file paths, and deeplinks instantly; current v2 Quicklinks also include tags, pinning, and dynamic placeholders, which is exactly why this repo must not claim broad launcher/platform north-star status.
- Docker Compose profiles remain a named-subset model for activating only the needed services, which maps cleanly to WT Launcher's named presets.
- WCAG 2.2 includes Dragging Movements as a Level AA criterion; this makes non-drag reorder a real accessibility contract, not cosmetic polish.
- Kubernetes continues to separate desired object data from observed status; WT Launcher has desired config/preset/settings objects but no run-history/status object yet.

## Core judgement

WT Launcher is already a north-star implementation for a narrow problem:

```text
open known local workspaces quickly
  + keep launch entries portable and reviewable
  + save repeated subsets as named launch groups
  + preserve one explicit launch boundary
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
- Evidence: launch workflow tests cover confirm/cancel paths, dry-run text, and process handoff.
- Verdict: north-star for explicit local launching.
- Remaining gap: launch success only means `wt.exe` process start was requested; no terminal-session health/status receipt exists.

### 5. Persistent checkbox selection and batch launch

- Real problem: users often launch a repeated slice of workspaces together.
- Current data owner: `selected` in `config/config.json`.
- Evidence: selection changes are saved, batch launch uses selected entries, and tests cover selection outcomes and batch confirmation/cancellation.
- Verdict: strong, and locally north-star as the bridge from the retired HTA one-click flow.
- Remaining gap: `selected` is stateful UI memory; presets are the cleaner long-term object for named intent.

### 6. Named launch groups / presets

- Real problem: repeated subsets should be named, portable, inspectable objects instead of fragile transient checkbox state.
- Current data owner: `config/presets.json`, referencing stable entry ids.
- Evidence: preset schema is documented, validated by `tools/verify.ps1`, and covered by store/workflow/ViewModel tests.
- Verdict: north-star. This is the best current data-structure move in the project.
- Remaining gap: no external import/export yet; not needed for the current local workflow.

### 7. First-class preset actions

- Real problem: a named group must be created, updated, and deleted inside the app, otherwise users fall back to hand-editing JSON.
- Current data owner: `config/presets.json`.
- Evidence: ViewModel commands save current selection as a new group, update current group, delete editable groups, and preserve locked groups.
- Verdict: north-star inside this scope.
- Remaining gap: no conflict UI for simultaneous external JSON edits; current repo-local app does not need multi-writer machinery.

### 8. Preset-vs-selection preview

- Real problem: applying a group blindly is dangerous because it rewrites saved selection.
- Current data owner: preview is derived from current selection and selected preset; it is not a second source of truth.
- Evidence: tests cover apply/preview/no-op states and disabled no-op apply.
- Verdict: north-star. It solves the real "what will change?" problem without a second persistent model.
- Remaining gap: visual screenshot receipts are useful only after UI-heavy changes.

### 9. Add/edit/delete launcher buttons

- Real problem: the launcher must stay maintainable without manual JSON edits for common entry changes.
- Current data owner: `config/config.json`, with preset/settings references updated or rolled back together.
- Evidence: entry mutation tests cover add/edit/delete, failed save rollback, preset/settings coordination, and status text.
- Verdict: strong.
- Remaining gap: no bulk import; that would be overbuilding until a real import workload exists.

### 10. Custom order and drag reorder

- Real problem: users build spatial memory; alphabetical order is not always the fastest launch order.
- Current data owner: `settings.json` `customEntryOrder`; drag session is a single transient object.
- Evidence: drag reorder session/workflow tests cover preview, commit, cancel, rollback, and single-session ownership; context-menu `上移排序` / `下移排序` commands persist the same `customEntryOrder` without creating a second reorder model.
- Verdict: north-star for mouse-first local use.
- Remaining gap: none proven. Non-drag reorder now uses the same data owner and persistence path as drag reorder.

### 11. Sort modes

- Real problem: users need different scan modes: custom memory order, newest updated, or alphabetic lookup.
- Current data owner: `settings.json` `sortMode`.
- Evidence: tests cover custom/updated/alphabetic sort behavior and reject unknown mode ids.
- Verdict: north-star; three modes are enough and not a framework.
- Remaining gap: none proven.

### 12. View modes: Cards / Details / Names

- Real problem: one layout cannot solve readable actions, dense field scanning, and overview at the same time.
- Current data owner: `settings.json` `entryViewMode`.
- Evidence: code exposes `cards`, `details`, and `names`; XAML has three roots; tests cover mode persistence and root selection.
- Verdict: north-star data model; README needed to name the already-implemented `Names` mode explicitly.
- Remaining gap: screenshot-backed comfort pass only after visual changes.

### 13. Adaptive card sizing and density

- Real problem: fixed cards waste space on large windows and become unusable on small windows.
- Current data owner: `LauncherEntryLayoutDensity`, derived from available size and visible entry count.
- Evidence: layout tests cover Normal/Compact/Micro density and filtered visible-entry sizing.
- Verdict: strong, close to north-star.
- Remaining gap: exact visual comfort is partly perceptual; tests prove structure, not taste.

### 14. Global launch confirmation

- Real problem: launch safety should have one visible rule; per-command "danger" guesses create false confidence and hidden branches.
- Current data owner: `settings.json` `requireConfirmationForLaunches`.
- Evidence: launch confirmation tests cover saved setting, confirmation/cancel, and `LaunchPlan.RequiresConfirmation`.
- Verdict: north-star. One switch is simpler and safer than command-classification theater.
- Remaining gap: none proven.

### 15. Portable settings

- Real problem: UI preferences should move with the repo and stay reviewable, not hide in AppData or registry.
- Current data owner: `config/settings.json`.
- Evidence: settings schema is documented and tests cover sort/view/confirmation/filter/order/window fields.
- Verdict: north-star for this local tool.
- Remaining gap: no cross-machine path normalization; this is fine because launch directories are machine-local facts.

### 16. JSON persistence, backup, and rollback compensation

- Real problem: config edits must not leave half-written JSON or half-applied multi-file state.
- Current data owner: config/preset/settings stores plus mutation snapshots.
- Evidence: store tests cover temp writes, `.bak` backups, skip-backup rollback writes, and multi-file rollback failure statuses.
- Verdict: north-star for a single-process local app.
- Remaining gap: no file locking or merge resolution for concurrent external edits; that is not a current requirement.

### 17. Core/App/Tests separation

- Real problem: launch argument rules and config behavior must be testable without opening the GUI.
- Current data owner: Core owns pure records/rules; App owns ViewModels/Views; Tests own behavior contracts.
- Evidence: solution layout and root verify enforce Core/App/Tests presence, and tests cover Core helpers independently.
- Verdict: north-star for maintainability.
- Remaining gap: none proven.

### 18. Self-contained win-x64 publish and native gate

- Real problem: a desktop launcher is only real if it can be restored, tested, published, and smoke-started.
- Current data owner: `tools/verify.ps1`, `src/avalonia-launcher/verify.ps1`, and ignored publish artifacts.
- Evidence: root verify checks retired paths, config/presets/settings, docs, tests, publish, and hidden startup smoke.
- Verdict: north-star for repo-local Windows release proof.
- Remaining gap: no installer/update channel; that belongs to distribution scope, not current launcher correctness.

### 19. Retired legacy surfaces

- Real problem: two active UI lines create duplicate truth and slow failure.
- Current data owner: Avalonia mainline; retired HTA/WinForms are archive/reference only.
- Evidence: root verify rejects old WinForms/spike/root executable return and keeps retirement docs readable.
- Verdict: north-star. The old line is retired instead of kept alive by compatibility branches.
- Remaining gap: none proven.

### 20. Design benchmark and giants-shoulders library

- Real problem: UI/product decisions should not be made from vibes or copied frameworks.
- Current data owner: `DESIGN.md`, `docs/design-benchmark-digest.md`, and `docs/giants-shoulders/`.
- Evidence: docs translate external references into local acceptance rules and tests keep the digest referenced.
- Verdict: strong, not automatically north-star.
- Remaining gap: source freshness must be refreshed when a decision depends on a changed external standard.

## Gaps that block a broad-domain north-star claim

These are not defects in the current narrow product. They are the reasons not to claim "best launcher/automation tool in the whole domain":

1. no first-class import/export ecosystem for entries or groups;
2. no run history/status receipt model separated from desired config;
3. no installer/update/signing channel;
4. no screenshot-backed comfort receipt for visual taste after future visual-heavy layout changes.

Do not build these preemptively. They become work only when the real user problem appears.

## Final answer

The current project is already a north-star implementation for the narrow domain of a portable Windows Terminal workspace launcher. The strongest mappings are launch entries, named presets, global launch confirmation, portable settings, JSON persistence/rollback, and the native verify/publish gate.

It is not a broad-domain north star for every launcher or automation product. That would require import/export, status history, distribution/update, and broader accessibility receipts. Building those now would be speculative.
