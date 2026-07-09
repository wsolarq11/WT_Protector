# 文档漂移整编 (reorg docs)

> 父任务：`07-09-project-wide-reorg`
> 输入：`.trellis/tasks/archive/2026-07/07-09-reorg-audit-pass/audit-findings.md` D1 八项 exec-now

## Goal

落地 audit-findings.md D1 的 8 项文档漂移修复，让 `AGENTS.md` / `README.md` / `.trellis/spec/tests/index.md` / 归档 PRD 的计数、检查清单、引用路径与当前仓库实际状态一致；顺便跑一次 `dotnet build -warnaserror` 处置编译警告（若有）。

**纯文档与机械修改**：不改任何 `.cs` / `.axaml` / `.ps1` 源码行为，不改 `config/launcher.json`，不动 `retired/`。

## Scope (D1 八项 exec-now + 1 项 D2 编译警告核实)

| # | 文件 | 改动 |
|---|------|------|
| 1 | `AGENTS.md:18` | "26 文件" → "24 文件"（Core .cs 计数） |
| 2 | `AGENTS.md:20` | "34 文件，单 MainWindowViewModel + 9 partial" → "28 文件，单 MainWindowViewModel + 7 partial" |
| 3 | `AGENTS.md:21` | "15 文件" → "13 文件"（Views） |
| 4 | `AGENTS.md:39` | "16 项检查" → "18 项检查"；同步补全当前实际检查项的关键分类（`no-stale-three-file-refs`、`settings-single-source-buildstorefile`、`settings-json` 等）。**注**：原审计用裸 `grep -c "Add-Check"` 多算了第 12 行 function 定义，实际是 18 而非 19 |
| 5 | `README.md:138-150` | 重写 "The verifier currently checks:" 列表，对齐当前 18 项；**推荐**改为引用 `tools/verify.ps1` 的 `Add-Check` 清单作为单一权威源，避免未来再漂移 |
| 6 | `.trellis/spec/tests/index.md:9` | "71 test files" → "61 test files"；`410 [Fact]/[Theory]` 计数本轮顺便核实并修正 |
| 7 | `.trellis/tasks/archive/2026-07/07-09-composition-di-async-evaluation/prd.md:44` | "8 partials total" → "1 main + 7 partials (8 files)"；2305 行计数保留 |
| 8 | 7 处幻影引用 `feedback_audit_driven_proposals_verify_each_source.md` | **删除引用**（不补建文件）。改为通用表述"每条提案必须用当前代码/文档验证后再决定动作"。涉及文件：`07-09-project-wide-reorg/prd.md`、`07-09-reorg-audit-pass/{prd,design,implement}.md`（已归档，仍改）、4 份归档 PRD（`07-09-composition-di-async-evaluation`、`07-09-rename-evaluation`、`07-09-trivial-cleanups`、`07-09-07-09-residual-backlog-clearance`） |
| 9 | （D2 编译警告核实，顺便） | 在 `src/avalonia-launcher/` 跑 `dotnet build -warnaserror`；若产生警告则批量处置（未用 `using` / 未用变量等）；零警告则跳过 |

## Out of scope

- `retired/` 目录的任何改动（父任务 out-of-scope）
- 源代码行为变更（即使 dotnet build 警告核实发现"应改"项，也只做机械清理如删未用 using，不做重构）
- 重新审计或扩大漂移检查范围（audit-pass 已完成，本轮只落地 D1）
- D4 R1/R2/R6/R7 任一翻案（全部仍 defer，不在本轮）
- 改写 `docs/feature-reality-north-star-audit.md` / `docs/design-benchmark-digest.md` / `docs/giants-shoulders/**`（21/12/12 计数已核实准确，无需改）
- 改写 `CLAUDE.md`（刚由 `/init` 创建，无漂移）

## Requirements

- 每项改动前后用 `git diff` 自查，确保只改目标行（机械修改易牵动相邻行）
- AGENTS.md 改动后视觉对齐保留（表格列宽 / tree 字符画不被破坏）
- README.md "verifier currently checks" 重写后**优先**采用"引用 verify.ps1 Add-Check 清单"形式，让 verify.ps1 成为唯一权威源；只在用户认为必要时保留详细 bullet 列表
- 删除 `feedback_audit_driven_proposals_verify_each_source.md` 引用时，替换为**等价语义**的简短表述，不删除"严守审计准则"的意图本身
- 已归档 PRD 的修改：保留 git 历史，不做"重写归档"动作；只做措辞/计数/路径修正

## Acceptance criteria

- [ ] D1 八项 exec-now 全部落地，每项 `git diff` 可追溯到 audit-findings.md 对应条目
- [ ] `dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror` 通过（零警告）
- [ ] `tools/verify.ps1` 全绿（18/18）
- [ ] `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release` 全过
- [ ] `retired/` 目录字节级未变（`git diff --stat retired/` 为空）
- [ ] 无源代码行为变更（`git diff src/` 只可能含未用 using 等机械清理，无逻辑改动）
- [ ] AGENTS.md tree 字符画 / 表格列宽未被破坏（视觉自查）
- [ ] 父任务 `07-09-project-wide-reorg/prd.md` 的 phantom 引用也已清理（写入"audit-driven verify"通用表述）
- [ ] 审计准则本身（"每条提案必须验证"）在父 PRD 与子 PRD 中仍可读，只是不再指向不存在的文件

## Notes

- 本子任务本质是 mechanical doc sweep + 可选 build warning sweep，不应超过一次提交
- 若 `dotnet build -warnaserror` 发现非机械警告（如 obsolete API 使用、真实代码缺陷），**停下来**升级为单独子任务，不在本轮处置
