# 集中管理与视觉降噪

## Goal

把 WT Launcher 从"扁平 18 个 entry"升级为"按 command/directory 集中管理 + 视觉折叠 + 频率排序"。让用户在 14 个 `$codex` 入口、3 对重复 directory、1 个裸命令（没走模板）等场景下，能批量管理 + 视觉降噪 + 高频自动靠前，而不是逐个 AddEntry/EditEntry。

## Background

驱动本任务的现状证据（来自 `config/launcher.json` 2026-07-11 快照）：

- 18 个 entry，去重后 15 个独立 directory，**3 对重复**：
  - `D:\<PROJECT_ROOT>\WT_Protector` × 2（codex + opencode）
  - `D:\<PROJECT_ROOT>\ER\crawler\Data_Acquisition_and_Analysis` × 2（codex + claude）
  - `D:\<PROJECT_ROOT>\ER\crawler\phoenix-crawler` × 2（codex + claude）
- entry.command 分布：`$codex` × 14, `$claude` × 2, `$opencode` × 1, **裸 `codex` × 1**（未走 `commandTemplates`，没吃到 `--enable goals --dangerously-bypass-approvals-and-sandbox`）
- `commandTemplates` 已经把命令参数集中（`$codex` → 固定参数串），命令去重发生在数据层；entry 的 `directory` 是注入到模板的变量。这个模型本质等价于 Alfred Action+Trigger 模型。

三方调研（`.trellis/research/launcher-data-models.md` / `wt-ecosystem-config-models.md` / `matrix-ui-and-directory-library.md`）一致结论：

- **不要重构 schema**。当前 `entry = (directory, command)` 二元组 + `commandTemplates` + `presets` 已对齐业界标准（Alfred Action/Trigger、tmuxinator 声明式二元组、Cmder task group）。schema 重构会破坏 preset/搜索/排序且业界无人这么做。
- "15 个 codex entry 重复"是 JSON 表象，不是结构性重复。视图层折叠即可解决。
- "目录扫描器"是 WT 生态空白（WT 自身只扫 shell 不扫工作目录），是有价值的差异化。
- zoxide frecency aging 是 30 年成熟算法，加 `launchCount` 字段即可套用。

## Scope (6 项叠加 → 5 个 child)

| # | Child | 范围 | 改动面 |
|---|---|---|---|
| 1 | `07-11-cards-command-grouping` | Cards 视图加分组维度（无/按 command/按 directory）+ 组头"启所有"按钮（同时是 Preset quick-launch） | View 层 + `LauncherEntryVisibility` |
| 2 | `07-11-entry-launch-count-frecency` | entry schema 加 `launchCount` 字段（optional, 默认 0，schemaVersion 保持 2）+ 新排序模式 `frequent`（zoxide frecency 公式） | Core schema + Layout 排序 |
| 3 | `07-11-command-management-dialog` | Modal dialog Tab 1：去重表格列 command / 引用次数 / 引用 entry；双击弹 edit dialog（用模板/裸命令切换）；引用 > 1 时同步更新确认；右键编辑/查重/合并/删除 | View + Dialog + 批量 mutation |
| 4 | `07-11-directory-management-dialog` | Modal dialog Tab 2：同上针对 `entry.directory`；edit dialog 含资源管理器浏览按钮 | View + Dialog + 批量 mutation |
| 5 | `07-11-directory-scan-wizard` | Discovery 对话框扩展：除扫 PATH 找 CLI，新增"扫已有 entry 的 directory 列表"模式；选中 directory → 选命令 → 一键创建 entry | Discovery + 新 DirectoryList 视图 |

注：原 #6 Preset quick-launch 与 #1 视图折叠共用代码路径（组头"启所有"按钮），合并到 Child 1。

## Requirements

### Parent-level（跨 child 一致性）

- **R-P1**: 保持 `schemaVersion=2`，所有字段新增都是 optional + 默认值（如 `launchCount: 0`）。向后兼容现有 v2 读取，不引入 v3 schema migration。整套方案对旧 store 文件零破坏。
- **R-P2**: 所有批量 mutation 走现有 `LauncherEntryMutationSnapshot` 快照/回滚路径，失败时还原 entries + presets + settings。
- **R-P3**: 所有 UI 文案与现有风格一致（中文，与 `StatusText` / `LauncherEntryStatus` 等同语种），与 `MainWindow.axaml` 现有 design tokens 一致（不引入新色号、字号、间距）。
- **R-P4**: 所有 mutations 通过现有 `SaveStoreFile` 原子写路径，不允许直接 `File.WriteAllText`。
- **R-P5**: 不引入新 NuGet 依赖（dialog/扫描/排序全部用 stdlib + 现有依赖）。

### Per-child

详见各 child `prd.md`。本 prd 仅锁范围 + acceptance。

## Acceptance Criteria

**每条 AC 都要求有对应单元测试覆盖**（不只手动 smoke）。手动 smoke 列在 AC 后面 "(smoke)" 标记。

