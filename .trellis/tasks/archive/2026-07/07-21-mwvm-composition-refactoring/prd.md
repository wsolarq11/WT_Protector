# MWVM 组合重构 — 6 阶段 composition refactoring

## 问题

MainWindowViewModel 44 个字段、15 个 partial 文件、~8k 行。13 个 partial 共享同一组私有字段（`_entries`, `_settings`, `_presets`, `_presetFile`），没有状态隔离。改一个 partial 的行为需要确认不破坏其他 12 个文件。

## 目标

将 MainWindowViewModel 的职责分解为 6 个独立协作类，每个类持有自己的那片状态，不共享字段引用。MainWindowViewModel 降为纯编排者（~500 行）。

## 验收标准

1. 全部 670 测试通过，不修改测试的公开行为 assertion
2. 12 个 partial 文件被删除（IEntryCardHost.cs 和 IEntryListViewModel.cs 随重构消失）
3. MainWindowViewModel 主文件从 432 行缩减到 ~500 行
4. 每个新 Manager 可以独立构造（零初始化依赖）
5. 每个 Manager 顶部有失败半径契约注释
6. EntryListBridge 的 17 个 lambda 被消除
7. 构建 0 error 0 warning，.NET nullable 0 warning

## 约束

- 不改 LauncherEntryViewModel, LauncherPresetViewModel 等已独立类
- 不改持久化层（LauncherStore, LauncherJson）
- 不改 EntryListViewModel（已独立）
- 不改 DragWorkflow（已独立）
- 不改测试的公开行为 assertion，只改因类提取导致的构造方式变化
- 每个阶段独立提交，阶段之间门禁全绿
