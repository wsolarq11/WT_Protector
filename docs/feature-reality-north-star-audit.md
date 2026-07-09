# Feature reality and north-star audit

Reviewed on: 2026-06-24

Purpose: evaluate whether each active WT Launcher feature mapping solves a real user problem and whether the implementation is already a domain north star. This is an audit document, not marketing copy.

## Scope and standard

This audit covers the active Avalonia mainline and repository-level launcher contract:

- `src/avalonia-launcher/`
- `config/launcher.json` (unified store: entries + presets + settings + defaultPresetId)
- `README.md`
- `DESIGN.md`
- `docs/design-benchmark-digest.md`
- `docs/reference/github-similar-projects-snapshot.json`
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

Proof taxonomy used below:

- `repo-battle-tested`: current source, tests, or native gate prove the behavior in this repository. This is local implementation proof, not market supremacy.
- `market-informed`: external products or standards prove the pattern is real. This does not prove WT Launcher is stronger than those products.
- `legacy-flow-informed`: retired HTA/WinForms usage proves the local workflow existed before the Avalonia mainline. This is not a broad-market claim.
- `accessibility-informed`: external accessibility standards shape the feature contract. Source/tests must still prove the implementation.
- `source-backed`: source-tracked docs or benchmark material support the decision, but the item is weaker than a runtime feature proof.

Verdict levels used below:

- `narrow-north-star`: best current shape for the narrow portable Windows Terminal launcher scope.
- `strong`: real and useful, but not enough evidence or polish to call the feature a north-star even in the narrow scope.
- `source-backed`: useful source-backed decision support, not a runtime feature verdict.

Gap classes used below:

- `none-proven`: no current narrow-scope gap has evidence.
- `future-workload`: possible future capability, but no current workload justifies it yet.
- `external-process-limit`: after external process handoff, the app cannot prove terminal-session health or arbitrary command side effects.
- `visual-proof`: visual or perceptual comfort needs screenshot/human evidence after visual-heavy changes.
- `distribution-scope`: installer, update, signing, or package-channel work belongs to distribution scope, not current launcher correctness.
- `source-freshness`: external references must be refreshed before they are used as current evidence.

## Fresh external yardstick spot-check

Current external references were spot-checked on 2026-06-01 before making the north-star judgment:

External source class tokens used below:

- `command-line-contract`: external tool exposes an explicit command boundary.
- `portable-context-contract`: external tool proves portable context files are a real pattern.
- `frequent-action-market-proof`: external product proves repeated instant launch is a real market workload.
- `named-subset-contract`: external spec proves named subsets are a stable data shape.
- `accessibility-contract`: external standard turns a UI path into a real accessibility requirement.
- `spec-status-contract`: external system separates desired state from observed status.

Local implication tokens used below:

- `supports-current-entry-contract`: supports this repo's `directory` + `command` launch-entry shape.
- `supports-portable-json-contract`: supports human-readable config/preset/settings files instead of hidden state.
- `blocks-broad-launcher-claim`: proves the broader launcher/platform market has stronger axes than this narrow app.
- `supports-named-presets`: supports named launch groups as the right small data structure.
- `supports-non-drag-reorder-contract`: supports non-drag reorder as a real accessibility boundary.
- `identifies-run-status-gap`: exposes the missing status/history object that blocks absolute side-effect claims.

- External reference id: windows-terminal-command-line
  - Source class: command-line-contract
  - Local implication: supports-current-entry-contract
  - Claim boundary: supports explicit launch-plan generation; does not prove terminal-session health after `wt.exe` accepts the request.
  - Evidence: Microsoft Windows Terminal still treats `wt.exe` as an explicit command line with `-d` / `--startingDirectory` for the starting folder, which matches this repo's `directory` + `command` entry contract.
- External reference id: vscode-workspace-context
  - Source class: portable-context-contract
  - Local implication: supports-portable-json-contract
  - Claim boundary: supports source-visible context objects; does not prove broad editor/workspace-platform parity.
  - Evidence: VS Code still separates user/workspace settings and multi-root workspace files, which supports portable, human-readable context objects instead of hidden local state.
- External reference id: raycast-quicklinks
  - Source class: frequent-action-market-proof
  - Local implication: blocks-broad-launcher-claim
  - Claim boundary: proves frequent-action launch is a real market workload and also proves WT Launcher must not claim broad launcher/platform north-star status.
  - Evidence: Raycast Quicklinks still solve the same frequent-action problem by opening URLs, file paths, and deeplinks instantly; current v2 Quicklinks also include tags, pinning, and dynamic placeholders.
- External reference id: docker-compose-profiles
  - Source class: named-subset-contract
  - Local implication: supports-named-presets
  - Claim boundary: supports named subsets; does not justify importing Docker orchestration concepts into this app.
  - Evidence: Docker Compose profiles remain a named-subset model for activating only the needed services, which maps cleanly to WT Launcher's named presets.
- External reference id: wcag-dragging-movements
  - Source class: accessibility-contract
  - Local implication: supports-non-drag-reorder-contract
  - Claim boundary: supports non-drag reorder as a required path; does not prove all visual comfort or accessibility concerns are solved.
  - Evidence: WCAG 2.2 includes Dragging Movements as a Level AA criterion; this makes non-drag reorder a real accessibility contract, not cosmetic polish.
