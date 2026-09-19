# Development Workflow

---

## Core Principles

1. **Plan before code** — 动手前先想清楚要做什么
2. **Specs injected, not remembered** — 规范经 hook/skill 注入，不靠记忆
3. **Persist everything** — 研究、决策、教训全部落盘；对话会被压缩，文件不会
4. **Incremental development** — 一次一个任务
5. **Capture learnings** — 每个任务后回顾并把新知识写回 spec

---

## Trellis System

### Developer Identity

首次使用时初始化身份：

```bash
python ./.trellis/scripts/init_developer.py <your-name>
```

创建 `.trellis/.developer`（gitignored）+ `.trellis/workspace/<your-name>/`。

### Spec System

`.trellis/spec/` 按包与层组织编码规范。

- `.trellis/spec/<package>/<layer>/index.md` — 入口，含 **Pre-Development Checklist** + **Quality Check**；正文在它指向的 `.md`。
- `.trellis/spec/guides/index.md` — 跨包思维指南。

```bash
python ./.trellis/scripts/get_context.py --mode packages   # 列出 packages / layers
```

**何时更新 spec**：新模式/约定、要固化的 bug 防复发、新技术决策。

### Task System

每个任务在 `.trellis/tasks/{MM-DD-name}/` 下持有 `task.json`、`prd.md`，可选 `design.md`、`implement.md`、`research/`，以及子代理平台的上下文清单（`implement.jsonl`、`check.jsonl`）。

```bash
# 任务生命周期
python ./.trellis/scripts/task.py create "<title>" [--slug <name>] [--parent <dir>]
python ./.trellis/scripts/task.py start <name>          # 设为 active task（会话级）
python ./.trellis/scripts/task.py current --source      # 显示 active task 与来源
python ./.trellis/scripts/task.py finish                # 清除 active task（触发 after_finish）
python ./.trellis/scripts/task.py archive <name>        # 移到 archive/{year-month}/
python ./.trellis/scripts/task.py list [--mine] [--status <s>]
python ./.trellis/scripts/task.py list-archive

# 代码规范上下文（经 JSONL 注入 implement/check 子代理）。
# `implement.jsonl` / `check.jsonl` 在 `task create` 时种下（子代理平台）；AI 在规划期整理真实
# spec + research 条目。清单仍为空时 `validate` 失败且 `start` 拒绝（子代理会零上下文运行）；
# 有意为之才用 `start --allow-empty-context`。
python ./.trellis/scripts/task.py add-context <name> <action> <file> <reason>
python ./.trellis/scripts/task.py list-context <name> [action]
python ./.trellis/scripts/task.py validate <name>

# 任务元数据
python ./.trellis/scripts/task.py set-branch <name> <branch>
python ./.trellis/scripts/task.py set-base-branch <name> <branch>    # PR 目标
python ./.trellis/scripts/task.py set-scope <name> <scope>

# 层级（parent/child）
python ./.trellis/scripts/task.py add-subtask <parent> <child>
python ./.trellis/scripts/task.py remove-subtask <parent> <child>

# PR 创建
python ./.trellis/scripts/task.py create-pr [name] [--dry-run]
```

> 权威清单以 `python ./.trellis/scripts/task.py --help` 为准。

**当前任务机制**：`create` 建目录并（会话身份可用时）自动设 active-task 指针；`start` 幂等写同一指针并把 `task.json.status` 从 `planning` 翻到 `in_progress`。状态存 `.trellis/.runtime/sessions/`。无上下文键（hook 输入 / `TRELLIS_CONTEXT_ID` / 平台原生会话变量）时没有 active task，`start` 报会话身份提示。`finish` 删当前会话文件（状态不变）。`archive` 写 `status=completed`、移目录到 `archive/` 并清掉指向该任务的运行时会话文件。

### Workspace System

`.trellis/workspace/<developer>/` 记录每次 AI 会话。

- `journal-N.md` — 会话日志，**单文件上限 2000 行**，超出自动新建 `journal-(N+1).md`。
- `index.md` — 个人索引（总会话数、最近活动）。

