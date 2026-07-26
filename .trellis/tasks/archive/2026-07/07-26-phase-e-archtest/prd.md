# Phase E: 架构强制增强

## 根因

当前架构规则只在 CI `dotnet test` 时执行（反馈延迟 >10 分钟）。verify.ps1 有一个安全门禁路径指向不存在位置。

## 实施列表

### E1: verify.ps1 安全审计门禁路径修复
- `verify.ps1:612`：`ValueObjects\DirectoryPath.cs` → `DirectoryPath.cs`
- `verify.ps1:652`：注释中的 `ValueObjects\EntryId.cs` → `EntryId.cs`

### E2: verify.ps1 架构违规预检查组
- 添加 `dotnet test --filter "FullyQualifiedName~ArchitectureRules"` 快速检查
- 架构违规在本地验证时即被发现，无需等到 CI

### E3: verify.ps1 分析器接线验证
- 构建后检查分析器是否实际加载
- 与 Phase A 的 Analyzer 引用修复配套

### E4: NetArchTest 迁移评估
- 评估 `ArchitectureRules.cs` 从 ArchUnitNET 迁移到 `NetArchTest.Rules`
- NetArchTest 更轻量，nuget 引用即用，API 更贴近 C#
- 迁移仅在性能优化必要时执行

## 验收标准
- [ ] verify.ps1 安全审计 F1/F3 路径正确
- [ ] verify.ps1 在架构违规时提前阻断
- [ ] `powershell -File tools\verify.ps1` 通过
- [ ] `dotnet build -warnaserror` 通过
