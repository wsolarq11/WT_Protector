# AI Agent CLI Discovery Feature

Parent: `07-10-v2-agent-discovery-and-hygiene`

## Goal

Add a new feature to WT Launcher that scans the local machine for installed AI agent CLIs (claude, codex, opencode, gemini/antigravity, aider, qwen-code, etc.), detects their versions, and presents them as **recommended entries** in a GUI dialog. Users select which to add; one click generates `LaunchEntry` records through the existing atomic-write pipeline.

This shifts data-entry cost from user to scanner — the v2 product leap.

## Background — 2026 AI Coding CLI Landscape

Per Aug 2026 industry surveys ([kilo.ai](https://kilo.ai/articles/best-cli-coding-agents), [morphllm.com](https://www.morphllm.com/ai-coding-agent), [pinggy.io](https://pinggy.io/blog/top_cli_based_ai_coding_agents)):

| Agent | Exec name | Vendor | Status (2026-07) |
|---|---|---|---|
| Claude Code | `claude` | Anthropic | Active, Opus 4.7 |
| Codex CLI | `codex` | OpenAI | Active, GPT-5.5, Terminal-Bench #1 |
| Cursor CLI | `cursor` | Cursor | Active since 2026-01, Cloud Handoff |
| Antigravity CLI | `antigravity` | Google | Active (replaces Gemini CLI 2026-05-19) |
| Gemini CLI | `gemini` | Google | Retired 2026-06-18, may still be installed |
| opencode | `opencode` | community | 180k stars, MIT, model-agnostic |
| Cline | `cline` | community | 64k stars, Apache-2.0 |
| Aider | `aider` | community | git-aware pair programmer |
| Qwen Code | `qwen-code` | Alibaba | Free API |
| Kilo Code | `kilo` | community | VS Code/JetBrains/CLI |
| Windsurf | `windsurf` | Codeium | Desktop, may not be CLI |

All major agents support `--version` (standard convention).

## Requirements

### R1 — Known-agent catalog (Core, static)

A static `AgentCatalog` table in `WTLauncher.Core` listing known AI agent CLIs with their recommended launch parameters. Each entry:

| Field | Example | Purpose |
|---|---|---|
| `ExecName` | `claude` | What to find on PATH (try `.exe`/`.cmd`/bare on Windows) |
| `DisplayName` | `Claude Code` | Human label |
| `Vendor` | `Anthropic` | Catalog grouping |
| `VersionArgs` | `--version` | Args to get version string |
| `RecommendedArgs` | (empty by default; user can override) | Pre-filled command when generating entry |
| `Category` | `TerminalNative` / `IDEExtension` / `DesktopApp` | Filtering |
| `HomePageUrl` | `https://claude.ai/code` | Optional "learn more" link |

**Constraint**: catalog is **static + hardcoded** in v2 — no plugin marketplace, no dynamic download. Add new agents by editing the table. This is the ponytail: one file, one source.

### R2 — PATH scanner (Core, pure function)

`AgentDiscoveryScanner` in `WTLauncher.Core`:
- Input: catalog + optional `PATH` string (default `Environment.GetEnvironmentVariable("PATH")`)
- For each catalog entry, search PATH directories for `ExecName` / `ExecName.exe` / `ExecName.cmd`
- Return `DiscoveredAgent { CatalogEntry, ResolvedPath, Version }[]`
- **Version detection**: spawn process with `VersionArgs`, capture stdout first line, parse version substring. Timeout 2s. Failure → version is `""` (not a fatal error).
- **Pure function**: no static state, no Avalonia, no `HttpClient`. Testable in xUnit.
- ArchUnitNET: stays in Core, no UI deps.

### R3 — Recommendation dialog (Views)

New dialog window `DiscoverAgentsWindow.axaml`:
- Header: "🔍 发现已安装的 AI Agent"
- Body: `ItemsControl` of `DiscoveredAgentCard` (one per found agent)
  - Card shows: DisplayName + Version + ResolvedPath (truncated) + Vendor badge
  - Checkbox to select for import
  - "ℹ️" button shows recommended args + homepage URL
- Already-added agents (matched by exec name on existing entries' `command`) shown greyed out with "已添加" label
- Footer: "添加选中 (N)" button + "取消" + "重新扫描"

**Token constraint**: all visuals via `{StaticResource …}` / `LauncherDesignTokens`. No hardcoded colors.

### R4 — One-click add pipeline (ViewModels)

`DiscoverAgentsCommand` in `MainWindowViewModel`:
1. Open dialog
2. User selects + confirms
3. For each selected, build a `LaunchEntryDraft` from `DiscoveredAgent`
   - `Name` = DisplayName (append " (copy)" if duplicate)
   - `Directory` = user home (`%USERPROFILE%`) — user can change later
   - `Command` = `$key` template reference if catalog recommends, else raw `ExecName`
   - `selected` = false
4. **Reuse existing add-entry pipeline**: `LauncherEntryDrafts.Validate` → `LauncherEntryMutationSnapshot` snapshot → mutate → `LauncherStore.Save` → rollback on failure
5. Status bar: "已添加 N 个 AI Agent 启动项"

### R5 — Trigger surface

Two trigger points:
- **Manual**: a "🔍 发现" button next to existing "添加" button in main window header
- **First-run**: if entries is empty AND user has not dismissed before, auto-open dialog on startup (single-shot, recorded in `settings.discoveryDismissedAtUtc`)

### R6 — Test coverage (per feature-first-class audit pattern)

- `AgentCatalogTests` — catalog integrity (no duplicate exec names, all have VersionArgs)
- `AgentDiscoveryScannerTests` — pure function: synthetic PATH, fake executables in temp dir, version parsing edge cases
- `DiscoverAgentsCommandTests` — dialog boundary mock + add-pipeline integration
- `LauncherEntryDraftsTests` extension — adding from DiscoveredAgent produces valid draft

Aim: ≥25 new `[Fact]`/`[Theory]` covering the new code paths.

## Acceptance Criteria

- [ ] Catalog ships with ≥8 known agents (claude/codex/cursor/antigravity/gemini/opencode/aider/qwen-code minimum).
- [ ] Scanner finds at least the 3 agents this dev machine has (verify with `Get-Command claude,codex,opencode` before demo).
- [ ] Version detection works for ≥1 agent end-to-end.
- [ ] Recommendation dialog renders correctly in all three view modes (Cards/Details/Names) — actually it's modal so just verify it opens/closes cleanly.
- [ ] One-click add creates valid `LaunchEntry` records, persisted to `config/launcher.json` atomically.
- [ ] Snapshot+rollback works: simulate save failure, no partial writes.
- [ ] `tools/verify.ps1` 18-check gate green.
- [ ] `dotnet test` adds ≥25 new tests, all pass.
- [ ] ArchUnitNET R1-R6 still green (no Core → Avalonia leakage).
- [ ] No bare `catch (Exception)` in new code (use `IOException` / `InvalidOperationException` etc.).
- [ ] DESIGN.md token ownership rules respected.
- [ ] docs/feature-reality-north-star-audit.md updated with the new capability (becomes feature #24).

## Out of Scope

- **Per-agent brand theming / icons** — v2 uses default tokens. Future: `LauncherAgentCatalog` with icon resources.
- **Auto-update of agents** — winget configure (sibling task) handles declaratively.
- **Cross-machine config sync** — out of v2 scope.
- **Custom agent definitions** — v2 ships the static catalog only; user can still manually add entries for unknown agents.
- **Background re-scan / file watcher** — user clicks "🔍 发现" or "重新扫描" to trigger.
- **Cost / token tracking** — explicitly deferred per existing audit.

## Open Questions (resolved during design)

- Q: Should version detection be async? — A: Yes, scanner runs on background thread; dialog shows progress.
- Q: What if user runs scanner, then cancels dialog? — A: No state mutation; just close. Pure ephemeral.
- Q: What if catalog entry's RecommendedArgs contains `$key` template? — A: Generate entry using existing `LauncherCommandTemplateResolver` — add a new template if needed.

## Notes

- Estimated work: 3-5 days.
- This is the **primary v2 deliverable** — its success defines v2.
- Pair with sibling `repo-hygiene-cleanup` so the new feature lands on a clean baseline.