- External reference id: kubernetes-spec-status
  - Source class: spec-status-contract
  - Local implication: identifies-run-status-gap
  - Claim boundary: supports current desired-object modeling and exposes the missing observed run-history/status object.
  - Evidence: Kubernetes continues to separate desired object data from observed status; WT Launcher has desired config/preset/settings objects but no run-history/status object yet.

## Current GitHub similar-project snapshot

Live GitHub repository search/API checks were refreshed using current queries for `"Windows Terminal" workspace launcher`, `"Windows Terminal" session manager`, and `"Claude Code" launcher`. The current structured evidence is source-tracked in `docs/reference/github-similar-projects-snapshot.json`, including summary counts and controlled `yardstickVerdict` values, and refreshed by `tools/update-github-similar-projects-snapshot.ps1`, captured with `gh repo view <owner>/<repo> --json nameWithOwner,url,description,stargazerCount,forkCount,pushedAt,updatedAt,primaryLanguage,isArchived`. GitHub evidence capturedAtUtc: `2026-06-09T15:47:55Z`. The result is blunt: a universal "stronger than any similar GitHub project" claim is not provable, and in the broad launcher market it is false. Flow Launcher and Wox are mature broad launchers with thousands of stars, plugin ecosystems, package channels, and community proof. WT Launcher should not fight them as a generic launcher. That would be stupid scope creep.

The relevant fight is narrower: portable Windows Terminal workspace launching with inspectable entries, named launch groups, side-effect-free plan preview, explicit launch confirmation, rollback-aware JSON persistence, and a native repo gate.

Representative GitHub yardsticks:

GitHub yardstick verdict tokens used below:

- `direct-narrow-weaker`: direct or near-direct project, weaker on WT Launcher's audited portable-config/named-group/preview/rollback/native-gate axis.
- `adjacent-axis-stronger`: stronger on at least one adjacent axis, but not proof of broad universal superiority.
- `broad-market-stronger`: stronger broad-market proof, explicitly outside WT Launcher's narrow domain.

- GitHub yardstick id: sydrox-dev-layout
  - Repository: SyDroX/dev-layout
  - Yardstick verdict: direct-narrow-weaker
  - Classification: direct WT workspace launcher
  - Stronger axes: fixed Claude Code multi-repo layout; session resume; cd blocking
  - WT Launcher axes: editable portable UI entries; named launch groups; rollback-aware JSON persistence; native gate
  - Claim boundary: supports WT Launcher's audited narrow axis only; not a universal repository ranking.
- GitHub yardstick id: augustdragon-terminal-manager
  - Repository: augustdragon/terminal-manager
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: direct WT profile/session manager
  - Stronger axes: WT profile editing; split-pane groups; import/export; WT settings sync
  - WT Launcher axes: smaller local JSON-to-wt contract; no local web server; does not mutate Windows Terminal settings
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: noambrand-launchpad-cli
  - Repository: noambrand/Launchpad-CLI
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: Claude Code launcher/installer
  - Stronger axes: installer; folder picker; right-click launch; statusline distribution
  - WT Launcher axes: portable editable WT workspace groups; rollback-aware config gate; smaller app scope
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: kevinkicho-win-screen-setup
  - Repository: kevinkicho/win-screen-setup
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: Windows multi-monitor workspace launcher
  - Stronger axes: visual screen/app positioning
  - WT Launcher axes: narrower side-effect surface; portable launch groups; native repo gate
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: ewdieckman-devworkspace
  - Repository: ewdieckman/DevWorkspace
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: PowerShell WT multi-pane workspace module
  - Stronger axes: fixed split-pane layouts
  - WT Launcher axes: editable GUI entries; rollback-aware persistence; native publish/verify gate
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: jsoulet42-claude-launcher-app
  - Repository: jsoulet42/claude-launcher-app
  - Yardstick verdict: direct-narrow-weaker
  - Classification: Claude Code WT TUI workspace launcher
  - Stronger axes: adjacent TUI flow
  - WT Launcher axes: portable editable GUI entries; named groups; rollback-aware JSON; native gate
  - Claim boundary: supports WT Launcher's audited narrow axis only; not a universal repository ranking.
- GitHub yardstick id: a-ema755-claude-dashboard
  - Repository: A-ema755/claude-dashboard
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: Claude Code CLI session launcher/preset manager
  - Stronger axes: split-pane layouts; cost tracking
  - WT Launcher axes: small local WT launcher contract; portable GUI config; native gate
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: adityacodepublic-wtsm-cli
  - Repository: adityacodepublic/wtsm-cli
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: Windows Terminal session manager CLI
  - Stronger axes: named terminal sessions CLI
  - WT Launcher axes: GUI editing; launch-group preview; repo-native rollback/verify evidence
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: pramin-agentspace
  - Repository: pramin/AgentSpace
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: Avalonia agent workspace launcher
  - Stronger axes: embedded PTY; SSH file tree; code preview
  - WT Launcher axes: smaller WT handoff scope; portable JSON groups; native gate
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: joyahmed-devgo-app
  - Repository: joyahmed/devgo-app
  - Yardstick verdict: adjacent-axis-stronger
  - Classification: WSL-first developer launcher
  - Stronger axes: WSL; VS Code; tmux workflows; installer ambition
  - WT Launcher axes: verified portable WT launch groups; rollback-aware JSON; narrower scope
  - Claim boundary: blocks universal superiority because the repository is stronger on at least one adjacent axis.
