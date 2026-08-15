# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## MCP Knowledge Graph

> 未验证 (2026-08-15): MCP 服务器配置位于仓库之外（本仓库无 .mcp.json 或等效配置），
> 以下项目名与工具行为无法以仓库证据核实，以实际 MCP 服务器配置为准。
> 仓库内线索: `.trellis/spec/guides/pitfalls.md` §1.4 记录了该项目名的踩坑案例。

If the codebase-memory-mcp server is configured for this repo, its project name is
**`D-_AI_-_projects-WT_Protector`** — pass it as the `project` parameter to all graph
tools (`get_architecture`, `search_graph`, `trace_path`, `get_code_snippet`, `query_graph`).
There is no `list_projects` tool.

## What this is

`WT Launcher` / `WT Protector` — a local Windows Terminal workspace launcher. Avalonia 12 + .NET 10, win-x64 self-contained. All launcher state lives in one atomic JSON store at `config/launcher.json` (entries + presets + settings + `defaultPresetId` + `runHistory` together). No AppData, no registry, no DB.

Active mainline: `src/avalonia-launcher/`. Everything under `retired/` is archive-only (HTA config remnant + WinForms design lessons) — never restore it.

## Commands — the gate is the contract

Run everything from the **repository root**. PowerShell on Windows.

```powershell
# Full native gate — 40 checks (growing), must be green before any "done" claim
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1

# Stable release publish → src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1

# Run the app against the canonical config/launcher.json
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1

# Avalonia-only gate (restore → test → publish → hidden GUI smoke on Windows)
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\verify.ps1

# Just the launch-argument tests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-launch-args.ps1
```

Raw dotnet (when iterating locally):

```bash
dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln
dotnet test   src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release
dotnet build  src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
# Single test (filter by name):
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~LaunchArgumentBuilder"
dotnet publish src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -r win-x64 --self-contained
```

`-warnaserror` is real — CI (`/.github/workflows/verify.yml`) builds with it, and so does the gate. Treat warnings as failures from the start.

## Scheme B architecture decisions (2026-07-23, 3 phases landed)

All dead code removal, serialization, exception, and concurrency changes from the
deep architecture research (`07-23-arch-deep-research/`) are committed. Key facts:

- **Fluxor is gone.** 7 Fluxor files + ExceptionBoundary + LauncherIcons deleted.
  State management: `StoreGrain` (single container, SemaphoreSlim, Dispatch/DispatchErrorOr
  for mutations). CommunityToolkit.Mvvm for VM bindings.
- **AOT-safe serialization.** `JsonSerializer.Deserialize(stream, jsonTypeInfo)` /
  `Serialize(value, jsonTypeInfo)` — NO `JsonSerializerOptions`-based overloads,
  which have `[RequiresUnreferencedCode]`. Options are set via `[JsonSourceGenerationOptions]`
  on `LauncherJsonContext`.
- **Loose Read + Strict Write.** Unknown JSON keys are silently skipped at load time
  (forward compat). Source-gen only writes known fields. No `UnmappedMemberHandling.Disallow`.
- **StoreGrain.Mutate removed.** All callers use `Dispatch` / `DispatchErrorOr` (no `DispatchAsync` exists).
- **ExceptionFilters.IsRecoverable** is the single catch-filter entry point. DEBUG guard
  rethrows logic errors. Delegates to `LauncherErrors.IsRecoverable` (Core) for production.
  No more duplicate `UnwrapAggregates`.
- **FileLauncherJsonIO** has an instance-level `SemaphoreSlim(1,1)` on `Save` — defense-in-depth. (`LauncherJson.Save` is a static facade that delegates to `ILauncherJsonIO`.)
- **MemoizedSelector._hasCached** is `volatile`.
- **AgentDiscoveryScanner.Scan()** accepts `CancellationToken ct = default`.
- **DiscoverAgentsViewModel** propagates CancellationToken through `_scanFunc`.
- **CI:** AOT publish job (informational), `dotnet nuget audit` (was no-op restore),
  Dependabot (NuGet + GitHub Actions), `.editorconfig`.

`.snow/`, `.claude/`, `.agents/`, `artifacts/`, `*.bak*`, `*.log`, `*.tmp` are git-ignored local/runtime state — never gate or commit on them.

