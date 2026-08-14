# 失效目录可见化与一键修复 — Implement

## Order

1. 新增 warning design tokens 并注入。
2. 新增 `LauncherDirectoryHealth` Core 规则与单元测试。
3. 在 `LauncherEntryViewModel` 增加 `IsStaleDirectory` 计算投影和 `RepairDirectoryCommand`。
4. 在 `EntryListViewModel` 增加 `StaleDirectoryCount` / `HasStaleDirectories` 与变更通知，覆盖 Replace/Add/Edit/Delete。
5. 在 `MainWindowViewModel` 转发 stale 属性。
6. 更新 XAML：三种视图 stale 标记、修复菜单、顶部计数摘要、card style。
7. 增加 ViewModel 测试与 XAML/token 合约测试。
8. 运行 `dotnet test -c Release`；通过后再跑聚焦门禁检查。

## Verification Gates

- `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore`
- `dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror`
- 手动/自动化冒烟：含失效目录的配置可加载；编辑后标记消失；删除后计数/引用更新。

## Rollback

- 所有新增功能都是增量叠加；回滚 = 移除新增文件/属性/XAML 节点。
- 不改启动链路和持久化 schema，因此不影响现有 752 测试的契约。