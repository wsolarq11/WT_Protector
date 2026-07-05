# 修复 UltraReview 全部发现

## Goal

修复 2026-07-05 UltraReview 中发现的全部 55 个问题（5 CRITICAL + 8 HIGH + 20 MEDIUM + 22 LOW），提升代码质量、消除硬 Bug、修复安全漏洞、同步文档与实现。

## Background

本次修复基于 UltraReview 结果（8 个并行审查代理 + 人工代码阅读产出），覆盖以下层面：
- `WTLauncher.Core` — 36 个文件
- `WTLauncher.App.ViewModels` — 31 个文件
- `WTLauncher.App.Views` — 14 个文件
- `WTLauncher.Tests` — 57 个测试文件
- Scripts/Tooling — ~10 个文件
- Docs/Specs — ~10 个文件

## Requirements (按优先级分组)

### P0 — 必须修复 (CRITICAL)

| ID | Severity | File | Issue |
|----|----------|------|-------|
| F01 | CRITICAL | LiteDbStore.cs | `storePath` 参数被忽略违反 IStore 契约 |
| F02 | CRITICAL | LiteDbStore.cs | `DeleteAll`+`Insert` 非原子写入，崩溃即丢数据 |
| F03 | CRITICAL | LauncherPresetWorkflow.cs | Presets 为 null 时 NPE |
| F04 | CRITICAL | ShellCommand.cs + LaunchArgumentBuilder.cs | ShellCommand 配置完全失效（死代码+逻辑脱钩） |
| F05 | CRITICAL | MainWindow.axaml.cs | AutoCompleteBox Enter 键检测永远为 false，搜索历史无法保存 |

### P1 — 高优先级 (HIGH)

| ID | Severity | Issue | Layer |
|----|----------|-------|-------|
| F06 | HIGH | 旧三文件格式 → 统一 store.json 迁移缺失 | Core/Migration |
| F07 | HIGH | 坏 store 文件导致应用启动崩溃（无降级） | ViewModel |
| F08 | HIGH | ApplyPresetFileState 先写 `_presetFile` 后验证 | ViewModel |
| F09 | HIGH | AddPreset/DeletePreset 缺少 try-catch 保护 | ViewModel |
| F10 | HIGH | `--config` UNC 路径导致 NTLM 凭据中继 | Core/Security |
| F11 | HIGH | Edit/Delete 回滚后 ViewModel 引用孤立 | ViewModel |
| F12 | HIGH | LiteDB 路径缺少 Schema 版本检查 | Core/LiteDb |
| F13 | HIGH | 测试套件：LauncherStore.Load 无任何错误路径测试 | Tests |

### P2 — 中优先级 (MEDIUM)

共 20 项，涵盖：
- JSON MaxDepth 未显式设置
- `--config` 路径仅约束文件名不限制位置
- LiteDB 未使用事务
- NormalizeStoreTimestamp 无 DateTime 格式验证
- ErrorLog 旋转窗口日志丢失
- 对话框 lambda 引用 MainWindow 未清理
- 拖拽 hover 闪烁 / ForceCleanup 可重入
- GenerateUniqueCloneId 未规范化 sourceId
- ExceptionFilters 循环浪费
- ShellCommand 结构体死代码
- LiteDB 数据库未加密
- 测试：TempDirectory 泄漏 (4处) / 死代码 / 拖拽测试死代码
- CI: dotnet test 无 TRX / PS 冗余子进程
- PowerShell 脚本未设置 StrictMode
- 批次取消后 DryRunText 残留
- 三文件 SchemaVersion 标记无迁移
- 拖拽 _lastDragInsertAfterDecision 无 hover 防抖

### P3 — 低优先级 (LOW)

共 22 项，含代码格式、可读性、边界优化、PS 脚本改进等。

### 文档同步 (DOC)

| ID | Severity | Issue |
|----|----------|-------|
| D01 | HIGH | README + spec 仍描述三文件分离架构，实际已统一为 LauncherStoreFile |
| D02 | HIGH | spec 引用的持久化文件（4 个 .cs）已不存在 |
| D03 | HIGH | LiteDB 依赖注入 Core 层违反零依赖不变性规则 |
| D04 | MEDIUM | 审计文档特征计数不一致（21 vs 23） |
| D05 | MEDIUM | 测试数量在文档间不一致（542 vs 556 vs 410） |
| D06 | MEDIUM | spec 内 File.Move 原子性声明自相矛盾 |
| D07 | MEDIUM | feature-first-class-upgrade-audit 中 searchHistory 判定已过时 |
| D08 | MEDIUM | design-benchmark-digest 已 46 天未更新 |
| D09 | LOW | 多个 spec 文件语言声明与实际不符 |
| D10 | LOW | LauncherConfigFile.cs 已 obsolete 未清理 |

## Constraints

- 所有修复必须是只读分析的延续，不破坏现有功能
- 修复后必须通过 `tools/verify.ps1` gate
- CRITICAL 级修复优先于其他级别
- 文档同步与代码修复同步进行

## Acceptance Criteria

- [ ] 所有 CRITICAL 级问题（F01-F05）已修复并验证
- [ ] 所有 HIGH 级代码问题（F06-F12）已修复并验证
- [ ] HIGH 级测试问题（F13）已补充测试覆盖
- [ ] MEDIUM 级问题处理≥80%
- [ ] 文档同步问题（D01-D10）已修复
- [ ] `tools/verify.ps1` gate 通过
- [ ] 全部现有测试通过

## Out of Scope

- 本次不进行大规模架构重构（如将 LiteDbStore 移出 Core 层）
- 不修改 UI/UX 设计（仅修复 Bug）
- 不新增功能特性

## Open Questions

（将在 Brainstorm 过程中逐一确认）
