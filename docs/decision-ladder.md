Status: Active
Purpose: 三类架构统一决策阶梯与变化闭包，防止改动散落和重复机制。
Update when: 目录、代码分层、函数职责、文档分类或单一源分配规则变化时。

# 决策阶梯与变化闭包

## 决策阶梯

三层共用一条顺序，从高到低，禁止跳级：

1. 事实取证：先实测当前行为，区分实测确证与源码静态推断。
2. 语义归类：这个变化属于哪类事实（目录、代码/函数、视觉、命令、不变量、文档），影响哪个闭包。
3. 唯一源修改：只改该层的唯一源，不动消费方。
4. 消费方复验：跑 `npm run verify`、`npm test`、`typecheck`、`release:contract`。
5. 证据归档：更新 `docs/branch-evidence-matrix.md`，把静态推断转成实测确证。

## 变化闭包

| 层 | 唯一源 | 消费方 | 禁区 |
| --- | --- | --- | --- |
| 文件目录 | `AGENTS.md` 结构基线表 + `scripts/checks/run.ts` 基线常量 | `structure-baseline-consistency` | 手改目录后不改基线 |
| 代码分层 | `src/avalonia-launcher/README.md` 五层架构 | Analyzer 层规则、门禁 | 跨层直接调用、造第二个可写状态源；其他入口文档复制架构表 |
| 函数职责 | 每个文件首导出的核心入口 + 后续函数按调用图拓扑排列 | `grep ^func`/`^public` 的执行叙事 | 模糊命名、过早抽象、重复逻辑 |
| 视觉 | `docs/visual-perspective-system.md` + `LauncherDesignTokens.cs` | `App.axaml` 全局皮肤 | XAML 裸色值、窗口内联重复皮肤 |
| 命令 | `package.json`（机器源）+ `AGENTS.md` 脚本工具箱（唯一人工视图） | `README.md`/`CLAUDE.md`/`src/avalonia-launcher/README.md` 只引用 | 各入口文档各自重列命令清单 |
| 项目不变量 | `AGENTS.md` 核心规则 | `README.md` 安全口径（用户视角）、`.trellis/spec/`（代码层） | 第二份不变量表（如 `CLAUDE.md` 自列） |
| 门禁分层 | `AGENTS.md` | `CLAUDE.md` 只引用 | 另一份 gate tier 说明 |
| 文档 | `docs/README.md` 索引 | 藤曼路径、`task-ledger-citations-resolve` | 归档后仍留在 Active、旧三文件引用、`Status` 头与目录与索引分节三者不一致 |

## 治理成本

加门禁前先过盈亏平衡点：漂移定价与治理定价都算完，再选手段。

- 漂移定价看读者：AI 读者盲目信任不试错，AI 入口文档（`AGENTS.md`/`CLAUDE.md`）必须准，值得锚定；人类读者会试错纠偏，人类入口可略旧，约定即可。
- 手段按成本从低到高：不做 → 流程约束（归档登记旧路径）→ 少量关键锚点（`wiring-map` 类名）→ 双向门禁（命令表镜像）→ 真生成（脚本产出文档）。能用便宜的不用贵的。
- 门禁是负债不是资产：每加一个 = 加一个要维护的漂移源 + 一个把文档焊死的点；焊点累积会杀死可重构性。定期按引用密度判据（`.trellis/spec/guides/pitfalls.md` §2.26）审计删过剩门禁。
- 可重构性 > 完整性：文档能被放心重构，比所有事实都被门禁钉死更重要。

## 目录架构

- `src/avalonia-launcher/` 是唯一活动源码，`retired/` 永不恢复。
- `docs/` 四类：Active / Reference / Research / Archive，唯一入口是 `docs/README.md`。
- 生命周期判据：`Status` 头、所在目录、索引分节三者必须一致。
- 新文档先问：它是不是 Active 事实源；不是则进 Reference/Research/Archive。

## 代码架构

- 五层：Core → Commands → Persistence → Orchestration → Views。
- 一层只有一个可写状态拥有者；其他层只读投影。
- 抽象必须说明防住什么，说不清就删。

## 函数架构

- 文件按职责拆分，500 行提示值。
- 早返回，缩进不超过 2 层；重复逻辑抽公共函数。
- 函数按执行流拓扑排列，`grep` 抓出的序列就是执行叙事。