```bash
python ./.trellis/scripts/add_session.py --title "Title" --commit "hash" --summary "Summary"
```

### Context Script

```bash
python ./.trellis/scripts/get_context.py                            # 完整会话运行时
python ./.trellis/scripts/get_context.py --mode packages            # 可用 packages + spec 层
python ./.trellis/scripts/get_context.py --mode phase --step <X.Y>  # 某一步的详细指南
```

---

<!--
  WORKFLOW-STATE 契约（编辑下方标签块前必读）
  - [workflow-state:STATUS] 块是各平台 UserPromptSubmit hook 每轮注入的
    <workflow-state> 面包屑唯一来源；STATUS 字符集 [A-Za-z0-9_-]+。
  - 不变量：每个 [required · once] 步骤必须在其阶段的 [workflow-state:*]
    块中有对应执行行，否则 AI 会静默跳过（1.4 planning gate / 3.4 commit 都曾因此跳过）。
  - 范围映射：no_task→Phase 1 前；task_error→记录不可读；planning→Phase 1；
    planning-inline→Codex inline Phase 1；in_progress→Phase 2 + 3.2-3.4；
    in_progress-inline→Codex inline Phase 2/3；completed→目前 DEAD。
  - 改完跑 trellis update；完整运行时契约见 .trellis/spec/cli/backend/workflow-state-contract.md。
-->

## Phase Index

```
Phase 1: Plan    → classify, get task-creation consent, then write planning artifacts
Phase 2: Execute → implement only after task status is in_progress
Phase 3: Finish  → verify, update spec, commit, and wrap up
```

### Request Triage

- Simple conversation / small task：只问本轮是否要建 Trellis 任务；用户说否就跳过 Trellis。
- Complex task：问能否建任务并进入 planning；说否则不做宽泛 inline 实现，改为解释、澄清范围或建议拆小。
- 同意建任务不等于同意实现；规划仍先行。

### Planning Artifacts

- `prd.md` — 需求、约束、验收标准；不放技术设计或执行清单。
- `design.md` — 复杂任务技术设计：边界、契约、数据流、权衡、兼容、rollout/rollback 形态。
- `implement.md` — 复杂任务执行计划：有序清单、验证命令、评审门、回滚点。
- `implement.jsonl` / `check.jsonl` — 子代理上下文清单，不替代 `implement.md`。
- 轻量任务可仅 `prd.md`；复杂任务 `task.py start` 前须有 `prd.md` + `design.md` + `implement.md`。

### Parent / Child Task Trees

Parent 拥有源需求集、任务图、跨子任务验收标准、最终集成评审，通常不直接实现。Child 承载可独立计划/实现/检查/归档的交付物。父子结构不是依赖系统；若 child B 依赖 child A，把顺序写进 B 的 `prd.md`/`implement.md`。

建子任务用 `task.py create "<title>" --slug <name> --parent <parent-dir>`；链接用 `task.py add-subtask <parent> <child>`，解链用 `task.py remove-subtask <parent> <child>`。

<!-- Per-turn breadcrumb: no active task (before Phase 1) -->

[workflow-state:no_task]
No active task. First classify the current turn and ask for task-creation consent before creating any Trellis task.
Simple conversation / small task: ask only whether this turn should create a Trellis task. If the user says no, skip Trellis for this session.
Complex task: ask the user if you can create a Trellis task and enter the planning phase. If the user says no, explain, clarify scope, or suggest a smaller split.
[/workflow-state:no_task]

<!-- Per-turn breadcrumb: active task record cannot be read -->

[workflow-state:task_error]
The active task record could not be read. Do not create or activate another task.
Inspect the task directory named above and repair its task.json. It must be a valid JSON object with a non-empty status.
Preserve existing task fields and artifacts. If the correct status cannot be determined safely, ask the user before reconstructing the record.
[/workflow-state:task_error]

