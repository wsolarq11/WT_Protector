# 测试层级标记与快速反馈 - PRD

## Goal

让测试可按层级独立运行，CI 和本地都有快速反馈路径。

## Requirements

- 所有测试类带 `[Trait("Category", ...)]`。
- 新增 `tools/test-fast.ps1`。
- CI `build-and-test` 改为快速层。

## Acceptance

- `dotnet test --filter Category=CoreUnit` 等层级可独立运行。
- 快速层不包含 ViewModelBehavior/Persistence/UiContract。
- 全量测试数量不减少。
