Status: Active
Purpose: AI 代理导航中枢：全仓地图、命令人工视图、结构基线、项目级不变量唯一源。
Update when: 目录结构、脚本入口、结构基线、核心规则或导航变化时。

# WT Launcher / WT Protector

Local Windows Terminal workspace launcher: open saved folders with a chosen shell command. Avalonia/.NET 10, win-x64 self-contained.

## 读者分层

| 读者 | 入口 |
| --- | --- |
| 人类用户 | `README.md` |
| AI 代理 | 本文件 |
| DeepSeek Harness | `.dsh/DSH.md` |
| 设计决策 | `DESIGN.md` |
| 深度知识 | `docs/`（索引 `docs/README.md`） |

## 全仓地图

```
WT_Protector/
├── AGENTS.md              ← 本文件：AI 导航中枢 + 结构基线 + 项目级不变量唯一源
├── README.md              ← 用户/产品文档：快速上手、配置 schema、验证、运行、安全、状态
├── DESIGN.md              ← 设计契约：品牌、原则、组件所有权、Token
├── .dsh/DSH.md            ← DeepSeek Harness 的 Trellis 接入说明
├── .wt-protector.dsc.yaml ← WinGet Configuration：新机前置依赖
├── config/launcher.json   ← 唯一运行时状态（entries/presets/settings/defaultPresetId/runHistory）
├── package.json / scripts/ ← Node/TypeScript 脚本层（命令机器源 + verify / build-release / release-contract / smoke）
├── src/avalonia-launcher/ ← 活动主线：Avalonia/.NET 源码 + 测试 + 构建（五层架构唯一源）
├── tools/                 ← .NET 辅助工具（smoke-probe / uninstaller）
├── docs/                  ← 深度文档，唯一索引见 docs/README.md（Active/Reference/Research/Archive）
└── retired/               ← 退役归档（旧 HTA + WinForms 教训），永不恢复
```

## 脚本工具箱

命令机器源是 `package.json`；本表是其完整镜像，`command-table-consistency` 门禁双向校验一致。`README.md`/`src/avalonia-launcher/README.md` 只引用本表，不各自重列。

| 命令 | 用途 |
| --- | --- |
| `npm run make` | 唯一产品编译：build + smoke + package:friend，产出可交付 zip |
| `npm run verify` | 全量门禁：static + local + native + snapshot；提交前必须跑 |
| `npm run verify:mainline` | 仅 Avalonia 主线门禁 |
| `npm run release:contract` | 发布契约：locked restore + AOT publish + clean tree + receipt |
| `npm run release:build` | 发布构建：AOT exe → artifacts/publish/win-x64/ |
| `npm run start` | 运行主线应用 |
| `npm run test:fast` | CoreUnit + PropertyTest + Analyzer 聚焦测试 |
| `npm run test:coverage` | 覆盖率门禁 |
| `npm run test:launch-args` | 启动参数测试 |
| `npm run smoke` | 已发布 exe 关键路径冒烟 |
| `npm run install:portable` | 便携安装 |
| `npm run uninstall` | 卸载 |
| `npm run package:friend` | 好友交接 zip |
| `npm run update:github-snapshot` | 刷新 GitHub 对标快照 |
| `npm run apply:config` | 新机 WinGet 配置 |
| `npm run apply:config:dry` | 新机 WinGet 配置（预演） |
| `npm run typecheck` | 脚本层 TypeScript 类型检查 |
| `npm run test` | 脚本层 vitest 单测 |
| `npm run generate:icon` | 生成应用图标 |
| `npm run gen:structure-baseline` | 重新生成结构基线派生表 |

## 结构基线（Structure Baseline）

仓库结构事实的派生视图。本表由 `npm run gen:structure-baseline` 生成，勿手工编辑；`structure-baseline-up-to-date` 门禁校验生成物未过期。

<!-- DERIVED:structure-baseline:START -->
| 基线项 | 数量 |
|--------|------|
| WTLauncher.Core C# files | 53 |
| ViewModels C# files | 66 |
| MainWindowViewModel partials | 16 |
| Features C# files | 15 |
| Views C# files | 26 |
| Views AXAML files | 21 |
| WTLauncher.Analyzers C# files | 8 |
| Tests C# files | 114 |
| Test attributes [Fact]+[Theory] | 722 |
| Gate steps (verify.ts) | 43 |
<!-- DERIVED:structure-baseline:END -->

## 核心规则

项目级不变量唯一源；代码层规范在 `.trellis/spec/`。

- `.snow/` 是本地忽略状态，不追踪、不校验、不作为完成依据
- 行尾由 `.gitattributes` 统一为 LF
- 声称完成前必须跑通 `git diff --check` + `npm run verify`
- `config/launcher.json` 是唯一权威源，无 AppData/registry 后备
- 源码在 `src/avalonia-launcher/`，退役物在 `retired/`，不恢复退役路径
- 每类事实只有一个权威源；完整分配见 `docs/decision-ladder.md` 变化闭包

## 藤曼路径（Vine Trail）

按「我要做什么」直达唯一入口；完整生命周期索引见 `docs/README.md`。

| 我要做什么 | 唯一入口 |
| --- | --- |
| 了解全貌 / 配置 schema | `README.md` |
| 查命令 | 本文件「脚本工具箱」（机器源 `package.json`） |
| 改源码 / 加 workflow / 加端口 | `src/avalonia-launcher/README.md`（五层）+ `docs/wiring-map.md` |
| 接线（View→VM→Store→外部进程） | `docs/business-wiring-boundaries.md` |
| 做 UI/UX 决策 | `DESIGN.md` |
| 改视觉样式 | `docs/visual-perspective-system.md` |
| 审计功能边界 / claim | `docs/feature-reality-north-star-audit.md` |
| 参考外部设计 | `docs/design-benchmark-digest.md` + `docs/giants-shoulders/` |
| 统一架构 / 单一源决策 | `docs/decision-ladder.md` |
| 踩坑 / 代码层规范 | `.trellis/spec/` |
| dsh 平台 Trellis 接入 | `.dsh/DSH.md` |
| 查任何文档 | `docs/README.md` |

<!-- TRELLIS:START -->
# Trellis Instructions

These instructions are for AI assistants working in this project.

This project is managed by Trellis. The working knowledge you need lives under `.trellis/`:

- `.trellis/workflow.md` — development phases, when to create tasks, skill routing
- `.trellis/spec/` — package- and layer-scoped coding guidelines (read before writing code in a given layer)
- `.trellis/workspace/` — per-developer journals and session traces
- `.trellis/tasks/` — active and archived tasks (PRDs, research, jsonl context)

If a Trellis command is available on your platform (e.g. `/trellis:finish-work`, `/trellis:continue`), prefer it over manual steps. Not every platform exposes every command.

If you're using Codex or another agent-capable tool, additional project-scoped helpers may live in:
- `.agents/skills/` — reusable Trellis skills
- `.codex/agents/` — optional custom subagents

Managed by Trellis. Edits outside this block are preserved; edits inside may be overwritten by a future `trellis update`.

<!-- TRELLIS:END -->