### Phase 1: Plan
- 1.0 Create task `[required · once]` (only after task-creation consent)
- 1.1 Requirement exploration `[required · repeatable]` (`prd.md`; complex tasks also need `design.md` + `implement.md`)
- 1.2 Research `[optional · repeatable]`
- 1.3 Configure context `[required · once]` — Claude Code, Cursor, OpenCode, Codex, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Grok, Kimi Code (sub-agent-dispatch platforms only; inline platforms skip)
- 1.4 Activate task `[required · once]` (review gate, then `task.py start`; status → in_progress)
- 1.5 Completion criteria

<!-- Per-turn breadcrumb: shown throughout Phase 1 (status='planning') -->

[workflow-state:planning]
Load `trellis-brainstorm`; stay in planning.
Lightweight: `prd.md` can be enough. Complex: finish `prd.md`, `design.md`, and `implement.md`; ask for review before `task.py start`.
Multi-deliverable scope: consider a parent task plus independently verifiable child tasks; dependencies must be written in child artifacts, not implied by tree position.
Sub-agent mode: curate `implement.jsonl` and `check.jsonl` as spec/research manifests before start.
[/workflow-state:planning]

<!-- Per-turn breadcrumb: shown throughout Phase 1 when codex.dispatch_mode=inline -->

[workflow-state:planning-inline]
Load `trellis-brainstorm`; stay in planning.
Lightweight: `prd.md` can be enough. Complex: finish `prd.md`, `design.md`, and `implement.md`; ask for review before `task.py start`.
Multi-deliverable scope: consider a parent task plus independently verifiable child tasks; dependencies must be written in child artifacts, not implied by tree position.
Inline mode: skip jsonl curation; Phase 2 reads artifacts/specs via `trellis-before-dev`.
[/workflow-state:planning-inline]

### Phase 2: Execute
- 2.1 Implement `[required · repeatable]`
- 2.2 Quality check `[required · repeatable]`
- 2.3 Rollback `[on demand]`

<!-- in_progress 覆盖 Phase 2 + Phase 3.2-3.4（只有 archive 才翻转状态）。 -->

Sub-agent dispatch protocol applies to all platforms and all sub-agents, including native Codex `SubagentStart` context injection with child-side pull fallback, class-2 Gemini/Qoder/Copilot/Reasonix/Trae/Grok/Kimi Code, hook-backed ZCode/Snow, and `trellis-research`: every dispatch prompt starts with `Active task: <task path from task.py current>` before role-specific instructions. On Grok Build, use `spawn_subagent` with `subagent_type` set to the Trellis agent name (e.g. `trellis-implement`). On Kimi Code, dispatch the built-in `coder` / `explore` sub-agent with the matching `.kimi-code/skills/trellis-<role>/SKILL.md` instructions.

[workflow-state:in_progress]
Tools: `trellis-implement` / `trellis-research` are sub-agent types only (Task/Agent tool, NOT Skill; there is no skill by these names). `trellis-update-spec` is a skill. `trellis-check` exists as both; prefer the Agent form when verifying after code changes.
Flow: `trellis-implement` -> `trellis-check` -> `trellis-update-spec` -> commit (Phase 3.4) -> `/trellis:finish-work`.
Main-session default: dispatch implement/check sub-agents. Sub-agent self-exemption: if already running as `trellis-implement`, do NOT spawn another `trellis-implement` or `trellis-check`; if already running as `trellis-check`, do NOT spawn another `trellis-check` or `trellis-implement`. Dispatch is main session only.
Dispatch prompt starts with `Active task: <task path from task.py current>`. Read context: jsonl entries -> `prd.md` -> `design.md if present` -> `implement.md if present`.
[/workflow-state:in_progress]

<!-- Per-turn breadcrumb: in_progress when codex.dispatch_mode=inline -->

[workflow-state:in_progress-inline]
Flow: `trellis-before-dev` -> edit -> `trellis-check` -> validation -> `trellis-update-spec` -> commit (Phase 3.4) -> `/trellis:finish-work`.
Do not dispatch implement/check sub-agents in inline mode.
Read context: `prd.md` -> `design.md if present` -> `implement.md if present`, plus relevant spec/research loaded by skills.
[/workflow-state:in_progress-inline]

