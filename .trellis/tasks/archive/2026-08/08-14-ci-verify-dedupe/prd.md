# CI 与 verify 去重 - PRD

## Goal

避免 CI 重复执行全量测试，保持完整验收只跑一轮。

## Requirements

- `build-and-test` 只跑快速层。
- `full-gate` 是唯一全量验收阶段。
- 保留 TRX/coverage artifact。

## Acceptance

- workflow 日志中全量测试只跑一轮。
- 本地 `tools/verify.ps1` 仍为完整验收。
