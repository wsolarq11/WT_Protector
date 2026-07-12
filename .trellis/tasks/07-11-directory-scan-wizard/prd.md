# Discovery 扫描向导扩展（扫已有 entry 目录）

## Goal

扩展 `DiscoverAgentsWindow`：在原有"扫 PATH 找 CLI"模式之外，新增"扫已有 entry 的 directory"模式。用户从已有 directory 列表挑一个 → 选命令模板（来自 `commandTemplates`）→ 一键创建 entry。比走 Add Entry 表单更快，因为 directory 不用重输。

## Background

- 父任务 `07-11-centralize-declutter` AC-5 明确要求："Discovery 对话框新增'扫已有目录'模式，列出当前所有独立 directory 让用户挑；选中后选命令 → 一键创建 entry。"
- 现有 `DiscoverAgentsWindow` + `DiscoverAgentsViewModel` 已实现 PATH 扫描 + 批量 import（子任务 3 之前的工作）。
- 子任务 3/4 已交付 `commandTemplates` + directory 去重表；本任务复用其去重逻辑，不需要重写。
- 用户原始诉求："扫描已有按钮内的地址进行同一管理"——本任务交付"挑已有地址 → 一键创 entry"的最短路径。

## Requirements

### R1 — Discovery 对话框新增 mode 切换

- 在 `DiscoverAgentsWindow` 顶部新增 `TabStrip`（或两个 `RadioButton`）切换两个模式：
  - `发现 AI Agent`（现有 PATH 扫描模式，默认选中）
  - `从已有目录创建`（新模式）
- 切换不销毁/重建 dialog；两个模式 panel 同时存在，靠 `IsVisible` 切换。
- 取消/确认按钮位置不变；确认按钮文字根据当前 mode 动态变化：
  - PATH 模式：`添加选中`（现有）
  - 目录模式：`创建启动项`

### R2 — 目录模式：去重 directory 列表

- 列出当前 `_entries` 中所有 distinct `directory`（按 `StringComparer.OrdinalIgnoreCase` 去重）。
- 每行展示：directory 路径 + 引用计数（"被 N 个 entry 使用"）+ 单选 RadioButton（选中后高亮）。
- 引用计数为 0 的情况不会出现（因为来源就是已有 entry）。
- 排序：按引用计数 desc，再按路径 asc。
- 不需要扫描文件系统验证存在性（已有 entry 的 directory 在 schema 校验时已被验证；扫描磁盘是父任务 out of scope）。

### R3 — 目录模式：命令选择

- 选中 directory 后，下方出现命令选择 `ComboBox`：
  - 选项 = `settings.commandTemplates` 的所有 key（如 `codex` / `claude` / `opencode`）。
  - 默认选第一个 key（按字母序）。
  - 选中后展示该模板展开后的完整命令（只读预览，灰底）。
- 确认按钮（`创建启动项`）在 directory + command 都选中后启用。

### R4 — 一键创建 entry（与 Add Entry 路径一致）

- 确认按钮调用现有 `AddEntry` mutation 路径（snapshot → mutate → save → rollback on failure）。
- 新 entry 字段：
  - `Id`：新生成的 GUID（与现有 Add Entry 一致）
  - `Name`：默认 = directory 的文件夹名（`Path.GetFileName(directory.TrimEnd('\\'))`）；用户可改，但默认填好减少输入。
  - `Directory`：选中的 directory
  - `Command`：选中的模板 key（如 `$codex`）—— 与现有 Add Entry 同形态
  - `Selected`：false
  - `UpdatedAtUtc` / `LaunchedAtUtc`：empty string
- 创建成功后 dialog 关闭，主窗口可见到新 entry（按当前 sort 模式刷新）。
- 创建失败（save 失败）：快照回滚 + StatusText 显示错误，dialog 不关。

### R5 — UX 一致性

- 文案与现有 dialog 风格一致（中文，与 `DirectoryManagementWindow` / `CommandManagementWindow` 同语种）。
- 颜色/字号/间距走 `LauncherDesignTokens`，不引入新 token。
- 取消按钮永远可用，关闭 dialog 不产生任何 mutation。

### R6 — Gate 通过

- `tools/verify.ps1` 16 检查全绿。
- `-warnaserror` 干净。
- GUI smoke 在两个 mode 切换时不崩溃。
- 新增 ≥5 个单元测试（R2 去重 + R4 创建路径 + R3 命令预览展开）。

## Acceptance Criteria

- [ ] AC-1：Discovery dialog 顶部有 mode 切换控件；切换不重建 dialog、不丢失另一 mode 的状态（如 PATH 模式已扫到的 agent 列表）。
- [ ] AC-2：切到目录模式后，列出当前所有 distinct directory，按引用计数 desc + 路径 asc 排序；选中某行后高亮。
- [ ] AC-3：选中 directory 后，命令 ComboBox 列出 `commandTemplates` 所有 key；选 key 后下方预览展开命令（`$codex` → 实际命令字符串）。
- [ ] AC-4：确认按钮在 (directory 选中) AND (command 选中) 后启用；点击后创建新 entry，走 snapshot/save/rollback 路径；创建成功 dialog 关闭，主窗口刷新可见新 entry。
- [ ] AC-5：创建失败（注入 save 失败）时，`_entries` 不变（快照回滚），dialog 不关，StatusText 显示错误。
- [ ] AC-6：取消按钮永远可用；点击后 dialog 关闭，无任何 mutation。
- [ ] AC-7：Name 字段默认填 directory 的文件夹名（如 `D:\_AI_\foo` → `foo`），用户可改。
- [ ] AC-8：单元测试覆盖：(a) directory 去重列表构造（含引用计数），(b) 命令 ComboBox 选项 = commandTemplates keys，(c) 确认按钮启用条件（directory + command 都选中），(d) 创建 entry 走 AddEntry 路径，(e) rollback 路径。
- [ ] AC-9：`tools/verify.ps1` 通过。

## Out of Scope

- 扫描文件系统查找新 directory（仅扫已有 entry 的 directory 集合）。
- 批量创建（一次只创建 1 个 entry）。
- 编辑 commandTemplates（在子任务 3 命令管理面板已交付）。
- 合并/重命名 directory（在子任务 4 地址管理面板已交付）。
- 自定义 entry 名字 prompt（直接用文件夹名默认值，用户后续可在主界面 edit）。
- 拖拽 directory 到主界面创建 entry（YAGNI）。

## Open Questions

无。所有决策点已在 Requirements 与 design.md ADR 中锁定。
