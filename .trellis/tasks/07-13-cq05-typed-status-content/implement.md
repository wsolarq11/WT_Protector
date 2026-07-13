# Implement — CQ05 Typed Status Content

## Steps

- [ ] 新增 LauncherStatusContent 与验证/四 tone 单元测试。
- [ ] 将 MainWindowViewModel 权威状态改为 StatusContent，保留兼容只读投影和通知。
- [ ] 将 ServiceCanceled/ServiceFailure 状态契约迁移为 LauncherStatusContent。
- [ ] 按工作流迁移 LauncherStatusText formatter 返回值并修复调用点。
- [ ] 清理主窗口直接 string 拼接和 LauncherStatusPresentation.Classify。
- [ ] 更新 status/service/ViewModel 契约测试，运行聚焦测试。
- [ ] 执行 trellis-check、完整 Release tests、git diff --check 与 tools/verify.ps1。
- [ ] 更新 views/tests spec，提交并归档。

## Rollback Points

- StatusContent 投影必须先单独 build，避免把 ViewModel 通知与 formatter 迁移错误混在一起。
- ServiceResult 迁移失败时只回退该契约，不改变 CQ-02 transaction 或 CQ-06 成功值。
- 不以保留文本 marker heuristic 作为长期兼容路径。
