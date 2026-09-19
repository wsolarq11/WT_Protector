Status: Active
Purpose: 三类架构统一决策阶梯与变化闭包，防止改动散落和重复机制。
Update when: 目录、代码分层、函数职责或文档分类规则变化时。

# 决策阶梯与变化闭包

## 决策阶梯

三层共用一条顺序，从高到低，禁止跳级：

1. 事实取证：先实测当前行为，区分实测确证与源码静态推断。
2. 语义归类：这个变化属于目录、代码/函数、文档中的哪一层，影响哪个闭包。
3. 唯一源修改：只改该层的唯一源，不动消费方。
4. 消费方复验：跑 `npm run verify`、`npm test`、`typecheck`、`release:contract`。
5. 证据归档：更新 `docs/branch-evidence-matrix.md`，把静态推断转成实测确证。

## 变化闭包

| 层 | 唯一源 | 消费方 | 禁区 |
| --- | --- | --- | --- |
| 文件目录 | `AGENTS.md` 结构基线表 + `scripts/checks/run.ts` 基线常量 | `structure-baseline-consistency` | 手改目录后不改基线 |
| 代码分层 | `src/avalonia-launcher/README.md` 五层架构 | Analyzer 层规则、门禁 | 跨层直接调用、造第二个可写状态源 |
| 函数职责 | 每个文件首导出的核心入口 + 后续函数按调用图拓扑排列 | `grep ^func`/`^public` 的执行叙事 | 模糊命名、过早抽象、重复逻辑 |
| 视觉 | `docs/visual-perspective-system.md` + `LauncherDesignTokens.cs` | `App.axaml` 全局皮肤 | XAML 裸色值、窗口内联重复皮肤 |
| 文档 | `docs/README.md` 索引 | 藤曼路径、`task-ledger-citations-resolve` | 归档后仍留在 Active、旧三文件引用 |

## 目录架构

- `src/avalonia-launcher/` 是唯一活动源码，`retired/` 永不恢复。
- `docs/` 四类：Active / Reference / Research / Archive，唯一入口是 `docs/README.md`。
- 新文档先问：它是不是 Active 事实源；不是则进 Reference/Research/Archive。

## 代码架构

- 五层：Core → Commands → Persistence → Orchestration → Views。
- 一层只有一个可写状态拥有者；其他层只读投影。
- 抽象必须说明防住什么，说不清就删。

## 函数架构

- 文件按职责拆分，500 行提示值。
- 早返回，缩进不超过 2 层；重复逻辑抽公共函数。
- 函数按执行流拓扑排列，`grep` 抓出的序列就是执行叙事。
