# F13b + E8 北极星 UX 修复

## Goal

修复 2 项与业界北极星（VS Code / Docker）有差距的 UX 缺陷。

## Requirements

### F13b: 视图切换滚动位置保持
- 切换到不同视图时，保存当前视图滚动位置到 ViewModel
- 切回时恢复之前保存的滚动位置
- 状态管理: `Dictionary<LauncherEntryViewMode, double>` in ViewModel

### E8: 预演失败时展示确认对话框
- 当 `preflight.Report.FailedCount > 0` 时，显示确认弹窗
- 展示失败项 + 有效项完整画面
- 用户确认后继续，取消则中止

## Acceptance Criteria
- [ ] 切换 Cards↔Details↔Names 后切回，滚动位置保持
- [ ] 预演失败时仍显示确认弹窗
- [ ] `dotnet build` 0 errors 0 warnings
- [ ] `dotnet test` 556+ passed

## Out of Scope
- ScrollViewer 容器物理统一（三种视图布局不同，不可能共用同一容器）
