# 测试体系结构路线图落实 - Design

## 结构

```text
08-14-test-structure-roadmap (parent)
├── test-map-baseline        # P0 taxonomy + docs/test-structure.md
├── test-level-traits-fast-path # P1 traits + fast lane
├── test-directory-layering  # P2 folders/namespaces
├── ui-contract-semantics    # P3 semantic UI contracts
├── critical-journey-smoke   # P4 published-app smoke
├── coverage-gates           # P5 coverage reports/gates
├── ci-verify-dedupe         # P6 CI/test dedupe
└── test-roadmap-acceptance  # P7 docs + final gate
```

## 关键决策

- 测试项目保持单一 `WTLauncher.Tests`，优先目录/命名空间分层，不拆项目。
- 层级通过 xUnit `Trait` 表达，CI 可用 `--filter` 快速筛选。
- UI 契约从字符串断言迁移到语义化控件树/稳定契约断言。
- E2E 数量控制在 1-3 条，独立 job 或脚本。
- 覆盖率只对关键路径设门禁，全仓覆盖率只报告。
- CI `build-and-test` 只做快速层，`full-gate` 做唯一完整测试。

## 不变式

- 任一阶段结束后，被测代码行为不变。
- 每个子任务完成前必须能单独运行对应测试子集。
- 没有 skipped 测试，没有 sleep/时序依赖。