- [ ] **AC-1** (Child 1): Cards 视图工具栏有"分组"切换，选"按 command"时同 command 的 entry 折叠成 1 个组头（含计数 + "启所有"按钮），点击展开看所有同 command 子项。(smoke: 选"按 command"后 codex entry 折叠成 1 组)
- [ ] **AC-2** (Child 2): 启动任意 entry 后 `launchCount` 自增并持久化；切到 `frequent` 排序模式后，按 zoxide frecency 公式（exponential recency decay，具体公式在 Child 2 design.md 定）排序，高频 entry 自动靠前。单元测试断言 `launchCount` 持久化 + 排序结果符合公式。
- [ ] **AC-3** (Child 3): 命令管理面板按 command 去重列出（含引用次数）；批量改 command 时弹同步更新确认 dialog，确认后所有引用 entry 的 command 同步更新。单元测试覆盖：(a) 去重表格构造，(b) 批量改 command 写回所有引用 entry，(c) snapshot/rollback 在保存失败时还原。(smoke: 把 `codex` 裸命令改成 `$codex`，1 个 entry 同步)
- [ ] **AC-4** (Child 4): 地址管理面板按 directory 去重列出（含引用次数）；合并两个 directory 时用户选保留方，失效方的所有 entry 引用更新到保留方。单元测试覆盖：(a) 去重表格，(b) 合并 mutation 写回，(c) 用户选保留方的语义（保留方 directory 不变，失效方 entry.directory = 保留方 directory）。(smoke: 3 对重复 directory 各合并一次)
- [ ] **AC-5** (Child 5): Discovery 对话框新增"扫已有目录"模式，列出当前所有独立 directory 让用户挑；选中后选命令（来自 `commandTemplates`）→ 一键创建 entry。单元测试覆盖：(a) directory 去重列表构造，(b) 批量创建 entry 路径与现有 DiscoverAgentsAsync 一致。
- [ ] **AC-6** (跨 child): `tools/verify.ps1` 16 项全 PASS。测试数估算（per child）：管理面板 +10、视图折叠 +5、frecency +5、扫描向导 +5、跨 child 集成 +5，总计 ≥ +30 个新测试。
- [ ] **AC-7** (跨 child): `config/launcher.json` 在所有 mutation 后保持 schema 合法（`schemaVersion=2` 不变；旧 store 文件零迁移可读）。
- [ ] **AC-8** (跨 child): 无新 `catch (Exception)` bare catch；新异常类型走 `ExceptionFilters.IsRecoverable` + `ErrorLog.Write`。

## Out of Scope

- schema v3 重构（`directories[]` + `commands[]` 顶级数组 + ID 引用）—— 调研明确否决。
- 跨设备同步、云备份。
- 启动后动效、详情弹窗动画、皮肤切换。
- 全盘扫描磁盘找新目录（仅扫已有 entry 的 directory 集合 + 用户手输/资源管理器选）。
- 新 view mode（不加第 4 种 view mode，分组是 Cards 子选项）。
- 调整 `settings.sortMode` 默认值（当前 `updated`，即使加 `frequent` 模式也保持 `updated` 默认，避免偷换用户偏好）。
- Linux/macOS 适配（仍 win-x64 only）。

## Open Questions

- **OQ-1**: 07-10-drag-scan-fixes 任务何时 archive？（建议：本任务的 Commit 1 收尾后立即 archive 07-10）
- **OQ-2**: Child 2 frecency 公式直接套 zoxide 实现还是简化（如 `score = launchCount / (1 + daysSinceLastLaunch)`）？进入 Child 2 design.md 时决定。
- **OQ-3**: Child 3 命令管理面板是否合并 `commandTemplates` CRUD 到同一面板？或分两个 tab？进入 Child 3 design.md 时决定。

## Task Structure

```
Parent: 07-11-centralize-declutter (集中管理与视觉降噪) — 本文件
├── Child 1: 07-11-cards-command-grouping (视图折叠 + Preset quick-launch) — P1
├── Child 2: 07-11-entry-launch-count-frecency (频率排序) — P2
├── Child 3: 07-11-command-management-dialog (命令管理面板) — P1
├── Child 4: 07-11-directory-management-dialog (地址管理面板) — P1
└── Child 5: 07-11-directory-scan-wizard (扫描向导) — P2
```

实施顺序建议：Child 3 + 4（管理面板，最具体痛点） → Child 1（视图折叠，最大视觉杠杆） → Child 2（frecency，小改动高收益） → Child 5（扫描向导，依赖管理面板字段定义）。

## References

- `.trellis/research/launcher-data-models.md` — Raycast/Alfred/ueli/uTools 数据模型调研
- `.trellis/research/wt-ecosystem-config-models.md` — WT/Cmder/Tabby/WezTerm 配置模型调研
- `.trellis/research/matrix-ui-and-directory-library.md` — 二维 UI + 文件管理器地址库调研
- `docs/giants-shoulders/launcher-first-class-patterns.md` — launcher 一等公民模式（gate 引用文件）
- `CLAUDE.md` — 非协商不变量（原子写、单源真、catch(Exception) 禁用等）
- `docs/feature-reality-north-star-audit.md` — claim 边界
