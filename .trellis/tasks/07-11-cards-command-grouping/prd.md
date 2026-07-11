# Cards 视图按 command/directory 分组 + 组头 quick-launch

## Goal

在 Cards 视图加"分组"维度(无 / 按 command / 按 directory),把当前 14 个 `$codex` entry 视觉折叠成 1 个组头(含计数 + 启所有 + 存为启动组按钮)。点击组头折叠/展开,用户能:
- 查重视觉提示(一眼看到我有几个 codex / 几个 claude)
- 一键启动整组(替代逐个点击)
- 一键把整组存为 preset(快速建立启动组)
- 折叠后大幅释放屏幕空间(14 个 codex 折叠成 1 行)

## Background

来自 parent `07-11-centralize-declutter` + `config/launcher.json` 2026-07-11 现状:

- 18 个 entry,command 分布严重不均:`$codex` × 14, `$claude` × 2, `$opencode` × 1, 裸 `codex` × 1
- Cards 视图当前扁平渲染 18 个 card,14 个 codex 视觉上散乱排列,占了大量屏幕
- 用户已能用 sort mode 排序,但排序 ≠ 分组 — 没有"组头 + 计数 + 启所有"的概念
- Child 3/4 命令/地址管理 dialog 解决"集中改"的问题;本任务解决"集中看"的问题

## Scope

- **新 Core 类型**: `LauncherCardsGroupKey` 枚举(none/command/directory)+ `CardsGroupMode` 静态 IDs
- **新 Core helper**: `LauncherEntryGrouper` 纯函数,从 entries 派生分组
- **新 App 类型**: `LauncherCardGroupViewModel` (group header VM,含 IsExpanded / 启所有 / 存为启动组)
- **新设置字段**: `settings.cardsGroupKey` (optional,默认 "none",schemaVersion 保持 2)
- **新 view 模式子选项 UI**: Cards 工具栏加 ComboBox(分组模式选择)
- **XAML 改动**: CardsEntriesItemsControl 渲染 VisibleGroups 而非 VisibleEntries,ItemTemplate 含 header + nested ItemsControl
- **mutation 路径**: 走现有 `LauncherEntryMutationSnapshot` + `SaveEntryCollectionChanges`(组头"启所有"用现有 batch launch,"存为启动组"用现有 PresetDetailsWindow)

## Out of Scope

- Details / Names 视图的分组(本任务仅 Cards)
- 多级分组(如先按 directory 再按 command)
- 组头拖动重排组(组的顺序按引用计数倒序,固定)
- 折叠状态持久化(session-only)
- 分组排序规则自定义(默认引用次数倒序,相同次数按 key 字典序)
- 跨组拖动 entry(组内顺序保持现有 custom sort)

## Requirements

### R-1 分组维度
- ComboBox 选项: 无分组 / 按 command / 按 directory
- 默认 "无分组"(保持现有 Cards 视图行为)
- 改分组模式立即重渲染 + 持久化到 settings.cardsGroupKey

### R-2 组头呈现
- 当分组模式非"无",Cards 视图按组渲染: 每组一个组头 + 该组所有 card
- 组头显示: 组标识(mono 字体,$codex / D:\proj\foo) + 计数("14 个") + ▶ 启所有 + 💾 存为启动组 + ▼/▶ 展开/折叠 toggle
- 组默认展开(用户看到所有 card);点击 toggle 折叠
- 组按引用次数倒序,相同次数按组标识字典序

### R-3 启所有按钮
- 点击 → 弹现有 batch launch 确认(若 settings.requireConfirmationForLaunches=true) → 启动组内所有 entry
- 启动失败走现有 batch launch 错误报告

### R-4 存为启动组按钮
- 点击 → 弹现有 PresetDetailsWindow,prefill 该组所有 entry IDs
- 用户填 preset 名 → 保存 → 走现有 preset 保存路径

### R-5 不变量
- 分组模式 = 无 时,行为与现有 Cards 完全一致(渲染 / 排序 / 拖动 / 键盘导航)
- 折叠状态不影响 entry 的存储状态
- 所有 mutations 通过 `SaveStoreFile` 原子写
- 无新 `catch (Exception)` bare catch
- 不引入新 NuGet 依赖

### R-6 兼容性
- `settings.cardsGroupKey` 是 optional,旧 store 文件读取默认 "none"
- schemaVersion 保持 2
- 未识别的值(如未来 "tag")按 "none" 处理(forward compat)

## Acceptance Criteria

- [ ] **AC-1**: Cards 工具栏新增"分组"ComboBox,3 选项。
- [ ] **AC-2**: 选"按 command"后,14 个 `$codex` entry 折叠成 1 个组头(含计数"14"+ 启所有 + 存为启动组 + 折叠按钮); `$claude` 折叠成 1 组(2 个); `$opencode` 折叠成 1 组(1 个); 裸 `codex` 折叠成 1 组(1 个)。
- [ ] **AC-3**: 点击 ▶ 启所有 → 弹 batch launch 确认(若开启)→ 启动组内所有 entry。
- [ ] **AC-4**: 点击 💾 存为启动组 → 弹 PresetDetailsWindow,该组 entry IDs 已 prefill。
- [ ] **AC-5**: 点击 ▼ 折叠按钮 → 组内 card 隐藏,只显示组头;再点 ▶ 展开 → card 显示回来。
- [ ] **AC-6**: 选"按 directory"后,按 entry.directory 分组(3 对重复 directory 各 1 组,9 个独立 directory 各 1 组)。
- [ ] **AC-7**: 选"无分组" → Cards 视图完全等同现有行为(无组头)。
- [ ] **AC-8**: 重启应用后 settings.cardsGroupKey 保持(持久化)。
- [ ] **AC-9**: 单元测试覆盖: (a) `LauncherEntryGrouper` 分组逻辑, (b) ViewModel 在 groupKey 改变时正确重建 VisibleGroups, (c) 组头"启所有"调 batch launch 路径。≥ +6 个新测试。
- [ ] **AC-10**: `tools/verify.ps1` 全 PASS,无新 bare catch。

## References

- Parent prd: `.trellis/tasks/07-11-centralize-declutter/prd.md`
- 既有 batch launch: `MainWindowViewModel.Batch.cs`
- 既有 preset save: `MainWindowViewModel.Presets.cs`
- 既有 view mode pattern: `LauncherEntryViewMode.cs`
- 既有 settings: `LauncherSettings.cs`
- 既有 Cards XAML: `MainWindow.axaml:460-602` (CardsEntriesItemsControl)
