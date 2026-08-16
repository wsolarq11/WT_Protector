# Design: TS 根门禁迁移

## Goal

把 `tools/verify.ps1` 的 44 项检查迁移为 `scripts/verify.ts` + `scripts/checks/*`，并让 `npm run verify` 成为 CI full-gate 权威入口。旧 PS 根门禁保留到 P4 清理，但改为调用 TS mainline/coverage 做双轨校验。

## Module Contracts

| Module | Responsibility |
|---|---|
| `scripts/checks/common.ts` | check collection, JSON helpers, file walking, git, native command execution |
| `scripts/checks/static.ts` | repo policy, docs/claims, code/architecture scans, installer/update contracts |
| `scripts/checks/schema.ts` | GitHub snapshot, launcher/config/example/shortcut schema |
| `scripts/checks/run.ts` | locked restore, TS mainline verify, coverage gate, architecture rules, analyzer wiring, structure baseline |
| `scripts/verify.ts` | ordered orchestration of all 44 checks and root receipt |

## Contract

- `npm run verify` must emit a `root-verify` receipt with the same `kind`, `root`, and 44 `checks` shape as `tools/verify.ps1`.
- `structure-baseline-consistency` must stay the last check and self-count 44.
- CI `full-gate` runs `npm ci && npm run verify`; `build-and-test` runs `npm run test:fast`.
- Retained `tools/verify.ps1` must still pass and call `npm run verify:mainline` plus `npm run test:coverage -- --no-build`.

## Out of Scope

- Removing `tools/verify.ps1` or remaining PowerShell smoke/installer helpers (P4).
- Renaming existing PS scripts.