- GitHub yardstick id: flow-launcher-flow-launcher
  - Repository: Flow-Launcher/Flow.Launcher
  - Yardstick verdict: broad-market-stronger
  - Classification: broad Windows launcher
  - Stronger axes: market proof; plugins; search; packaging; community
  - WT Launcher axes: narrow portable WT workspace-launcher slice only
  - Claim boundary: contradicts broad launcher supremacy and stays outside WT Launcher's narrow product domain.
- GitHub yardstick id: wox-launcher-wox
  - Repository: Wox-launcher/Wox
  - Yardstick verdict: broad-market-stronger
  - Classification: broad cross-platform launcher
  - Stronger axes: market proof; cross-platform packaging; plugin/theme stores; AI/MCP direction
  - WT Launcher axes: narrow portable WT workspace-launcher slice only
  - Claim boundary: contradicts broad launcher supremacy and stays outside WT Launcher's narrow product domain.

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

## Feature verdict summary

The counts below are a summary ledger for the 21 feature sections. They are not a substitute for the per-feature evidence.

- Feature count: 21
- Verdict count: narrow-north-star=21
- Gap count: none-proven=5,future-workload=8,external-process-limit=4,visual-proof=3,distribution-scope=1,source-freshness=1
- Proof count: repo-battle-tested=20,market-informed=4,legacy-flow-informed=1,accessibility-informed=1,source-backed=1
- Completion boundary: all 21 feature verdicts support the narrow portable Windows Terminal workspace-launcher claim only; broad, unimprovable, universal-superiority, and absolute zero-side-effect claims remain blocked by the objective and gap ledgers.
- Absolute boundary: future-workload, external-process-limit, visual-proof, distribution-scope, and source-freshness gaps block "100% confidence / 0% side effects" as a product-wide claim.

## Feature mapping audit

### 1. Launch entry object

- Feature id: launch-entry-object
- Real problem: repeated local workspaces need stable names, working directories, and shell commands instead of ad-hoc manual terminal setup.
- Proof type: repo-battle-tested, market-informed
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: `config/launcher.json` entry rows with `id`, `name`, `directory`, `command`, `selected`, optional `updatedAtUtc`.
- Evidence: `README.md` documents the schema; `LaunchArgumentBuilder.Build` turns a row into the `wt.exe` launch plan; repository config tests build dry-run plans for active config rows.
- Verdict: north-star for this narrow Windows Terminal launcher.
- Remaining gap: no import/export format for external launch definitions; keep that out of scope until real user need appears.

### 2. Existing-directory contract

- Feature id: existing-directory-contract
- Real problem: a stale launcher button should fail before click time, not after the user has trusted the UI.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: external-process-limit
- Current data owner: `config/launcher.json` `directory`; current local truth is the filesystem.
- Evidence: `tools/verify.ps1` rejects active config rows whose `directory` is not an existing container.
- Verdict: north-star. This is the right boundary: fail fast at the repo gate instead of adding UI guesswork.
- Remaining gap: command executability is intentionally not probed because commands are arbitrary trusted local shell text.

### 3. Search/filter

- Feature id: search-filter
- Real problem: a launcher with many entries is only useful if the target can be found by name, path, or command.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: in-memory `FilterText` derived from portable `launcher.json` settings when saved.
- Evidence: XAML placeholder says it searches name/directory/command; tests cover filtering across all three fields, persisted filter state, runtime filter mutations and core filter projection rejecting null instead of inventing an empty filter, projection rejection of null entry text/timestamps or blank ids, and filter/sort projection returning a read-only collection snapshot instead of a mutable projection list.
- Verdict: north-star for current entry count and local use.
- Remaining gap: fuzzy ranking is unnecessary until exact substring search stops solving the real workload.

### 4. Direct single launch

- Feature id: direct-single-launch
- Real problem: one known workspace should open with one explicit action.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: external-process-limit
- Current data owner: `LaunchPlan` built from the selected entry and global confirmation setting.
- Evidence: launch workflow tests cover confirm/cancel paths, dry-run text, process handoff, honest `已请求启动` status wording, transient single-launch canceled/request/failure receipts, receipt rejection of blank entry names instead of rendering fake `(unnamed)` launches, single launch snapshotting entry data before confirmation so receipt/status cannot drift if the live entry changes during confirmation, single launch confirmation target carries entry id/name plus the generated plan instead of exposing the live mutable entry to the dialog, launch plans themselves reject blank executable/argument/working-directory/command fields before process handoff, unexpected single-launch exceptions replacing stale dry-run text with a failure receipt instead of leaving old success-looking output behind, and unexpected dry-run exceptions replacing stale preview text with a preview failure receipt.
- Verdict: north-star for explicit local launching.
- Remaining gap: launch request success still cannot prove terminal-session health after `wt.exe` accepts the process start request.

### 5. Persistent checkbox selection and batch launch

