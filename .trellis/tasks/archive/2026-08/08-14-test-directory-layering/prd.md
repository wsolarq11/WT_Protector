# 测试目录命名空间分层 - PRD

## Goal

在单一测试项目内建立物理分层，让层级不再依赖人工记忆。

## Requirements

- 测试文件移入 `CoreUnit/ Persistence/ ViewModelBehavior/ Analyzer/ Architecture/ UiContract/ PropertyTest/ Support/`。
- 每个目录使用对应命名空间。
- 不改变被测代码和测试行为。

## Acceptance

- 目录与 `docs/test-structure.md` 一致。
- 全量测试通过，数量不减少。
- `tools/verify.ps1` PASS。
