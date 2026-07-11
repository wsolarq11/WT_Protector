# 地址管理面板（含查重/合并/浏览）

## Goal

把分散在各 entry 的 `directory` 字段集中到一个 modal dialog 管理,与 Child 3 (命令管理) 共用模式。用户能:

- 查重 — 一眼看到哪些 entry 共用同一 directory(常见: codex+claude 同跑同一项目目录)
- 合并两个 directory(失效方的所有 entry.directory 改成保留方)
- 重命名 directory(cascades 所有引用 entry)
- 删除未引用 directory(仅清理视图,无 entry 受影响)
- 浏览 — edit dialog 内嵌资源管理器 folder picker,从 GUI 改地址而不用手输

## Background

来自 parent `07-11-centralize-declutter` + `config/launcher.json` 2026-07-11 现状:

- 18 个 entry,15 个独立 directory,**3 对重复**:
  - `D:\<PROJECT_ROOT>\WT_Protector` × 2(codex + opencode)
  - `D:\<PROJECT_ROOT>\ER\crawler\Data_Acquisition_and_Analysis` × 2(codex + claude)
  - `D:\<PROJECT_ROOT>\ER\crawler\phoenix-crawler` × 2(codex + claude)
- 重复合并后 entry 数减少,但 directory 仍是 entry 的字段(不抽成顶级 `directories[]` 数组 — schema 重构被调研明确否决)。
- Child 3 已建立 dialog 模式 + Func 注入 + snapshot/rollback 路径,本任务直接复用。

## Scope

- **新 View**: `DirectoryManagementWindow` (Avalonia Window, 纯 C# code-behind,与 `CommandManagementWindow` 一致)
- **新 ViewModel**: `DirectoryManagementViewModel` (ObservableObject,ObservableCollection<DirectoryRow>)
- **新 Core helper**: `DirectoryUsageAnalyzer` (去重 + 引用计数,纯函数,与 `CommandUsageAnalyzer` 同模式)
- **新 Core records**: `DirectoryRow` / `DirectoryManagementEdit` / `DirectoryManagementResult`
- **注入路径**: `MainWindowViewModel` 加 `Func<Func<DirectoryManagementEdit, DirectoryManagementResult>, Task>` 注入点
- **入口**: Cards 工具栏"管理"按钮的旁边或同 dialog 第 2 tab(本任务选独立 dialog,避免 Child 3 dialog 复杂化)
- **mutation 路径**: 走现有 `LauncherEntryMutationSnapshot` 快照/回滚 + `SaveEntryCollectionChanges`
- **浏览按钮**: `OpenFolderDialog` (Avalonia 原生)集成到 edit dialog

## Out of Scope

- 全盘扫描磁盘找新目录(留给 Child 5 扫描向导)
- directory 收藏夹/标签
- directory 历史(最近打开)
- 单 dialog 同时管理 command + directory(本任务与 Child 3 独立 dialog)
- 撤销/redo 栈

## Requirements

### R-1 数据来源
- 单 tab "地址引用": 数据派生自 `_entries` 的 `entry.Directory` 去重。引用次数 = 同 directory 的 entry 数。

### R-2 表格呈现
- 列 = `Directory` (mono 字体) | `引用次数` | `引用的 entry 名` (逗号拼, tooltip 全显) | 操作图标列
- 默认按引用次数倒序,相同次数按 directory 字典序
- 引用 = 0 的行特别标注"(无引用 — 安全删除)"

### R-3 双击行为
- 双击行 → 弹 edit dialog
- edit dialog 含: directory 文本框 + "📁 浏览..." 按钮(打开 `OpenFolderDialog`)
- 改 directory → 若被多个 entry 引用 → 弹"该地址被 N 个 entry 引用。同步更新所有引用?"[全部同步] [仅此项] [取消]

### R-4 右键菜单/工具栏
- 编辑/重命名(F2)
- 合并 → 弹"选保留方" dialog → 失效方的所有 entry.directory 改成保留方
- 删除(Delete) — 引用次数 > 0 阻止,弹"无法删除:N 个 entry 仍在引用此地址。请先合并或修改引用。"

### R-5 不变量
- 所有 mutation 走 `LauncherEntryMutationSnapshot` + `SaveEntryCollectionChanges` (R-P2)
- 所有 mutations 通过 `SaveStoreFile` 原子写(R-P4)
- 无新 `catch (Exception)` bare catch(R-P5 / AC-8)
- 中英文 mixed 文案保持现有风格(中文为主)(R-P3)
- 不引入新 NuGet 依赖(R-P5)

## Acceptance Criteria

- [ ] **AC-1**: Cards 工具栏"管理"按钮或同区域加"地址"入口,点击弹 `DirectoryManagementWindow` modal。
- [ ] **AC-2**: 列出当前所有去重 directory + 引用次数。当前数据: 15 个独立 directory,其中 3 对各 2 个引用,9 个 1 个引用。
- [ ] **AC-3**: 双击 `D:\<PROJECT_ROOT>\WT_Protector`(引用 2) → 弹 edit dialog → 点"📁 浏览..."选新目录 → OK → 弹"该地址被 2 个 entry 引用。同步更新所有引用?" → "全部同步" → 2 个 entry 的 directory 都更新。
- [ ] **AC-4**: 选两个重复 directory → 合并 → 选保留方 → 失效方行消失,原失效方的 entry.directory = 保留方。
- [ ] **AC-5**: 选有引用的 directory → 删除 → 弹"无法删除:N 个 entry 仍在引用此地址。" → 阻止。
- [ ] **AC-6**: 选无引用 directory → 删除 → 仅清理视图(无 entry 改动),状态栏显示成功消息。
- [ ] **AC-7**: 任意 mutation 失败(模拟 store 不可写) → entries + presets + settings 全部回滚到 mutation 前。`config/launcher.json` 保持合法。
- [ ] **AC-8**: 单元测试覆盖: (a) `DirectoryUsageAnalyzer` 去重 + 引用计数, (b) `ApplyDirectoryManagementEdit` 各 edit 类型, (c) snapshot/rollback 还原。≥ +8 个新测试。
- [ ] **AC-9**: `tools/verify.ps1` 全 PASS,无新 bare catch。

## References

- Parent prd: `.trellis/tasks/07-11-centralize-declutter/prd.md`
- Child 3 (兄弟任务,同模式): `.trellis/tasks/archive/2026-07/07-11-command-management-dialog/`
- 既有 dialog 模式: `src/avalonia-launcher/src/WTLauncher.App/Views/CommandManagementWindow.cs`
- 既有 mutation 路径: `MainWindowViewModel.Entries.cs:SaveEntryCollectionChanges`
- Avalonia `OpenFolderDialog`: https://docs.avaloniaui.net/docs/concepts/dialogs
