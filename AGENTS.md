# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command. Avalonia/.NET 8, win-x64 self-contained publish.

## 全仓地图 (Repo Map)

```
WT_Protector/
├── AGENTS.md              ← 你在这里。全仓导航中枢
├── README.md              → 详细项目文档（配置schema、安全说明、功能清单）
├── DESIGN.md              → 设计契约（品牌、原则、视觉语言、组件所有权）
├── config/                → 运行时配置（JSON, 人可读写）
│   ├── config.json        → 启动条目（id/name/directory/command/selected）
│   ├── presets.json       → 命名启动组（引用 config.json entry ids）
│   └── settings.json      → 便携UI偏好（排序/视图/确认/窗口尺寸）
├── src/
│   └── avalonia-launcher/ → 活动主线：Avalonia/.NET 源码 + 测试 + 构建
│       ├── README.md      → 源码架构（5层模型）、构建与验证
│       ├── src/
│       │   ├── WTLauncher.Core/   → Layer 0: 纯数据/规则（24文件, 零Avalonia依赖）
│       │   └── WTLauncher.App/    → Layer 1-4: ViewModels + Views
│       │       ├── ViewModels/    → 33文件, 单 MainWindowViewModel + 10 partial
│       │       └── Views/        → 18文件, 单 MainWindow.axaml 三视图
│       ├── tests/WTLauncher.Tests/ → 73测试文件, xUnit
│       ├── run.ps1        → 开发运行
│       └── verify.ps1     → 主线门禁（restore → test → publish → GUI smoke）
├── tools/                 → 🧰 脚本工具箱（详见下方）
├── docs/                  → 设计审计、基准对标、巨人肩膀库
│   ├── feature-reality-north-star-audit.md → 21项功能审计 + 声明边界账本
│   ├── design-benchmark-digest.md          → 12项外部设计基准 → 本地验收规则
│   ├── giants-shoulders/  → 巨人肩膀库（外部项目教训）
│   └── reference/         → GitHub对标快照 + 快捷方式退役清单
├── retired/               → 退役归档（旧HTA启动器 + WinForms设计教训）
│   └── README.md          → 退役说明
└── omx_wiki/              → 项目知识库（持久化本地决策与模式总结）
```

## 🧰 脚本工具箱 (tools/)

| 脚本                                          | 用途                                                                                                                                                                                                     | 何时用                                               |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| `verify.ps1`                                  | **全量门禁**：16 项检查（行尾策略、退役路径、主线布局、README、功能审计、声明边界、GitHub 快照、config/presets/settings schema、快捷方式退役、WinForms 退役、HTA 归档、Avalonia 主线测试+发布+GUI 冒烟） | 每次提交前；声称"完成"前                             |
| `build-release.ps1`                           | **稳定发布构建**：win-x64 self-contained publish → `src/avalonia-launcher/artifacts/publish/win-x64/`                                                                                                    | 需要产出可分发的 exe 时                              |
| `config-lib.ps1`                              | **共享 PowerShell 函数库**：被 verify.ps1、build-release.ps1 等引用                                                                                                                                      | 不直接调用；被其他脚本 dot-source                    |
| `test-launch-args.ps1`                        | **聚焦测试**：仅运行 LaunchArgumentBuilder 测试                                                                                                                                                          | 快速验证启动参数逻辑                                 |
| `update-github-similar-projects-snapshot.ps1` | **刷新 GitHub 对标快照**：通过 `gh repo view` 拉取 12 个对标项目的最新数据                                                                                                                               | 需要更新市场对标证据时（快照超过 30 天会被门禁拒绝） |

## 藤曼路径 (Vine Trail) — 新会话导航

任何新会话中，从本文件出发，按需深入：

1. **想了解项目全貌？** → `README.md`（配置 schema、安全说明、功能清单、实现状态）
2. **想改源码？** → `src/avalonia-launcher/README.md`（5 层架构、关键不变量）
3. **想做 UI/UX 决策？** → `DESIGN.md`（品牌、原则、组件所有权、Token 规则）
4. **想审计功能边界？** → `docs/feature-reality-north-star-audit.md`（21 项功能、声明边界账本）
5. **想参考外部设计？** → `docs/design-benchmark-digest.md` + `docs/giants-shoulders/`
6. **想运行/测试？** → `powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1`
7. **想验证全仓？** → `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`

## 核心规则

- `.snow/` 是忽略的本地运行时/索引状态，不追踪、不校验、不作为完成声明依据
- 行尾由 `.gitattributes` 管理：全部文本归一化为 LF
- 声称仓库变更"完成"前必须跑通：`git diff --check` + `tools\verify.ps1`
- 源码在 `src/avalonia-launcher/`，退役物在 `retired/`，不恢复退役路径
- 配置文件（`config/*.json`）是唯一权威源，无 AppData/registry 后备