### Phase 3: Finish
- 3.2 Debug retrospective `[on demand]`
- 3.3 Spec update `[required · once]`
- 3.4 Commit changes `[required · once]`
- 3.5 Wrap-up reminder

> Note: step 3.1 was folded into 2.2 (last-iteration full-scope check) and 3.4 (commit preamble). Numbering kept stable to avoid breaking external references.

<!-- Per-turn breadcrumb: status='completed'. Currently DEAD in normal flow. -->

[workflow-state:completed]
Code committed. Run `/trellis:finish-work`; if dirty, return to Phase 3.4 first.
[/workflow-state:completed]

### Rules

1. Identify which Phase you're in, then continue from the next step there
2. Run steps in order inside each Phase; `[required]` steps can't be skipped
3. Phases can roll back (e.g., Execute reveals a prd defect → return to Plan to fix, then re-enter Execute)
4. Steps tagged `[once]` are skipped if the output already exists; don't re-run
5. Artifact presence informs the next step; missing `design.md` / `implement.md` is valid for lightweight tasks and incomplete planning for complex tasks.

### Active Task Routing

当用户请求命中 active task 内的这些意图时，先路由，再按需加载详细步骤。

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

- Planning or unclear requirements -> `trellis-brainstorm`.
- `in_progress` implementation/check -> dispatch `trellis-implement` / `trellis-check`.
- Repeated debugging -> `trellis-break-loop`; spec updates -> `trellis-update-spec`.

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

- Planning or unclear requirements -> `trellis-brainstorm`.
- Before editing -> `trellis-before-dev`; after editing -> `trellis-check`.
- Repeated debugging -> `trellis-break-loop`; spec updates -> `trellis-update-spec`.

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

### Guardrails

- Task creation approval is not implementation approval; implementation waits for `task.py start` after artifact review.
- PRD-only is valid for lightweight tasks; complex tasks need `design.md` + `implement.md`.
- Planning must be persisted to task artifacts; checks must run before reporting completion.

### Loading Step Detail

每步运行以下命令获取详细指南：

```bash
python ./.trellis/scripts/get_context.py --mode phase --step <step>
# e.g. python ./.trellis/scripts/get_context.py --mode phase --step 1.1
```

---

## Phase 1: Plan

Goal: classify the request, get task-creation consent when a task is needed, and produce the planning artifacts required before implementation.

#### 1.0 Create task `[required · once]`

仅在取得建任务同意后建目录。该命令写 `task.json`（status=planning）、建默认 `prd.md`，并在会话身份可用时自动指向新任务：

```bash
python ./.trellis/scripts/task.py create "<task title>" --slug <name>
```

`--slug` 只是可读名，**不要**带 `MM-DD-` 日期前缀（`create` 会自动加）。任务树先建 parent，再逐个 `--parent <parent-dir>` 建 child；只 start 拥有下一可验证交付物的 child，不因有 children 就 start parent。

命令成功后面包屑自动切到 `[workflow-state:planning]`。这里只跑 `create`，不跑 `start`——`start` 会把状态翻到 `in_progress`，在规划产物评审前就切实现阶段；`start` 留到 1.4。

`python ./.trellis/scripts/task.py current --source` 已指向任务时跳过本步。

#### 1.1 Requirement exploration `[required · repeatable]`

加载 `trellis-brainstorm`，按技能指引与用户交互探索需求：一次一问；能研究就不问用户；优先给选项而非开放问题；每次回答后立即更新 `prd.md`；可独立验证时拆 parent/child；`prd.md` 聚焦需求与验收标准；复杂任务在实现前产出 `design.md` + `implement.md`。

Parent/child 拆分：parent 拥有源需求、子任务映射、跨子任务验收标准、最终集成评审；child 拥有可独立计划/实现/检查/归档的交付物；父子不是依赖系统，child B 依赖 child A 就写进 B 的 `prd.md`/`implement.md`；start 拥有下一交付物的 child。需求变化时回到本步修订相关产物。

#### 1.2 Research `[optional · repeatable]`

