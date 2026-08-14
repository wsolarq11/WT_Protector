# 测试地图与基线 - PRD

## Goal

建立测试分类、目录规则和基线数据，作为后续所有分层工作的单一事实来源。

## Requirements

- 定义 `CoreUnit / Persistence / ViewModelBehavior / Analyzer / Architecture / UiContract / PropertyTest`。
- 生成 `docs/test-structure.md`。
- 记录测试文件数、测试数、全量耗时。

## Acceptance

- 每个现有测试文件都能归入一个分类。
- 文档含分类定义、文件归属规则、基线数据。
- `tools/verify.ps1` 仍 PASS。
