# 理想典范全维度差距与重构清单

## Goal

基于双方源码和可验证体验证据，评估 WT Launcher 与一个适合其窄领域目标的复合“理想典范”之间的代码质量、用户视觉和用户实操差距，并给出最小、分阶段、可验证的重构清单。

## Requirements

- 当前侧读取 Core、App/ViewModels、Views/XAML、测试、发布脚本和设计契约的真实实现。
- 典范侧读取已索引 CodexSwitch/CodexSwitchUI 代表性源码，并复用仓库已有 12 类 launcher/workspace 与设计系统研究。
- 明确 CodexSwitch 仅是视觉/组件与发布参照；其超大 `MainWindowViewModel` 和无关 AI 代理业务不是代码质量目标。
- 对每个差距给出当前证据、典范做法、用户影响、根因、最小重构、可行性、风险、估算级别和验证方式。
- 与现有 `07-12-fma-parent`、集中降噪、扫描向导、frecency 等任务去重；已有任务覆盖的事项应引用而非重复创建实现任务。
- 不扩大到插件市场、跨平台、更新器、工作流图或隐藏状态数据库，除非证据显示当前用户任务无法由更小机制覆盖。

## Acceptance Criteria

- [x] 产出一份可独立阅读的差距/重构审计文档。
- [x] 至少覆盖代码质量、视觉、实操三个一级维度及其关键子维度。
- [x] 结论引用当前源码/图谱/测试和典范源码/研究的具体证据。
- [x] 清单按 P0/P1/P2/拒绝或延期分类，并标明与现有 Trellis 任务的关系。
- [x] 给出 Now/Next/Later 等价阶段路线，但不把未获需求证明的项目包装成承诺。
- [x] 审计本身不修改产品运行时逻辑。

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
