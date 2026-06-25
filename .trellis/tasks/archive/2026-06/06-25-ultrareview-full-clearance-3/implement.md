# Implement: UltraReview 全部发现清零

## Phase 1 — Critical + High (20项)

### 1.1 安全修复（4项，`LaunchArgumentBuilder.cs`）

- [ ] R2.1 UNC 路径阻止：`Build()` 中 directory 计算后添加 UNC 检测
- [ ] R2.2 wt.exe 路径加固：`ResolveTerminalExecutable()` 移除裸名回退
- [ ] R2.3 全角% 防御：ExpandEnvironmentVariables 前检测 U+FF05
- [ ] R2.4 BiDi/格式字符：`StripControlCharacters()` 扩展剥离 Cf 类字符
- [ ] 验证：`dotnet test --filter "LaunchArgumentBuilder"`

### 1.2 数据完整性修复（3项）

- [ ] R1.2 代理对截断：`LauncherEntryIds.cs:17` + `LauncherPresetIds.cs:42`
- [ ] R2.5 ID Unicode 规范化：`LauncherEntryIds.Normalize()` 添加 FormC
- [ ] R2.6 LastUsedPresetId 移除：`LauncherSettings.cs` + `Presets.Helpers.cs`
- [ ] R2.7 UpdatePreset ID 查找：`LauncherPresetWorkflow.cs:50-51`
- [ ] 验证：`dotnet test --filter "LauncherEntryIds|LauncherPreset|PresetWorkflow"`

### 1.3 备份/恢复修复（3项）

- [ ] R1.1 备份无限循环：3 个 Store Load 方法重构
- [ ] R1.3 TOCTOU 竞态：catch 过滤条件添加 FileNotFoundException
- [ ] R3.1+3.2 非原子备份→temp-file-then-rename：`LauncherJson.cs` + Store 恢复
- [ ] 验证：`dotnet test --filter "ConfigStore|PresetStore|SettingsStore|LauncherJson"`

### 1.4 异常安全修复（5项）

- [ ] R2.8 MutationPersistence 异常一致性
- [ ] R2.9+2.10 Debug.WriteLine → ErrorLog.Write
- [ ] R2.13 async void 异常安全：MainWindow.Drag.cs
- [ ] R2.14 NRE 防御：MainWindowViewModel.cs
- [ ] R2.15 ErrorLog 并发丢弃诊断
- [ ] 验证：`dotnet test`

### 1.5 视图/拖拽修复（2项）

- [ ] R2.11 键盘焦点泄漏：MainWindow.axaml.cs
- [ ] R2.12 拖拽决策泄漏：MainWindow.Drag.cs
- [ ] 验证：`dotnet test --filter "Drag|Keyboard|Focus"`

### 1.6 测试修复（2项）

- [ ] R1.4 AssertJsonPropertyNames 双向检查
- [ ] R1.5 TerminalExecutable 断言改为具体验证
- [ ] 验证：`dotnet test --filter "RepositoryConfigContract"`

### 1.7 Phase 1 终验

- [ ] `dotnet test` 全量通过
- [ ] `tools\verify.ps1` 全量通过
- [ ] `git diff --check` 通过

## Phase 2 — Medium (22项)

### 2.1 文件 I/O（5项）

- [ ] R3.3 fsync：LauncherJson.cs Flush(true)
- [ ] R3.4 NaN 窗口坐标：IsFinite 替换 IsInfinity
- [ ] R3.5 多文件原子性：stage-all-then-commit（或记录为已知限制）
- [ ] 验证：`dotnet test --filter "SettingsStore|LauncherJson"`

### 2.2 错误处理（3项）

- [ ] R3.6 Refresh() 错误处理：try/catch 包裹
- [ ] R3.7 finally 块异常替换：try/catch 包裹 File.Delete
- [ ] R3.8 冗余 catch 清理

### 2.3 性能（4项）

- [ ] R3.9 移除 AsReadOnly()：LauncherEntryVisibility.cs:86
- [ ] R3.10 合并双 JSON 解析：LauncherConfigStore.cs
- [ ] R3.11 GetSelectedEntryIds 单 pass：MainWindowViewModel.Selection.cs
- [ ] R3.12 SyncVisibleEntries 重用集合

### 2.4 逻辑正确性（3项）

- [ ] R3.13 命令模板键大小写：HashSet 比较器改为 Ordinal
- [ ] R3.14 FailureIndex ??=：LauncherBatchLaunchReport.cs
- [ ] R3.15 RenameEntryId 相同 ID 早退

### 2.5 JSON/序列化（3项）

- [ ] R3.16 UnmappedMemberHandling.Skip
- [ ] R3.17 selected 值类型验证
- [ ] R3.18 Unicode 规范化文档

### 2.6 视图层/拖拽（4项）

- [ ] R3.19 键盘焦点索引重验证
- [ ] R3.20 滚动偏移恢复竞态
- [ ] R3.21 拖拽期间禁用 ComboBox
- [ ] R3.22 FastWrapPanel 900px 文档化

### 2.7 Phase 2 终验

- [ ] `dotnet test` 全量通过
- [ ] `tools\verify.ps1` 全量通过

## Phase 3 — Low (30+项)

### 3.1 数据验证批量修复（5项）

### 3.2 结构一致性批量修复（5项）

### 3.3 视图/无障碍批量修复（5项）

### 3.4 资源/生命周期修复（2项）

### 3.5 文件 I/O + 测试补充（3项）

### 3.6 其他低优项（10+项）

### 3.7 Phase 3 终验

- [ ] `dotnet test` 全量通过（≥ 557 tests）
- [ ] `tools\verify.ps1` 全量通过
- [ ] `git diff --check` 通过

## 回滚点

- Phase 1 完成后 → commit checkpoint
- Phase 2 完成后 → commit checkpoint
- Phase 3 完成后 → final commit
- 任一步骤失败 → `git checkout .` 回滚当前 Phase
