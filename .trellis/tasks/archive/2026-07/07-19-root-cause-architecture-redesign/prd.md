# 根源架构调研与重设计

## Goal

不满足于逐项修 bug / 加 token / 收口视觉一致性，而是深度研究同类项目架构，结合当前代码库问题源头分类，提出一套能剪掉整类问题的架构改进方案。

## 已知问题源头类别

| 类别 | 表现 | 根因 |
|------|------|------|
| A. 神类 ViewModel | `MainWindowViewModel` 横跨 entry/preset/settings/discovery 等所有领域，partial class 13+ 文件 | 没有按功能域拆分为可独立测试的 Service |
| B. 重复的视觉常量 | dialog padding 20/22/24 散落各处，XAML 硬编码 hex color | 没有强制性的单一 token 引用机制（已部分修复但仍有遗漏） |
| C. 异常处理不一致 | 部分 `catch { }`，部分 `catch (Exception)`，部分只 catch IO | 没有层级化的异常处理策略和强制检查 |
| D. AOT 序列化胶水 | JSON converter 写了但不注册，AOT sourcegen 不完整 | 没有自动化验证 converter 注册的工具 |
| E. 自动检查盲区 | verify.ps1 硬编码 feature count、catch 检查用 regex 有漏报 | 没有从源码自动推导的检查契约 |
| F. 内存/生命周期 | `PropertyChanged` 订阅不取消、`async void` 无 try/catch | 没有生命周期管理契约 |
| G. 状态投影漂移 | drag state 在 refresh 前需 discard 否则泄漏 | 可变状态没有集中管理 |

## 调研范围

1. **同类桌面启动器**：Flow Launcher（C#）、Wox、Keypirinha、ueli、Launchy 的架构设计——它们怎么组织代码、处理持久化、管理扩展功能
2. **Avalonia 生产实践**：大型 Avalonia 应用的推荐架构（feature modules、segmented shells、命令总线模式）
3. **社区桌面应用架构模式**：WPF/Avalonia/WinUI3 领域验证能消减上述根因的模式
4. **反模式对照**：当前架构中的反模式在社区中是否已有已知的解决方案

## Deliverables

- [ ] D1: 调研报告（含每个参考项目的架构分析 + 关键引用链接）
- [ ] D2: 对照分析（当前架构问题 × 参考项目的解决方案）
- [ ] D3: 目标架构提案（layers、boundaries、data flow、关键约束）
- [ ] D4: 迁移路径（从当前到目标的分阶段计划）

## Acceptance Criteria

- [ ] AC1: 至少调研 5 个外部参考项目，每个有架构描述和关键代码组织方式
- [ ] AC2: 每个根因类别至少有一个已知的架构模式可以消除它
- [ ] AC3: 目标架构方案是可执行的——有具体的迁移阶段和验收标准
- [ ] AC4: 方案产出后后续 session 按阶段推进，而非回到逐项修 bug 模式

## Out of Scope

- 具体实现代码（此任务是调研和提案）
- 第三方依赖选型的具体升级计划
- 性能基准测试