研究可在需求探索期间随时进行，不限于本地代码，可用任何工具（MCP、技能、web 搜索等）查外部信息（第三方库文档、行业实践、API 参考等）。

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

派发研究子代理：**Agent type** `trellis-research`；**Task description** Research <specific question>；**Key requirement** 输出必须落到 `{TASK_DIR}/research/`。

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

在主会话直接研究并把结论写入 `{TASK_DIR}/research/`。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

**研究产物约定**：一文件一主题（如 `research/auth-library-comparison.md`）；记录第三方用法示例、API 参考、版本约束；记下发现的 spec 路径。brainstorm 与研究可自由交织。**关键原则**：研究结果必须写文件，不能只留在对话里。

#### 1.3 Configure context `[required · once]`

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

整理 `implement.jsonl` 与 `check.jsonl`，让 Phase 2 子代理拿到正确的 spec/research 上下文。`task create` 时已种下一条自描述 `_example` 行，本步填入真实条目。

**位置**：`{TASK_DIR}/implement.jsonl`、`{TASK_DIR}/check.jsonl`（已存在）。**格式**：每行一个 JSON 对象 `{"file": "<path>", "reason": "<why>"}`，路径相对仓库根。

**放什么**：spec 文件（`.trellis/spec/<package>/<layer>/index.md` 及本任务相关的指南文件）；研究文件（`{TASK_DIR}/research/*.md`）。**不放什么**：代码文件（实现期由子代理直接读，不预注册）；即将修改的文件。

**两文件分工**：`implement.jsonl` → 实现子代理需要的 spec + research；`check.jsonl` → 检查子代理的 spec（质量指南、检查约定、需要的同份 research）。清单不替代 `implement.md`。

**发现 spec**：`python ./.trellis/scripts/get_context.py --mode packages` 列出每个包及其 spec 层路径。**追加条目**（也可直接编辑 jsonl）：

```bash
python ./.trellis/scripts/task.py add-context "$TASK_DIR" implement "<path>" "<reason>"
python ./.trellis/scripts/task.py add-context "$TASK_DIR" check "<path>" "<reason>"
```

有真实条目后删掉种子 `_example` 行（可选，消费方会自动跳过）。**就绪门**：两个 jsonl 至少各含一条真实 `{"file": "...", "reason": "..."}` 才能 `task.py start`；仅种子行不算。两文件已有真实条目时跳过本步。

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

跳过本步；上下文由 Phase 2 的 `trellis-before-dev` 直接加载。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

#### 1.4 Activate task `[required · once]`

产物评审通过后翻转状态：

```bash
python ./.trellis/scripts/task.py start <task-dir>
```

轻量任务 `prd.md` 即可；复杂任务须 `prd.md` + `design.md` + `implement.md` 齐备且评审通过；子代理平台还须两个 jsonl 都有真实条目。成功后切到 `[workflow-state:in_progress]`。若 `start` 报会话身份错误，按提示配置会话身份后重试。

#### 1.5 Completion criteria

| Condition | Required |
|------|:---:|
| `prd.md` exists | 是 |
| User confirms task should enter implementation | 是 |
| `task.py start` has been run (status = in_progress) | 是 |
| `research/` has artifacts (complex tasks) | recommended |
| `design.md` exists (complex tasks) | 是 |
| `implement.md` exists (complex tasks) | 是 |

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

| `implement.jsonl` and `check.jsonl` each contain at least one real curated entry (seed row does not count) | 是 |

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

---

## Phase 2: Execute

Goal: turn reviewed planning artifacts into code that passes quality checks.

#### 2.1 Implement `[required · repeatable]`

[Claude Code, Cursor, OpenCode, codex-sub-agent, CodeBuddy, Droid, Pi, ZCode, Snow, Oh My Pi]

派发实现子代理：**Agent type** `trellis-implement`；**Task description** Implement the reviewed task artifacts, consulting materials under `{TASK_DIR}/research/`; finish by running project lint and type-check；**Dispatch prompt guard** 必须以 `Active task: <task path>` 开头，并告知它是 `trellis-implement` 子代理、必须直接实现而非再派发 `trellis-implement`/`trellis-check`。平台 hook 自动读 `implement.jsonl` 注入 spec/research，并注入 `prd.md`、`design.md`（如有）、`implement.md`（如有）；Codex 用 `SubagentStart` 原生注入，child-side 加载为兜底。