- Feature id: persistent-checkbox-selection-and-batch-launch
- Real problem: users often launch a repeated slice of workspaces together.
- Proof type: repo-battle-tested, legacy-flow-informed
- Verdict level: narrow-north-star
- Gap class: future-workload, external-process-limit
- Current data owner: `selected` in `config/launcher.json`, representing the current ad-hoc launch group; named repeatable intent belongs to `config/launcher.json`.
- Evidence: selection changes are saved, batch launch uses selected entries, tests cover selection outcomes and batch confirmation/cancellation, and config-save failures during select-all/clear/checkbox mutation now prove visible `配置保存失败` status plus memory/file rollback instead of a false saved action. `LauncherLaunchWorkflow.PreflightBatch` prepares every selected launch plan before confirmation or external process start. Each preflight item snapshots the entry id and name, so confirmation text and batch receipts cannot drift if the mutable entry object changes after preflight. Batch preflight exposes its prepared item list as read-only state instead of leaking the mutable staging list. Batch launch reports are immutable snapshots, and starting a preflight returns a separate launch report instead of mutating the preflight report. Batch confirmation snapshots its item list as read-only state plus a required target label, so selected-entry confirmation says `已勾选项` and direct preset launch says the selected launch group instead of pretending every batch is checkbox state. The batch confirmation receives the generated `LaunchPlan` items, and if an external start still fails after earlier starts, the workflow records the failure and stops the remaining starts instead of compounding side effects. Named presets can now launch directly from `config/launcher.json` without rewriting checkbox state first, and confirmation-cancel/failure tests prove that direct preset launch receives preflighted generated command lines before any process start while preserving the preset name in cancellation and unexpected-failure status. Current-group preview uses the same preflight output and formatter, then writes only transient dry-run/status text instead of starting processes or persisting checkbox state. Batch launch writes a transient `启动回执` that lists requested, failed, and skipped plans, exposes batch failure messages as read-only report state instead of handing formatters the mutable failure list, rejects blank failure entry names instead of rendering fake `(unnamed)` rows, reports blank-name batch preflight/start failures without inventing fallback names, and unexpected batch exceptions now replace stale dry-run text with a failure receipt instead of leaving old success-looking output behind; UI status says `已请求启动` instead of pretending the terminal session completed healthy.
- Verdict: north-star for current ad-hoc group launch. It is not a compatibility branch: `selected` is the one portable current-group owner, presets own named groups, and both share the same preflight/confirmation/receipt boundary.
- Remaining gap: named repeatable intent belongs in presets, not in another checkbox model. Once an external process has started, the app cannot honestly guarantee zero side effects if a later external start fails; that is a property of launching real processes, not a missing fallback branch.

### 6. Named launch groups / presets

- Feature id: named-launch-groups-presets
- Real problem: repeated subsets should be named, portable, inspectable objects instead of fragile transient checkbox state.
- Proof type: repo-battle-tested, market-informed
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: `config/launcher.json`, referencing stable entry ids.
- Evidence: preset schema is documented, validated by `tools/verify.ps1`, and covered by store/workflow/ViewModel tests, including missing `entryIds` rejection and invalid preset references rejected before label/preview/launch projection. The ViewModel load path propagates missing or failed `launcher.json` presets instead of replacing it with an empty launch-group model, and preset view models snapshot preset facts plus read-only entry ids instead of exposing the live mutable preset object. The selected preset can be launched directly, preserving current checkbox state instead of forcing a selection rewrite before launch, while still sharing the same preflight/confirmation path as selected-item batch launch. Applying, previewing, or launching a preset also discards uncommitted drag-sort preview state first, so a visual ghost order is not silently persisted by a launch-group action. Applying a preset has a failure contract too: config backup/write failures leave the preset selection and config file unchanged and report `配置保存失败` instead of claiming the group was applied.
- Verdict: north-star. This is the best current data-structure move in the project.
- Remaining gap: no external import/export yet; not needed for the current local workflow.

### 7. First-class preset actions

- Feature id: first-class-preset-actions
- Real problem: a named group must be created, updated, and deleted inside the app, otherwise users fall back to hand-editing JSON.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: `config/launcher.json`.
- Evidence: ViewModel commands save current selection as a new group, update current group, delete editable groups, preserve locked groups, report dialog-boundary failures without mutating portable files, roll back memory/file state on preset save failures, save-new-preset dialogs receiving a read-only snapshot target so the saved entry ids cannot drift during the dialog, reject blank/duplicate preset entry ids before add/update mutation, reject invalid fallback preset references, blank fallback preset ids, and missing default-preset state before delete mutation instead of repairing corrupt state, reject missing selected-preset ids during preset state restore instead of silently selecting the first group, and report the new current launch group from the selected fallback view model after deletion instead of hiding fallback selection or rendering `(none)`.
- Verdict: north-star inside this scope.
- Remaining gap: no conflict UI for simultaneous external JSON edits; current repo-local app does not need multi-writer machinery.

### 8. Preset-vs-selection preview

- Feature id: preset-vs-selection-preview
- Real problem: applying a group blindly is dangerous because it rewrites saved selection.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: visual-proof
- Current data owner: preview is derived from current selection and selected preset; it is not a second source of truth.
- Evidence: tests cover apply/preview/no-op states, disabled no-op apply, and preview collection outputs that cannot be cast back to mutable selection/preset/missing-id storage.
- Verdict: north-star. It solves the real "what will change?" problem without a second persistent model.
- Remaining gap: visual screenshot receipts are useful only after UI-heavy changes.

### 9. Preset launch plan preview

