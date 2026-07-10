# WinGet Configure Machine-Ready Layer

Parent: `07-10-v2-agent-discovery-and-hygiene`

## Goal

Add an optional, declarative **machine-ready configuration** layer to WT Launcher via WinGet Configuration + PowerShell DSC v3. A single `.wt-protector.dsc.yaml` file declares the prerequisites a fresh Windows machine needs to be "WT-Launcher-ready" (Windows Terminal, PowerShell 7, optional agent CLIs). Running `winget configure` against it sets the machine to the declared state.

This is **complementary**, not a replacement for the GUI launcher. It addresses a different concern: "I'm on a new machine — how do I get the same workspace setup?"

## Background

Per [Microsoft Build 2025 BRK228](https://www.youtube.com/watch?v=C3JQiQb7k60) and [Microsoft Learn WinGet Configuration docs](https://learn.microsoft.com/en-us/windows/package-manager/configuration), WinGet Configuration is the official Microsoft declarative setup story:

- YAML-formatted `*.dsc.yaml` or `*.winget` files
- Backed by PowerShell DSC v3
- Single `winget configure` command installs + configures
- `winget configure -export` produces a YAML from current machine state
- Microsoft's stated direction for "ready-to-code" dev environment setup

## Requirements

### R1 — `.wt-protector.dsc.yaml` artifact

A new file at repo root declaring:
- **Required**:
  - `Microsoft.WindowsTerminal` (stable)
  - `Microsoft.PowerShell` (7.x)
- **Optional** (commented out by default, user enables):
  - `Anthropic.ClaudeCode` (if winget-published) — TBD, claude is currently `irm | iex` install
  - `OpenAI.Codex` — TBD
  - `sbssts.opencode` or equivalent
  - other catalog agents as winget packages become available
- **Properties**:
  - `schemaVersion` per DSC v3
  - `properties: <registry of resources>`
  - Each resource declares `assert/extract/apply` semantics inline (idempotent)

### R2 — Optional Companion script `tools/apply-machine-config.ps1`

Thin wrapper:
```powershell
param([switch]$DryRun)
winget configure --file .\.wt-protector.dsc.yaml $(if ($DryRun) { '--validate' })
```

User runs `.\tools\apply-machine-config.ps1` on a fresh machine. No silent install — user sees every step and confirms interactively.

### R3 — Documentation

Update `README.md` with a new section "Fresh-machine setup":
- When to use WinGet Configuration vs the launcher
- Quick start: `winget configure --file .wt-protector.dsc.yaml`
- How to extend with additional agents (commented examples)
- Relation to Agent CLI Discovery (sibling task): WinGet configure installs the agent; Agent Discovery then offers it as a launch entry

### R4 — Schema validation

Add a verify.ps1 check `winget-config-schema-valid` that runs `winget configure --validate --file .wt-protector.dsc.yaml` on Windows hosts (skipped elsewhere). Catches YAML syntax errors before commit.

## Acceptance Criteria

- [ ] `.wt-protector.dsc.yaml` exists at repo root, schema-valid.
- [ ] `tools/apply-machine-config.ps1` works (DryRun mode at minimum).
- [ ] README.md has the "Fresh-machine setup" section.
- [ ] `tools/verify.ps1` gains the `winget-config-schema-valid` check (Windows-only, skipped on others).
- [ ] All 18 prior gate checks stay green.
- [ ] `dotnet test` unchanged (no test impact).
- [ ] No new dependency added to the C# solution (this is purely additive infra).
- [ ] AGENTS.md repo map mentions the new file + script.

## Out of Scope

- **Auto-discovery of winget package IDs for every agent** — too volatile; users add their own.
- **CI matrix testing on a clean Windows image** — future scope; for now, dry-run validation is sufficient.
- **Replacing the GUI launcher** — explicitly out of scope. This task is the **auxiliary setup layer**, not an alternative architecture.
- **Cross-platform (Linux/macOS)** — WinGet Configuration is Windows-only by design; the launcher itself remains Windows-focused.

## Notes

- Estimated work: 1-2 days.
- This task is **P3 / optional** — only do it if the v2 feature work (sibling) lands on time. The launcher's primary value does not depend on this.
- If winget package IDs for major agents (claude/codex) are not yet stable, ship with Terminal+PowerShell only and leave the agent section as commented examples.
- Per ponytail: do not build a custom DSC resource — use only built-in `WinGetPackage` and `WinGetUserSettings` resources.
