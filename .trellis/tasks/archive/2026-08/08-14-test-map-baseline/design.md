# 测试地图与基线 - Design

## 方案

- 用文件清单 + 文件名规则建立分类映射。
- `docs/test-structure.md` 包含分类、目录、判定规则、基线表。
- 基线由 PowerShell 收集：文件数、`[Fact]/[Theory]` 数量、`dotnet test` 耗时。

## 边界

- 只新增文档和基线数据，不改测试代码。
