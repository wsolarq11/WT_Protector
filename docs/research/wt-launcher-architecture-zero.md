# Architecture Zero — Eliminate the Launcher Entirely

> **Status (2026-07-10): Contingency record, not an active recommendation.**
> This document explores eliminating the GUI entirely. After the v2 AI Agent
> Discovery feature lands (parent task `07-10-v2-agent-discovery-and-hygiene`),
> the GUI's product value increases. Keep this doc as the "if we ever abandon
> the GUI" fallback audit; do not act on it as a migration plan.

> Research result: "深度全网deepsearch调研出新架构来剪掉一切问题的源头"
> 2026-07-06
>
> **Verification (2026-08-15)** — `.trellis/tasks/08-15-docs` protocol. Dates confirmed via git
> (`61c6bc64` 2026-07-06 created; `2af3be2d` 2026-07-10 repositioned as contingency). This is a dated
> fallback-plan record — per AGENTS.md it is research only, NOT an action plan; the Options A/B/C
> proposal below is kept intact. Verified / corrected facts:
> - ✅ "1 solution, 3 projects, 1 CI pipeline" — measured at `61c6bc64` (WTLauncher.Avalonia.sln,
>   WTLauncher.App/Core/Tests csproj, `.github/workflows/verify.yml`).
> - ⚠️ Source/test line counts and test counts corrected to measurements at `61c6bc64`
>   (≈7,981 non-test .cs lines; ≈7,509 test .cs lines; 63 test files; 418 `[Fact]`/`[Theory]`
>   attributes; 6 PackageReference declarations) — see inline edits.
> - ⚠️ The "19 entries" launcher.json claim is unverifiable at its date: no tracked
>   `config/launcher.json` existed at `61c6bc64` (the tracked config then was
>   `retired/config/config.json`, an 18-line flat structure). As of 2026-08-15 the tracked
>   `config/launcher.json` has 12 entries and 2 presets (incl. `core-projects`).
> - Windows Terminal capability table and `wt.exe` examples: external product facts researched
>   2026-07-06; not verifiable from this repo — kept as dated research.


## The Root Cause

The WT Protector project is a C#/Avalonia GUI application (measured at commit `61c6bc64`, 2026-07-06: ≈7,981 non-test .cs lines, ≈7,509 test .cs lines, 6 PackageReference declarations, 1 solution, 3 projects, 1 CI pipeline) whose **sole function** is to construct `wt.exe` command lines — something Windows Terminal already does natively.

Every bug, every over-engineering pattern, every architecture debate, every UltraReview round — they all stem from one root decision:

> **Building a GUI framework around a command-line task.**

## Discovery: Windows Terminal Already Ships This Feature

| Capability | WT Launcher | Windows Terminal Native |
|---|---|---|
| Launch tabs in different directories | ✅ | ✅ `wt -d <dir>` |
| Multiple tabs in one window | ✅ | ✅ `wt new-tab -d <dir>` |
| Split panes | ❌ | ✅ `wt split-pane` |
| Profiles that remember directories | ❌ | ✅ `profiles[].startingDirectory` |
| Startup workspace layout | ❌ | ✅ `startupActions` in settings.json |
| Named windows | ❌ | ✅ `wt -w <name>` |

### Proof: The Entire Config Is a Single `startupActions` String

The `launcher.json` of the time had 19 entries — unverifiable at its date: no tracked `config/launcher.json` existed at commit `61c6bc64` (the tracked config then was `retired/config/config.json`, an 18-line flat structure). Measured 2026-08-15: `config/launcher.json` has 12 entries and 2 presets. For the "core-projects" preset (2 entries at the time):

```powershell
wt -w "core-projects" `
  -d "C:\Users\<USER>\Desktop\启动脚本" pwsh -NoExit -Command "codex --enable goals --dangerously-bypass-approvals-and-sandbox" `
  ; new-tab -d "D:\<PROJECT_ROOT>\WT_Protector" pwsh -NoExit -Command "opencode"
```

This single `wt.exe` command replaces the entire C# launcher for the user's most-used workflow.

### Windows Terminal `startupActions` (Zero-Code Solution)

Add to `%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json`:

```json
{
  "startupActions": "-p \"PowerShell\" -d \"D:\\<USER>\\Desktop\\启动脚本\" pwsh -NoExit -Command \"codex --enable goals --dangerously-bypass-approvals-and-sandbox\" ; new-tab -p \"PowerShell\" -d \"D:\\<PROJECT_ROOT>\\WT_Protector\" pwsh -NoExit -Command \"opencode\""
}
```

No external tool. No build. No dependencies.

## Proposed Architecture: Three Options

### Option A: Windows Terminal Profiles Only (Zero Code)

Map each launcher entry to a Windows Terminal profile in `settings.json`.

**Pros:** Zero code to write, zero maintenance, zero bugs.
**Cons:** No dynamic batch launch (select entries at runtime), no template variables, no GUI.

