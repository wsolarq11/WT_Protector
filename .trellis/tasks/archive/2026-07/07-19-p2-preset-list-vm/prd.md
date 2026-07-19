# P2-提取 PresetListViewModel

## Goal

从 MainWindowViewModel 提取预设状态和管理逻辑到独立的 PresetListViewModel，消除 Presets.cs (~507 行) 对 shell 的直接依赖。遵循 P1 EntryListViewModel 相同模式。

## Requirements

- PresetListViewModel 拥有 `_presetFile`、`_presets`、`SelectedPreset`、`PresetCount` 等预设状态
- 与 shell 共享 `_presets` ObservableCollection（与 P1 共享 `_entries` 相同模式）
- PresetListBridge 回调处理：条目选择、条目ID、状态显示、保存/回滚、启动预览
- shell 中预设相关 `[RelayCommand]`（Apply/Launch/Preview/Save/Update/Delete）委托给 PLVM
- 回滚通过 PLVM 内部的快照机制，桥接只暴露 save/store 回调

## Decision

**跳过。** `IPresetService` 已经干净地抽象了预设 CRUD（增/删/改/应用/启动预览），`_presetFile` 在 shell 的 6+ partial 文件中被引用 50+ 处。提取 PresetListViewModel 会创建一个薄的 `IPresetService` 包装器，引入 PresetListBridge 却几乎没有消除耦合。当前预设代码的模块化程度已满足架构目标。

## Constraints

- `_presets` ObservableCollection 必须在 shell 和 PLVM 之间共享引用
- PresetListBridge 是临时桥接，后续阶段替换为消息总线
- 预设操作仍然需要访问条目选择状态（`GetSelectedEntryIds()` 等），这些通过桥接回调

## Acceptance Criteria

- [ ] PresetListViewModel 独立文件创建，包含所有预设状态和 CRUD 逻辑
- [ ] PresetListBridge 定义 shell↔PLVM 回调契约
- [ ] PresetSnapshot 记录回滚状态
- [ ] IPresetListViewModel 公开接口（可选，与 P1 一致）
- [ ] shell 中 Presets.cs 委托给 PLVM
- [ ] 构建通过，653+ 测试通过
- [ ] 构造函数中 PLVM 在 `ApplyPresetFileState` 前创建（与 P1 一样）
