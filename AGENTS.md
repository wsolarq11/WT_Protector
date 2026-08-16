# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command. Avalonia/.NET 10, win-x64 self-contained publish.

## 全仓地图 (Repo Map)

```
WT_Protector/
├── AGENTS.md              ← 你在这里。全仓导航中枢
├── README.md              → 详细项目文档（配置schema、安全说明、功能清单）
├── DESIGN.md              → 设计契约（品牌、原则、视觉语言、组件所有权）
├── .wt-protector.dsc.yaml → WinGet Configuration：新机就绪层（Windows Terminal + PowerShell 7 + Node LTS）
├── config/                → 运行时配置（JSON, 人可读写）
│   └── launcher.json      → 统一存储：启动条目 + 命名启动组 + UI 偏好 + defaultPresetId + runHistory
├── package.json / package-lock.json → Node/TypeScript 脚本系统
├── scripts/               → TS 脚本（config-lib / build-release / release-contract / verify / verify-mainline / test-* / smoke + Vitest）
├── src/
│   └── avalonia-launcher/ → 活动主线：Avalonia/.NET 源码 + 测试 + 构建
│       ├── README.md      → 源码架构（5层模型）、构建与验证
│       ├── src/
│       │   ├── WTLauncher.Core/     → Layer 0: 纯数据/规则（53文件, 零Avalonia依赖）
│       │   ├── WTLauncher.Analyzers/ → Roslyn 分析器（7文件, 架构规则强制）
│       │   └── WTLauncher.App/      → Layer 1-4: ViewModels + Features + Views
│       │       ├── ViewModels/      → 65文件（MainWindowViewModel + 15 partial）
│       │       ├── Features/        → 16文件（Entries/Presets/Status/Updates/Dialogs/ConfigExchange）
│       │       ├── Infrastructure/  → 9文件（持久化 IO + IStorePersistence）
│       │       ├── Platform/        → 3文件（平台接线）
│       │       └── Views/           → 44文件（24 cs + 20 axaml; MainWindow.axaml 承载 Cards/Details/Names 三视图, 视图体在 LauncherEntriesView.axaml）
│       ├── tests/WTLauncher.Tests/ → 129测试文件, xUnit（8层, 门禁实测 832 用例）
├── tools/                 → .NET 辅助工具（smoke-probe / uninstaller）
├── docs/                  → 设计审计、基准对标、巨人肩膀库
│   ├── feature-reality-north-star-audit.md → 22项功能审计 + 声明边界账本
│   ├── feature-first-class-upgrade-audit.md → 23项功能一等公民改造审计 (2026-06-10)
│   ├── business-wiring-boundaries.md         → 业务接线总账（View→VM→StoreGrain→外部进程）
│   ├── design-benchmark-digest.md          → 12项外部设计基准 → 本地验收规则
│   ├── giants-shoulders/  → 巨人肩膀库（外部项目教训）
│   ├── reference/         → GitHub对标快照 + 快捷方式退役清单
│   ├── archive/           → 历史完成/审查文档归档（phase*-complete、ultrareview 等）
│   └── research/          → 探索性研究（含 wt-launcher-architecture-zero.md 退路方案，非行动方案）
└── retired/               → 退役归档（旧HTA启动器 + WinForms设计教训）
    └── README.md          → 退役说明
```

## 脚本工具箱 (npm scripts + tools/)

