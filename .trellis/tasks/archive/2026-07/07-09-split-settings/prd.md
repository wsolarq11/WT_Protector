# Split Settings into UiSettings + LauncherConfig

## Goal

调查并定案：是否需要拆分 `LauncherSettings` 以解决根源 S13（UI/业务状态纠缠）。

## 调查结论：无需拆分（S13 被误识别）

审查 `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs` 与 `LauncherStoreFile` 后确认：UI 状态和业务状态在数据结构层面**已经分离**，S13 不存在。

### LauncherSettings 全部字段（仅 UI/表现层状态）

- `SchemaVersion`（元数据）
- `LastFilterText`（UI 搜索框文本）
- `SortMode`（UI 排序模式）
- `EntryViewMode`（UI 视图模式：cards/details/names）
- `RequireConfirmationForLaunches`（UI 行为开关）
- `CustomEntryOrder`（UI 自定义排序）
- `CommandTemplates`（UI 命令模板辅助）
- `SearchHistory`（UI 下拉历史）
- `Window`（UI 窗口边界）

### LauncherStoreFile 顶层结构（业务状态已分离）

```
LauncherStoreFile
├── SchemaVersion（元数据）
├── Entries（业务：启动条目）
├── Presets（业务：命名启动组）
├── DefaultPresetId（业务：默认预设引用）
└── Settings（UI 状态，作为嵌套节点）
```

### 判定依据

1. **数据结构正确**：UI 状态作为 `Settings` 子节点嵌套在 `LauncherStoreFile` 内，业务状态（Entries/Presets/DefaultPresetId）位于顶层。结构清晰，单一权威源。
2. **单文件原子性优于多文件**：ponytail pitfalls §6.19 —— 单文件 + temp-file→File.Replace→.bak 轮转已经实现原子持久化。拆分为两个文件会引入跨文件一致性陷阱（写 settings 成功但写 entries 失败的窗口）。
3. **无真实问题需要解决**：当前 19 entries / 2 presets / 9 UI 设置项在单文件下运行良好，542 测试通过。

## Requirements

- **无代码变更**。本任务为"调查并拒绝"型，不需要实现工作。
- 更新 `spec/core/persistence.md`：明确记录 Settings = UI 状态嵌套节点，业务状态位于顶层，单文件原子写入是有意选择。
- 更新父任务 PRD：将 S13 标记为"误识别，无需行动"，从根源账本中移除。

## Acceptance Criteria

- [x] `LauncherSettings.cs` 与 `LauncherStoreFile` 字段审查完成，确认 UI/业务状态已分离
- [x] `spec/core/persistence.md` 更新，记录 Settings 嵌套设计与单文件原子写入选择
- [x] 父任务 `prd.md` 更新：S13 标记为误识别
- [x] `dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln` 通过（无代码变更，应保持绿）
- [x] `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln` 542 测试全部通过
- [x] `tools/verify.ps1` 16 项检查通过

## Notes

- 这是"调查并拒绝"型任务：调查本身是交付物，拒绝拆分是有意决定，不是偷懒。
- 真正的根源切断将在 T7（schema-first-persistence）中通过 SchemaVersion + 校验化迁移完成；此处无需提前拆分。
