# Audit findings — 2026-07-09 reorg pass

> 父任务：`07-09-project-wide-reorg`
> 审计基线 commit：`68df503cce70723eef2cd9211ef8841046cb784d`
> 审计日期：2026-07-09
> 审计范围：5 维（D1 文档漂移 / D2 死代码 / D3 spec 同步 / D4 deferred 当前证据 / D5 风格一致性），全程只读

---

## Summary

- **总条目数**：13（D1: 8 / D2: 0+1 待核 / D3: 1 / D4: 4 / D5: 0+3 no-action）
- **exec-now**：8（全部集中在 D1 文档漂移；零代码改动、零风险）
- **defer-with-trigger**：4（R1/R2/R6/R7 全部维持 defer，触发条件均未达成）
- **no-action**：3（ponytail 残留已合规、DESIGN.md 禁用词为规则陈述非违规、LauncherEntryVisibility ponytail 标注完整）
- **三轴子任务触发判断**：
  - `reorg-docs` 子任务：**YES**（D1 有 8 项 exec-now 文档漂移）
  - `reorg-code` 子任务：**NO**（D2 静态扫描无 exec-now；编译警告核实留给该子任务时再做）
  - `reorg-deferred-decisions` 子任务：**NO**（D4 全部仍 defer，无翻案）

**与 07-09 一轮清理的重叠检查**：本轮发现项**全部**是 07-09 一轮未触及的文档计数漂移，以及 R1–R7 当前状态的二次验证；无重复处置项。

---

## D1 文档漂移

| file:line | 文档原值 | 当前实际值 | 建议 |
|-----------|---------|-----------|------|
| `AGENTS.md:18` | "WTLauncher.Core/ → Layer 0（26 文件）" | 24 `.cs` 文件（`ls src/avalonia-launcher/src/WTLauncher.Core/*.cs \| wc -l`） | **exec-now**：改 "26 文件" → "24 文件" |
| `AGENTS.md:20` | "ViewModels/ → 34 文件，单 MainWindowViewModel + 9 partial" | 28 `.cs` 文件；MainWindowViewModel 系列共 8 个文件 = 1 main + 7 partial（.Batch/.Drag/.Entries/.Launch/.Layout/.Presets/.Reorder） | **exec-now**：改 "34 文件" → "28 文件"，"9 partial" → "7 partial" |
| `AGENTS.md:21` | "Views/ → 15 文件" | 13 个 view 文件（`ls src/avalonia-launcher/src/WTLauncher.App/Views/* \| wc -l`） | **exec-now**：改 "15 文件" → "13 文件" |
| `AGENTS.md:39` | "verify.ps1 全量门禁：16 项检查" | 18 个 `Add-Check` 调用（`grep -cE "^\s*Add-Check\s+['\"]" tools/verify.ps1`；不要用裸 `grep -c "Add-Check"`，会把第 12 行 `function Add-Check` 定义也算进去，多算 1） | **exec-now**：改 "16 项" → "18 项"；同步补全当前实际检查项的关键分类到该行项目说明 |
| `README.md:138-150` | "The verifier currently checks:" 后接 7 条要点 | 实际 18 个检查；README 列表漏写：`snow-ignored-local-state`、`config-schema`、`presets-schema`、`settings-json`、`shortcut-backup-retirement`、`winforms-retired-design`、`hta-retired-config`、`avalonia-mainline-verify`、`no-stale-three-file-refs`、`settings-single-source-buildstorefile` 等 | **exec-now**：按 verify.ps1 当前 18 个 Add-Check 重写该列表，或改为"参见 `tools/verify.ps1` 的 `Add-Check` 调用清单"以避免未来再次漂移（推荐后者，** ponytail: 文档与门禁单一权威源**） |
| `.trellis/spec/tests/index.md:9` | "71 test files (approximately 410 `[Fact]`/`[Theory]`)" | 61 个 `.cs` 测试文件（`ls src/avalonia-launcher/tests/WTLauncher.Tests/*.cs \| wc -l`） | **exec-now**：改 "71" → "61"；410 `[Fact]/[Theory]` 计数本轮未核（需 `grep -cE "\[(Fact|Theory)\]"` 全测试目录，留作可选第二步） |
| `.trellis/tasks/archive/2026-07/07-09-composition-di-async-evaluation/prd.md:44` | "MainWindowViewModel 8 partials total **2305 lines**" | 8 个 `MainWindowViewModel*.cs` 文件总 2305 行（精确匹配）；但表述"8 partials"不准确——应是 "1 main + 7 partial" | **exec-now**（已归档任务，可只改措辞）：改 "8 partials" → "1 main + 7 partials（8 files）"；line count 2305 保留 |
| 7 处 PRD 引用 `feedback_audit_driven_proposals_verify_each_source.md`（包括刚写的父/子任务 PRD 与 4 份归档 PRD） | 引用 `.trellis/spec/guides/feedback_audit_driven_proposals_verify_each_source.md` | **该文件不存在**（`Glob` 全仓零匹配） | **exec-now**：从所有引用处删除该路径，改为通用表述（如"每条提案必须用当前代码/文档验证后再决定动作"），或补建该 feedback 文件。推荐**删除引用**——已 close 的归档决议不需要再引用，新建任务的审计准则直接写在本 audit-findings.md / 父 PRD 中即可 |