**Config impact:** Each launcher entry becomes a WT profile:
```json
{
  "profiles": {
    "list": [
      {
        "name": "启动脚本",
        "commandline": "pwsh -NoExit -Command \"codex --enable goals --dangerously-bypass-approvals-and-sandbox\"",
        "startingDirectory": "D:\\<USER>\\Desktop\\启动脚本"
      }
    ]
  }
}
```

Then launch with: `wt -p "启动脚本"`

**Files removed:** Entire `src/`, `tools/`, `config/`, `tests/` directories.

### Option B: PowerShell Script Launcher (~80 lines)

A single `launch.ps1` that reads `launcher.json` and constructs `wt.exe` commands.

```powershell
param([string]$Preset = "core-projects")

$config = Get-Content "config/launcher.json" | ConvertFrom-Json
$preset = $config.presets | Where-Object { $_.id -eq $Preset }
$entries = $config.entries | Where-Object { $preset.entryIds -contains $_.id }

$wtArgs = @()
foreach ($entry in $entries) {
    $cmd = $config.settings.commandTemplates."$($entry.command -replace '^\$','')"
    $wtArgs += "-d `"$($entry.directory)`" pwsh -NoExit -Command `"$cmd`""
    if ($wtArgs.Count -gt 1) { $wtArgs += ";" }
}

Start-Process -NoNewWindow wt.exe -ArgumentList ($wtArgs -join " ")
```

**Files kept:** `config/launcher.json` (config), `launch.ps1` (script).
**Files removed:** Entire `src/`, `tools/`, `tests/` directories.

### Option C: Minimal .NET Console App (~200 lines)

If template expansion or multi-select batch launch is essential, a console app without a GUI framework:

- Single `.cs` file
- No Avalonia, no XAML, no NuGet
- Reads JSON, prints `wt.exe` command line
- Pipe to `Invoke-Expression` or `Start-Process`

**Dependencies:** .NET runtime only (same as current). No Avalonia, LiteDB, or CommunityToolkit.

## What Gets Eliminated

| Artifact | Lines | Complexity Source |
|---|---|---|
| Avalonia UI framework | ~8,000 (src) | XAML, views, code-behind, data binding |
| ViewModel layer | ~4,000 | MVVM, partial files, observable collections |
| Value objects (EntryId etc.) | ~500 | Validation, normalization, Result<T,E> wrapper |
| Persistence (JSON + LiteDB) | ~1,000 | 2 backends, migration, snapshot/rollback |
| Build system | ~200 | csproj, solution, NuGet restore, CI |
| Tests | ≈7,509 (measured at 61c6bc64) | xUnit, 418 [Fact]/[Theory] attributes, 63 test files (measured at 61c6bc64) |
| Total | ≈15,490 (measured: 7,981 src + 7,509 tests) | All of it |

> Row-level line counts other than Tests are rough 2026-07-06 estimates and were not individually
> verified; measured totals at commit `61c6bc64`: 7,981 non-test .cs lines, 7,509 test .cs lines.

## What Remains

| Artifact | Lines | Purpose |
|---|---|---|
| `config/launcher.json` | ~100 | Entry definitions (already exists) |
| `launch.ps1` | ~80 | Read config + call wt.exe |
| **Total** | **~180** | **99% reduction** |

## Test Strategy for the Replacement

The C# launcher at the time (2026-07-06; measured at commit `61c6bc64`: 418 `[Fact]`/`[Theory]` attributes across 63 test files) had tests verifying:
- Entry ID normalization (testing string parsing)
- Batch launch preflight logic (testing selection + command construction)
- Drag reorder (testing in-memory list reordering)
- Persistence snapshot/rollback (testing file I/O error handling)
- Layout calculation (testing card sizing math)

In Option B (PowerShell script), these tests are replaced by:
- **Integration test:** Run the script, verify `wt.exe` starts with correct arguments
- **Corner cases:** Test the JSON config parser separately (1 PowerShell test)
- **Everything else:** Cannot fail because there's no code to fail.

In Option A (Windows Terminal profiles only), the "test" is:
- Does Windows Terminal start with the expected tabs? (manual verification, once)

## Risk Assessment

| Risk | Mitigation |
|---|---|
| Template variable expansion (`$codex` → full command) | Handled by Option B (PowerShell substitution) or Option A (profile commandline) |
| Dynamic multi-entry selection at runtime | Option B with `-Preset` parameter; Windows Terminal's `startupActions` covers fixed workspaces |
| Migration from current config format | Config already exists; just need to transform it |
| Loss of search/filter/visual card UI | These are features of a launcher, not of launching. If they're essential, keep the C# app. But question: how often do you search when you have 19 entries and 2 presets? (entry count unverifiable at the date — no tracked launcher.json at `61c6bc64`; measured 2026-08-15: 12 entries / 2 presets) |
