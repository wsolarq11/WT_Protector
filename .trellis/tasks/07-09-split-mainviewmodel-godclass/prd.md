# 拆 MainWindowViewModel god class (Stage 1)

## Goal

把 2796 行 MainWindowViewModel.cs 拆成 partial 文件,按 spec 已规划方向。本任务是 **Stage 1**:抽 2 个边界最清晰的 partials(Drag + Reorder),作为物理拆分的奠基。剩余 7 个 partials 留 follow-up task,因为它们需要更细致的依赖分析(共享 helper、status text 类等)。

## Scope (Stage 1)

### 抽 `.Drag.cs`
- `_dragReorderWorkflow` 字段
- `_lastPreviewParams` / `_lastPreviewTick` 字段(drag-preview debounce)
- `HasActiveDragReorder` 内部属性
- `WouldChangePreview` / `BeginDragReorder` / `PreviewMoveEntry` / `ReorderVisibleEntryForDrag` / `ClearDragHover` / `CommitDragReorder` / `CancelDragReorder` 方法
- `DiscardDragReorderPreview` 方法(调用 `_dragReorderWorkflow.Cancel`)

### 抽 `.Reorder.cs`
- `CanMoveEntryUp` / `CanMoveEntryDown`
- `MoveEntryUp` / `MoveEntryDown`
- `MoveEntryRelative` (private)
- `RefreshReorderCommandState` (private)

## Requirements

- R1: 新 partial 文件必须 `public sealed partial class MainWindowViewModel`(不重复 sealed,partial 自动继承)
- R2: 新 partial 文件必须有正确 using directives
- R3: 主文件保留所有共享 state(`_settings`, `_presetFile`, `_entries`, etc.)和共享 helper(`ApplyFilter`, `ApplyStatusText`)
- R4: 451 tests 必须全部通过
- R5: verify.ps1 必须通过
- R6: 不改变任何逻辑——纯搬移

## Out of Scope (Stage 2 / follow-up task)

- `.Entries.cs` (Add/Edit/Delete/Clone commands)
- `.Launch.cs` (single launch)
- `.Layout.cs` (filter/sort/view/scroll)
- `.Presets.cs` + `.Presets.Persistence.cs`
- `.Batch.cs` (selection + batch launch)
- 把 status text classes 抽到 LauncherStatusText.cs

这些 partials 共享更多 helper(`SaveStoreFile` / `BuildStoreFile` / `CaptureSettingsMutationSnapshot`),需要更深的设计。留单独任务。

## Acceptance Criteria

- [ ] `MainWindowViewModel.Drag.cs` 存在,包含 drag 相关全部成员
- [ ] `MainWindowViewModel.Reorder.cs` 存在,包含 reorder 相关全部成员
- [ ] 主文件减少约 250 行(从 2796 → ~2540)
- [ ] `dotnet test -c Release` 仍 451 passing
- [ ] `tools/verify.ps1` 仍 PASS
- [ ] grep 验证:drag/reorder 方法名在新 partial 文件中,主文件中无残留

## Notes

- 单次 commit 完成 Stage 1
- 创建 follow-up task 处理剩余 partials
- 不动 spec(directory-structure.md 已说明 god-class 状态,Stage 1 后会更新)