- Feature id: preset-launch-plan-preview
- Real problem: launching a saved group is the highest-risk routine action; users need to inspect the exact generated `wt.exe` commands without rewriting selection or starting external processes.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: external-process-limit
- Current data owner: `config/launcher.json` owns desired group membership; `LaunchPlan` owns generated command lines; `DryRunText` is transient UI output, not persisted state.
- Evidence: `PreviewSelectedPresetCommand` calls the same `LauncherLaunchWorkflow.PreflightBatch` path as launch, formats plans through `LauncherBatchLaunchPlanText`, starts no process, and preserves checkbox state; tests cover successful preview, mixed valid/invalid plan-failure preview, including valid generated command lines marked as not started plus the failure list, and blank-name preview preflight failures replacing stale preview text with a preview failure receipt.
- Verdict: north-star for the repo-owned side-effect boundary. It converts the impossible "0% side effects after launch" demand into a true pre-launch no-side-effect inspection path.
- Remaining gap: once the user chooses real launch, arbitrary external commands still have side effects outside this app's control.

### 10. Add/edit/delete launcher buttons

- Feature id: add-edit-delete-launcher-buttons
- Real problem: the launcher must stay maintainable without manual JSON edits for common entry changes.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: `config/launcher.json`, with preset/settings references updated or rolled back together.
- Evidence: entry mutation tests cover add/edit/delete, dialog-boundary failure without file mutation, dialog-boundary handler assignments rejecting null instead of storing a broken runtime callback, failed save rollback, preset/settings coordination, status text, blank/duplicate reference rejection before reference rename/remove mutation, entry delete confirmation snapshotting target id/name before dialog mutation so references and rollback cannot drift, preset delete confirmation snapshotting target id/name/count before dialog mutation so the target and status cannot drift, entry view models exposing snapshot copies instead of public live `LaunchEntry` rows, entry edit dialogs receiving a snapshot entry instead of the live mutable row, and entry edit dialogs rejecting null initial/submitted text instead of converting it into empty required-field input.
- Verdict: north-star for current local entry CRUD. It keeps one config owner, snapshots dialog/delete targets, updates references in one mutation slice, and fails before corrupting portable files.
- Remaining gap: no bulk import; that remains rejected scope until a real import workload beats current manual add/edit/delete with less complexity.

### 11. Custom order and drag reorder

- Feature id: custom-order-and-drag-reorder
- Real problem: users build spatial memory; alphabetical order is not always the fastest launch order.
- Proof type: repo-battle-tested, accessibility-informed
- Verdict level: narrow-north-star
- Gap class: none-proven
- Current data owner: `launcher.json` settings `customEntryOrder`; drag session is a single transient object.
- Evidence: drag reorder session/workflow tests cover preview, commit, cancel, rollback, single-session ownership, sessions not publicly exposing mutable source view models or settings snapshots, drag sessions deriving the dragged id from the source entry instead of accepting a second mismatched id, required drag workflow dependencies failing fast at construction, custom-order index returning a read-only dictionary snapshot instead of a mutable index map, reorder bounds capture and animation scheduling exposing read-only cloned snapshots instead of caller-owned mutable dictionaries, invalid move/custom-order rejection before mutation or sort projection, missing or stale custom-order ids rejected before sort projection instead of buried at the end, and custom-order sync rejection of invalid order or known ids before mutation; context-menu `上移排序` / `下移排序` commands persist the same `customEntryOrder` without creating a second reorder model, and when invoked during a drag preview they discard the preview before resolving targets so ghost order cannot become a hidden source of truth.
- Verdict: north-star for mouse-first local use.
- Remaining gap: none proven. Non-drag reorder now uses the same data owner and persistence path as drag reorder.

### 12. Sort modes

- Feature id: sort-modes
- Real problem: users need different scan modes: custom memory order, newest updated, or alphabetic lookup.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: none-proven
- Current data owner: `launcher.json` settings `sortMode`.
- Evidence: tests cover custom/updated/alphabetic sort behavior and reject missing or unknown mode ids.
- Verdict: north-star; three modes are enough and not a framework.
- Remaining gap: none proven.

### 13. View modes: Cards / Details / Names

- Feature id: view-modes-cards-details-names
- Real problem: one layout cannot solve readable actions, dense field scanning, and overview at the same time.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: visual-proof
- Current data owner: `launcher.json` settings `entryViewMode`.
- Evidence: code exposes `cards`, `details`, and `names`; XAML has three roots; tests cover mode persistence and root selection.
- Verdict: north-star data model; README needed to name the already-implemented `Names` mode explicitly.
- Remaining gap: screenshot-backed comfort pass only after visual changes.

### 14. Adaptive card sizing and density

- Feature id: adaptive-card-sizing-and-density
- Real problem: fixed cards waste space on large windows and become unusable on small windows.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: visual-proof
- Current data owner: `LauncherEntryLayoutDensity`, derived from available size and visible entry count.
- Evidence: layout tests cover Normal/Compact/Micro density, one typed density owner, one typed available-size owner, filtered visible-entry count sizing, golden-ratio card shape, readable minimum before scroll fallback, and name-wall multi-column fit.
- Verdict: north-star for the engineering-owned visual comfort boundary.
- Remaining gap: screenshot or human visual receipts are required only after future visual-heavy changes; they are not needed to prove the current layout data structure.

### 15. Global launch confirmation

