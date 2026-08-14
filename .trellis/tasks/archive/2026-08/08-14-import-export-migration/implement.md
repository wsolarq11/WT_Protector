# 配置导入导出与多机迁移 — Implement

## Order

1. Core: `LauncherConfigExchange` 导出、导入预览、AddNew/Replace 合并与路径映射。
2. Core tests：导出 schema、重复 id、失效预设、路径映射、替换策略。
3. `IDialogService` + `DialogService` 文件选择与导入窗口。
4. `ImportConfigViewModel` + `ImportMigrationWindow`。
5. `MainWindowViewModel.Migration.cs` 管理菜单命令。
6. VM tests：导出全部/选中、新增、替换、重复跳过、映射写盘。
7. README 功能说明。
8. build + dotnet test + Avalonia verify。

## Verification Gates

- `dotnet build ... -warnaserror`
- `dotnet test ... -c Release --no-restore`
- `src/avalonia-launcher/verify.ps1`
- 手动冒烟：导出文件可被 `LauncherStore.Load` 读回；导入窗口可映射缺失目录并写盘。

## Rollback

- 新功能增量；回滚 = 删除新增 Core/VM/View 文件与菜单项。
- 不改现有启动链路、selection、preset 或 drag-sort 契约。
