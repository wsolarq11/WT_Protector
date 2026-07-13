# Implement — CQ04 MainWindow XAML Modularity

## Steps

- [x] 建立当前 XAML section、named control 与 MainWindow/Drag 使用矩阵。
- [x] 抽取 `LauncherControlBar.axaml(.cs)`，提供 search/display mode 窄访问器并更新 MainWindow 引用。
- [x] build + 控制栏结构测试，确认键盘搜索和显示模式事件仍接线。
- [x] 抽取 `LauncherEntriesView.axaml(.cs)`，迁移三视图、templates、empty states 与窄访问器。
- [x] 转发 context menu/double tap/drag pointer 事件，更新 MainWindow/Drag 引用。
- [x] 添加模块结构、named-control ownership 与禁止重复模板的契约测试。
- [x] 运行聚焦、完整 Release tests、`git diff --check`、`tools/verify.ps1`。
- [x] 更新 views/tests spec，提交并归档。

## Rollback Points

- 控制栏若绑定/焦点语义回归则恢复该 section，不影响 entries。
- entries 若 pointer routing 或 scroll restore 不稳定则恢复该 section；不以禁用 drag/scroll 换取模块化。