- Feature id: global-launch-confirmation
- Real problem: launch safety should have one visible rule; per-command "danger" guesses create false confidence and hidden branches.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: none-proven
- Current data owner: `launcher.json` settings `requireConfirmationForLaunches`.
- Evidence: launch confirmation tests cover saved setting, confirmation/cancel, and `LaunchPlan.RequiresConfirmation`; missing, unknown, or wrong-case JSON fields now fail fast, so obsolete per-entry `confirm` cannot silently compete with the global setting.
- Verdict: north-star. One switch is simpler and safer than command-classification theater.
- Remaining gap: none proven.

### 16. Portable settings

- Feature id: portable-settings
- Real problem: UI preferences should move with the repo and stay reviewable, not hide in AppData or registry.
- Proof type: repo-battle-tested, market-informed
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: `config/launcher.json`.
- Evidence: settings schema is documented and tests cover sort/view/confirmation/filter/order/window fields; null settings fields, null saved filter state at ViewModel construction, null runtime filter mutations, null runtime sort/display mode assignments, missing/blank mode ids, invalid in-memory settings during close-time window save, and non-positive persisted window dimensions now fail fast instead of being rewritten or carried into default/broken UI state; unused `theme` state was removed instead of kept as a fake preference.
- Verdict: north-star for this local tool.
- Remaining gap: no cross-machine path normalization; this is fine because launch directories are machine-local facts.

### 17. JSON persistence, backup, and rollback compensation

- Feature id: json-persistence-backup-and-rollback-compensation
- Real problem: config edits must not leave half-written JSON or half-applied multi-file state.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: future-workload
- Current data owner: config/preset/settings stores plus mutation snapshots.
- Evidence: store tests cover temp writes, `.bak` backups, skip-backup rollback writes, multi-file rollback failure statuses, config load returning a read-only entry collection snapshot instead of a mutable load list, rollback snapshot accessors cloning or read-only wrapping captured state instead of exposing mutable rollback storage, invalid in-memory mutation data and invalid rollback snapshots propagating instead of being reported as ordinary save/rollback failure, and fail-fast rejection of missing files plus missing, null, blank mode, non-positive window geometry, unknown, or wrong-case JSON fields. ViewModel refresh now shares that fail-fast preset-load contract instead of synthesizing a second empty preset truth.
- Verdict: north-star for a single-process local app.
- Remaining gap: no file locking or merge resolution for concurrent external edits; that is not a current requirement.

### 18. Core/App/Tests separation

- Feature id: core-app-tests-separation
- Real problem: launch argument rules and config behavior must be testable without opening the GUI.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: none-proven
- Current data owner: Core owns pure records/rules; App owns ViewModels/Views; Tests own behavior contracts.
- Evidence: solution layout and root verify enforce Core/App/Tests presence, tests cover Core helpers independently, ViewModel-owned collections expose read-only projections instead of public mutable collection owners, startup arguments reject null argument lists plus unknown or duplicate `--config` inputs instead of silently choosing one, and config path resolution rejects missing search roots instead of silently falling back to `Environment.CurrentDirectory`.
- Verdict: north-star for maintainability.
- Remaining gap: none proven.

### 19. Self-contained win-x64 publish and native gate

- Feature id: self-contained-win-x64-publish-and-native-gate
- Real problem: a desktop launcher is only real if it can be restored, tested, published, and smoke-started.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: distribution-scope
- Current data owner: `tools/verify.ps1`, `src/avalonia-launcher/verify.ps1`, and ignored publish artifacts.
- Evidence: root verify checks retired paths, config/presets/settings, docs, the GitHub similar-project snapshot schema plus refresh script and freshness window, tests, publish, and hidden startup smoke.
- Verdict: north-star for repo-local Windows release proof.
- Remaining gap: no installer/update channel; that belongs to distribution scope, not current launcher correctness.

### 20. Retired legacy surfaces

- Feature id: retired-legacy-surfaces
- Real problem: two active UI lines create duplicate truth and slow failure.
- Proof type: repo-battle-tested
- Verdict level: narrow-north-star
- Gap class: none-proven
- Current data owner: Avalonia mainline; retired HTA/WinForms are archive/reference only.
- Evidence: root verify rejects old WinForms/spike/root executable return and keeps retirement docs readable.
- Verdict: north-star. The old line is retired instead of kept alive by compatibility branches.
- Remaining gap: none proven.

### 21. Design benchmark and giants-shoulders library

- Feature id: design-benchmark-and-giants-shoulders-library
- Real problem: UI/product decisions should not be made from vibes or copied frameworks.
- Proof type: source-backed, market-informed
- Verdict level: narrow-north-star
- Gap class: source-freshness
- Current data owner: `DESIGN.md`, `docs/design-benchmark-digest.md`, and `docs/giants-shoulders/`.
- Evidence: docs translate external references into local acceptance rules, tests keep the digest referenced, verify the giants-shoulders admission rule and current entries, preserve the first-class launcher pattern table plus "what not to copy" boundary, `docs/reference/github-similar-projects-snapshot.json` preserves the current GitHub market yardstick used by this audit, `tools/update-github-similar-projects-snapshot.ps1` refreshes it from GitHub API state, and root verify validates the snapshot schema, freshness window, representative direct projects, and broad-market repositories.
- Verdict: north-star for source-tracked decision support in this repo. It converts outside lessons into local acceptance rules and explicit rejection boundaries instead of importing frameworks.
- Remaining gap: source freshness must be refreshed when a decision depends on a changed external standard; stale references are not current proof.

## Gaps that block a broad-domain north-star claim

These are not defects in the current narrow product. They are the reasons not to claim "best launcher/automation tool in the whole domain":

