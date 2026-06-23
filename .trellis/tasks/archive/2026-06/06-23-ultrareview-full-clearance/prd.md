# ultrareview 全量清零 — Phase 2

## Goal

清零 ultrareview 全部剩余延后项，置信度无限逼近 100%。

## Phase 1 已完成（549 tests, 0 failures）

17 硬 Bug + 12 边界缺陷 + 7 代码质量 + 4 文档 + 27 新测试。

## Phase 2 — 剩余待实施（8 项）

### 硬 Bug
- B2: 删除 `_dragCardCache` 有缺陷的回退路径（MainWindow.Drag.cs:166-171）

### 边界缺陷
- E4: --config 显式路径加 File.Exists 验证
- E12: `StatusFor` 消除硬编码依赖 — LauncherBatchLaunchReport 加 `FailureIndex`

### 代码质量
- Q2: CompareEntries 四个 bool 标志位 → enum `LauncherSortMode`
- Q4: LauncherPresetViewModel.EntryCount 构造函数缓存

### 设计/布局
- F13: Details 视图列宽从 LauncherDesignTokens 引用

### 测试
- F15: 全局确认 preset 路径专项测试（~3 tests）

### 文档
- D5: 从 README.md 和 AGENTS.md 删除 omx_wiki/ 引用

## 明确不做（有代码库证据支撑）

| 项 | 不做原因 | 证据 |
|----|---------|------|
| B14 pwsh EncodedCommand | 与项目哲学冲突 | DESIGN.md: "不再判断命令危险不危险"; README: "Treat every config file as trusted local input" |
| E8 Preflight UX | 无真实用户投诉 | README 已记录当前行为，预演失败时仍展示失败列表 |
| Q8 Store 接口抽象 | 提前抽象 | 单进程桌面应用，静态 Store 无测试困难 |
| F3 searchHistory autocomplete | future-workload | 审计自身标记，无真实用户需求 |

## Acceptance Criteria

- [ ] `dotnet build` 0 errors 0 warnings
- [ ] `dotnet test` 全部通过（≥549）
- [ ] B2/E4/E12/Q2/Q4/F13/F15/D5 全部实施
