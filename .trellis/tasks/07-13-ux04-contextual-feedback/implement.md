# Implement — UX04 Contextual Feedback

## Steps

- [ ] 通过 codebase-memory 盘点所有 `StatusText` / `ApplyStatusText` 写入及现有 formatter 所有者。
- [ ] 增加最小状态 severity enum 与 owner 原子应用 helper；不建立消息对象层级。
- [ ] 将高频 operation result 映射到 severity，保持筛选/选择等状态 neutral。
- [ ] 在 MainWindow 状态区域绑定可测试的视觉/自动化状态，复用现有 tokens。
- [ ] 添加 formatter/owner 映射测试和 MainWindow 结构契约测试。
- [ ] 运行 Release 全量测试、`git diff --check`、`tools/verify.ps1`。
- [ ] 必要时更新 views/tests spec，提交并归档。

## Rollback Points

- enum/helper 提交前可整体移除，不影响文本契约。
- 若 XAML class binding 在 GUI smoke 不稳定，退回显式只读 brush/border properties；不使用 code-behind订阅。
