# TS 辅助脚本与 PS 清理 — Design

## Change Boundary

- Migrate remaining PowerShell tooling to Node/TypeScript under `scripts/`, retaining .NET helpers only where Windows process/window/install behavior requires them.
- Replace `install-portable.ps1` with `scripts/install-portable.ts` plus a .NET self-contained `tools/uninstaller`; the installer no longer writes `uninstall.ps1`.
- Replace GUI smoke's PowerShell wrapper with `tools/smoke-probe` (a small .NET console helper launched by `scripts/smoke-critical-journey.ts`).
- Archive every active `.ps1` under `retired/scripts/`; active tooling paths contain zero PowerShell scripts.
- Keep PowerShell 7 as the product runtime shell only. Repository commands and CI use npm/dotnet.

## Decisions

- `runSmoke` creates the temp config in TS and invokes `dotnet run --project tools/smoke-probe`; no `powershell` invocation remains in mainline verification.
- `install-portable.ts` builds `tools/uninstaller` as a self-contained single-file exe, copies it as `uninstaller.exe`, and creates shortcuts through the same .NET helper's `--create-shortcut` mode.
- Uninstaller backs up `config`, removes Start Menu/Desktop shortcuts, deletes the install directory, and uses a detached cleanup process only for self-deletion of its temp copy.
- `package-friend.ts` uses `archiver`; `update-github-similar-projects-snapshot.ts` ports the existing `gh repo view` flow; `apply-machine-config.ts` wraps `winget configure`.
- `generate-icon.ts` ports the SVG/PNG/ICO generation path with an optional `--out` for safe regeneration checks.

## Out of Scope

- Changing the product launch shell.
- Cross-platform installers.
- Rewriting historical/archive documentation or research snapshots.