## The 5-layer architecture (this is the part that needs multiple files to see)

Source under `src/avalonia-launcher/src/`. Layers depend only on lower layers.

| Layer | Where | Owns | Cannot do |
|-------|-------|------|-----------|
| 0 — Core | `WTLauncher.Core/` | Pure data records (`LaunchEntry`, `LauncherPreset`, `LauncherStoreFile`), `LaunchArgumentBuilder` (wt.exe quoting), `LauncherJson` (temp-file + `File.Replace` atomic write), `LauncherStore` schema validation, `LauncherDesignTokens` (single constants source), pure layout/order calculators | Reference Avalonia / CommunityToolkit.Mvvm / any UI framework |
| 1 — Command mutations | `WTLauncher.App/ViewModels/MainWindowViewModel*.cs` | add/edit/delete/clone/launch command handlers: validate → mutate → save | Persist without going through `LauncherStore` |
| 2 — Persistence + rollback | `WTLauncher.App/Infrastructure/` (`StoreGrain.Dispatch`, `FileStorePersistence`) | save-first-then-commit: reducer → save to disk → apply to memory; on save failure memory is unchanged and the caller sees the error (snapshot classes removed 2026-07-21) | Leave partial writes on disk after a failed save |
| 3 — Orchestration | `MainWindowViewModel` (the single writable owner of UI state) | delegates reusable rules to Core, injects dialog `Func<>`s through `MainWindow` ctor | Keep a second copy of state that drifts from the single owner |
| 4 — Views | `WTLauncher.App/Views/` (`MainWindow.axaml` hosts the 3 view modes — Cards / Details / Names — whose bodies live in `LauncherEntriesView.axaml`) | XAML, drag ghost + insert indicators, code-behind = windowing only (drag hit-test, dialog injection, keyboard nav) | Business decisions / `if` statements about launch logic |

Tests: `src/avalonia-launcher/tests/WTLauncher.Tests/` — xUnit, 743 `[Fact]`/`[Theory]` attributes across all layers (incl. Roslyn analyzer suite; the gate runs 826 cases). These are the mainline gate.

## Non-negotiable invariants (these bite when broken)

