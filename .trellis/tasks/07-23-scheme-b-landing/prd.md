# 架构方案 B 落地实施

## Goal

基于深源架构调研（07-23-arch-deep-research）推荐的方案 B（状态统一），分三个阶段彻底落地：清理+序列化修复 → 状态+异常+异步修复 → 门禁+CI自动化

## Requirements

- Phase 1: 死代码清理（Fluxor, ExceptionBoundary, LauncherIcons）+ 序列化修复（Loose/Strict 拆分）
- Phase 2: 状态管理统一（AppStore 演进）+ 异常合并（ErrorOr+ExceptionFilters）+ 异步并发修复
- Phase 3: 门禁加固（bare catch 检测）+ CI AOT 验证 + editorconfig + 自动化

详细设计见 `07-23-arch-deep-research/research/10-synthesis.md`

## Acceptance Criteria

- [ ] Phase 1: 死代码清理完毕，verify.ps1 全 PASS，410+ 测试全 PASS
- [ ] Phase 2: 统一状态容器 + 异常策略 + 异步修复，全 PASS
- [ ] Phase 3: CI 多阶段 + 门禁加固，全 PASS

## Notes

- 0 新 NuGet 依赖
- 每个 Phase 完成后 gate 必须全绿
- 引用研究产出的详细设计，不在本 PRD 中重复
