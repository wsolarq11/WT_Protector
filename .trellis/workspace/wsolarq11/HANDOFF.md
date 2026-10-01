# Handoff — 2026-10-01（StoreGrain TOCTOU 修复 + 证据矩阵校正 + 重排决策链收口）

## 最新现状

- `main` 最新 commit `1ef5844c`（StoreGrain TOCTOU 修复），已 push 到 `origin/main`。
- 本地 `npm run verify` 全量 PASS（43 步门禁 + 870 测试 + AOT），`git diff --check` 干净。
- 远端 CI run `36805277377` 已绿（`conclusion: success`）；仓库已转回 **PRIVATE**。

## 本轮（10-01）完成——严格占优决策链

核心决策：**治理带停止线**——漂移按「静默/响亮 × 昂贵/便宜」四象限分类，静默昂贵进编译期、响亮便宜不治理、其余退回设计，判定一次即停（终止条件消除「治理的治理」无限递归）。

### 代码改动（4 文件，+113/-17）

1. `StoreGrain.cs`：`ReloadFromDisk` 的 `Load` 移入锁内，与 `Dispatch` 的 `Save` 对称，消除 reload/dispatch TOCTOU——修复「thread-safe」声称与实际行为的漂移（旧实现锁外读盘 + 锁内赋值，与 Dispatch 交错时旧值覆盖新值，内存与盘分叉）。
2. `ConcurrencyStressTests.cs`：+88 行，确定性竞态测试 `StoreGrain_ReloadFromDisk_SerializesWithDispatch_NoOverwrite`（`BlockingLoadPersistence` 复现交错时序）。A/B 验证：撤销修复 → 测试确定性失败 `Expected "dispatched" / Actual "fresh"`；恢复 → 转绿。非假绿。
3. `AGENTS.md`：结构基线 `Test attributes` 739→740（新增 1 个 `[Fact]`，生成物）。
4. `branch-evidence-matrix.md`：证据矩阵校正——9 条过时「静态推断」缺口逐条实测验证为已覆盖（AppStoreTests / LegacyMigrationTests / FileLauncherJsonIOTests / RefreshFailureTests / EntryListViewModelSaveFailureTests / UpdateCheckServiceTests / MigrationTests），唯一真实缺口 `CatchExceptionMustHaveWhenCodeFixProvider` 零测试登记为低风险不治理（IDE 工具、非运行时路径、治理成本 > 价值）。

### 重排决策链收口（本会话论证结论）

- 拖拽 vs 键盘重排：非严格占优（帕累托前沿）；`MoveRelative` 已是唯一顺序算子，公式统一不值得。
- 拖拽期间键盘重排的 discard 是双重防护（30 处显式 + `RebuildVisibleProjection` 结构性兜底），无漏洞。
- 结论沉淀在 `branch-evidence-matrix.md`「关键结论」+「Top 无覆盖分叉」段。

## CI 流程规则（权威源 AGENTS.md 核心规则）

- 私有仓库触发 CI 前必须先转公开（本 session 已执行，当前 PUBLIC）。
- **禁止**先私有 push 触发 CI、失败后才发现账单拦截再转公开重跑。
- CI 绿后转回私有：`gh repo edit wsolarq11/WT_Protector --visibility private --accept-visibility-change-consequences`。

## 遗留提醒

- CI 已绿、仓库已转回私有，无需再操作可见性。
- 本会话唯一登记不治理项：`CatchExceptionMustHaveWhenCodeFixProvider` 零测试（低风险，见 `branch-evidence-matrix.md`）。
- 编译/门禁前若 exe 被运行中的「WT Launcher」锁定，`Stop-Process -Name WTLauncher.App -Force` 后重跑。

## 关键命令

- 查 CI：`gh run view 36805277377 --json status,conclusion`
- 转回私有：`gh repo edit wsolarq11/WT_Protector --visibility private --accept-visibility-change-consequences`
- 全量门禁：`npm run verify`
- 主线门禁：`npm run verify:mainline`
