# TS 根门禁迁移

## Goal

把 `tools/verify.ps1` 的 44 项 `Add-Check` 按模块迁移为 TypeScript 根门禁，保持行为等价。

## Requirements

- 新增 `scripts/verify.ts` check registry。
- 按类别迁移：repo hygiene、mainline layout、docs/audit、config/schema、retirement、machine setup、Avalonia mainline、structure baseline、security/architecture。
- 每个迁移组有 Vitest parity 测试。
- 更新 AGENTS.md、README、CLAUDE.md 和 docs 中的命令与基线。

## Acceptance Criteria

- [ ] `npm run verify` 与原根门禁检查名一致。
- [ ] receipt schema 一致。
- [ ] 44 项检查全部迁移并有测试锚点。
- [ ] `tools/verify.ps1` 不再作为权威入口。

## Out of Scope

- 修改业务功能。
- 新增业务门禁语义。