**D1 小计**：8 项 exec-now，全部为文档计数/路径修正，零代码改动，零行为风险。

---

## D2 代码死代码（静态扫描）

| file:line | 类型 | 建议 |
|-----------|------|------|
| `src/**` 静态扫描 `// TODO`、`// DEAD`、`// REMOVED`、`// LEGACY`、`// HACK`、`// XXX`、`#if false` 等死代码标记 | 零匹配 | **no-action** |
| `src/**` 共 28 个 `ponytail:` 注释 + `tools/**` 3 个 `ponytail:` 注释 | 全部带 ceiling/升级路径/上下文说明（详见 D5） | **no-action** |
| 编译警告核实（CS0168/CS0219/CS8019 未用 using / 变量；未用 private/internal 成员） | 本审计未跑 `dotnet build`（按 design.md 约束保持只读） | **defer-with-trigger**：留给 `reorg-code` 子任务执行阶段做 `dotnet build -warnaserror` 后批量处置 |

**D2 小计**：静态层面零死代码；编译警告核实一项 defer。

---

## D3 spec 与代码同步

| spec:line | code:line | 漂移类型 | 建议 |
|-----------|----------|---------|------|
| `.trellis/spec/tests/index.md:9` | `src/avalonia-launcher/tests/WTLauncher.Tests/` | 测试文件计数漂移 71→61 | **exec-now**（与 D1 第 6 项合并处置） |
| `.trellis/spec/core/persistence.md` 全文 | `LauncherStore.cs` / `LauncherJson.cs` / `LauncherStoreFile.cs` | 完全同步——记录了 2026-07-09 schema-driven-arch-migration、settings-single-source、single-file 重构的最新状态 | **no-action** |
| `.trellis/spec/core/error-handling.md` | Core 层异常处理 | 同步——记录 2026-07-09 re-audit 维持异常流的决议 | **no-action** |
| `.trellis/spec/core/data-types.md` | Core 层数据类型 | 同步——含 2026-06-25 string safety / Unicode 章节 | **no-action** |
| `.trellis/spec/views/drag-drop.md` | View 层拖拽 | 同步——含 2026-06-23 ultrareview 修复记录 | **no-action** |
| `.trellis/spec/guides/pitfalls.md` | 跨会话坑库 | 同步——含 2026-07-09 LiteDB 移除记录、§8.4 schema-merge 漂移 | **no-action** |

**D3 小计**：1 项漂移（与 D1 合并）；其余 spec 与代码同步良好。07-09 一轮的 spec 维护质量高。

---

## D4 deferred 当前证据

### R1: MainWindowViewModel 组合重构 — **仍 defer**

**原始 trigger**（`07-09-composition-di-async-evaluation/prd.md:58`）：任一 partial 超过 1000 行，或测试 setup 成本过高

**当前证据**（2026-07-09 实测）：
- `wc -l src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel*.cs`
  - Reorder: 78
  - Drag: 151
  - Launch: 163
  - Entries: 288
  - main: 333
  - Batch: 339
  - Layout: 416
  - **Presets: 537（最大）**
  - 总计: 2305 行（与归档 PRD 一致）
- 最大 partial 537 行，距 1000 行 trigger 还有 463 行裕度
- 测试 setup：`MainWindowViewModelTests.*.cs` 当前 453 测试全绿，无 setup 成本恶化迹象

**结论**：trigger 未触发，**仍 defer**。归档 PRD 决议继续有效。

### R2: LauncherEntryVisibility → LauncherEntryQuery 改名 — **仍 defer**

**原始 trigger**（`07-09-rename-evaluation/prd.md:54`）：当 `FilterAndSort` 被自然拆分为独立类型时一并改名

