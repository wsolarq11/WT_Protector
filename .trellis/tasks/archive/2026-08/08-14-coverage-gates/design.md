# 覆盖率与质量门禁 - Design

## 方案

- 用 `dotnet test --collect:"XPlat Code Coverage"` 生成 Cobertura。
- `tools/test-coverage.ps1` 解析 Core 和 App 的 line/sequence 覆盖率。
- 阈值写入脚本：Core line >= 70%，App line >= 50%（以首次实测为准调整）。
- 全仓报告只打印，不设 100%。
