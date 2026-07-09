# Design — reorg audit pass

> 复杂任务要求 design.md。本子任务虽只读，但因覆盖 5 维 × 全仓，仍是 complex 任务。

## Boundary

- 输入：当前 `HEAD` 的全仓快照
- 输出：仅 `.trellis/tasks/07-09-reorg-audit-pass/audit-findings.md`
- 不触碰：`src/`、`docs/`、`tools/`、`config/`、`.trellis/spec/`、`README.md`、`AGENTS.md`、`DESIGN.md`、`CLAUDE.md`、`retired/`

## Audit method per dimension

### D1 文档漂移

**信息源**：
- `README.md`、`AGENTS.md`、`DESIGN.md`、`CLAUDE.md`、`src/avalonia-launcher/README.md`
- `docs/feature-reality-north-star-audit.md`、`docs/design-benchmark-digest.md`、`docs/giants-shoulders/**`
- `.trellis/spec/**/index.md` 与各 spec 文件

**核实方式**：
- 文件计数类声明（如"26 Core 文件"/"34 ViewModels"/"77 测试"）：用 `Glob` 取当前实际数对照
- 文件路径类声明：用 `Glob` 验证存在
- schema 字段名/枚举值：与 `src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreFile.cs`、`LauncherSettings.cs`、`LauncherEntry.cs`、`LauncherPreset.cs` 对照
- 命令示例：与 `tools/*.ps1` 当前参数集对照
- 章节链接 / 行号：用 `Grep -n` 验证

**输出**：每条 `file:line + 文档原值 + 当前实际值 + 建议`。

### D2 代码死代码

**信息源**：`src/avalonia-launcher/src/**/*.cs`（不含 `bin/`、`obj/`、`TestResults/`、`artifacts/`）

**核实方式**：
- `dotnet build -warnaserror` 编译警告中的 CS0168/CS0219/CS8019（未用 using / 变量）作为初筛
- `Grep` 找 `ponytail:` 注释，逐条评估是否还合理（ceiling 是否仍存在、是否已被解决）
- 对每个 `private`/`internal` 成员，用 `trace_path` 或 `Grep` 找 inbound call；零 caller 即死代码候选
- 孤儿类型：`search_graph` 找类型定义，`trace_path` 找引用

**输出**：每条 `file:line + 类型 + 建议`。**注意**：编译警告项需要先 `dotnet build` 才能核实，本审计仅做静态扫描，编译警告核实移到 `reorg-code` 子任务执行阶段。

### D3 spec 与代码同步

**信息源**：`.trellis/spec/**/*.md`

**核实方式**：
- 提取每个 spec 文件中提到的类名/方法名/常量名/文件路径
- 用 `search_graph` / `Grep` 验证在当前 `src/` 中是否还存在
- 特别关注 `schema-driven-arch-migration`、`settings-single-source`、`split-mainviewmodel-stage2` 之后 spec 是否同步更新过（从 git log 时间线判断）

**输出**：每条 `spec:line + code:line (或 missing) + 漂移类型 + 建议`。

### D4 deferred 当前证据

**信息源**：
- R1/R6/R7：`.trellis/tasks/archive/2026-07/07-09-composition-di-async-evaluation/prd.md`（及其 design/conclusions）
- R2：`.trellis/tasks/archive/2026-07/07-09-rename-evaluation/prd.md`
- R3/R4/R5 已落地，无需复议
- 当前代码状态：`src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel*.cs`、`LauncherEntryVisibility.cs`、调用 sync I/O 的位置

**核实方式**：
- 读原始 defer 文档，提取每个 R 的 revisit trigger 文本（"当 X 信号出现时重新评估"）
- 用 `Grep` / `Read` 判断 trigger 当前是否触发
- 给出三选一：`仍 defer`（trigger 未触发，且当前代码未恶化）/ `触发执行`（trigger 已触发）/ `正式否决`（trigger 不会触发，或证据表明方向错误）

**输出**：每个 R 一段：原始 trigger / 当前证据 / 结论 / 触发哪个后续动作。

### D5 风格一致性

**信息源**：`src/**/*.cs`、`src/**/*.axaml`、`tools/*.ps1`、所有 `.md` 文档

**核实方式**：
- `Grep "ponytail:"` 全仓扫，对照 ponytail ultra 规则（每条 `ponytail:` 必须带 ceiling 标注 + 升级路径）
- 命名一致性：`Launcher*` vs `WTLauncher*` 前缀、`Launch*` vs `Launcher*`、缩写大小写
- 中英文：user-facing 文案应保持中文一致，代码标识符应保持英文一致；混合处是否合理
- 文档用语：是否还有已弃用术语（"危险命令"、"需确认项"等 DESIGN.md 已禁用词）

**输出**：每条 `file:line + 类型 + 建议`。

## Data flow

```
HEAD snapshot
    │
    ├─ D1 docs        → Grep/Read/Glob → 漂移表
    ├─ D2 dead-code   → Grep + 编译警告核实(留给code子任务) → 死代码表
    ├─ D3 spec sync   → Grep + search_graph → 漂移表
    ├─ D4 deferred    → Read archive + Grep current → R1/R2/R6/R7 结论
    └─ D5 style       → Grep ponytail: + 命名 → 风格表
                              │
                              ▼
                  audit-findings.md (单一产出)
                              │
                              ▼
        父任务据此决定是否建 docs/code/deferred 三子任务
```

## Tradeoffs

- **不做编译警告核实**：D2 维度的"未用 using"等编译警告本审计只做静态扫描，警告核实留给 `reorg-code` 执行阶段。理由：审计保持只读，编译需要 .NET SDK + 还原时间，且警告项处置需要落地动作。
- **不做性能基准**：D2/D5 不测性能，性能问题若存在会在 D4 R7（async I/O）路径上重新评估。
- **不做 UX 体验审计**：D5 不评 DESIGN.md 中 brand/principle，只查术语和命名一致性。
- **不动 spec**：D3 发现 spec 漂移只记录，不改 spec；spec 修改由 `reorg-docs` 子任务统一处置（spec 在 docs 轴下）。

## Compatibility / rollback

- 本子任务只新增 `audit-findings.md`，无修改、无删除
- Rollback：`git rm audit-findings.md` 即可
- 不影响 `tools/verify.ps1`、CI、用户运行

## Open questions

- 无。审计范围、产出格式、与父任务及后续子任务的衔接均已明确。