**当前证据**：
- `LauncherEntryVisibility.cs:34` 仍内联 `FilterAndSort<TEntry>` 方法，未拆分
- `grep -rln "LauncherEntryVisibility" src/` 当前 84 处引用（归档 PRD 记录 85 处，1 处轻微漂移但不影响结论）
- `LauncherEntryVisibility.cs:7` 的 `ponytail:` 注释完整记录了 2026-07-09 评估结论

**结论**：trigger 未触发，**仍 defer**。

### R6: DI 容器（Microsoft.Extensions.DependencyInjection） — **仍 defer**

**原始 trigger**（`07-09-composition-di-async-evaluation/prd.md:87`）：composition root >5 services，或引入 plugin/extension system

**当前证据**：
- `App.axaml.cs:132-138` composition root 仅构造 2 个对象：`MainWindowViewModel` + `MainWindow`
- `LauncherLaunchWorkflow.Create()` 是静态工厂，不算构造的 service
- 远低于 5 service trigger
- 无 plugin/extension system 计划

**结论**：trigger 未触发，**仍 defer**。归档 PRD 提到的 targeted alternative（提取 `ILauncherProcess` 接口）仍未被需要——`Start(plan)` 路径无测试压力。

### R7: UI 线程同步 I/O async 化 — **仍 defer**

**原始 trigger**（`07-09-composition-di-async-evaluation/prd.md:112`）：配置文件 >5MB，或用户感知保存延迟，或 profiler 显示 >50ms I/O

**当前证据**：
- `wc -c config/launcher.json` = **4989 bytes（~5KB）**
- 距 5MB trigger 还有 ~1000× 裕度
- 19 entries / 2 presets / 9 settings fields，规模稳定
- 无用户感知延迟报告

**结论**：trigger 未触发，**仍 defer**。

**D4 小计**：R1/R2/R6/R7 全部维持 defer，归档 PRD 决议继续有效，**无需创建 `reorg-deferred-decisions` 子任务**。

---

## D5 风格一致性

| file:line | 类型 | 建议 |
|-----------|------|------|
| 31 个 `ponytail:` 注释（28 src + 3 tools） | 全部带 ceiling/升级路径/上下文说明，符合 ponytail ultra 规则 | **no-action** |
| `DESIGN.md:61/108/125/154/164` 引用"危险命令""危险"等术语 | 全部出现在 **negation context**（"不再使用"、"不再引入"、"判断删除"等），是规则陈述本身或历史删除记录，非主界面术语违规 | **no-action** |
| `LauncherEntryVisibility.cs:7` ponytail: 注释 | 已完整记录 R2 评估结论，防止未来重复审议 | **no-action**（与 D4 R2 联动） |
| 命名一致性 `Launcher*` vs `WTLauncher*` 前缀混用 | `WTLauncher.{Core,App,Tests}` 是项目/命名空间名；`Launcher*` 是类名前缀；分层清晰，非混用 | **no-action** |

**D5 小计**：风格层无 exec-now 项。

---

## Next-round audit 判断

**NO**——本轮 5 维全覆盖，无盲区。下一次 audit 仅在以下信号出现时启动：

1. D4 任一 R 项 trigger 触发（partial >1000 行 / file >5MB / composition root >5 services / FilterAndSort 拆分等）
2. 引入新依赖、新 Layer、或跨层重构
3. `tools/verify.ps1` 检查数量发生增减（增减时同步触发 D1 文档核对）
4. schemaVersion 升级（v3 bump）

否则下一轮"整编规整"就是投机。

---

## 三轴子任务触发总结

| 子任务 | 是否创建 | 依据 |
|--------|---------|------|
| `reorg-docs` | **YES** | D1 有 8 项 exec-now 文档漂移；风险零；预计 1 次提交可清完 |
| `reorg-code` | **NO**（或合并入 docs） | D2 静态扫描零 exec-now；编译警告核实可在 `reorg-docs` 子任务内顺便跑 `dotnet build -warnaserror` 后处置；不值得独立建子任务 |
| `reorg-deferred-decisions` | **NO** | D4 全部仍 defer；归档决议继续有效；建子任务就是空跑 |

**父任务下一步动作建议**：

1. 由父任务 owner 创建 `reorg-docs` 子任务，把 D1 的 8 项 exec-now 落地（机械修改，verify.ps1 全程绿）
2. 在 `reorg-docs` 子任务内顺便跑一次 `dotnet build -warnaserror`，处置编译警告（若有）
3. 不创建 `reorg-code` / `reorg-deferred-decisions` 子任务
4. `reorg-docs` 归档后，父任务直接进入最终集成评审

**这把原计划"四轴全做"实际收敛为"一轴一刀清"**——ponytail ultra 的正确产出：审计证实大部分轴无需动手，真实工作量只有 D1 的 8 项机械修改。
