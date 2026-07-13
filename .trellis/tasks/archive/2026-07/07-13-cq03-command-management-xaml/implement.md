# Implement — CQ03 CommandManagementWindow XAML

## Steps

- [x] 读取完整 window/VM 与 command-management tests，列出每个控件行为和 enablement contract。
- [x] 新建 `.axaml`，迁移窗口资源、标题、tab、toolbar、两套 ItemsControl/DataTemplate、空状态与 footer。
- [x] 精简 `.axaml.cs` 为初始化、关闭和 editor-dialog 路由；删除所有手工 render/sync helpers。
- [x] 给 ViewModel 增加最小只读 projection/command，不迁移业务到 View。
- [x] 添加 XAML 结构契约并运行 command-management 聚焦测试。
- [x] 运行完整 Release tests、`git diff --check`、`tools/verify.ps1`。
- [x] 更新 views spec，提交并归档。

## Risks

- Avalonia compiled binding 对 record command parameter 类型解析失败：使用 `x:DataType` 和明确 `CommandParameter={Binding}`。
- modal editor 需要 owner Window：保留 code-behind event handler，不把 Window 注入 ViewModel。
- `ItemsControl` 空状态叠层：使用同一 Grid 中基于现有 `Has*Rows`/empty projection 的 `IsVisible`，不手工重绘。