- Broad gap id: import-export-ecosystem
  - Gap class: future-workload
  - Blocks requirement id: prove-unimprovable-current-ability, prove-stronger-than-any-github-similar-project
  - Evidence: no first-class import/export ecosystem for entries or groups.
- Broad gap id: run-history-terminal-health
  - Gap class: future-workload
  - Blocks requirement id: prove-unimprovable-current-ability, prove-absolute-confidence-and-zero-side-effects
  - Evidence: no persisted run history or terminal-session health model separated from desired config; transient launch receipts exist for requested/failed/skipped plans.
- Broad gap id: installer-update-signing-channel
  - Gap class: distribution-scope
  - Blocks requirement id: prove-unimprovable-current-ability, prove-stronger-than-any-github-similar-project
  - Evidence: no installer/update/signing channel.
- Broad gap id: screenshot-backed-visual-comfort
  - Gap class: visual-proof
  - Blocks requirement id: prove-unimprovable-current-ability
  - Evidence: no screenshot-backed comfort receipt for visual taste after future visual-heavy layout changes.
- Broad gap id: universal-supremacy-zero-side-effects
  - Gap class: external-process-limit
  - Blocks requirement id: prove-stronger-than-any-github-similar-project, prove-absolute-confidence-and-zero-side-effects
  - Evidence: no honest way to prove "stronger than every GitHub similar project" or "0% side effects" for real process launch behavior.

Do not build these preemptively. They become work only when the real user problem appears.

## Objective completion audit

This section checks the user's full objective against current evidence instead of redefining success around the work already done.

Objective verdict classes used below:

- `proven-current-scope`: current repository evidence proves the requirement inside the documented WT Launcher scope.
- `proven-audit-only`: the evaluation is complete, but it does not prove universal product supremacy.
- `contradicted`: current evidence directly contradicts the absolute requirement.
- `false-broad-scope`: broad-market GitHub evidence contradicts the claim, even if a narrow slice remains strong.
- `preserved-current-scope`: the current design keeps the app small by rejecting speculative scope.
- `impossible-absolute`: the requirement cannot be honestly proven as an absolute for software that starts external processes.

Objective status count: proven-current-scope=1,proven-audit-only=1,contradicted=1,false-broad-scope=1,preserved-current-scope=1,impossible-absolute=1
Full objective result: not-complete-as-written
Feature north-star coverage: current-active-features=21,narrow-north-star=21,non-north-star=0
Blocked absolute requirements: unimprovable-product,stronger-than-every-github-similar,absolute-confidence-zero-side-effects

Objective claim impact tokens used below:

- `supports-current-feature-coverage`: proves the audit covers the current active feature set.
- `classifies-without-supremacy`: proves classification work, not universal superiority.
- `blocks-unimprovable-claim`: contradicts the requested unimprovable-product claim.
- `blocks-broad-github-claim`: contradicts the requested stronger-than-any-GitHub-similar-project claim.
- `preserves-small-scope`: supports the small-and-beautiful constraint inside current scope.
- `blocks-absolute-side-effect-claim`: makes the 100% confidence / 0% side effects claim impossible as a product-wide absolute.

- Requirement: evaluate every active feature.
  - Requirement id: evaluate-every-active-feature
  - Claim impact: supports-current-feature-coverage
  - Evidence: the 21 feature sections above cover every active feature mapping documented in `README.md` and the current Avalonia mainline scope.
  - Status: proven-current-scope — the audit covers the current repository scope.
- Requirement: decide whether each feature is battle-practice or market-proven north-star.
  - Requirement id: classify-battle-practice-or-market-proven
  - Claim impact: classifies-without-supremacy
  - Evidence: every feature section has a real problem, controlled proof type, controlled verdict level, controlled gap class, data owner, evidence, verdict, and remaining gap; all 21 current feature sections now have `Verdict level: narrow-north-star`; the GitHub snapshot separates narrow direct competitors from broad launchers.
  - Status: proven-audit-only — the classification is proven as an audit, not as a universal supremacy claim.
- Requirement: current ability is already unimprovable.
  - Requirement id: prove-unimprovable-current-ability
  - Claim impact: blocks-unimprovable-claim
  - Evidence: the broad-domain gap list still names import/export, persisted terminal-session health/history, distribution/update, visual receipts, and external side-effect limits.
  - Status: contradicted — there are possible improvements, but most are intentionally not current work until real demand appears.
- Requirement: stronger than any similar GitHub project.
  - Requirement id: prove-stronger-than-any-github-similar-project
  - Claim impact: blocks-broad-github-claim
  - Evidence: Flow Launcher and Wox have far stronger broad-market proof; Terminal Manager, Launchpad-CLI, win-screen-setup, DevWorkspace, claude-dashboard, wtsm-cli, and AgentSpace each beat WT Launcher on at least one adjacent axis such as WT profile management, installer/statusline distribution, visual multi-monitor layout, split panes/cost tracking, named-session CLI scope, or embedded PTY/SSH scope. WT Launcher is stronger only in the narrower portable config + named group + rollback-aware native gate slice.
  - Status: false-broad-scope — broad launcher supremacy is false; support exists only for the narrow local Windows Terminal workspace-launcher slice.
