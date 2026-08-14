# 配置导入导出与多机迁移 — Design

## Change Boundary

- 当前只有一个本机 `config/launcher.json`，没有可审查的导出契约；本机绝对路径无法直接搬到另一台机器。
- 目标：`管理` 菜单提供“导出全部 / 导出选中 / 导入配置”，导入时显式处理路径差异、重复 id 和失效 preset 引用，仍走统一 store + 原子写盘。

## Data Flow

```text
管理菜单 -> IDialogService file picker
  -> LauncherConfigExchange.BuildExportFile(selected ids)
  -> LauncherJson.Save(export.json)            // 只导可审查统一 store，不含本机窗口/搜索历史

导入文件 -> LauncherStore.Load(import.json)    // schema 校验
  -> LauncherConfigExchange.AnalyzeImport       // 重复/缺失目录/失效预设引用
  -> ImportConfigViewModel + ImportMigrationWindow
  -> LauncherConfigExchange.ApplyImport(plan)   // AddNew / Replace + path overrides + skipped ids
  -> SaveStoreFile(merged) -> ApplyStoreFileToVm
  -> SaveStoreFile failure -> RestoreVmStateFromStore
```

## Key Decisions

- 导出文件本身就是一个通过 `LauncherStoreFile` schema 校验的 JSON；导出内容保留 entries/presets/settings/defaultPresetId，但清空本机搜索历史和窗口坐标。
- 导入校验和合并规则放 Core，UI 只负责收集用户决定；不做自动路径猜测，不做自动改名。
- 合并策略只有“新增到现有配置”和“替换为导入配置”；新增模式跳过重复 entry id 和重复 preset id，替换模式以导入文件为权威。
- 路径映射以条目为单位：保持原路径 / 输入映射 / 跳过；映射后的目录必须由用户明确给出。
- 导入写盘继续走 `StoreGrain.Dispatch` + `LauncherStore.Save`，失败时恢复 StoreGrain 状态并同步 VM。

## Files Expected to Change

- Core: `LauncherConfigExchange.cs`（导出、预览、合并规则）
- App ViewModels: `ImportConfigViewModel.cs`、`MainWindowViewModel.Migration.cs`
- App Views: `ImportMigrationWindow.axaml` + code-behind、`LauncherControlBar.axaml`、`DialogService.cs`
- Dialog contract: `IDialogService.cs`
- Tests: Core migration rules + VM import/export + XAML/menu contract
- Docs: `README.md`

## Explicitly Not Doing

- 不做云同步、自动跨机同步、注册表/AppData 状态。
- 不做自动路径迁移、插件格式、任意 JSON 导入。
- 不新增第二个配置编辑界面。
