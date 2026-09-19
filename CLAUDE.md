# CLAUDE.md

Repo-wide navigation and invariants live in `AGENTS.md`; documentation index lives in `docs/README.md`.

## MCP Knowledge Graph

If the codebase-memory-mcp server is configured, pass project name `D-_AI_-_projects-WT_Protector` to graph tools (`get_architecture`, `search_graph`, `trace_path`, `get_code_snippet`, `query_graph`). No `list_projects` tool. This name is not verifiable from repo files; trust the live MCP config.

## What This Is

WT Launcher: local Windows Terminal workspace launcher. Avalonia 12 + .NET 10, win-x64 self-contained. One atomic store: `config/launcher.json`. No AppData, no registry, no DB.

## Commands

```bash
npm ci
npm run typecheck
npm test
npm run verify              # full gate before any "done" claim
npm run release:contract    # CI release authority
npm run start
npm run verify:mainline
npm run test:fast
npm run test:coverage
npm run test:launch-args
npm run smoke
npm run apply:config
npm run install:portable
npm run uninstall
npm run package:friend
```

Raw dotnet (iterate locally; use `--no-restore` after the mandated restore):

```bash
dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln -r win-x64 -p:EnableNativeAot=true
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
dotnet publish src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -r win-x64 --self-contained --no-restore
```

`-warnaserror` is real in CI and the gate.

## 5-Layer Architecture

| Layer | Location | Owns | Forbidden |
| --- | --- | --- | --- |
| 0 Core | `WTLauncher.Core/` | data records, `LaunchArgumentBuilder`, `LauncherJson`, `LauncherStore`, design tokens | UI framework references |
| 1 Commands | `ViewModels/MainWindowViewModel*.cs` | validate → mutate → save handlers | persisting outside `LauncherStore` |
| 2 Persistence | `Infrastructure/StoreGrain.cs` | save-first-then-commit rollback | partial writes after failed save |
| 3 Orchestration | `MainWindowViewModel` | single writable UI-state owner | second drifting state copy |
| 4 Views | `Views/` | XAML, drag ghost, code-behind windowing | business decisions |

## Non-Negotiable Invariants

- One writable source per state; projections are read-only.
- Atomic config write through `LauncherJson.Save` (temp-file + `File.Replace`).
- Save-first-then-commit in `StoreGrain.Dispatch`.
- `catch (Exception)` is forbidden; narrow to known types.
- No hardcoded colors/sizes in XAML; use `LauncherDesignTokens` / `{StaticResource ...}`.
- Every active entry `directory` must exist.
- No obsolete per-entry `confirm` key.
- Never restore retired paths or `.snow/`, artifacts, `*.bak` files.

## Gate Tiers

`npm run verify` aggregates failures in one pass: `static` (blocks), `local` (machine-local path facts, advisory), `native` (needs .NET SDK; skipped with reason), `snapshot` (advisory). Step count is documented only in `AGENTS.md` structure baseline.

## Trellis

Trellis-managed repo. Read the relevant `.trellis/spec/{core,views,tests,guides}/index.md` before editing that layer. Non-trivial work follows `.trellis/workflow.md` Plan → Execute → Finish. Small fixes can be inline.

Key lookups:

- `docs/README.md` — document index
- `AGENTS.md` — repo map, structure baseline, vine trail
- `src/avalonia-launcher/README.md` — source architecture
- `.trellis/spec/guides/pitfalls.md` — session-evidenced pitfalls

## Common Gate Breaks

Hardcoding colors/sizes, duplicating a projection field, writing JSON outside `LauncherStore`, bare `catch (Exception)`, stale entry directories, restoring retired paths, committing `.snow/` or artifacts, editing inside `<!-- TRELLIS:START -->...<!-- TRELLIS:END -->`, or claiming unimprovable/stronger-than-all/zero-side-effect status.
