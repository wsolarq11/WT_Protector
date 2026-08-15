# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command. Avalonia/.NET 10, win-x64 self-contained publish.

## 全仓地图 (Repo Map)

```
WT_Protector/
├── AGENTS.md              ← 你在这里。全仓导航中枢
├── README.md              → 详细项目文档（配置schema、安全说明、功能清单）
├── DESIGN.md              → 设计契约（品牌、原则、视觉语言、组件所有权）
├── .wt-protector.dsc.yaml → WinGet Configuration：新机就绪层（Windows Terminal + PowerShell 7）
├── config/                → 运行时配置（JSON, 人可读写）
│   └── launcher.json      → 统一存储：启动条目 + 命名启动组 + UI 偏好 + defaultPresetId + runHistory
├── src/
│   └── avalonia-launcher/ → 活动主线：Avalonia/.NET 源码 + 测试 + 构建
│       ├── README.md      → 源码架构（5层模型）、构建与验证
│       ├── src/
│       │   ├── WTLauncher.Core/     → Layer 0: 纯数据/规则（53文件, 零Avalonia依赖）
│       │   ├── WTLauncher.Analyzers/ → Roslyn 分析器（7文件, 架构规则强制）
│       │   └── WTLauncher.App/      → Layer 1-4: ViewModels + Features + Views
│       │       ├── ViewModels/      → 53文件（MainWindowViewModel + 15 partial）
│       │       ├── Features/        → 15文件（Entries/Presets/Status/Updates/Dialogs）
│       │       ├── Infrastructure/  → 9文件（持久化 IO + IStorePersistence）
│       │       ├── Platform/        → 3文件（平台接线）
│       │       └── Views/           → 44文件（24 cs + 20 axaml; MainWindow.axaml 承载 Cards/Details/Names 三视图, 视图体在 LauncherEntriesView.axaml）
│       ├── tests/WTLauncher.Tests/ → 121测试文件, xUnit（8层, 门禁实测 818 用例）
│       ├── run.ps1        → 开发运行
│       └── verify.ps1     → 主线门禁（restore → test → publish → GUI smoke）
├── tools/                 → 脚本工具箱（详见下方）
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

## 脚本工具箱 (tools/)

| 脚本                                          | 用途                                                                                                                                                                                                                                                                                                                                          | 何时用                                               |
| --------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| `verify.ps1`                                  | **全量门禁**：38 项检查（行尾策略、`.snow/` 忽略、退役路径、无裸 catch、无硬编码颜色、主线布局、README、功能审计、声明边界、GitHub 快照、launcher.json/config/presets/settings 各 schema、快捷方式/WinForms/HTA 退役、Avalonia 测试+发布+GUI 冒烟、**结构基线**、settings-single-source、no-stale-three-file-refs、winget-config-schema、Views 免命令式 UI、安全审计 F1-F3、无瘦包装、VTK/Trinity 架构、AppStore 残留、架构规则、分析器连线） | 每次提交前；声称"完成"前                             |
| `build-release.ps1`                           | **稳定发布构建**：win-x64 self-contained publish → `src/avalonia-launcher/artifacts/publish/win-x64/`                                                                                                                                                                                                                                         | 需要产出可分发的 exe 时                              |
| `apply-machine-config.ps1`                    | **新机配置包装器**：`winget configure --file .wt-protector.dsc.yaml`；`-DryRun` 仅 validate                                                                                                                                                                                                                                                   | 拿到新 Windows 机器、想一键声明式安装前置依赖时      |
| `config-lib.ps1`                              | **共享 PowerShell 函数库**：被 verify.ps1、build-release.ps1 等引用                                                                                                                                                                                                                                                                           | 不直接调用；被其他脚本 dot-source                    |
| `test-launch-args.ps1`                        | **聚焦测试**：仅运行 LaunchArgumentBuilder 测试                                                                                                                                                                                                                                                                                               | 快速验证启动参数逻辑                                 |
| `update-github-similar-projects-snapshot.ps1` | **刷新 GitHub 对标快照**：通过 `gh repo view` 拉取 12 个对标项目的最新数据                                                                                                                                                                                                                                                                    | 需要更新市场对标证据时（快照超过 30 天会被门禁拒绝） |

## 结构基线（Structure Baseline）— 门禁强制

仓库结构事实的**唯一文档源**。`tools/verify.ps1` 的 `structure-baseline-consistency` 检查逐项校验
文件系统与本表：结构变化时必须**同步更新**本表与 `tools/verify.ps1` 中的基线常量，否则门禁失败。

| 基线项 | 数量 |
|--------|------|
| WTLauncher.Core C# files | 53 |
| ViewModels C# files | 53 |
| MainWindowViewModel partials | 15 |
| Features C# files | 15 |
| Views C# files | 24 |
| Views AXAML files | 20 |
| WTLauncher.Analyzers C# files | 7 |
| Tests C# files | 121 |
| Test attributes [Fact]+[Theory] | 735 |
| Gate checks (Add-Check) | 38 |

> 更新协议：结构变化 → 跑门禁看 `structure-baseline-consistency` 报什么 → 同步改本表与
> `tools/verify.ps1` 基线常量 → 门禁转绿。

## 藤曼路径 (Vine Trail) — 新会话导航

任何新会话中，从本文件出发，按需深入：

1. **想了解项目全貌？** → `README.md`（配置 schema、安全说明、功能清单、实现状态）
2. **想改源码？** → `src/avalonia-launcher/README.md`（5 层架构、关键不变量）
3. **想做 UI/UX 决策？** → `DESIGN.md`（品牌、原则、组件所有权、Token 规则）
4. **想审计功能边界？** → `docs/feature-reality-north-star-audit.md`（22 项功能、声明边界账本）
5. **想参考外部设计？** → `docs/design-benchmark-digest.md` + `docs/giants-shoulders/`
6. **想运行/测试？** → `powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1`
7. **想验证全仓？** → `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`

## 核心规则

- `.snow/` 是忽略的本地运行时/索引状态，不追踪、不校验、不作为完成声明依据
- 行尾由 `.gitattributes` 管理：全部文本归一化为 LF
- 声称仓库变更"完成"前必须跑通：`git diff --check` + `tools\verify.ps1`
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
