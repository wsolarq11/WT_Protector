# Stale three-file strings remediation + verify.ps1 gate

## Goal
Clean up all stale `config.json` / `presets.json` / `settings.json` references that misrepresent the current single-file `launcher.json` architecture, AND add a permanent negative gate in `tools/verify.ps1` to prevent future regressions.

## Background
- Commit e3b5658 (stale-strings fix) cleaned some references but missed several
- Commit a3a4c02 (migration comment fix) addressed store.json→launcher.json
- Recurring regression: docs say "three-file architecture" while code uses single launcher.json
- pitfalls §2.10: "文档同步是代码变更的必要组成部分"

## Stale files to fix (5 files, ~33 occurrences)
1. **AGENTS.md L13-15** — describes active config as three files
2. **DESIGN.md L100** — `settings.json 的 searchHistory`
3. **docs/feature-reality-north-star-audit.md** — 17 occurrences across Scope section + feature owner rows
4. **docs/feature-first-class-upgrade-audit.md** — 6 occurrences
5. **docs/giants-shoulders/launcher-first-class-patterns.md** — 6 occurrences

## Legitimate references (KEEP — do NOT modify)
- `retired/` — describes retired code, accurate
- `.trellis/spec/guides/pitfalls.md` — historical pitfalls with session-ids, frozen
- `.trellis/spec/core/persistence.md` MigrateFromLegacyFiles section — describes actual migration code path
- `.trellis/spec/core/error-handling.md` — already explicitly says "previous three-file is GONE"
- `.trellis/tasks/archive/` — historical task artifacts, frozen
- Windows Terminal's own `settings.json` references (different system)
- VS Code `.vscode/settings.json` (different system)

## verify.ps1 negative gate
Add to `tools/verify.ps1` (after the existing positive string checks):

```powershell
# Negative gate: docs must not reference the abandoned three-file architecture
$activeDocs = @(
    "README.md", "AGENTS.md", "DESIGN.md",
    "docs/feature-reality-north-star-audit.md",
    "docs/feature-first-class-upgrade-audit.md",
    "docs/giants-shoulders/launcher-first-class-patterns.md"
)
foreach ($doc in $activeDocs) {
    if (-not (Test-Path $doc)) { continue }
    $content = Get-Content $doc -Raw
    # Match config/presets/settings.json as a PORTABLE CONFIG reference (not retired/, not Windows Terminal's own settings.json)
    if ($content -match '(?m)^.*config[\\/](config|presets|settings)\.json.*$' -or
        $content -match '(?m)^.*\b(presets|settings)\.json\s+(的|references|owns|→|provides|provides|with|owns|in)\b.*$') {
        throw "Stale three-file reference in $doc (use launcher.json)"
    }
}
```

The regex must be precise enough to not match:
- `retired/config/config.json` (legitimate retired code path)
- Windows Terminal's `settings.json` (different system)
- pitfalls.md historical entries (skipped from active docs list)

## Acceptance Criteria
- [ ] 5 stale files have all `config/presets/settings.json` references replaced with `launcher.json` (or qualified as legacy if describing migration)
- [ ] `tools/verify.ps1` includes negative gate
- [ ] `tools/verify.ps1` passes on cleaned main
- [ ] `dotnet build -c Release` 0 warnings 0 errors
- [ ] `dotnet test -c Release` 451+ tests pass
- [ ] Manual sanity: re-running verify.ps1 after re-introducing a stale reference FAILS (negative gate works)

## Out of scope
- Rewriting feature audit docs to reflect 2026-07 architecture changes beyond file-name fixes (separate documentation task if needed)
- Removing `retired/` directory contents (decision: keep for historical reference)