- Requirement: remain small and beautiful.
  - Requirement id: preserve-small-and-beautiful
  - Claim impact: preserves-small-scope
  - Evidence: this audit rejects speculative plugin marketplace, local web server, split-pane editor, installer/update channel, and workflow graph features until they beat the current local gate.
  - Status: preserved-current-scope — currently preserved.
- Requirement: 100% confidence and 0% side effects.
  - Requirement id: prove-absolute-confidence-and-zero-side-effects
  - Claim impact: blocks-absolute-side-effect-claim
  - Evidence: current tests and gate prove batch preflight-before-confirmation, side-effect-free current-group preview with valid plans visible even when another plan fails preflight, exact generated command confirmation, transient single-launch canceled/request/failure receipts plus batch receipts for canceled/requested/failed/skipped plans, abort-after-first-start-failure, rollback-aware JSON persistence, and native publish smoke. They cannot prove that starting arbitrary external processes has zero side effects.
  - Status: impossible-absolute — impossible as an absolute claim; mitigated where the repository owns the boundary.

## Claim boundary ledger

Claim status tokens used below:

- `allowed`: safe to state only inside the documented current WT Launcher scope.
- `forbidden`: not proven by current repository, native gate, or GitHub yardstick evidence.

Evidence owner tokens used below:

- `feature-audit`: this document's feature mapping and objective completion audit.
- `native-gate`: `tools/verify.ps1` plus the focused repository contract tests.
- `github-snapshot`: `docs/reference/github-similar-projects-snapshot.json`.
- `scope-rejection`: deliberate refusal to add speculative broad-platform scope.
- `external-process-boundary`: repository-owned checks before handing control to `wt.exe`.

- Claim id: narrow-domain-north-star
  - Claim status: allowed
  - Scope: portable Windows Terminal workspace launcher.
  - Evidence owner: feature-audit,native-gate,github-snapshot
  - Claim: WT Launcher is a narrow-domain north-star for a portable Windows Terminal workspace launcher.
- Claim id: narrow-slice-stronger
  - Claim status: allowed
  - Scope: portable config, named group, side-effect-free preview, rollback-aware persistence, and native-gate slice.
  - Evidence owner: feature-audit,native-gate,github-snapshot
  - Claim: WT Launcher is stronger only on the portable config, named group, side-effect-free preview, rollback-aware persistence, and native-gate slice.
- Claim id: small-beautiful-preserved
  - Claim status: allowed
  - Scope: current small local launcher contract.
  - Evidence owner: feature-audit,scope-rejection
  - Claim: The small-and-beautiful constraint is currently preserved by refusing speculative plugin marketplace, local web server, split-pane editor, workflow graph, and distribution/update scope.
- Claim id: pre-handoff-zero-side-effect-boundary
  - Claim status: allowed
  - Scope: repository-owned checks before external process handoff.
  - Evidence owner: native-gate,external-process-boundary
  - Claim: The strongest zero-side-effect claim applies only before external process handoff: preflight, generated-command preview, confirmation/cancel paths, and JSON rollback boundaries.
- Claim id: broad-launcher-platform-north-star
  - Claim status: forbidden
  - Scope: broad launcher or automation platform.
  - Evidence owner: feature-audit,github-snapshot
  - Claim: Do not claim broad launcher or automation-platform north-star status.
- Claim id: unimprovable-product
  - Claim status: forbidden
  - Scope: current product ability across all possible workflows.
  - Evidence owner: feature-audit
  - Claim: Do not claim the product is unimprovable.
- Claim id: stronger-than-every-github-similar
  - Claim status: forbidden
  - Scope: every similar GitHub project and every adjacent launcher axis.
  - Evidence owner: feature-audit,github-snapshot
  - Claim: Do not claim stronger-than-every-GitHub-similar-project status.
- Claim id: absolute-confidence-zero-side-effects
  - Claim status: forbidden
  - Scope: real `wt.exe` process launch behavior.
  - Evidence owner: feature-audit,external-process-boundary
  - Claim: Do not claim 100% confidence or 0% side effects for real `wt.exe` process launch behavior.
- Claim id: post-wt-terminal-health
  - Claim status: forbidden
  - Scope: terminal-session health after Windows Terminal accepts a start request.
  - Evidence owner: external-process-boundary
  - Claim: Do not claim terminal-session health after `wt.exe` accepts the start request.

## Final answer

The full objective remains not complete as written.

All 21 current feature mappings classify as narrow-domain north-star. The current project is a north-star implementation only for the narrow domain of a portable Windows Terminal workspace launcher. The strongest mappings are launch entries, named presets, adaptive card sizing/density, side-effect-free current-group preview, honest requested-start wording, transient single and batch launch receipts, global launch confirmation, portable settings, JSON persistence/rollback, batch preflight-before-confirmation, exact generated command confirmation, abort-after-first-start-failure, and the native verify/publish gate.

It is not a broad-domain north star for every launcher or automation product. GitHub market evidence contradicts that broad claim. Building import/export, status history, distribution/update, split panes, or a plugin system now would be speculative unless a real workflow beats the current small local contract.

This final answer follows allowed claim ids `narrow-domain-north-star`, `narrow-slice-stronger`, `small-beautiful-preserved`, and `pre-handoff-zero-side-effect-boundary`. It rejects forbidden claim ids `broad-launcher-platform-north-star`, `unimprovable-product`, `stronger-than-every-github-similar`, `absolute-confidence-zero-side-effects`, and `post-wt-terminal-health`.
