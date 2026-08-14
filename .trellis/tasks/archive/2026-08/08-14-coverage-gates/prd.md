# 覆盖率与质量门禁 - PRD

## Goal

建立可执行的关键路径覆盖率门禁。

## Requirements

- CI/本地能生成覆盖率报告。
- Core 和持久化关键路径有阈值。
- 全仓覆盖率只报告不强制。

## Acceptance

- `tools/test-coverage.ps1` 能产出报告。
- 报告包含 Core/Persistence 关键路径数据。
- 阈值低于设定时脚本非零退出。
