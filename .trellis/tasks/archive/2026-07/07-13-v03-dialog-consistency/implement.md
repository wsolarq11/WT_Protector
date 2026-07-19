# Implement — V03 Dialog Consistency

## Steps

- [ ] 添加三项 dialog token，并在 App 注入 XAML 所需资源。
- [ ] 统一 LauncherDialogText 标题字号与所有目标窗口主内容 padding。
- [ ] 统一独立 footer 顶距，保留父 Grid 已提供间距的显式 0 情形。
- [ ] 修复两个危险删除窗口的默认按钮与初始焦点一致性。
- [ ] 添加 dialog consistency 结构契约测试。
- [ ] build、聚焦测试和 published GUI smoke。
- [ ] trellis-check、完整 Release tests、git diff --check、tools/verify.ps1。
- [ ] 更新 core/views/tests spec，提交并归档。

## Rollback Points

- token/primitive 修改后先 build，再批量迁移窗口。
- 删除确认语义单独运行契约测试，避免视觉批改掩盖安全回归。
