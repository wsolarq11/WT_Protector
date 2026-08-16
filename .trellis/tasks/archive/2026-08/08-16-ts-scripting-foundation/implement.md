# Implement: TS 脚本系统骨架

## Execution Order

1. Add `package.json`, `tsconfig.json`, `.gitignore` entries for Node (`node_modules/`, `*.tsbuildinfo`).
2. Add `.wt-protector.dsc.yaml` Node LTS resource.
3. Add `scripts/config-lib.ts`.
4. Add `scripts/build-release.ts`.
5. Add `scripts/release-contract.ts`.
6. Add `scripts/tests/config-lib.test.ts`, `scripts/tests/build-release.test.ts`, `scripts/tests/release-contract.test.ts` covering hash/version/validation/status helpers.
7. Add npm scripts:
   - `npm run release:build`
   - `npm run release:contract`
   - `npm test`
   - `npm run typecheck`
8. Switch CI `release-contract` job to `npm ci && npm run release:contract`, with `actions/setup-node`.
9. Update `AGENTS.md`, `README.md`, `CLAUDE.md`, `src/avalonia-launcher/README.md` command references.
10. Validation:
   - `npm ci`
   - `npm run typecheck`
   - `npm test`
   - `npm run release:contract` in a clean worktree
   - Compare TS receipt with PS receipt contract fields.

## Parity Checks

- `npm run release:build` must not rewrite `packages.lock.json`.
- `npm run release:contract` must report clean worktree.
- TS receipt must contain same required fields as PS receipt.
- CI job must be required, no `continue-on-error`.

## Rollback

- Keep `tools/release-contract.ps1` until TS release contract passes on a clean checkout.
- Any mismatch returns to PowerShell authority; do not remove PS scripts.