[/Claude Code, Cursor, OpenCode, codex-sub-agent, CodeBuddy, Droid, Pi, ZCode, Snow, Oh My Pi]

[Gemini, Qoder, Copilot, Reasonix, Trae, Grok, Kimi Code]

派发实现子代理：同上。pull 式子代理定义自动完成上下文加载——用 `task.py current --source` 解析 active task，读 `prd.md`/`design.md`/`implement.md`，并读 `implement.jsonl` 要求每个条目在编码前加载。

[/Gemini, Qoder, Copilot, Reasonix, Trae, Grok, Kimi Code]

[Kiro]

派发实现子代理：同上。平台 prelude 自动读 `implement.jsonl` 注入 spec/research，并注入 `prd.md`/`design.md`/`implement.md`。

[/Kiro]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

1. Load the `trellis-before-dev` skill to read project guidelines
2. Read `{TASK_DIR}/prd.md`, then `design.md` if present, then `implement.md` if present
3. Consult materials under `{TASK_DIR}/research/`
4. Implement the code per reviewed artifacts
5. Run project lint and type-check

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

#### 2.2 Quality check `[required · repeatable]`

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

派发检查子代理：**Agent type** `trellis-check`；**Task description** Review all code changes against specs and task artifacts; fix any findings directly; ensure lint and type-check pass；**Dispatch prompt guard** 以 `Active task: <task path>` 开头，告知它是 `trellis-check` 子代理、必须直接评审/修复而非再派发。检查内容：对 specs 评审、对 `prd.md`/`design.md`/`implement.md` 评审、自动修复发现的问题、跑 lint/typecheck 验证。

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

加载 `trellis-check` 技能并按其指引验证：spec 合规、lint/type-check/tests、跨层一致性（跨层改动时）。发现问题 → 修复 → 复查，直到全绿。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

**Final pass（3.4 提交前）**：任务最后一次 2.2 必须全范围，不只查最新实现块。用 `python ./.trellis/scripts/get_context.py --mode packages` 列出所有受影响包，再加载各包 spec index 的 Quality Check 段，捕获跨层/多包问题。

#### 2.3 Rollback `[on demand]`

- `check` 暴露 prd 缺陷 → 回 Phase 1 修 `prd.md`，再重做 2.1
- 实现跑偏 → 还原代码，重做 2.1
- 需要更多研究 → 同 Phase 1.2，结论写入 `research/`

---

## Phase 3: Finish

Goal: ensure code quality, capture lessons, record the work.

#### 3.2 Debug retrospective `[on demand]`

若任务反复调试（同一问题修了多次），加载 `trellis-break-loop`：归类根因、解释此前修复为何失败、提出预防，把教训固化避免同类复发。

#### 3.3 Spec update `[required · once]`

加载 `trellis-update-spec`，判断是否产出值得记录的新知识：新发现的模式/约定、踩过的坑、新技术决策。据此更新 `.trellis/spec/`。即使结论是“无可更新”也要走完判断。

#### 3.4 Commit changes `[required · once]`

**Spec-sync preamble**：起草提交前先问——本次是否修了 bug 或发现应写入 `.trellis/spec/` 的非显然知识？是则先回 3.3，spec 写入属于同一任务的提交批次。

AI 驱动本任务代码的批量提交，让 `/finish-work` 之后干净运行。目标：先 work 提交，后 bookkeeping（archive + journal）提交，绝不交错。

1. **Inspect dirty state**：
   ```bash
   git status --porcelain
   ```
   快照所有脏路径；工作树干净则跳到 3.5。

2. **Learn commit style**：
   ```bash
   git log --oneline -5
   ```
   记前缀约定（`feat:`/`fix:`/`chore:`/`docs:`...）、语言、长度风格。

