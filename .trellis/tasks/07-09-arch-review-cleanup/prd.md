# 架构审查清理落地 - 父任务

## Goal

基于 2026-07-09 7 轮架构审查的全面清理落地。核心目标:**让代码、依赖、spec 三者重新一致**,消除死代码、僵尸抽象、虚构文档。

## Background

审查发现的核心问题(详见审查报告 21 章):
- LiteDB 完整链路(代码 + 依赖 + 测试 + spec 注释)是 IStore 抽象删除后遗留的僵尸,生产路径永不触发
- spec 65% 过时,其中 5 个文件主动误导(描述已删除/不存在的架构)
- LauncherPresetFile 是从三文件时代遗留的中间容器,单文件 store 下冗余
- MainWindowViewModel 2796 行 god class,spec 已规划 9 partial 拆分但从未实施
- 多处轻量问题(陈旧注释、模板漏洞、文件名中间态)

## Child Tasks(按 ROI 顺序)

| # | Task | Priority | 风险 | 预期删除/改动 |
|---|------|----------|------|----------------|
| 1 | remove-litedb-deadcode | P0 | 极低 | -250 行 + 1 NuGet 依赖 |
| 2 | rewrite-stale-specs | P0 | 低(纯文档) | ~1100 行重写 |
| 3 | remove-preset-file-abstraction | P1 | 中(改类型引用) | -80 行 + 改名 |
| 4 | split-mainviewmodel-godclass | P1 | 高(大重构) | 拆 1 文件成 9 partial |
| 5 | misc-light-cleanup | P2 | 极低 | 注释/改名/模板修复 |

## Cross-Task Acceptance Criteria

- [ ] 5 个 child 全部完成 plan/execute/finish 全流程
- [ ] 每个 child 提交后有独立 commit(便于回滚)
- [ ] 全程 `dotnet test -c Release` 必须通过
- [ ] 全程 `tools/verify.ps1` 必须通过(最终验收)
- [ ] 不引入新依赖(本次是减肥,不是增重)
- [ ] 不删除任何有真实业务价值的代码或测试

## Coordination Rules

- Child #1 必须最先做(其他 child 的 spec 引用都会受影响)
- Child #4(拆 god class)最后做,因为它最大且其他 child 可能间接影响 MainWindowViewModel
- Child #2 和 #3 可并行,但 #2 应先(避免 spec 引用 LauncherPresetFile 后又要改)
- Child #5 放最后,作为清理收尾

## Rollback Strategy

每个 child 一个 commit。若 `verify.ps1` 失败,`git reset --hard HEAD~1` 回滚该 child 全部改动,重新规划。

## Out of Scope

- 功能新增(本次只做删除/重构/文档同步)
- 测试基础设施重构
- 性能优化(spec 提到的 O(n²) 等 ceiling 保留)
- pitfalls.md 内容核实(740 行太大,留给单独任务)