| 命令 / 工具                                     | 用途                                                                                                                                                                                                                                                                                                                                          | 何时用                                               |
| --------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| `npm run verify`                              | **全量门禁**：45 项检查（行尾策略、`.snow/` 忽略、退役路径、无 active PowerShell、无裸 catch、无硬编码颜色、主线布局、README、功能审计、声明边界、GitHub 快照、launcher.json/config/presets/settings 各 schema、快捷方式/WinForms/HTA 退役、Avalonia 测试+发布+GUI 冒烟、**结构基线**、release-lockfile-consistency、settings-single-source、no-stale-three-file-refs、winget-config-schema、Views 免命令式 UI、安全审计 F1-F3、无瘦包装、VTK/Trinity 架构、AppStore 残留、架构规则、分析器连线） | 每次提交前；声称"完成"前                             |
| `npm run release:contract`                    | **中央发布契约**：locked-mode restore + Native AOT publish + clean worktree + receipt/manifest 校验                                                                                                                                                                                                                                        | CI 合并前；声称发布完成前                            |
| `npm run apply:config` / `apply:config:dry`   | **新机配置包装器**：`winget configure --file .wt-protector.dsc.yaml`；dry-run 仅 validate                                                                                                                                                                                                                                                   | 拿到新 Windows 机器、想一键声明式安装前置依赖时      |
| `npm run test:launch-args`                    | **聚焦测试**：仅运行 LaunchArgumentBuilder 测试                                                                                                                                                                                                                                                                                               | 快速验证启动参数逻辑                                 |
| `npm run update:github-snapshot`              | **刷新 GitHub 对标快照**：通过 `gh repo view` 拉取 12 个对标项目的最新数据                                                                                                                                                                                                                                                                    | 需要更新市场对标证据时（快照超过 30 天会被门禁拒绝） |
| `npm run install:portable` / `uninstall`      | **便携安装/卸载**：部署 AOT 发布产物 + `uninstaller.exe`（.NET self-contained），卸载不依赖 Node/PowerShell                                                                                                                                                                                                                                  | 安装/卸载本机便携版时                                |
| `npm run package:friend`                      | **好友交接包**：将 AOT exe、快速上手、示例 config 打成单 zip                                                                                                                                                                                                                                                                                  | 产出可分发的 friend zip 时                           |

> TS 发布入口：`scripts/build-release.ts` + `scripts/release-contract.ts`，执行 `npm run release:contract`（CI 唯一权威入口）。
> TS 根门禁：`scripts/verify.ts` + `scripts/checks/*`，执行 `npm run verify`（CI full-gate 唯一权威入口）。
> TS 主线入口：`scripts/run.ts`、`scripts/verify-mainline.ts`、`scripts/test-fast.ts`、`scripts/test-coverage.ts`、`scripts/test-launch-args.ts`、`scripts/smoke-critical-journey.ts`；对应 `npm run start`、`verify:mainline`、`test:fast`、`test:coverage`、`test:launch-args`、`smoke`。

## 结构基线（Structure Baseline）— 门禁强制

仓库结构事实的**唯一文档源**。`scripts/verify.ts` 的 `structure-baseline-consistency` 检查逐项校验
文件系统与本表：结构变化时必须**同步更新**本表与 `scripts/checks/run.ts` 基线常量，否则门禁失败。

| 基线项 | 数量 |
|--------|------|
| WTLauncher.Core C# files | 53 |
| ViewModels C# files | 65 |
| MainWindowViewModel partials | 15 |
| Features C# files | 16 |
| Views C# files | 24 |
| Views AXAML files | 20 |
| WTLauncher.Analyzers C# files | 7 |
| Tests C# files | 129 |
| Test attributes [Fact]+[Theory] | 749 |
| Gate checks (Add-Check) | 45 |

> 更新协议：结构变化 → 跑门禁看 `structure-baseline-consistency` 报什么 → 同步改本表与
> `scripts/checks/run.ts` 基线常量 → 门禁转绿。

## 藤曼路径 (Vine Trail) — 新会话导航

任何新会话中，从本文件出发，按需深入：

1. **想了解项目全貌？** → `README.md`（配置 schema、安全说明、功能清单、实现状态）
2. **想改源码？** → `src/avalonia-launcher/README.md`（5 层架构、关键不变量）
3. **想做 UI/UX 决策？** → `DESIGN.md`（品牌、原则、组件所有权、Token 规则）
4. **想审计功能边界？** → `docs/feature-reality-north-star-audit.md`（22 项功能、声明边界账本）
5. **想参考外部设计？** → `docs/design-benchmark-digest.md` + `docs/giants-shoulders/`
6. **想运行/测试？** → `npm run start` / `npm run verify:mainline`
7. **想验证全仓？** → `npm ci && npm run verify`
8. **想跑 TS 发布契约？** → `npm ci && npm run release:contract`

## 核心规则

- `.snow/` 是忽略的本地运行时/索引状态，不追踪、不校验、不作为完成声明依据
- 行尾由 `.gitattributes` 管理：全部文本归一化为 LF
- 声称仓库变更"完成"前必须跑通：`git diff --check` + `npm run verify`
- 源码在 `src/avalonia-launcher/`，退役物在 `retired/`，不恢复退役路径
- 配置文件 `config/launcher.json` 是唯一权威源（entries + presets + settings + defaultPresetId + runHistory 一体），无 AppData/registry 后备

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
