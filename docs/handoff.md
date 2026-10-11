# 交接快照

> 最后一棒状态：「现在是什么状态」。

## 当前状态（2026-10-11 ~08:55 UTC）

**WT_Protector** — 收敛维护期。
- 本地 `npm run verify`：42 门禁步，820 测试，0 阻断 ✅
- 远端 CI (docs restructure)：`success` ✅
- 远端 CI (CI simplify)：`success` (run 38125445129) ✅
- 最新 commit：`484aa6b8`（本会话 4 个 commit）

## 本次会话完成的变更

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