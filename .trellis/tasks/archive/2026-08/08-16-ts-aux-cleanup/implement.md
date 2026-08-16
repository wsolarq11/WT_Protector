# TS 辅助脚本与 PS 清理 — Implement

## Steps

1. Add `tools/smoke-probe` .NET helper and switch `scripts/smoke-critical-journey.ts` to it.
2. Add `tools/uninstaller` .NET self-contained project and `scripts/install-portable.ts`.
3. Migrate `apply-machine-config`, `package-friend`, `update-github-similar-projects-snapshot`, and `generate-icon` to TS.
4. Add npm scripts and dependencies (`archiver`, `@resvg/resvg-js`, `png-to-ico`).
5. Add `no-active-powershell-scripts` gate check and update structure baseline to 45.
6. Archive all active `.ps1` under `retired/scripts/`.
7. Update README, AGENTS, CLAUDE, src README, DESIGN, active specs, and distribution docs to npm/dotnet commands.
8. Run `npm run verify`, `npm run release:contract` from a clean worktree, and confirm active `.ps1` count is zero.

## Validation

- `npm run smoke` returns `launch+title` without PowerShell.
- Temp install with `--skip-shortcuts` installs `uninstaller.exe`; running it removes the install directory and leaves no temp copy.
- `npm run package:friend` produces a zip.
- `npm run verify` passes 45 checks.
- `npm run release:contract` passes from a clean worktree.
