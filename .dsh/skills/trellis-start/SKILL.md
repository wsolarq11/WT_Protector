---
name: trellis-start
description: "Initializes an AI development session by reading workflow guides, developer identity, git status, active tasks, and project guidelines from .trellis/. Classifies incoming tasks and routes to brainstorm, direct edit, or task workflow. Use when beginning a new coding session, resuming work, starting a new task, or re-establishing project context."
---

# Start Session

初始化 Trellis 会话。dsh 无 session-start hook，需手动加载等价上下文。

## Step 1: Current State

```bash
python ./.trellis/scripts/get_context.py
```

若输出含 `Trellis update available:`，逐字保留该行，不要缩短命令提示。

## Step 2: Workflow Overview

```bash
python ./.trellis/scripts/get_context.py --mode phase
```

完整指南在 `.trellis/workflow.md`（按需读）。

## Step 3: Guideline Indexes

```bash
python ./.trellis/scripts/get_context.py --mode packages
cat .trellis/spec/guides/index.md
cat .trellis/spec/<package>/<layer>/index.md
```

index 列出实际编码前要读的具体 guideline。

## Step 4: Decide Next Action

- `planning` + 无 `prd.md` → Phase 1.1，加载 `trellis-brainstorm`
- `planning` + 有 `prd.md` → 停留 Phase 1；复杂任务补 `design.md` + `implement.md`
- `in_progress` → Phase 2.1：
  ```bash
  python ./.trellis/scripts/get_context.py --mode phase --step 2.1 --platform dsh
  ```
- 无活动任务 → 先分类；简单任务只问是否建 Trellis 任务，复杂任务问是否进入规划

## Skill Routing

| User intent | Skill |
| --- | --- |
| New feature / unclear | `trellis-brainstorm` |
| About to code | `trellis-before-dev` |
| Done coding | `trellis-check` |
| Repeated bug | `trellis-break-loop` |
| Learned something | `trellis-update-spec` |
