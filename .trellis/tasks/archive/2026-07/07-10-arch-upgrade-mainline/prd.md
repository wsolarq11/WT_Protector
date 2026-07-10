# 架构升级主线：5项剪根式改造

## Goal

基于 2026-07-10 架构评估报告，把识别出的 12 个"问题源头"通过 5 项剪根式架构改造落地，让架构不变量从 prose 规则升级为编译期/测试期强制规则。**不是修症状，是干掉问题源头。**

## Sub-tasks (按执行顺序)

1. **改造 3 — ArchUnitNET 架构测试**（`07-10-arch-test-archunitnet`）：把 spec prose 不变量翻译成可执行测试。**最低风险，先做，为后续提供 guard。**
2. **改造 4 — CommunityToolkit.Mvvm source generator 完整采纳**（`07-10-mvvm-source-generator-full`）：把手写 `OnPropertyChanged` 投影升级为 `[ObservableProperty]` + `[NotifyPropertyChangedFor]`。
3. **改造 1 — Source-generated strongly-typed 域原语**（`07-10-strongly-typed-primitives`）：把 `LaunchArgumentBuilder` 的 8 层手防御塞进类型构造器。
4. **改造 2 — JSON Schema-first contract**（`07-10-schema-first-contract`）：把隐式 C# schema 升级为显式 JSON Schema + codegen，干掉 verify.ps1 中 5 处 schema 校验。
5. **改造 5 — UserControl 拆分 + CompiledBindings 重评**（`07-10-usercontrol-split-compiled-bindings`）：把 `MainWindow.axaml` 894 行三视图拆为 3 个 UserControl，重评 CompiledBindings 开关。

## Cross-cutting Acceptance Criteria

- [ ] 每个子任务完成后 `tools/verify.ps1` 必须 PASS（18 项检查 + Avalonia 主线 + GUI smoke）
- [ ] 每个子任务独立提交保住进度（commit message 引用 task slug）
- [ ] 不破坏现有 5 层架构、单源真值、原子持久化等核心不变量
- [ ] 不恢复 retired 路径
- [ ] 不降低现有安全纵深（LaunchArgumentBuilder 的 8 层防御等价或更强）

## Out of Scope

- 改造 6（SQLite WAL）— 当前 JSON+atomic write 已 work，单用户无并发，明确不做。
- DI 容器 — 单窗口 app 不需要。
- Event Sourcing — state mutation 复杂度未到。

## Notes

- 改造依赖顺序：3 → 4 → 1 → 2 → 5（按风险升序，每步为后续提供安全网）
- 改造 5 完成后需重跑 P7 评估（CompiledBindings + Native AOT ROI 反转判断）
- 每个改造完成后必须运行验证 agent（subagent_type="verification"）
