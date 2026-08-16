# Implement: 中央发布契约与 PS 迁移 TS

## Execution Order

### P0: Central Contract Baseline (child: central-release-contract)

1. `tools/build-release.ps1`
   - Add `[switch]$LockedMode`.
   - Restore: `dotnet restore <solution> -r win-x64 -p:EnableNativeAot=true --locked-mode`.
   - Publish: keep `-p:EnableNativeAot=true`, add `--no-restore`.
2. Add `tools/release-contract.ps1`.
   - Call `build-release.ps1 -LockedMode`.
   - Assert clean worktree.
   - Validate receipt and update manifest.
3. `.github/workflows/verify.yml`
   - Rename `aot-verify` to `release-contract`.
   - Remove `continue-on-error: true`.
   - Run `tools/release-contract.ps1`.
   - Upload artifacts.
4. `tools/verify.ps1`
   - Add `release-lockfile-consistency` check with `--locked-mode`.
   - Update check count to 44.
5. Update `AGENTS.md` baseline and docs.
6. Validation:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\release-contract.ps1`
   - `git diff --check`

### P1: TS Scripting Foundation (child: ts-scripting-foundation)

1. Add `package.json`, `tsconfig.json`, `.gitignore` entries for Node.
2. Add `.wt-protector.dsc.yaml` Node LTS requirement.
3. Add `scripts/config-lib.ts` and tests.
4. Add `scripts/build-release.ts` parity with PS.
5. Add `scripts/release-contract.ts`.
6. CI switches release job to `npm ci && npm run release:contract`.
7. Validation:
   - `npm run release:contract`
   - Compare old PS receipt with TS receipt on clean checkout.
   - `npm test` for script tests.

### P2: Mainline Scripts (child: ts-mainline-scripts)

1. Add `scripts/run.ts`.
2. Add `scripts/verify-mainline.ts`.
3. Add `scripts/test-fast.ts`, `scripts/test-coverage.ts`, `scripts/test-launch-args.ts`.
4. Add `scripts/smoke-critical-journey.ts`.
5. Validation:
   - `npm run verify:mainline`
   - `npm run test:fast`
   - `npm run smoke`

### P3: Root Gate Migration (child: ts-root-gate)

1. Add `scripts/verify.ts` check registry.
2. Migrate existing checks by category:
   - repo hygiene
   - mainline layout
   - docs and audit
   - config and schema
   - retirement
   - machine setup
   - Avalonia mainline
   - structure baseline
   - security and architecture
3. Add `scripts/checks/*.ts` and `scripts/tests/verify.test.ts`.
4. Update `AGENTS.md`, README, CLAUDE.md, docs references.
5. Validation:
   - `npm run verify`
   - Check names and receipt match old PS gate.

### P4: Auxiliary Scripts and Cleanup (child: ts-aux-cleanup)

1. Migrate `apply-machine-config.ts`, `generate-icon.ts`, `package-friend.ts`, `update-github-similar-projects-snapshot.ts`, `add-test-category-traits.ts`, `layer-test-directories.ts`.
2. Add `.NET` self-contained uninstaller for `install-portable.ts`.
3. Delete or archive all active `.ps1`.
4. Update all docs and CI to remove PowerShell calls.
5. Final validation:
   - `npm run verify`
   - `npm run release:contract`
   - `rg -n "powershell|pwsh|\.ps1" .github tools scripts src/avalonia-launcher -g "!**/node_modules/**"` only expected runtime references.

## PR Split

- PR 1: P0 central contract.
- PR 2: P1 TS foundation and release contract.
- PR 3: P2 mainline scripts.
- PR 4: P3 root gate.
- PR 5: P4 auxiliary scripts and cleanup.

## Rollback Points

- Before each PR: old PS remains.
- After PR 1: release contract is green.
- After PR 2: TS release contract is parity with PS.
- After PR 3-4: each script group has parity tests.
- After PR 5: active PS removed only after full TS gate green.

## Risks

- `verify.ps1` migration is the largest risk; must be category-by-category.
- Node becomes a new prerequisite; must be added to WinGet config and CI.
- Generated uninstaller must not require Node.
- Dual-track drift; each phase must close with a single authority.