- **Single source per state.** Read-only projections (`IsCompactLayout`, `IsCardsViewMode`, `IsDetailedListMode`, `IsNamesViewMode`, `IsCustomSortMode`, `IsDragPreviewSource/Target`, drag insert booleans) are computed from one writable field (`LauncherEntryLayoutDensity`, `LauncherEntryViewMode`, `SelectedSortMode`, `DragCueState`, `DragReorderSession?`). Never store a projection as a second writable field — they will drift. Same for sort mode: `IsCustomSortMode` is a read-only projection of `SelectedSortMode`.
- **Atomic config write.** Everything lives in one `config/launcher.json`. `LauncherJson.Save` does temp-file + `File.Replace`. Never write JSON outside `LauncherStore`. Save-first-then-commit in `StoreGrain.Dispatch` is the rollback mechanism: the reducer result is persisted before memory is updated, so a failed save leaves memory unchanged and surfaces the error to the caller.
- **Selection has two write paths, both visible.** User checkbox → `IsSelected` saves immediately. Batch/preset/edit → `SetSelectionFromOwner` then one batched save. No global bulk-suppression flag.
- **Drag state must be discarded before refresh.** `MainWindowViewModel.Refresh()` starts with `DiscardDragReorderPreview()`. Changing sort/filter/view while a drag preview is live otherwise leaks the ghost onto the wrong entry.
- **Design tokens are the only color/spacing source.** Use `{StaticResource PrimaryBrush}` etc., never hardcode `#2563EB` in XAML — it will diverge from `LauncherDesignTokens` on the next change.
- **`catch (Exception)` is forbidden.** Narrow to the known exception types (`IOException`, `UnauthorizedAccessException`, `SecurityException`, …). Bare catches swallow real bugs.
- **`directory` must exist.** The gate rejects stale launcher buttons (every active entry's `directory` must exist on this machine).
- **No obsolete per-entry `confirm` key.** Launch confirmation has one authority: `settings.requireConfirmationForLaunches`. There is no per-entry danger metadata.

## Config schema — quick reference

`config/launcher.json` is `LauncherStoreFile`, `schemaVersion: 2`. Top-level: `entries[]`, `presets[]`, `settings{}`, `defaultPresetId`, `runHistory[]`. Entry `command` supports `$key` template references resolved against `settings.commandTemplates` (missing key = launch validation error, not silent). `settings.sortMode ∈ {custom, recent, updated, alphabetical, frequent}`, `settings.entryViewMode ∈ {cards, details, names}`, plus `searchHistory[]` and `window {width, height}` (positive). Unknown JSON keys are silently skipped (forward compat); missing/null/blank mode keys are rejected.

## Trellis workflow is installed

This repo is Trellis-managed. New non-trivial work should follow the Trellis phase flow (`Plan → Execute → Finish`) under `.trellis/workflow.md`. Key resources:

- `.trellis/spec/{core,views,tests,guides}/` — layer-scoped coding guidelines. **Read the relevant `index.md` before editing in that layer** (Core especially — see `quality-guidelines.md` and the "single source of truth" rules).
- `.trellis/spec/guides/pitfalls.md` — pitfalls distilled from 50+ real sessions (each entry carries session-id evidence). Skim before coding; search by keyword when a weird failure shows up; append new lessons with session evidence.
- `.trellis/tasks/` — active + archived tasks (PRD/design/implement + jsonl manifests).
- `.trellis/workspace/<dev>/journal-*.md` — per-developer journal.
- `.codex/agents/` and `.agents/skills/` — optional sub-agents (`trellis-research`, `trellis-implement`, `trellis-check`) for dispatch-capable platforms.
- `AGENTS.md` — full repo navigation map and "vine trail" entry path; start there when lost.
- `AGENTS.md` 「结构基线」表 is the single documented source of structure facts (file counts, partials, test attributes, checks); gate check `structure-baseline-consistency` enforces it — change structure → update the table and the `tools/verify.ps1` baseline constants together.

Trellis tasks are **not** required for small fixes; `init`-style documentation, single-line fixes, and obvious tweaks can be inline. For multi-file features or anything that touches the gate, create a task and plan first.

## Where to look for the why

- `README.md` — full feature list, safety notes, schema, current implementation status. Read this before any user-facing change.
- `DESIGN.md` — brand, principles, the four-box header alignment rule, three-view-mode contract, single-source token/component ownership table. Read before any visual/UX change.
- `AGENTS.md` — repo map and the "vine trail" navigation. The `<!-- TRELLIS:START -->…:END -->` block is managed by `trellis update`; don't edit inside it.
- `docs/feature-reality-north-star-audit.md` — 22-feature north-star audit + claim-boundary ledger (what narrow claims are allowed vs. forbidden broad/unimprovable/universal claims). The gate validates this file's structure.
- `docs/giants-shoulders/` — external project lessons (CodexSwitch + Avalonia + Native AOT, etc.) for stack decisions.

## Common ways to break the gate

- Hardcoding colors, sizes, or layout constants in XAML instead of `LauncherDesignTokens` / `{StaticResource …}`.
- Adding a second writable field that duplicates an existing projection (e.g., storing `IsCustomSortMode` as a settable bool).
- Writing JSON anywhere except `LauncherStore` (no direct `File.WriteAllText` of `launcher.json`).
- Catching bare `Exception`.
- Letting `config/launcher.json` carry an obsolete per-entry `confirm` key.
- Adding a launch button whose `directory` doesn't exist on the dev machine.
- Restoring retired code paths (`WT Launcher.exe`, `src/winforms-launcher/`, `spikes/`).
- Letting `.snow/`, build artifacts, or `*.bak` files get committed.
- Editing inside the `<!-- TRELLIS:START -->…:END -->` block of `AGENTS.md`.
- Claiming the launcher is "unimprovable", "stronger than every GitHub alternative", or "0% side effects" — the claim-boundary ledger explicitly forbids these in `docs/feature-reality-north-star-audit.md`.

## Line endings & encoding

`.gitattributes` normalizes all text to LF on commit. Do not introduce CRLF churn. JSON files are UTF-8.
