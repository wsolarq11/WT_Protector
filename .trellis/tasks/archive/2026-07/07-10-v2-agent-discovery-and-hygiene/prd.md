# WT Launcher V2 — AI Agent Discovery + Repo Hygiene

## Goal

Upgrade WT Launcher from a manual-entry launcher to a v2 that **auto-discovers installed AI agent CLIs** (claude / codex / opencode / etc.) and offers one-click entry generation, while clearing accumulated repo hygiene drift. Preserve the existing C# Avalonia 5-layer architecture as the correct product substrate — GUI is product value, not tech debt.

## Background

The product's stated north star is "fast multi-workspace, multi-AI-agent CLI launching with a visually pleasant UX." Today every entry is hand-typed. The natural v2 is: scan this machine for installed agent CLIs and offer to add them as entries. This shifts the data-entry cost from user to scanner.

## Task Map (Parent owns this; children own their own deliverables)

| Child | Slug | Priority | Complexity | Deliverable |
|---|---|---|---|---|
| 1 | `repo-hygiene-cleanup` | P2 | trivial | Delete orphan dir, sync AGENTS.md map, reposition architecture-zero.md |
| 2 | `agent-cli-discovery` | P1 | complex | New feature: PATH scan + version detect + recommendation dialog + one-click add |
| 3 | `winget-machine-ready` | P3 | complex | Optional: `.wt-protector.dsc.yaml` declaring machine prerequisites via WinGet Configuration |

## Cross-Child Acceptance Criteria

- [ ] All three children reach `completed` status with passing local gate.
- [ ] `tools/verify.ps1` 18-check gate stays green after each child lands.
- [ ] `config/launcher.json` schema v2 contract is preserved (no breaking change to existing entries / presets / settings).
- [ ] 5-layer architecture invariants hold (ArchUnitNET R1-R6 still pass).
- [ ] No retired paths (`WT Launcher.exe`, `src/winforms-launcher/`, `spikes/`, HTA) reintroduced.
- [ ] No bare `catch (Exception)` introduced anywhere.
- [ ] No new design-token drift (all new visuals use `{StaticResource …}` or `LauncherDesignTokens`).
- [ ] AGENTS.md task map reflects post-v2 reality.
- [ ] Parent task PRD/design/implement reviewed and children archived under `.trellis/tasks/archive/2026-07/`.

## Constraints

- **Stack frozen**: .NET 10 + Avalonia 12 + xUnit v3 + ArchUnitNET. No new NuGet.
- **Architecture frozen**: Core (Layer 0) zero UI deps; ArchUnitNET R1-R6 stay green.
- **Persistence frozen**: single `config/launcher.json`, atomic write via `LauncherJson.Save`.
- **No AOT / CompiledBindings** flip-flop — keep current deliberate-deferral stance.
- **Net-additive for feature work**: agent discovery is **new code**, not a refactor of existing paths.
- **Boring over clever**: no plugin marketplace, no hidden fallback, no auto-mutation of user config without explicit confirmation dialog.

## Out of Scope (deferred)

- Per-agent icon / brand color theming (use default token colors for v2; can add a `LauncherAgentCatalog` later).
- Cross-machine config sync (covered conceptually by child 3's winget configure layer).
- Auto-update of agent binaries (out of launcher scope; winget configure handles declaratively).
- "Run-history / terminal-health" tracking — explicitly deferred per existing audit.

## Notes

- Each child must run `git diff --check` + `tools/verify.ps1` before its own `task.py finish`.
- Parent task owns the integration review after all children land; do not start the parent as the implementation target.
- This PRD is the source of truth for the v2 scope. Children's PRDs elaborate, they do not redefine.
