# 交接快照

> 最后一棒状态：「现在是什么状态」。

## 当前状态（2026-10-15）

**WT_Protector** — 收敛维护期。维护模式：继续维护；拍板日期与承诺全文唯一源见 [roadmap.md](roadmap.md)「维护承诺」。
- 本地 `npm run verify`：static/local 全 PASS；native 四层在本机因 nuget.org 502（NU1900）失败，属环境（干净 HEAD 可复现，CI 可达 nuget.org 为绿）
- 交付物：`artifacts/friend/` 下与当前 tag 对齐的 friend zip（tag `v0.1.0`，含 StoreGrain TOCTOU 修复）
- 远端 CI：见仓库 Actions 最新 run；`main` 已开分支保护（required check `verify`）
- 远端 `origin/main` 已重写（见下「历史重写」），commit 哈希与重写前不再对应

## 本次会话（2026-10-15 门禁内核重构）

把 42 项门禁从「一份 id 数组 + 一份实现数组，靠断言互相看齐」重构成值/端口/代数模型。行为零漂移，纯结构收敛。

### 问题
`scripts/gate-steps.ts` 存 id 清单、`scripts/verify.ts` 存实现数组，二者只能靠一条断言同步；跳过策略散在 `if (isCi)` 分支里，receipt 的 layering 是字符串拼装。结果是「跳过」与「通过」在类型上同形——同一字符串可以既表示真跑过、又表示没跑（本次会话前刚落地的 `guiSmokeAttempts` 修复正是这个病的实例）。

### 新结构（`scripts/kernel/`）
- **World 端口**（`world.ts`）：唯一允许碰 `node:fs`/`execa` 的模块。其余检查代码只依赖 `World` 接口，因此判定逻辑可在内存世界里跑单测，不需要真文件系统
- **Verdict 代数**（`verdict.ts`）：`pass(evidence) | fail(reason) | skip(reason)` 判别联合。跳过与通过是**不同的值**而非同一字符串的两种拼法——receipt 说谎在类型上不可表达
- **Check 值**（`check.ts`）：`defineCheck({id, tier, needs?, run})`。`createCtx(world)` 把旧 helper 名（`readText`/`walkFiles`/`assertMatcherDetects`…）绑到 `World`，迁移期检查体改动最小
- **层策略表**（`policy.ts`）：`TIER_POLICY` 声明每层是否 blocking 及跳过原因；executor 里不再有 `if (isCi)`
- **执行器**（`runner.ts`）：按 `needs` 拓扑排序 → 按层排序 → 求值 → 投影成 receipt。`needs` 未通过的检查自动跳过，非阻塞层失败进 `advisory`
- **注册表**（`registry.ts`）：`gateChecks` 是 id 与实现的唯一同址权威源；`gate-steps.ts` 与 `checks/common.ts` 已删除

### 新增能力
- `npm run verify:static`：只跑 36 项静态检查（约 2s，不探测 .NET SDK），相对全量门禁（约 4min）是提交前快车道。CLI 只在 native 在范围内时才探测 SDK，避免把「未安装 SDK」误报给不涉及 .NET 的运行

### 等价性证明

用一个 `git worktree` 在重构前的 HEAD 上开出干净检出，把新 `scripts/` 覆盖进去，于是**新旧两版门禁跑在同一棵字节相同的树上**（只有门禁代码不同；本机 git-ignored 的 `config/launcher.json` 已预置，避免文件缺失造成的假差异）。逐项 diff 两版 receipt：

- **判定投影完全相同**：`failures` = 4 条 native（本机 nuget.org 502，与改动无关）、`advisory` = 1 条 local、`skipped` = 空——阻断/告警/跳过的划分两版逐字节一致。
- 明细只剩 2 处差异，都可由改动本身解释：`command-table-consistency` 的命令数 20→21（新增 `verify:static`），`no-active-powershell-scripts` 的扫描数 +4（新增 `kernel/` 5 个文件 + `registry.ts`，减去删除的 `gate-steps.ts` 与 `checks/common.ts`）。
- `launcher-store` 的记录粒度从「1 条 + 4 条内联」变为注册表汇总的 `records` 结构，是重构的预期产物，判定不变。

加上 `tsc --noEmit` 0 error、脚本层 52 tests 全绿、`--tier=static` 实测 PASS。

### 未做（有意）
没有把 coverage 的 `--collect` 折进 mainline 的那次 `dotnet test`。看似是「省一次全量测试」的优化，但 coverage 门禁是**故意**只统计 6 类测试中的 4 类（`CoreUnit|PropertyTest|Persistence|ViewModelBehavior`，排除 `Analyzer`/`UiContract`）来算 9 条关键路径阈值的；合并会抬高被测行覆盖率，静默放宽阈值。

## 上一次会话（2026-10-15 CI 加固）

### CI 加固（诚实性与牙）
审计发现 receipt 在 GUI 冒烟上自相矛盾，已修复并给每条新不变量配上机器强制层：
- **receipt 说谎**：`verify-mainline.ts` 无条件调用 `processSmoke()`（windows runner 有桌面会话、冒烟真跑），随后 `isCi` 分支把真实结果覆盖成常量 `"skipped-ci-headless"`——一次超时失败与一次干净通过在 receipt 里写成同一字符串。`861fde60` 原本用 `-not $isCi` 真正跳过，TS 迁移（`559619ec`）丢了守卫只留下标签。现改为：如实记录 `launch+title`，仅在平台不支持时记 `skipped-non-windows`；冷启动首次上窗超时做有界重试（2 次），次数写入新字段 `guiSmokeAttempts`，抖动可审计
- **SDK 溯源**：`detectDotnet10Sdk` 取 `--list-sdks` 首个 10.x，在 CI 上把 10.0.301 报成镜像预装的 10.0.112。改为 `dotnet --version`（尊重 `global.json`），并抽 `isSupportedDotnetSdk` 纯函数
- **workflow 加固**：`permissions: contents: read`、`concurrency`（PR 取消被取代的运行，main/tag 不取消）、`timeout-minutes: 30`、`tags: ["v*"]` 触发（tag→产物绑定由机器验证）、两个 workflow 的 action 全钉 40 位 SHA（浮动标签可被上游移动）
- **门禁扩权**：`ci-fast-lane-no-publish-duplication` 新增 4 条不变量 + 全 workflow SHA 钉固校验，并对每条正则做注入自检；反例已实测——把 checkout 改回 `@v7` 即被捕获
- **单测**：新增 `gui-smoke-honesty.test.ts`（5 例）与 SDK 溯源 2 例，脚本层 46 tests 全绿

### 仓库设置
- `main` 分支保护：required check `verify`、`enforce_admins: true`、禁 force-push、禁删除、线性历史
- 安全开关：secret scanning、push protection、dependabot security updates 已启用；`non_provider_patterns` / `validity_checks` 需 GitHub Advanced Security，保持关闭

### 文档漂移收口
- `docs/spec/core/quality-guidelines.md`：删「CI 无头检测跳过 GUI smoke」的过时描述，改为如实记账不变量
- `docs/evidence/branch-evidence-matrix.md`：第 18 项更新为加固后的实测值（含反例注入）

## 上一次会话（2026-10-14）

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