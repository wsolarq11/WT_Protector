# 验证台账 — docs/roadmap-testing-structure.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档创建: 2026-08-15（提交 1d8dbadd）；文中无日期头，已补充
> 文中"最终指标"为 2026-08-14 基线（与 docs/test-structure.md 基线表一致）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 状态: 已完成（P0-P7 已实现并归档） | VERIFIED | .trellis/tasks/archive/2026-08/ 下 8 个测试体系任务全部存在（08-14-test-map-baseline、08-14-test-level-traits-fast-path、08-14-test-directory-layering、08-14-ui-contract-semantics、08-14-critical-journey-smoke、08-14-coverage-gates、08-14-ci-verify-dedupe、08-14-test-roadmap-acceptance） | 保留 |
| 目标: 升级"单测试项目 + 804 个测试 + 全量跑"结构 | HISTORICAL | 804 为改造前/完成时点数；与 2026-08-14 基线一致；当前 121 文件/728 属性/811 用例（AGENTS.md 结构基线） | 保留并注明当前值 |
| 现状基线: 测试集中在单项目、无层级标记 | HISTORICAL | git ls-tree ee2a85a9(07-23): tests/WTLauncher.Tests 下无分层子目录（扁平） | 保留 |
| coverlet 已引用 | VERIFIED | WTLauncher.Tests.csproj:22 PackageReference coverlet.collector | 保留 |
| 最终指标 120 文件/721 属性/804 用例 | HISTORICAL | 与 docs/test-structure.md「基线（2026-08-14）」一致；当前 121/728/811 | 保留并注明当前 |
| 快速层约 12.4s / 全量约 16.7s | HISTORICAL | 2026-08-14 基线记录（可漂移计时，不设硬断言） | 保留 |
| P0: 输出 docs/test-structure.md | VERIFIED | 文件存在 | 保留 |
| P1: [Trait("Category", …)] | VERIFIED | 测试文件含 [Trait("Category", TestCategories.CoreUnit/Analyzer/Architecture/...)] | 保留 |
| P1: tools/test-fast.ps1 只跑快速层 | VERIFIED | test-fast.ps1 filter = Category=CoreUnit|PropertyTest|Architecture|Analyzer | 保留 |
| P2: 8 层目录落地 | VERIFIED | Support/CoreUnit/Persistence/ViewModelBehavior/Analyzer/Architecture/UiContract/PropertyTest 全部存在；verify.ps1:560 test-structure-layering 强制 | 保留 |
| P3: XAML 契约语义化 | VERIFIED | UiContract 测试断言 token 常量/code-behind 标记（Assert.Contains("DialogTitleFontSize = 20"…)），不再匹配整段 XAML 字符串 | 保留 |
| P4: 关键旅程冒烟 2 条 + 临时配置 + --config | VERIFIED | tools/smoke-critical-journey.ps1 checks=launch+title；临时目录 launcher.json；src/avalonia-launcher/verify.ps1:103-109 接入 | 保留 |
| P5: 覆盖率门禁（关键路径 + 不设全仓目标） | VERIFIED | tools/test-coverage.ps1 + tools/coverage-gate.json（9 条关键路径，notes: total report-only）；verify.ps1:603 coverage-gate | 保留 |
| P6: CI 去重（build-and-test 快速层；full-gate 唯一完整验收） | VERIFIED | .github/workflows/verify.yml: build-and-test 跑 test-fast.ps1 + TRX 上传（40-49 行）；full-gate 跑 tools/verify.ps1（107-109 行）；verify.ps1:562-567 ci-fast-lane-no-publish-duplication 强制 | 保留 |
| P6: 评估 -SkipMainlineTests 传参 | 未落地 | src/avalonia-launcher/verify.ps1 param() 无该参数 | 注明未加入 |
| P7: 更新 .trellis/spec/tests/test-quality.md | VERIFIED | 文件存在 | 保留 |
| P7: 更新 README 测试章节 | VERIFIED | README.md:247 引用 docs/test-structure.md 与 test-fast.ps1 | 保留 |
| 建议 Trellis 任务拆分 8 个（08-14/08-15-*） | 名称有出入 | 实际任务全部为 08-14-* 前缀（非文中 08-15-*） | 更新为实际归档任务名 |
| 风险与对策 | 保留 | 当日计划 | 保留 |

处理汇总: 补充日期头与当前基线注（121/728/811，以 AGENTS.md 为准）；任务拆分列表更新为实际归档名（08-14-*）；SkipMainlineTests 注明未加入；其余保留。
