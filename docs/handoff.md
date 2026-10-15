# 交接快照

> 最后一棒状态：「现在是什么状态」。

## 当前状态（2026-10-14）

**WT_Protector** — 收敛维护期。维护模式：继续维护；拍板日期与承诺全文唯一源见 [roadmap.md](roadmap.md)「维护承诺」。
- 本地 `npm run verify`：全部门禁步 PASS，0 阻断 ✅（步数与测试计数见 `AGENTS.md` 结构基线表）
- 交付物：`artifacts/friend/` 下与当前 tag 对齐的 friend zip（tag `v0.1.0`，含 StoreGrain TOCTOU 修复）
- 远端 CI：见仓库 Actions 最新 run
- 远端 `origin/main` 已重写（见下「历史重写」），commit 哈希与重写前不再对应

## 本次会话（2026-10-14）

### 文档漂移收口
证据矩阵与 handoff 的绝对计数违背 `docs/README.md` 规则 3（计数只存 `AGENTS.md` 结构基线派生表），已改为指针：
- `docs/evidence/branch-evidence-matrix.md`：删「44 步/37 步/870 tests/848/Core 51/787 属性」等硬编码；按当前 `gate-steps.ts` 重列 Static 1–36；移除两个已删门禁（`no-stale-three-file-refs`、`no-hardcoded-gate-counts`），补 `command-table-consistency`；`structure-baseline-consistency` → `structure-baseline-up-to-date`；`main-vm-line-budget` 记为 1200/1200 并注明计数口径（逐文件尾行）
- `docs/spec/core/directory-structure.md`、`docs/spec/views/directory-structure.md`：修正失效门禁名
- `docs/adr/decision-ladder.md`：复杂度收敛节的硬编码计数改为指针
- `docs/handoff.md`：状态句去掉绝对计数，只留状态词 + 指针
- `docs/spec/core/error-handling.md`、`docs/spec/guides/pitfalls.md`：失效的 commit 短哈希（原就不可解析，属 phantom reference）改为提交标题/任务名引用

### 历史重写（构建产物剥离）
历史中误提交过构建产物（`tools/uninstaller/bin|obj` 等，约 85MB 快照）。用 `git filter-repo --strip-blobs-with-ids` 剥离 242 个可再生产物 blob：
- `.git` 43MB → 5.4MB；1268 个提交全部保留；工作树哈希不变（前后均 `b574e745`）
- `--force-with-lease` 覆盖 `origin/main`；重写前完整历史存于仓库外 bundle（`WT_Protector-history-backup-20261014.bundle`）与本地分支 `backup/pre-rewrite`
- 未剥离 `.trellis/` 过程日志（那是刻意保留的证据，非产物）
- 代价：含被剥离产物的提交及其后代哈希改变（实测 1268 个旧提交中 114 个哈希改变、1154 个不变）；新增引用一律用 tag / 提交标题，不再写裸短哈希

### 交付物刷新
`npm run make` 重跑：AOT 发布 + critical-journey smoke（launch/title PASS）+ friend zip。首个版本 tag `v0.1.0` 打在重写后 HEAD，friend zip 与 release 资产以该 tag 为准（其后 2 个提交为文档/依赖升级，不改变交付二进制——`App` 运行时闭包与 tag 时一致，31 个库零差异）。

## 上一次会话（2026-10-11）

### docs/ 认识论四层重构
`docs/` 从生命周期分层 (Active/Reference/Research/Archive) 切换为认识论分层：
- **spec/** — 现状（是什么）：编码规范 (core/views/tests/guides) + 设计契约 (test-structure + visual-perspective-system)
- **adr/** — 决策（为什么）：decision-ladder / wiring-map / business-wiring-boundaries / feature-reality-north-star-audit / design-benchmark-digest
- **evidence/** — 证据（怎么来的）：branch-evidence-matrix / research/ / archive/
- **reference/** — 外部归档（别人的）：distribution-signing-strategy / github-snapshot / shortcut-backup-manifest / giants-shoulders/

### Status 头归一化
Status 按目录取值，不再有打架：
- `spec/` + `adr/` → `Active`（可被 Superseded）
- `evidence/` → `Evidence`
- `evidence/archive/` → `Archived`
- `reference/` → `External`

### 新增文件
- `docs/spec/index.md`、`docs/adr/index.md`、`docs/evidence/index.md`、`docs/reference/index.md` — 四层层索引
- `docs/handoff.md` — 交接快照（本文件）
- `docs/roadmap.md` — 路线图
- `docs/sop/upstream-sync.md` — 上游跟进 SOP

### 路径引用更新
- `AGENTS.md` — 藤曼路径 + 全仓地图 + 核心规则链
- `README.md` — claim boundary 链
- `DESIGN.md` — evidence + visual-perspective-system 链
- `scripts/checks/static.ts` — wiring-map 链
- `docs/adr/decision-ladder.md` — 目录架构节 + 证据引用链
- `docs/adr/feature-reality-north-star-audit.md` — design-benchmark + giants-shoulders 链

### CI 精简
`.github/workflows/verify.yml` 从 8 step → 4 step（23 行 YAML），砍掉两遍 Native AOT + `release:contract`：
- CI 现在是单 job 单命令：`npm run verify`
- `release:contract` 回到发布流程（打 tag / release 时跑），不在每次 push 触发
- `scripts/checks/static.ts` 的 `ci-fast-lane-no-publish-duplication` 检查同步更新

## 关键锚点

| 事实 | 唯一源 |
| --- | --- |
| 架构分层 | `src/avalonia-launcher/README.md` 五层 |
| 命令 | `package.json`（机器源）+ `AGENTS.md` 脚本工具箱 |
| 配置 schema | Core record 定义 + `LauncherStoreValidator` |
| 视觉 | `docs/spec/visual-perspective-system.md` + `LauncherDesignTokens.cs` |
| 文档 index | `docs/README.md` |
| 文档分层 | spec → adr → evidence → reference（依赖方向不可逆） |

## 待办

见 [roadmap.md](roadmap.md)。