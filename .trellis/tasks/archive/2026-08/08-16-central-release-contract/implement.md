# Implement: 中央发布契约

## Checklist

1. Update `tools/build-release.ps1`
   - Add `param([switch]$LockedMode)`.
   - Restore uses `--locked-mode` when `$LockedMode`.
   - Publish adds `--no-restore`.
2. Add `tools/release-contract.ps1`
   - Run `build-release.ps1 -LockedMode`.
   - Assert clean worktree via `git status --porcelain --untracked-files=all`.
   - Parse `receipt.json`; require `kind=release-build`, `status=PASS`, valid `publishedExe`, `bytes`, `sha256`.
   - Parse `update-manifest.json`; require required fields.
3. Update `.github/workflows/verify.yml`
   - Rename job to `release-contract`.
   - Remove `continue-on-error: true`.
   - Run release contract.
   - Upload publish folder and receipt.
4. Update `tools/verify.ps1`
   - Add `release-lockfile-consistency` Add-Check.
   - Run `dotnet restore <solution> -r win-x64 -p:EnableNativeAot=true --locked-mode`.
   - Update check count from 43 to 44.
5. Update `AGENTS.md`
   - Gate checks row 43 to 44.
   - Document `release-contract.ps1`.
6. Update docs: README, CLAUDE.md, src README.

## Validation

- Close any running `WTLauncher.App.exe`.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\release-contract.ps1`.
- Run `git diff --check`.
- Confirm `git status --porcelain --untracked-files=all` is clean after release contract.

## Rollback Points

- Before step 5: remove `release-contract.ps1` and revert workflow only if needed.
- Lockfile and restore alignment are baseline fixes and should not be rolled back.
