# Phase 1: 清理 + 序列化修复

## Goal

删除所有废弃架构残留（Fluxor、ExceptionBoundary 等死代码），修复序列化策略实现前向兼容。

## Requirements

### R1 死代码清理
- [ ] 删除 Fluxor NuGet 包包版本定义 (`Directory.Packages.props`)
- [ ] 删除 Fluxor PackageReference (`WTLauncher.App.csproj`)
- [ ] 删除 Fluxor 初始化代码 (`App.axaml.cs` ~20 行)
- [ ] 删除 `Infrastructure/Store/` 目录 (4 文件: LauncherActions, LauncherEffects, LauncherReducers, LauncherState)
- [ ] 删除 `Features/` 下 Fluxor 相关文件 (LayoutState, LayoutActions, LayoutReducers, ServiceResult 等)
- [ ] 删除 ExceptionBoundary 类 (4 方法 0 调用)
- [ ] 删除 LauncherIcons 类 (5 图标 0 引用)
- [ ] 删除 StoreReducers 文件 (Fluxor 配套，0 调用)
- [ ] 删除 Fluxor.Blazor.Web 包版本 (`Directory.Packages.props`)
- [ ] 确保所有 `using Fluxor;` 引用被移除
- [ ] 验证: gate 通过, 410+ 测试通过

### R2 序列化修复
- [ ] 拆分 `LauncherJson.Options` 为 `LooseOptions` (读) + `StrictOptions` (写)
- [ ] 从 `LauncherStoreFile` 移除 `[JsonUnmappedMemberHandling(Disallow)]`
- [ ] 从 `LauncherJsonContext` 移除 `UnmappedMemberHandling`
- [ ] 添加 `MinimumSupportedSchemaVersion` 检查
- [ ] 添加前向兼容测试 (未知字段静默跳过)
- [ ] 添加下界版本拒绝测试
- [ ] 验证: gate 通过, 新旧流程等价

## Acceptance Criteria

- [ ] `git grep "Fluxor"` 无匹配
- [ ] `ExceptionBoundary` 类删除, 无引用残留
- [ ] `LauncherIcons` 类删除, 无引用残留
- [ ] 序列化读使用 LooseOptions (Skip unmapped), 写使用 StrictOptions (Disallow)
- [ ] `verify.ps1` 全 PASS
- [ ] 所有 410+ 测试 PASS

## References

详细设计见 `../07-23-arch-deep-research/research/10-synthesis.md`
