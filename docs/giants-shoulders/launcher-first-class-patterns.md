# First-class launcher patterns from mature tools

Reviewed on: 2026-05-17
Last touched: 2026-07-09 (three-file-reference cleanup, `a5e1d757`)
Verified: 2026-08-15 — this file is in the `no-stale-three-file-refs` gate's activeDocs list (`scripts/verify.ts`); the rewrite below contains no `config/(config|presets|settings).json` references, only the single `config/launcher.json` store.

Purpose: preserve the design lessons from the 12-tool industry survey so WT Launcher does not rediscover the same first-class UX ideas later.

This page is source material for WT Launcher decisions, not a mandate to copy any product. The local acceptance gate remains:

```bash
npm run verify
```

## Local conclusion

The old `selected` checkbox state is useful because it matches the retired HTA's immediate one-click feel, but it is not the best long-term object. Mature tools usually promote repeated choice into a named, portable, inspectable first-class object:

```text
entry / command / profile       the thing that can run
preset / group / workspace      a named subset or context
settings / defaults             user and UI preferences
status / receipt                observed result, not desired config
```

WT Launcher's local translation is now (verified against `config/launcher.json`, keys: `schemaVersion`, `settings`, `entries`, `presets`, `defaultPresetId`, `runHistory`):

```text
config/launcher.json  unified store: launch entries + named launch groups (referencing entry ids, light CRUD) + portable UI preferences + defaultPresetId
```

A preset only rewrites checkbox state (`ApplyPreset` in `MainWindowViewModel.Presets.cs` → `SetSelectionFromOwner`). The actual launch still requires the explicit `一键启动已勾选` action (separate `LaunchSelectedCommand` button in `LauncherControlBar.axaml`), so group selection and command execution remain separate safety boundaries.

## The 12 borrowed design patterns

| Tool family | First-class object to borrow | WT Launcher translation | Source |
| --- | --- | --- | --- |
| VS Code Settings | layered, human-readable settings scopes | keep config portable and reviewable; avoid hidden AppData state | <https://code.visualstudio.com/docs/configure/settings> |
| VS Code Workspaces | saved multi-root context | a named launch group can represent a working context | <https://code.visualstudio.com/docs/editing/workspaces/workspaces> |
| Windows Terminal Profiles | profile objects plus defaults | entries should stay stable and addressable by id (`EntryId`/`PresetId` value types) | <https://learn.microsoft.com/en-us/windows/terminal/customize-settings/profile-general> |
| Windows Terminal JSON Fragments | external config fragments | future import/export can be additive instead of one giant file | <https://learn.microsoft.com/en-us/windows/terminal/json-fragment-extensions> |
| JetBrains Run/Debug Configurations | named runnable configurations | launch behavior should be a named object, not only a button state | <https://www.jetbrains.com/help/idea/run-debug-configuration.html> |
| Raycast Quicklinks/Snippets | searchable commands with labels and placeholders | launcher cards should remain fast, searchable command objects | <https://manual.raycast.com/quicklinks>, <https://manual.raycast.com/snippets> |
| Raycast Extensions | discoverable installable commands | future extension/import should not weaken the local config contract | <https://manual.raycast.com/extensions> |
| Alfred Workflows | visible input/action/output flows | later automation should be explicit graph-like steps, not hidden side effects | <https://www.alfredapp.com/workflows/> |
| Obsidian Workspaces/Bookmarks | named saved layouts and links | save user intent as named contexts instead of transient view state | <https://help.obsidian.md/Plugins/Workspaces>, <https://help.obsidian.md/plugins/bookmarks> |
| GitHub CLI Extensions | custom commands that feel native | custom launch entries can be native if they share one command contract | <https://docs.github.com/en/github-cli/github-cli/creating-github-cli-extensions> |
| Docker Compose Profiles | named active subsets | `launcher.json` presets select a subset of entries by id (`presets[].entryIds`) | <https://docs.docker.com/compose/how-tos/profiles/> |
| Kubernetes Objects | desired `spec` separated from observed `status` | launch config/presets stay separate from last-run receipts; run history lives as metadata in the same unified store (`runHistory`), never in a hidden external database | <https://kubernetes.io/docs/concepts/abstractions/overview/> |
| Apple Shortcuts | action lists with variables/automations | automation is future scope; do not hide automation behind a checkbox | <https://support.apple.com/guide/shortcuts/apdd2b316022/ios>, <https://support.apple.com/guide/iphone/shortcuts-iph47e1c9d7d/ios> |
| Figma Components/Variants | reusable components with variants/properties | UI cards/actions should become reusable components only when repetition proves it | <https://help.figma.com/hc/en-us/articles/360038662654-Guide-to-components-in-Figma>, <https://help.figma.com/hc/en-us/articles/360056440594-Create-and-use-variants> |

Note: the survey has 12 product lessons but 14 rows because VS Code and Windows Terminal each contributed two distinct first-class patterns. The local decision still reduces to one product move: named launch groups.

## What we intentionally did not copy

- No plugin marketplace or extension runtime yet.
- No hidden status database (run history lives in `config/launcher.json`, not in AppData/registry).
- No workflow graph engine.
- No new dependency.
- No automatic launch when a preset is selected (applying a preset only rewrites checkbox state; launching needs the explicit `一键启动已勾选` action).

Those ideas remain future candidates only if they beat the current gate with less routine cost or stronger safety.

## Landed local proof (verified 2026-08-15)

The slice proposed at review time has fully landed and is now gate-protected:

1. `launcher.json` presets exist as a human-readable named launch-group section in the unified store — verified: `config/launcher.json` contains `presets` (2 presets) and `defaultPresetId` (`core-projects`); example schema in `config/launcher.example.json`.
2. Preset ids and entry references are validated in both xUnit and the gate — verified: `scripts/verify.ts` `presets-schema` check plus `security-audit-f2-preset-entryid-validate` (`IPresetService.AddPreset/UpdatePreset` must validate against `knownEntryIds`); xUnit preset suites incl. `LauncherPresetIdsTests`, `LauncherPresetWorkflowTests`, `PresetServiceTests`, `MainWindowViewModelTests.PresetSelectionTests`.
3. UI selector plus explicit `应用启动组` action — verified: `LauncherPresetSelectionPreview.DefaultApplyButtonText` and the bound apply button; `MainWindowViewModel.Presets.cs` `ApplyPreset`/`CanApplyPreset`.
4. Light first-class actions `保存为新启动组` / `更新当前启动组` / `删除当前启动组` — verified: menu items in `Views/LauncherControlBar.axaml` (under the `启动组维护` button), commands `SaveSelectionAsPresetCommand` / `UpdateSelectedPresetCommand` / `DeleteSelectedPresetCommand`.
5. Existing `selected` checkboxes preserved as the HTA-compatible bridge — verified: `CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}"` in `Views/LauncherEntriesView.axaml`.

Rollback point (unchanged): drop the `presets` and `defaultPresetId` sections from `config/launcher.json`, remove `LauncherPreset*`, and remove the preset UI/tests; the old checkbox flow remains intact.
