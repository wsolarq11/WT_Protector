# Repo Hygiene Cleanup

Parent: `07-10-v2-agent-discovery-and-hygiene`

## Goal

Clear three accumulated repo-hygiene drift items found during the v2 architecture audit. All three are trivial; bundle them so the v2 feature work (sibling tasks) starts from a clean baseline.

## Requirements

### R1 — Delete `src/avalonia-launcher/config/` orphan directory

**Evidence of orphan status**:
- Contains `launcher.json.bak`, `launcher.json.bak.1`, `launcher.json.bak.2`, `store-log.db` (0 B), `store.db` (32 KB, non-SQLite magic header `00 00 00 00 01 ff…`).
- Files dated 2026-07-06; predates current canonical `config/launcher.json`.
- `grep -r "store\.db|store-log\.db|Microsoft\.Data\.Sqlite|sqlite3"` in `src/` returns **zero matches** — no code references.
- `.gitignore` already covers `*.db` and `*.bak*` so nothing leaks to git, but the directory's physical presence is misleading (violates CLAUDE.md "No DB" + "single config authority" visual contracts).

**Action**: `rm -rf src/avalonia-launcher/config/`.

**Verification**: directory gone; `tools/verify.ps1` still green (it does not reference this path).

### R2 — Sync AGENTS.md repo map with actual `docs/` contents

AGENTS.md "全仓地图" currently omits:
- `docs/research/wt-launcher-architecture-zero.md` (2026-07-06, "eliminate the launcher" research)
- `docs/feature-first-class-upgrade-audit.md` (2026-06-10, 23-feature first-class audit)

Both are already known to `tools/verify.ps1` (lines 457, 462). AGENTS.md lags the gate.

**Action**: add two lines under the `docs/` tree in AGENTS.md.

**Verification**: visual grep on AGENTS.md shows both files listed; gate still green.

### R3 — Reposition `docs/research/wt-launcher-architecture-zero.md` as fallback-only

The doc currently reads as an active "delete the launcher" proposal. After v2 (Agent CLI Discovery) the GUI's product justification becomes stronger, not weaker. Reposition the doc as a **contingency record**, not an active recommendation.

**Action**: add a header block at the top of the file:

```markdown
> **Status (2026-07-10): Contingency record, not an active recommendation.**
> This document explores eliminating the GUI entirely. After the v2 AI Agent
> Discovery feature lands, the GUI's product value increases. Keep this doc as
> the "if we ever abandon the GUI" fallback audit; do not act on it as a
> migration plan.
```

Do not delete the body — the analysis is valuable contingency memory.

**Verification**: header block present; no body edits.

## Acceptance Criteria

- [ ] `src/avalonia-launcher/config/` directory deleted.
- [ ] AGENTS.md `docs/` tree includes both missing entries.
- [ ] `docs/research/wt-launcher-architecture-zero.md` has the contingency-record header.
- [ ] `git diff --check` PASS.
- [ ] `tools/verify.ps1` 18-check gate PASS.
- [ ] `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release` PASS.
- [ ] Commit message follows repo style (see `git log --oneline -5`).

## Out of Scope

- Cleaning `.trellis/.backup-*` directories (Trellis self-managed, gitignored).
- Reducing root `config/*.bak*` rotation (legitimate atomic-write byproduct).
- Any change to actual source code.

## Notes

- Estimated work: 15 minutes.
- No design.md / implement.md needed — PRD-only is correct for a trivial cleanup.
