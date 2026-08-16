# TS 主线与测试脚本

## Goal

把 Avalonia mainline 的开发运行、验证、测试、覆盖率与冒烟脚本迁移到 TypeScript。

## Requirements

- `src/avalonia-launcher/run.ps1` 迁移为 `scripts/run.ts`。
- `src/avalonia-launcher/verify.ps1` 迁移为 `scripts/verify-mainline.ts`。
- `tools/test-fast.ps1`、`tools/test-coverage.ps1`、`tools/test-launch-args.ps1`、`tools/smoke-critical-journey.ps1` 迁移为 TS。
- 每个迁移脚本提供 parity 验证。

## Acceptance Criteria

- [ ] `npm run start` 等价于 `run.ps1`。
- [ ] `npm run verify:mainline` 等价通过 restore、test、publish、smoke。
- [ ] `npm run test:fast`、`npm run test:coverage`、`npm run test:launch-args`、`npm run smoke` PASS。
- [ ] 新旧脚本对同一仓库状态产生相同 exit code 和关键输出。

## Out of Scope

- 根门禁迁移。
- 测试业务逻辑改动。
