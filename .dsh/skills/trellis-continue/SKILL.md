---
name: trellis-continue
description: "Resume work on the current task. Loads the workflow Phase Index, figures out which phase/step to pick up at, then pulls the step-level detail via get_context.py --mode phase. Use when coming back to an in-progress task and you need to know what to do next."
---

# Continue Current Task

恢复当前任务：按 `.trellis/workflow.md` 定位阶段/步骤。

## Step 1: Current Context

```bash
python ./.trellis/scripts/get_context.py
```

## Step 2: Phase Index

```bash
python ./.trellis/scripts/get_context.py --mode phase
```

## Step 3: Decide Where You Are

按 `status` + 产物存在性路由：

- `planning` + 无 `prd.md` → **1.1** (`trellis-brainstorm`)
- `planning` + 仅 `prd.md` → 判断 lightweight / complex；complex 回 **1.1** 补 `design.md` + `implement.md`
- `planning` + complex 产物齐 + jsonl 未策展 → **1.3**
- `planning` + 产物齐 + jsonl 齐 → **1.4**（用户确认后 `task.py start`）
- `in_progress` + 未实现 → **2.1**
- `in_progress` + 已实现未检查 → **2.2**
- `in_progress` + 检查通过 → **3.3** → **3.4**
- `completed`（罕见）→ archive flow

阶段规则：步骤按序执行，`[required]` 不可跳，`[once]` 产物已存在即跳过。

## Step 4: Load Specific Step

```bash
python ./.trellis/scripts/get_context.py --mode phase --step <X.X> --platform dsh
```

完整规则在 `.trellis/workflow.md`。
