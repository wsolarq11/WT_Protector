# WT Launcher / WT Protector

Local Windows Terminal workspace launcher: open saved folders with a chosen shell command. Avalonia/.NET 10, win-x64 self-contained.

## 全仓地图

```
WT_Protector/
├── AGENTS.md              ← 本文件：导航中枢 + 结构基线
├── README.md              ← 用户/产品文档：配置 schema、验证、运行、安全、状态
├── DESIGN.md              ← 设计契约：品牌、原则、组件所有权、Token
├── CLAUDE.md              ← Claude Code 专用入口，指向本文件与 docs/README.md
├── .wt-protector.dsc.yaml ← WinGet Configuration：新机前置依赖
├── config/launcher.json   ← 唯一运行时状态（entries/presets/settings/defaultPresetId/runHistory）
├── package.json / scripts/ ← Node/TypeScript 脚本层（verify / build-release / release-contract / smoke）
├── src/avalonia-launcher/ ← 活动主线：Avalonia/.NET 源码 + 测试 + 构建
├── tools/                 ← .NET 辅助工具（smoke-probe / uninstaller）
├── docs/                  ← 文档唯一索引见 docs/README.md（Active/Reference/Research/Archive）
└── retired/               ← 退役归档（旧 HTA + WinForms 教训），永不恢复
```

## 脚本工具箱

| 命令 | 用途 |
| --- | --- |
| `npm run verify` | 全量门禁：static + local + native + snapshot；提交前必须跑 |
| `npm run release:contract` | 发布契约：locked restore + AOT publish + clean tree + receipt |
| `npm run verify:mainline` | 仅 Avalonia 主线门禁 |
| `npm run start` | 运行主线应用 |
| `npm run test:fast` / `test:coverage` / `test:launch-args` | 聚焦测试 |
| `npm run smoke` | 已发布 exe 关键路径冒烟 |
| `npm run install:portable` / `uninstall` | 便携安装/卸载 |
| `npm run package:friend` | 好友交接 zip |
| `npm run update:github-snapshot` | 刷新 GitHub 对标快照 |
| `npm run apply:config` / `apply:config:dry` | 新机 WinGet 配置 |

## 结构基线（Structure Baseline）

仓库结构事实的唯一文档源。`structure-baseline-consistency` 校验文件系统与本表一致；结构变化时必须同步更新本表与 `scripts/checks/run.ts` 基线常量。

| 基线项 | 数量 |
|--------|------|
| WTLauncher.Core C# files | 51 |
| ViewModels C# files | 64 |
| MainWindowViewModel partials | 15 |
| Features C# files | 15 |
| Views C# files | 24 |
| Views AXAML files | 20 |
| WTLauncher.Analyzers C# files | 7 |
| Tests C# files | 129 |
| Test attributes [Fact]+[Theory] | 765 |
| Gate steps (verify.ts) | 45 |

更新协议：结构变化 → 跑 `npm run verify` 看 `structure-baseline-consistency` 报错 → 同步改本表与 `scripts/checks/run.ts` → 转绿。

## 藤曼路径（Vine Trail）

1. 想了解项目全貌？→ `README.md`
2. 想改源码？→ `src/avalonia-launcher/README.md`（5 层架构）
3. 想做 UI/UX 决策？→ `DESIGN.md`
4. 想审计功能边界？→ `docs/feature-reality-north-star-audit.md`
5. 想参考外部设计？→ `docs/design-benchmark-digest.md` + `docs/giants-shoulders/`
6. 想查任何文档？→ `docs/README.md`

## 核心规则

- `.snow/` 是本地忽略状态，不追踪、不校验、不作为完成依据
- 行尾由 `.gitattributes` 统一为 LF
- 声称完成前必须跑通 `git diff --check` + `npm run verify`
- `config/launcher.json` 是唯一权威源，无 AppData/registry 后备
- 源码在 `src/avalonia-launcher/`，退役物在 `retired/`，不恢复退役路径

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
