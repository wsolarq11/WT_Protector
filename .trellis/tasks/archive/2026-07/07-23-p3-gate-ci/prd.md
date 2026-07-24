# Phase 3: 门禁加固 + CI 自动化

## Goal

在 Phase 1-2 的基础上，完成剩余的 CI/QA 自动化任务：修复 nuget 审计、添加 Dependabot、更新架构文档。

## Requirements

### 1. NuGet 审计修复
- CI 中的 `nuget-audit` job 当前仅运行 `dotnet restore`，未执行实际审计
- 添加 `dotnet nuget audit src/avalonia-launcher/WTLauncher.Avalonia.sln` 命令

### 2. Dependabot 配置
- 添加 `.github/dependabot.yml`，启用 NuGet 和 GitHub Actions 依赖自动更新
- 每周检查一次

### 3. 架构文档更新
- 更新 spec 核心指南，反映 Phase 1-2 的架构变更：
  - Fluxor 已删除
  - `LauncherJson` 序列化从 `Options` 模式改为 `JsonTypeInfo<T>` 源生成模式
  - `AppStore` 不再有 `Mutate`/`MutateAsync` 包装方法
  - `ExceptionFilters.IsRecoverable` 简化

## Acceptance Criteria

- [ ] `dotnet nuget audit` 在 CI 中运行（不阻塞，仅 informational）
- [ ] `.github/dependabot.yml` 存在且格式正确
- [ ] spec 核心指南已更新
- [ ] `verify.ps1` 全绿

## Notes

- 不涉及任何 C# 代码改动
- Dependabot 和 NuGet audit 在 PR 层面生效，不影响本地 gate
