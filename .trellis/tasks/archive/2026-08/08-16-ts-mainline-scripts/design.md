# Design: TS 主线与测试脚本

## Goal

把开发运行、主线验证、测试、覆盖率和 GUI 冒烟从 PowerShell 迁移到 TypeScript。旧 PS 保留到根门禁迁移完成，但每个命令提供 TS 入口和 parity 验证。

## Script Map

| PS | TS | npm script |
|---|---|---|
| `src/avalonia-launcher/run.ps1` | `scripts/run.ts` | `npm run start` |
| `src/avalonia-launcher/verify.ps1` | `scripts/verify-mainline.ts` | `npm run verify:mainline` |
| `tools/test-fast.ps1` | `scripts/test-fast.ts` | `npm run test:fast` |
| `tools/test-coverage.ps1` | `scripts/test-coverage.ts` | `npm run test:coverage` |
| `tools/test-launch-args.ps1` | `scripts/test-launch-args.ts` | `npm run test:launch-args` |
| `tools/smoke-critical-journey.ps1` | `scripts/smoke-critical-journey.ts` | `npm run smoke` |

## Module Contracts

### `run.ts`

- Locate repo root from `config/launcher.json` + `README.md` walking up from script dir.
- `dotnet run --project <app> --configuration Release -- --config <store>` with inherited stdio.

### `test-fast.ts`

- `dotnet test <solution> -c Release --no-restore --filter "Category=CoreUnit|Category=PropertyTest|Category=Architecture|Category=Analyzer"`.
- Optional `--logger` arg forwarded.
- Exit code and output match `tools/test-fast.ps1`.

### `test-coverage.ts`

- Read `tools/coverage-gate.json`.
- `dotnet test <solution> -c Release --no-restore --filter <manifest.filter> --collect "XPlat Code Coverage" --results-directory <coverage-dir>`.
- Parse newest `coverage.cobertura.xml`.
- Enforce `minLineRate` per critical path.
- Write `coverage-gate-receipt.json`.

### `test-launch-args.ts`

- `dotnet restore <solution> -r win-x64 -p:EnableNativeAot=true --locked-mode`.
- `dotnet test <tests> -c Release --no-restore --filter FullyQualifiedName~LaunchArgumentBuilder`.

### `smoke-critical-journey.ts`

- CLI: `--exe`, `--config-path`, `--working-directory`.
- Windows-only process smoke; create temp config when absent.
- Launch hidden, wait for main window, verify title contains `WT Launcher`, close gracefully.
- Emit `{ smoke, exe, config, checks }`.

### `verify-mainline.ts`

Parity with `src/avalonia-launcher/verify.ps1`:

1. Restore solution with `-r win-x64 -p:EnableNativeAot=true --locked-mode`.
2. Run tests and parse TRX counters.
3. Publish app to `artifacts/verify-publish/win-x64`.
4. Run contract validations (JSON required fields, save discard, TimeSpan literals).
5. Windows process smoke via `smoke-critical-journey.ts`.
6. Emit `avalonia-mainline-verify` receipt with same fields as PS.

## Out of Scope

- Root gate migration (P3).
- Removing PowerShell smoke helper (P4).
- Business/test logic changes.