3. **Classify dirty files into two groups**：**AI-edited this session**（本会话经 Edit/Write/Bash 改的）；**Unrecognized**（本会话未碰的，可能是用户手改或遗留 WIP，不静默纳入）。

4. **Draft a commit plan**：把 AI 改的文件按逻辑分组（一个连贯变更一个提交，不逐文件）。每条：`<commit message>` + 文件列表；未识别文件单列在底部。

5. **Present the plan once, ask for one-shot confirmation**：
   ```
   Proposed commits (in order):
     1. <message>
        - <file>
        - <file>
     2. <message>
        - <file>

   Unrecognized dirty files (NOT in any commit — confirm include/exclude):
     - <file>
     - <file>

   Reply 'ok' / '行' to execute. Reply with edits, or '我自己来' / 'manual' to abort.
   ```

6. **On confirmation**：逐批 `git add <files>` + `git commit -m "<msg>"`。不 amend、不 push。

7. **On rejection**（“不行”/“我自己来”/manual/任何反对）：停下，不再拟第二版计划；用户手工提交，确认后跳到 3.5。

**Rules**：任何地方都不 `git commit --amend`（三阶段三提交：work → archive → journal）；本步不 push；用户只改措辞但接受分组，改消息再确认一次；拒绝分组则转手工模式；批量计划一次提示，不逐提交提示。

#### 3.5 Wrap-up reminder

以上完成后提醒用户可运行 `/finish-work` 收尾（归档任务、记录会话）。

---

## Customizing Trellis (for forks)

本节面向修改 Trellis 工作流本身的开发者。全部定制都通过编辑本文件完成，脚本只是解析器。

### Changing what a step means

编辑上方 Phase 1/2/3 的步骤正文。关键不变量：无 active task 必须先 triage 并征求建任务同意；planning 必须区分轻量 PRD-only 与复杂任务（start 前需 `prd.md`+`design.md`+`implement.md`）；每条必需执行路径都要让 3.4 提交提醒在 `/trellis:finish-work` 之前可达。

所有标签块位于 `## Phase Index` 段内，紧跟在各阶段摘要之后：

| Scope | Corresponding tag |
|---|---|
| No active task (before Phase 1) | `[workflow-state:no_task]` |
| Active task record unreadable | `[workflow-state:task_error]` |
| All of Phase 1 | `[workflow-state:planning]` |
| Codex inline Phase 1 | `[workflow-state:planning-inline]` |
| Phase 2 + Phase 3.2–3.4 | `[workflow-state:in_progress]` |
| Codex inline Phase 2 + Phase 3.2–3.4 | `[workflow-state:in_progress-inline]` |
| After Phase 3.5 (archived) | `[workflow-state:completed]`（currently DEAD） |

### Changing the per-turn prompt text

直接编辑对应 `[workflow-state:STATUS]` 块正文。改后跑 `trellis update`（模板维护者）或重启 AI 会话（自有项目定制）。

### Adding a custom status

新增块：

```
[workflow-state:my-status]
your per-turn prompt text
[/workflow-state:my-status]
```

约束：STATUS 字符集 `[A-Za-z0-9_-]+`；必须有一个 lifecycle hook 写 `task.json.status` 为该值，否则标签永不被读取；lifecycle hooks 位于 `task.json.hooks.after_*`，绑定 `after_create / after_start / after_finish / after_archive`。

### Adding a lifecycle hook

给 `task.json` 加 `hooks` 字段：

```json
{
  "hooks": {
    "after_finish": [
      "your-script-or-command-here"
    ]
  }
}
```

支持事件：`after_create / after_start / after_finish / after_archive`。`after_finish` 不等于状态变更（只清 active-task 指针）；“任务完成”通知用 `after_archive`。

### Full contract

状态机运行时契约、所有 status 写入方、伪状态（`no_task`/`stale_<source_type>`）、hook 可达矩阵等详见 `.trellis/spec/cli/backend/workflow-state-contract.md`；实际解析器为 `.trellis/scripts/inject-workflow-state.py`（只读 workflow.md，无内嵌文本）。
