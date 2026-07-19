# 迁移路径：6 阶段实施

## 阶段 0：准备（立即做）

- [ ] P0-1: 添加 `RefreshAllProjections()` 方法到 MainWindowViewModel
- [ ] P0-2: JSON 序列化上下文注册验证测试
- [ ] P0-3: `ExceptionBoundary.RunAsync<T>` 辅助类标准化 catch 模式
- [ ] P0-4: 巩固 V03 视觉 token 迁移到 spec

## 阶段 1：EntryListViewModel 提取（2-3 天）

- [ ] P1-1: `Features/Entries/EntryListViewModel.cs` — 提取 entry 状态和命令
- [ ] P1-2: `IEntryService` 调整为不接收集合参数（Service 自持集合）
- [ ] P1-3: `LauncherEntryViewModel` 解除 `_owner` 引用（改用消息总线）
- [ ] P1-4: Shell ViewModel 持有 `IEntryListViewModel` 引用

## 阶段 2：PresetListViewModel 提取（1 天）

- [ ] P2-1: `Features/Presets/PresetListViewModel.cs`
- [ ] P2-2: `IPresetService` 调整
- [ ] P2-3: Shell 持有 `IPresetListViewModel` 引用

## 阶段 3：消息总线整合（1 天）

- [ ] P3-1: Core 层定义 `EntryAddedMessage` / `EntryDeletedMessage` / `SelectionChangedMessage`
- [ ] P3-2: EntryListViewModel 发布消息
- [ ] P3-3: PresetListViewModel 实现 `IRecipient<EntryDeletedMessage>`
- [ ] P3-4: Shell 订阅消息刷新投影

## 阶段 4：Drag/Layout 提取（2 天）

- [ ] P4-1: `DragReorderService`（从 Drag.cs + Reorder.cs 提取）
- [ ] P4-2: `LayoutViewModel`（从 Layout.cs 提取）
- [ ] P4-3: Shell 持有 `IDragReorderService` + `ILayoutViewModel`

## 阶段 5：收尾清理（1 天）

- [ ] P5-1: 删除已提取的 partial 文件
- [ ] P5-2: 全局 catch-log-return 标准化
- [ ] P5-3: 更新 spec 文档
- [ ] P5-4: 最终全量 gate 验证
