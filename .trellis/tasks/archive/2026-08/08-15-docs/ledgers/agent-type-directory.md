# 验证台账 — docs/agent-type-directory.md

> 验证日期: 2026-08-15 ｜ 判定词: VERIFIED / STALE / UNVERIFIABLE / HISTORICAL
> 全文性质: 2026-07-23 时点的 Agent 类型目录（点状历史记录）——保留全部叙述。

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 生成时间 2026-07-23；数据来源 `git log --grep="[Snow Team]"` 共 44 个提交（27 auto-commit + 17 merge） | STALE（计数漂移，时点记录） | 现 `git log --grep="Snow Team"` = 47 提交（29 non-merge + 18 merge）；按日期: 07-22 = 12（6 auto + 6 merge）、07-23 = 35（23 auto + 12 merge） | 保留时点计数 + 文首注记现状 |
| 共使用 27 种 Agent 类型、6 个类别 | VERIFIED（内部一致性） | `^### [0-9]+. ` 标题恰 27 个；类别求和 8+7+4+2+4+2 = 27 | 保留 |
| 27 个 Agent 的关键提交短哈希（ffb9cd6…af77a95） | UNVERIFIABLE | 逐一 `git log -1` 全部退出非零；`git cat-file -e` 失败；`git fsck --unreachable` = 0（历史重写，旧对象已 GC） | 保留为时点记录；注记: 27 个 Agent 名在现历史均有同名 auto-commit（例 core-fixer → 243deb97 07-22、app-fixer → bda37320 07-22、ci-enhancer → 921c29ea 07-23） |
| 各 Agent 修改文件数与 +/- 行数（如 +163/-6、+1398/-44） | UNVERIFIABLE | 哈希不可考，无法 `git show --stat` 复核 | 保留（时点记录） |
| pattern-researcher: 生成 `docs/architecture-research-report.md`（593 行） | VERIFIED | Test-Path True；(Get-Content docs/architecture-research-report.md).Count = 593 | 保留 |
| collab-debt-reviewer: 生成 `collab-debt-findings.md`（201 行） | HISTORICAL | 现位于 docs/collab-debt-findings.md（261 行）；2026-08-15 归档移动（git status: R collab-debt-findings.md -> docs/collab-debt-findings.md） | 裸文件名引用保留（按指示不改为路径）；注记新位置 |
| 使用模式: 顺序执行 / 脉冲式（07-22+07-23）/ merge 策略 / 粒度多样性 | VERIFIED（日期集中） | Snow Team 提交日期仅 2026-07-22（12）与 07-23（35）两天；每 auto-commit 后接 Merge 提交（18 个 merge 现存在） | 保留 |
| 目录未收录 fluxor-integrator / phase-e-worker | STALE（生成时点后新增） | 现历史含 44247cc7 fluxor-integrator: auto-commit、ee2a85a9 phase-e-worker: auto-commit、0224dbbf Merge phase-e-worker work（均 07-23） | 注记（不新增条目，保持时点记录） |

## 处理汇总

- 保留 27 条 Agent 记录与全部时点叙述。
- 文首新增 2026-08-15 核验注记: 哈希不可解析（历史重写）、现计数 47（29+18）、两个未收录 Agent 与 1 个 merge 的补充说明、路径核验结果。
- 无因归档移动失效的路径引用需改写（architecture-research-report.md 路径有效；collab-debt-findings.md 为裸名提及）。
