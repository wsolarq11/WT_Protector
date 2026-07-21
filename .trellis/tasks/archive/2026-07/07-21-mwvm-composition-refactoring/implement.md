# Implement: MWVM 组合重构

## 阶段顺序

### Stage 1: StatusPresenter 提取

1. 新建 `StatusPresenter.cs` in ViewModels/
2. 从 MainWindowViewModel 迁移: _statusContent, _dryRunText, _presetSelectionStatusText, _applyPresetButtonText, _totalCount, _visibleCount, _selectedCount, _presetCount 及全部投影属性
3. MainWindowViewModel 持有 StatusPresenter 引用，委托调用
4. `dotnet test` 确认 670 全绿
5. 提交

### Stage 2: PresetManager 提取

1. 新建 `PresetManager.cs` in ViewModels/Features/Presets/
2. 从 MainWindowViewModel.Presets.cs 迁移全部 preset CRUD
3. PresetManager 持有 _presetFile, _presets, _selectedPreset
4. 事件: PresetsChanged, SelectedPresetChanged
5. MainWindowViewModel 委托 preset 操作到 PresetManager
6. `dotnet test` 确认全绿
7. 提交

### Stage 3: LayoutManager 提取

1. 新建 `LayoutManager.cs` in ViewModels/Features/
2. 从 MainWindowViewModel.Layout.cs + Grouping.cs 迁移排序/过滤/视图模式/分组/卡片尺寸
3. LayoutManager 不持有 entry 数据，输出 LauncherEntryQuery
4. 事件: SortChanged, FilterChanged, ViewModeChanged
5. `dotnet test` 确认全绿
6. 提交

### Stage 4: EntryManager 提取 + 拆 Bridge

1. 新建 `EntryManager.cs` in ViewModels/Features/Entries/
2. 从 MainWindowViewModel.cs + Entries.cs + Batch.cs 迁移 entry CRUD + 选择 + 计数
3. 拆掉 EntryListBridge 的 17 个 lambda
4. 事件: EntriesChanged, CountChanged, SelectionChanged
5. `dotnet test` 确认全绿
6. 提交

### Stage 5: LaunchOrchestrator 提取

1. 新建 `LaunchOrchestrator.cs` in ViewModels/Features/
2. 从 MainWindowViewModel.Launch.cs + Batch.cs 迁移单启动/批启动/干运行
3. 不持有状态，纯编排
4. `dotnet test` 确认全绿
5. 提交

### Stage 6: Orchestrator 组装 + Partial 删除

1. 裁剪 MainWindowViewModel.cs 为 orchestrator
2. 删除 12 个 partial 文件
3. 事件拓扑注册
4. `dotnet test` 确认全绿
5. 提交

## 验证

每一阶段后运行:
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release
```

最终:
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
```

## 回滚

每个阶段独立提交。任一阶段失败：
```bash
git reset --hard HEAD~1
```
修复后重新提交。
