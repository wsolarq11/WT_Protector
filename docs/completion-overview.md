# 项目完成度总览

**生成日期**: 2026-07-23
**基线提交**: `4aebd24` (HEAD, 工作树干净)
**项目**: WT Protector (Avalonia/.NET Windows Terminal 工作区启动器)
**文档更新日期**: 2026-07-23 (新增 4 项实施: collab-debt-findings 更新 + ponytail 注释 + CI full-gate + 死代码评估)

---

## 目录

1. [增量 (Increments)](#1-增量-increments)
2. [待办 (Todos)](#2-待办-todos)
3. [剩余 (Remaining)](#3-剩余-remaining)
4. [遗留 (Legacy/Leftover)](#4-遗留-legacyleftover)
5. [后续 (Follow-ups)](#5-后续-follow-ups)
6. [附录](#6-附录)

---

## 1. 增量 (Increments)

### 1.1 近期工作概览 (最近 30 个提交)

最近 30 个提交覆盖了 2026-07-23 约 4 小时的高密度工作，包括 1 个手动修复提交、12 个 Snow Team 自动提交和 5 个 Snow Team 合并提交。主要工作集中在安全审计修复、架构改革基础建设、文档清理和测试增强四个方向。

### 1.2 安全审计修复 (已完成)

| 审计项                                   | 描述                                    | 代码修复                                                                        | 门禁                                                   |
| ---------------------------------------- | --------------------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------ |
| **F1: Path.GetFullPath** (MEDIUM)        | 路径归一化防御深度                      | `DirectoryPath.cs` JsonConverter.Read 添加 `Path.GetFullPath`                   | `verify.ps1` security-audit-f1-path-getfullpath        |
| **F2: Preset entryId 交叉验证** (MEDIUM) | 运行时验证 preset 引用的 entryId 有效性 | `IPresetService` 新增 `knownEntryIds` 参数，`PresetService.FindInvalidEntryIds` | `verify.ps1` security-audit-f2-preset-entryid-crossref |
| **F3: EntryId 同形字保护** (MEDIUM)      | Unicode Latin 脚本白名单防御同形字攻击  | `EntryId.cs` 新增 `LatinScriptRanges` + `EnsureLatinScript()`                   | `verify.ps1` security-audit-f3-homoglyph               |

修复提交: `4aebd24` (+47/-2, 3 个文件)

### 1.3 AppStore 线程安全重构

`b85dfb2` 是一次大规模重构 (35 个文件, +310/-730)，核心变更包括:

- 引入 `SemaphoreSlim(1,1)` 确保线程安全
- 新增 `Dispatch`/`DispatchAsync`/`DispatchErrorOr` Reducer 模式
- 新增 `SelectAsync` 异步安全读取
- 移除 `_stateChanged` 在 lock 内调用导致的死锁风险
- 新增架构规则 R11 (Infrastructure 不依赖 ViewModels) 和 R12 (Features.Dialogs 不引用 Views)
- `LauncherStatusContent`/`LauncherStatusTone` 从 ViewModels 迁移到 Features
- `DialogService` 从 Features.Dialogs 迁移到 Views
- 删除 4 个未使用的视图控件 (-530 行)
- 移除 emoji 字符
- ArchitectureRules.cs 新增 28 行测试

### 1.4 架构改革基础建设

| 变更                         | 提交      | 说明                                                                 |
| ---------------------------- | --------- | -------------------------------------------------------------------- |
| 架构改革提案                 | `dbe86ed` | `docs/architecture-reform-proposal.md` (+253)，识别 5 类 18 个脆弱点 |
| 架构模式研究报告             | `7625167` | `docs/architecture-research-report.md` (+593)                        |
| LayoutState/Actions/Reducers | `333ba2f` | 96 行新增，状态集中化第一步                                          |
| MemoizedSelector 工具        | `002963a` | 通用记忆化选择器 (28 行)，应用于 LayoutManager                       |
| 声明式属性重构               | `8c17f8f` | `LauncherEntryViewModel.Layout.cs` 24 行重构                         |
| 守卫逻辑清理                 | `e718330` | Layout.cs 守卫逻辑精简                                               |
| 路径统一清理                 | `c801a50` | 移除冗余 `ReplaceAllEntries` 方法 (-12 行)                           |

### 1.5 文档与配置更新

| 文件                                        | 变更                                                |
| ------------------------------------------- | --------------------------------------------------- |
| `docs/architecture-reform-proposal.md`      | 新增 (+253)                                         |
| `docs/architecture-research-report.md`      | 新增 (+593)                                         |
| `collab-debt-findings.md`                   | 新增 (+201)                                         |
| `ultrareview-2026-07-23.md`                 | 新增 (+139)                                         |
| `f2-complete.md` / `f3-complete.md`         | 新增 (共 +120)                                      |
| `phase2-complete.md` / `phase3-complete.md` | 新增 (共 +88)                                       |
| `README.md`                                 | 硬编码数字改为动态格式 + emoji 清理                 |
| `DESIGN.md`                                 | 日期同步为 2026-07-23 + emoji 清理                  |
| `AGENTS.md`                                 | emoji 清理 (2 处)                                   |
| `.wt-protector.dsc.yaml`                    | emoji 清理 (2 处)                                   |
| `config/launcher.json`                      | JSON 格式化 + 路径前缀修正 (`D:\_AI_\` -> `D:\AI\`) |

### 1.6 测试增强

| 阶段               | 测试数 | 增量 |
| ------------------ | ------ | ---- |
| UltraReview 审查时 | 671    | -    |
| F2+F3 安全审计修复 | 677    | +6   |
| Phase 3 测试补充后 | 688    | +11  |

Phase 3 详细增量: 3 个低断言测试文件补充，新增 9 个测试方法、16 个断言:

| 测试文件                               | 原断言数 | 现断言数 | 新增断言 |
| -------------------------------------- | -------- | -------- | -------- |
| `MainWindowDragInsertDecisionTests.cs` | 2        | 16       | +14      |
| `DiscoverAgentsViewModelTests.cs`      | 3        | 12       | +9       |
| `LauncherBatchLaunchPlanTextTests.cs`  | 3        | 11       | +8       |

### 1.7 代码清理

| 清理项                        | 提交      | 说明                                   |
| ----------------------------- | --------- | -------------------------------------- |
| 未使用视图控件                | `b85dfb2` | 4 个 .axaml + .axaml.cs 删除 (-530 行) |
| ReplaceAllEntries 冗余方法    | `c801a50` | -12 行                                 |
| DirectoryPathJsonConverter.cs | `c3dffa1` | 重复文件删除                           |
| test_output.txt               | `16ae3d6` | 陈旧测试输出删除 (-706 行)             |
| LaunchPlan.cs Obsolete        | `4aebd24` | 移除误导性 `[Obsolete]` 标记           |

### 1.8 正面评价

- `verify.ps1` 门禁设计优秀，22 项检查覆盖全面且有明确动机文档
- 系统化的死代码清理习惯 (15 个以上专门重构/清理提交)
- 3 波现代化全部完成并归档
- Agent 协作模式健康 (11.8% Agent 比例)，开发者主导方向
- Code-reviewer Agent 和 UltraReview 批量修复表明多层质量检查
- 单文件架构迁移成功，无残留三文件引用

---

## 2. 待办 (Todos)

### 2.1 代码中的 TODO 标记

**扫描结果**: 无遗留 TODO 标记。

- `src/avalonia-launcher/src/**/*.cs` -- 0 个 TODO 标记 (已排除 `ponytail:` 和 `Debug.WriteLine`)
- `tools/**/*.ps1` -- 0 个 TODO 标记 (已排除 `ponytail:` 和 `Debug.WriteLine`)

### 2.2 DESIGN.md 开放问题

`DESIGN.md` 的 "Open questions" 章节显示: **None currently**。当前无已记录的设计开放问题。

---

## 3. 剩余 (Remaining)

### 3.1 架构改革提案 - 未完成实施

**提案文件**: `docs/architecture-reform-proposal.md` (提交 `dbe86ed`)

提案提出 3 步架构改革消除 5 类 18 个系统性脆弱点。当前实施进度:

| 变革步骤                                   | 状态       | 说明                                                               |
| ------------------------------------------ | ---------- | ------------------------------------------------------------------ |
| 变革一: 派生数据优先 + 记忆化 Selector     | 部分完成   | MemoizedSelector 已创建并应用于 LayoutManager；守卫逻辑已移除      |
| 变革二: 状态集中化 + 不可变 State (Fluxor) | 初始完成   | LayoutActions、LayoutReducers、LayoutState 已创建 (96 行)          |
| 变革三: 声明式属性依赖                     | 部分完成   | LauncherEntryViewModel.Layout 已改为声明式属性                     |
| 迁移第 1 步 (1-2 天)                       | 部分完成   | TryRefreshCardSize 重构、CardSizeSelector、MemoizedSelector 已完成 |
| 迁移第 2 步 (3-5 天)                       | **未开始** | Fluxor State 全面迁移、ReplaceEntries/ReplaceAllEntries 统一       |
| 迁移第 3 步 (1 天)                         | 部分完成   | 手工 OnPropertyChanged 替换为 [NotifyPropertyChangedFor]           |

**剩余工作**:

- 迁移第 2 步: 将所有 VM 可变字段迁移到 Fluxor State，统一 `ReplaceEntries`/`ReplaceAllEntries`
- 迁移第 3 步: 完成所有手工 `OnPropertyChanged` 替换
- 验证: 确保所有测试通过

### 3.2 verify.ps1 ponytail 技术债

`verify.ps1` 中标记了 3 个已知局限性:

| 位置                                        | 局限性                               | 升级路径                                                      |
| ------------------------------------------- | ------------------------------------ | ------------------------------------------------------------- |
| `verify.ps1:529` (no-stale-three-file-refs) | 只检查 `$activeDocs` 列表中的文档    | 添加新文档到 `$activeDocs` 数组                               |
| `verify.ps1:555` (settings-single-source)   | 依赖特定代码模式指纹，重构后可能失效 | 未显式文档化                                                  |
| `verify.ps1:567` (winget-config-schema)     | 无 DSC v3 时无法执行完整 winget 验证 | 安装 DSC v3 后运行 `.\tools\apply-machine-config.ps1 -DryRun` |

此外，新增的 3 个安全审计门禁 (F1/F2/F3) 尚未添加 ponytail 注释说明局限性和升级路径。

### 3.3 collab-debt-findings 优化建议

以下建议来自技术债务审查报告，当前未处理:

- **Agent 类型目录** (发现 4.2-A): 建立 Agent 类型目录以复用最佳实践
- **CI 验证** (发现 4.4-B): merge 前自动运行 verify.ps1
- **方法级别死代码门禁** (发现 5.5-C): 目前无自动化检查

---

## 4. 遗留 (Legacy/Leftover)

### 4.1 已修复的遗留问题

以下问题在 `collab-debt-findings.md` 中被标记为未修复，但在最新代码中已确认修复:

| 问题                                     | 原始状态 | 修复提交             | 当前状态                                        |
| ---------------------------------------- | -------- | -------------------- | ----------------------------------------------- |
| **F1: Path.GetFullPath** (MEDIUM)        | 未修复   | `4aebd24`            | 已修复 - DirectoryPath.cs 调用 Path.GetFullPath |
| **F2: Preset entryId 交叉验证** (MEDIUM) | 部分解决 | `4aebd24` + 团队提交 | 已修复 - IPresetService 运行时验证              |
| **F3: 同形字保护** (MEDIUM)              | 未修复   | `4aebd24` + 团队提交 | 已修复 - EntryId.cs Latin 脚本白名单            |
| **安全审计 Gate 缺失** (HIGH)            | 无门禁   | `4aebd24`            | 已修复 - verify.ps1 新增 3 个回归门禁           |
| **Emoji 使用** (Medium)                  | 存在     | Phase 2 提交         | 已修复 - 零 emoji 残留                          |
| **Dead code: WeakReferenceMessenger**    | 未清理   | `77504425`           | 已完全清理                                      |
| **Dead code: LiteDB**                    | 未清理   | `3ce3619`, `ef978b7` | 已完全清理                                      |

### 4.2 确认已清理的项

- **三文件架构引用**: `no-stale-three-file-refs` gate 确认文档不引用废弃的三文件模式
- **BuildStoreFile 单路径**: `settings-single-source-buildstorefile` gate 确认仅 1 处 hand-assembly
- **命令式 UI 构建**: `no-imperative-view-ui` gate 确认 Views 层无 C# 命令式 UI 构建
- **DESIGN.md Open Questions**: 当前显示 "None currently"
- **死代码清理**: 15 个以上专门重构/清理提交
- **3 波现代化**: 全部完成并归档

### 4.3 待处理的遗留问题

| 问题                             | 来源                                          | 当前状态   |
| -------------------------------- | --------------------------------------------- | ---------- |
| `collab-debt-findings.md` 已过时 | 该文档在 F1/F2/F3 修复前编写                  | **待更新** |
| 团队任务列表与实际情况不一致     | 列表中有 F2/F3/Phase2/Phase3 待办但实际已修复 | **待同步** |

---

## 5. 后续 (Follow-ups)

### 5.1 短期改进

1. **更新 collab-debt-findings.md** -- 该文档的严重程度汇总和主要行动项已过时，需要反映 F1/F2/F3 已修复的当前状态
2. **补充安全审计门禁 ponytail 注释** -- verify.ps1 中新增的 3 个安全回归门禁尚未添加 ponytail 注释说明局限性和升级路径
3. **同步团队任务列表** -- F2/F3/Phase2/Phase3 实际已在 `4aebd24` 及之前提交中完成，需更新任务状态

### 5.2 中期改进

1. **架构改革第 2 步实施** -- Fluxor 状态集中化迁移 (3-5 天工作量)，消除双路径不一致和幽灵状态
2. **架构改革第 3 步收尾** -- 完成所有手工 `OnPropertyChanged` 替换为声明式属性
3. **CI 集成** -- 在 merge 前自动运行 verify.ps1，确保 gate 通过后再合并
4. **Ponytail 升级评估** -- 评估 verify.ps1 中 3 个 ponytail 注释标注的已知局限性是否需要处理

### 5.3 长期改进

1. **架构改革验证** -- 验证 Fluxor/Selector 架构是否有效消除了 5 类脆弱点
2. **Agent 类型目录** -- 建立 Agent 类型目录以复用最佳实践
3. **方法级别死代码检查** -- 考虑在 verify.ps1 中添加对"单个调用者的公有方法"模式的检测
4. **Gate 覆盖度扩展** -- 评估将更多安全审计发现纳入 verify.ps1 自动化门禁

---

## 6. 附录

### A. 测试统计

| 指标               | 值                    |
| ------------------ | --------------------- |
| 当前测试总数       | 688                   |
| 测试文件数         | 约 50+                |
| 最近增量 (Phase 3) | +9 测试方法, +16 断言 |
| 构建状态           | 0 错误, 0 警告        |

### B. verify.ps1 Gate 检查统计

| 类别            | 数量   | 说明                                                                                |
| --------------- | ------ | ----------------------------------------------------------------------------------- |
| 仓库卫生        | 2      | line-ending-policy, snow-ignored-local-state                                        |
| 遗留路径        | 1      | retired-paths-removed                                                               |
| 代码质量        | 3      | no-bare-catch-exception, app-raw-hex-zero, no-imperative-view-ui                    |
| 主目录布局      | 1      | mainline-layout                                                                     |
| 文档            | 4      | readme, feature-objective-boundary, claim-boundary-ledger, no-stale-three-file-refs |
| 市场证据        | 1      | github-similar-projects-snapshot                                                    |
| 配置模式        | 5      | config-schema, presets-schema, settings-json, settings-single-source-buildstorefile |
| 备份/退役       | 3      | shortcut-backup-retirement, winforms-retired-design, hta-retired-config             |
| 机器配置        | 1      | winget-config-schema-valid                                                          |
| 安全审计 (新增) | 3      | security-audit-f1/f2/f3                                                             |
| Avalonia 主目录 | 1      | avalonia-mainline-verify                                                            |
| **总计**        | **25** |                                                                                     |

### C. Git 统计

| 指标                     | 值                  |
| ------------------------ | ------------------- |
| 总提交数 (2026-07-01 起) | 274                 |
| Agent 提交数             | 32 (11.8%)          |
| 手动提交数               | 242 (88.2%)         |
| 工作树状态               | 干净 (无未提交变更) |
| 当前 HEAD                | `4aebd24`           |

### D. 项目健康度摘要

| 维度              | 状态     | 说明                                              |
| ----------------- | -------- | ------------------------------------------------- |
| 安全审计 F1/F2/F3 | 已完成   | 已修复并添加 verify.ps1 回归门禁                  |
| 门禁覆盖          | 25 项    | 涵盖仓库卫生、配置模式、安全审计、Avalonia 主目录 |
| 测试              | 688 通过 | 持续增长，Phase 3 补充低断言文件                  |
| 架构改革          | 部分完成 | 第 1 步进展良好，第 2 步 (Fluxor 迁移) 未开始     |
| 代码 TODO         | 0 个     | 代码干净，无遗留标记                              |
| 文档              | 完整     | README/DESIGN/spec 覆盖全面                       |
| 死代码            | 已清理   | 15+ 专门清理提交，现代化全部完成                  |
| 协作模式          | 健康     | 开发者主导 (88.2%)，Agent 辅助 (11.8%)            |
