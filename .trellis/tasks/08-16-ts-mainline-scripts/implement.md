# Implement: TS 主线与测试脚本

## Execution Order

1. Add `scripts/run.ts`.
2. Add `scripts/test-fast.ts`.
3. Add `scripts/test-launch-args.ts`.
4. Add `scripts/smoke-critical-journey.ts`.
5. Add `scripts/test-coverage.ts` with coverage XML parser.
6. Add `scripts/verify-mainline.ts` with TRX/coverage/contract checks.
7. Add npm scripts: `start`, `verify:mainline`, `test:fast`, `test:coverage`, `test:launch-args`, `smoke`.
8. Add Vitest coverage for XML/TRX helpers.
9. Validation:
   - `npm run typecheck`
   - `npm test`
   - `npm run verify:mainline` in a clean worktree
   - `npm run test:fast`
   - `npm run smoke` with a published exe.

## Parity Checks

- `npm run verify:mainline` receipt matches `avalonia-mainline-verify` schema.
- `npm run test:coverage` fails on critical path below `minLineRate`.
- `npm run start` uses canonical `config/launcher.json`.
- PowerShell scripts remain until P3/P4; no deletion in this task.
