# 命令管理面板（含 commandTemplates CRUD）

## Goal

把分散在各 entry 的 `command` 字段（含 `$codex`/`$claude` 模板引用 + 裸命令如 `codex`）集中到一个 modal dialog 管理。用户能：
- 查重（一眼看到哪些 entry 在用同一命令）
- 批量改命令（如把裸 `codex` 改成 `$codex`，同步更新所有引用）
- 合并重复命令
- 删除未引用的命令
- 顺带管理 `commandTemplates` 字典（加/改/删模板）

## Background

来自 parent brainstorm + `config/launcher.json` 2026-07-11 现状：

- 18 个 entry，command 分布：`$codex` × 14, `$claude` × 2, `$opencode` × 1, **裸 `codex` × 1**（未走模板，没吃到 `--enable goals --dangerously-bypass-approvals-and-sandbox`）
- `commandTemplates` 已经把命令参数集中，但**无 UI** —— 只能 launcher.json 手编
- OQ-3 决定：commandTemplates CRUD 合并到同一 dialog 第 2 个 tab

## Scope

- **新 View**: `CommandManagementWindow` (Avalonia Window, code-behind 模式，与 `DiscoverAgentsWindow` 一致)
- **新 ViewModel**: `CommandManagementViewModel` (ObservableObject, 含两个 tab 子 VM)
- **新 Core helper**: `CommandUsageAnalyzer` (去重 + 引用计数，纯函数)
- **注入路径**: `MainWindowViewModel` 加 `Func<CommandManagementResult?>` 注入点（与 `DiscoverAgentsDialogAsync` 同模式），由 `MainWindow.axaml.cs` 实例化 dialog
- **入口**: Cards 工具栏"管理"按钮（与"发现"按钮并列）
- **mutation 路径**: 走现有 `LauncherEntryMutationSnapshot` 快照/回滚 + `SaveEntryCollectionChanges`
- **commandTemplates CRUD**: 同 dialog 第 2 tab，编辑后写回 `_settings.CommandTemplates`

## Out of Scope

- 撤销/redo 栈（snapshot 是一次性 rollback，不支持多步 undo）
- commandTemplates 导入/导出 JSON
- 命令使用统计（如最近启动次数）—— 留给 Child 2 frecency
- 命令分组/标签 —— 留给后续可能扩展
- 跨 entry 批量勾选 + 批量改（每行 = 一个去重 command，双击 = 改该 command 的所有引用 entry；不做勾选）

## Requirements

### R-1 数据来源
- Tab 1 "命令引用": 数据派生自 `_entries` 的 `entry.Command` 去重。引用次数 = 同 command 的 entry 数。
- Tab 2 "命令模板": 数据来自 `_settings.CommandTemplates`。

### R-2 表格呈现
- Tab 1: 列 = `Command` (mono 字体) | `引用次数` | `引用的 entry 名` (逗号拼, tooltip 全显) | 操作图标列
- Tab 2: 列 = `Template Key` ($key 形式) | `展开内容` (mono) | 操作图标列
- 默认按引用次数倒序，相同次数按 command 字典序

### R-3 双击行为
- Tab 1: 双击行 → 弹 edit dialog
  - 单选 "用模板 / 裸命令"
  - "用模板": 模板 key 下拉（来自 `commandTemplates.Keys`），值显示为 `$key`
  - "裸命令": 文本框
  - OK 后若旧 command 被多个 entry 引用 → 弹"该命令被 N 个 entry 引用。同步更新所有引用？" [全部同步] [仅此项] [取消]
- Tab 2: 双击行 → 弹 edit dialog
  - 模板 key 文本框（不可改成已存在的 key）
  - 展开内容文本框
  - OK 后所有 `entry.Command == $oldKey` 自动更新为 `$newKey`（无需同步确认，因为 key 重命名是原子操作）

### R-4 右键菜单
- 编辑 (F2)
- 合并 (Tab 1 only) → 弹"选保留方" dialog → 失效方的所有 entry 引用更新到保留方
- 删除 (Delete)
  - Tab 1: 引用次数 > 0 阻止，弹"无法删除：N 个 entry 仍在引用此命令。请先合并或修改引用。" [OK]
  - Tab 2: 引用次数 > 0 阻止（`entry.Command == $key`），同样阻止提示

### R-5 查重 (Ctrl+F)
- 模糊匹配 command 字符串（忽略大小写、空白），高亮疑似重复行

### R-6 不变量
- 所有 mutation 走 `LauncherEntryMutationSnapshot` + `SaveEntryCollectionChanges`
- 所有 mutations 通过 `SaveStoreFile` 原子写
- 无新 `catch (Exception)` bare catch
- 中英文 mixed 文案保持现有风格（中文为主）

## Acceptance Criteria

- [ ] **AC-1**: Cards 工具栏新增"管理"按钮，点击弹 `CommandManagementWindow` modal。
- [ ] **AC-2**: Tab 1 列出当前所有去重 command + 引用次数。当前数据：`$codex` (14) / `$claude` (2) / `$opencode` (1) / `codex` (1)。
- [ ] **AC-3**: 双击 `codex` (裸) → 弹 edit dialog → 选"用模板" + `codex` → 确认 → 弹"该命令被 1 个 entry 引用。同步更新所有引用？" → 点"全部同步" → entry `codex` 的 command 变成 `$codex`，吃到 `--enable goals` 参数。
- [ ] **AC-4**: Tab 2 列出 `commandTemplates`: `codex → codex --enable goals --dangerously-bypass-approvals-and-sandbox` / `claude → claude --dangerously-skip-permissions` / `opencode → opencode`。双击 `codex` 模板 → 改展开为 `codex --enable goals --dangerously-bypass-approvals-and-sandbox --verbose` → OK → 所有 14 个 `$codex` entry 立即吃到新参数（无需同步确认，因为 key 没变）。
- [ ] **AC-5**: Tab 1 选 `codex` (裸) + `claude` (裸, 假设有) → 合并 → 选保留 `codex` → `claude` 行消失，原 `claude` entry 的 command 变成 `codex`。
- [ ] **AC-6**: Tab 1 选 `$codex` (引用 14) → 删除 → 弹"无法删除：14 个 entry 仍在引用此命令。请先合并或修改引用。" → 无法删。
- [ ] **AC-7**: 任意 mutation 失败（模拟 store 不可写）→ entries + presets + settings + commandTemplates 全部回滚到 mutation 前。`config/launcher.json` 保持合法。
- [ ] **AC-8**: 单元测试覆盖：(a) `CommandUsageAnalyzer` 去重 + 引用计数 + 合并语义，(b) `CommandManagementViewModel` 批量改 command 写回所有引用 entry，(c) snapshot/rollback 还原 commandTemplates。≥ +10 个新测试。
- [ ] **AC-9**: `tools/verify.ps1` 16 项全 PASS，无新 bare catch。

## References

- Parent prd: `.trellis/tasks/07-11-centralize-declutter/prd.md`
- 调研: `.trellis/research/launcher-data-models.md` (Alfred Action+Trigger 等价模型), `.trellis/research/matrix-ui-and-directory-library.md` (地址库标准做法 30 年成熟)
- 既有 dialog 模式: `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.cs`
- 既有 mutation 路径: `MainWindowViewModel.Entries.cs:SaveEntryCollectionChanges`
- 既有注入模式: `MainWindowViewModel.Discovery.cs:DiscoverAgentsDialogAsync`
