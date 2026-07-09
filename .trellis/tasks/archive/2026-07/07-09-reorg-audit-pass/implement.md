# Implement — reorg audit pass

> 复杂任务要求 implement.md。审计是只读工作，但 5 维覆盖全仓，需要有序执行才能保证不漏。

## Ordered checklist

### Phase A — 信息源快照 (准备)

- [ ] A1 `git rev-parse HEAD` 记录当前 commit hash（审计基线）
- [ ] A2 `git status` 确认 working tree 干净（除 `.trellis/tasks/07-09-reorg-audit-pass/` 与 `.trellis/tasks/07-09-project-wide-reorg/` 外无变更）
- [ ] A3 读父任务 prd.md 与本子任务 prd/design，确认范围理解一致
- [ ] A4 读 `.trellis/spec/guides/pitfalls.md` §1，确认审计准则（每条提案都是假设，必须用当前代码/文档验证）

### Phase B — D1 文档漂移

- [ ] B1 Grep `README.md`、`AGENTS.md`、`DESIGN.md`、`CLAUDE.md`、`src/avalonia-launcher/README.md` 中所有"数字 + 文件/类/方法"声明
- [ ] B2 用 Glob 取 `src/avalonia-launcher/src/WTLauncher.Core/*.cs`、`WTLauncher.App/ViewModels/*.cs`、`WTLauncher.App/Views/*`、`tests/WTLauncher.Tests/*.cs` 的实际计数
- [ ] B3 对照文档原值 vs 实际值，漂移条目写入 `audit-findings.md` D1 表
- [ ] B4 Grep 文档中的 schema 字段名引用，与 `LauncherStoreFile.cs` / `LauncherSettings.cs` / `LaunchEntry.cs` / `LauncherPreset.cs` 对照
- [ ] B5 Grep 文档中的命令示例，与 `tools/*.ps1` 当前参数集对照
- [ ] B6 Grep 文档中的章节链接/行号引用，用 `Grep -n` 验证

### Phase C — D2 代码死代码（静态扫描）

- [ ] C1 Grep `ponytail:` 全 `src/` 与 `tools/`，逐条评估 ceiling/升级路径是否仍合理
- [ ] C2 Grep 被注释掉的死代码块（`// TODO`、`// DEAD`、`// REMOVED`、`#if false` 等）
- [ ] C3 用 `search_graph` 找 `WTLauncher.*` 命名空间下所有类型，`trace_path` 找零 inbound call 的 private/internal 成员
- [ ] C4 Grep 已知模式：被 `// removed`、`// deleted`、`// legacy` 注释引用的代码
- [ ] C5 不跑 `dotnet build` 取警告（留给 `reorg-code`），只做静态扫描
- [ ] C6 死代码条目写入 `audit-findings.md` D2 表，每条标注"待编译警告核实"

### Phase D — D3 spec 与代码同步

- [ ] D1 列 `.trellis/spec/**` 文件清单，按 core/views/tests/guides 分类
- [ ] D2 对每个 spec 文件，Grep 提取类名/方法名/常量名/文件路径引用
- [ ] D3 用 `search_graph` / `Grep` 验证每个引用在当前 `src/` 中是否还存在
- [ ] D4 特别关注 schema-driven-arch-migration 之后 spec 是否同步（从 `git log --since=2026-07-08 -- .trellis/spec/` 时间线判断）
- [ ] D5 漂移条目写入 `audit-findings.md` D3 表

### Phase E — D4 deferred 当前证据

- [ ] E1 Read `.trellis/tasks/archive/2026-07/07-09-composition-di-async-evaluation/prd.md`，提取 R1/R6/R7 各自的 revisit trigger
- [ ] E2 Read `.trellis/tasks/archive/2026-07/07-09-rename-evaluation/prd.md`，提取 R2 的 revisit trigger
- [ ] E3 用当前代码证据评估 R1 trigger：MainWindowViewModel 当前 partial 数量、构造函数注入点、collaborator 边界
- [ ] E4 评估 R2 trigger：LauncherEntryVisibility 当前引用数（用 Grep count）、改名 churn 估计
- [ ] E5 评估 R6 trigger：测试是否已因缺 DI 而失败、是否已有需要 mock 的外部依赖
- [ ] E6 评估 R7 trigger：当前 sync I/O 在 UI 线程的实测延迟证据、配置文件大小
- [ ] E7 每个 R 写结论段（仍 defer / 触发执行 / 正式否决），写入 `audit-findings.md` D4

### Phase F — D5 风格一致性

- [ ] F1 Grep `ponytail:` 全仓，标注每条是否带 ceiling
- [ ] F2 Grep `Launcher*` vs `WTLauncher*` 前缀混用
- [ ] F3 Grep DESIGN.md 已禁用术语（"危险命令"、"需确认项"、"危险"等）在 user-facing 文案与文档中的残留
- [ ] F4 Grep 中英文混用是否合理（代码标识符应英文，UI 文案应中文）
- [ ] F5 风格条目写入 `audit-findings.md` D5 表

### Phase G — 汇总与路由

- [ ] G1 写 `audit-findings.md` Summary 段：总条目数、exec-now/defer/no-action 分布
- [ ] G2 给出三轴子任务触发判断：
  - docs 子任务是否建？取决于 D1 + D3 是否有 exec-now 项
  - code 子任务是否建？取决于 D2 + D5 是否有 exec-now 项
  - deferred-decisions 子任务是否建？取决于 D4 是否有任何 R 从 defer 翻到执行
- [ ] G3 写"下一轮 audit 判断"段（默认 NO，除非有具体证据表明本轮审计盲区）
- [ ] G4 `git status` 确认仅新增 `audit-findings.md`，无其他文件改动

### Phase H — Review gate

- [ ] H1 自查 implement.md 全部 checklist 完成
- [ ] H2 自查 acceptance criteria 全部满足
- [ ] H3 把 `audit-findings.md` 路径回报父任务，由父任务 owner 决定下一批子任务创建

## Validation commands

```bash
# 确认只读
git status --short
# 期望输出仅 .trellis/tasks/07-09-reorg-audit-pass/audit-findings.md 新增

# 确认审计基线
git rev-parse HEAD

# 确认 audit-findings.md 覆盖 5 维
grep -c "^## D" .trellis/tasks/07-09-reorg-audit-pass/audit-findings.md
# 期望 5
```

## Review gates

- G2（三轴触发判断）是关键 review gate：父任务 owner 必须确认判断与父 PRD 的 child task map 一致后才推进
- H2（acceptance 自查）通过后才提交归档

## Rollback points

- 任何 Phase 中若发现审计产出与父 PRD scope 冲突（例如误改源文件）：立即 `git checkout -- <file>` 还原
- 若发现 D4 的某个 R 项证据指向 Layer 0（out of scope）：仅记录到 audit-findings.md，不改源代码

## Estimated output size

- `audit-findings.md` 预期 200–500 行，取决于 5 维各自漂移密度
- 若某维产出为空，仍保留该维章节并写"无漂移"，避免后续读者误以为漏审
