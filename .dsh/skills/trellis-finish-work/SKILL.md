---
name: trellis-finish-work
description: "Wrap up the current session: verify quality gate passed, remind user to commit, archive completed tasks, and record session progress to the developer journal. Use when done coding and ready to end the session."
---

# Finish Work

收尾：归档活动任务（及用户确认的其他完成任务）并记录 journal。代码提交在 Phase 3.4，不在这里。

## Step 1: Survey

```bash
python ./.trellis/scripts/get_context.py --mode record
```

若发现其他已完成任务，向用户一次性确认是否一并归档，默认否。

## Step 2: Classify Dirty Paths

```bash
git status --porcelain
```

过滤 `.trellis/workspace/` 与 `.trellis/tasks/`（由脚本 auto-commit）。对剩余路径：当前任务相关 → 退出并提示回 Phase 3.4；无关并行工作 → 报告后继续；不确定 → 问一次。

## Step 3: Archive

```bash
python ./.trellis/scripts/task.py archive <task-name>
```

至少归档当前活动任务。

## Step 4: Record Journal

```bash
python ./.trellis/scripts/add_session.py \
  --title "Session Title" \
  --commit "hash1,hash2" \
  --summary "Brief summary"
```

`--commit` 用 Phase 3.4 的工作提交哈希，不包含 Step 3 的 archive 哈希。最终顺序：work commits → archive commit → journal commit。
