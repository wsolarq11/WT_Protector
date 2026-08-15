# 验证台账 — docs/test-structure.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档创建: 2026-08-15（提交 1d8dbadd）；文中基线表自带日期 2026-08-14

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 层级表 8 分类及其典型文件 | VERIFIED | 全部 17 个命名文件 Test-Path OK（LauncherEntryQueryTests、LauncherEntryOrderTests、LaunchArgumentBuilderTests、FileLauncherJsonIOTests、LauncherStoreLoadFailureTests、LauncherJsonContextRegistrationTests、MainWindowViewModelTests.*、LauncherLaunchWorkflowTests、CatchExceptionAnalyzerTests、LayerViolationAnalyzerTests、ArchitectureRules.cs、LauncherControlBarDeclutterContractTests、DialogConsistencyContractTests、CsCheckPropertyTests、LauncherEntryOrderPbtTests、TempDirectory、StubDialogService、TestRepositoryPaths） | 保留 |
| 目标目录 8 层已落地 | VERIFIED | 8 目录均存在且非空（verify.ps1:554-560 亦强制） | 保留 |
| ArchitectureRules.cs 在 Architecture/ 但保留根命名空间 | VERIFIED | 路径 tests/WTLauncher.Tests/Architecture/ArchitectureRules.cs；namespace WTLauncher.Tests | 保留 |
| 基线（2026-08-14）: 120 文件/721 属性/804 用例/12.4s/16.7s/1 项目 | HISTORICAL | 与 roadmap-testing-structure.md 最终指标一致；当前 121 文件/728 属性/811 用例（AGENTS.md 结构基线 + 复核计数 699+29=728） | 保留并注明当前以 AGENTS.md 为准 |
| P1 分类标记已补齐 / P2 物理分层已落地 | VERIFIED | [Trait("Category", TestCategories.*)] 存在；8 层目录存在 | 保留 |
| P4 E2E/关键旅程已补齐 | VERIFIED | smoke-critical-journey.ps1 + avalonia verify.ps1 接入 | 保留 |
| P5 覆盖率报告已生成并门禁 | VERIFIED | coverage-gate 检查 + full-gate 上传 coverage-report | 保留 |
| 验证命令（sln 路径、test-fast.ps1） | VERIFIED | src/avalonia-launcher/WTLauncher.Avalonia.sln 存在；tools/test-fast.ps1 存在 | 保留 |
| 快速层 = CoreUnit/PropertyTest/Architecture/Analyzer | VERIFIED | test-fast.ps1 filter 逐字一致 | 保留 |
| 冒烟 2 条断言（启动进主窗口、标题 WT Launcher） | VERIFIED | smoke 脚本 checks 名 launch+title；verify.ps1:109 校验 'launch+title' | 保留 |
| 冒烟用临时 launcher.json + --config，不触碰真实配置 | VERIFIED | smoke 脚本: 临时目录 wt-launcher-e2e-*；Start-Process -ArgumentList @('--config', $smokeStore) | 保留 |
| src/avalonia-launcher/verify.ps1 已接入冒烟脚本 | VERIFIED | verify.ps1:103-109 调用 tools/smoke-critical-journey.ps1 | 保留 |
| 冒烟命令中的 exe 路径 artifacts\verify-publish\win-x64\WTLauncher.App.exe | UNVERIFIABLE(构建产物) | Test-Path MISS；src/avalonia-launcher/artifacts 目录不存在（构建输出 git-ignored） | 保留命令并注明 exe 为门禁发布产物 |
| 覆盖率门禁: test-coverage.ps1 + coverage-gate.json + verify.ps1 执行 | VERIFIED | 三者均存在；verify.ps1:596-606 调 test-coverage.ps1 -NoBuild 并 Add-Check 'coverage-gate' | 保留 |
| 关键路径清单覆盖保存/原子写入/序列化/校验/启动参数/ID 安全/导入导出/持久化与回滚 | VERIFIED | coverage-gate.json 9 条 criticalPaths 与描述逐一对应 | 保留 |
| 全仓总覆盖率只报告不设硬目标 | VERIFIED | coverage-gate.json notes: "Total repository coverage remains report-only" | 保留 |
| CI: build-and-test = build+快速层+TRX，不 publish | VERIFIED | verify.yml:37-49（Build、Test (fast lane) 用 test-fast.ps1 -Logger trx、Upload test results） | 保留 |
| CI: full-gate = 唯一完整验收，含全量测试/发布/冒烟/覆盖率 | VERIFIED | verify.yml:89-116 full-gate 跑 .\tools\verify.ps1 + 上传 coverage-report | 保留 |

处理汇总: 基线表保留为 2026-08-14 历史记录，顶部补"当前以 AGENTS.md 结构基线为准（121/728/811）"注；冒烟 exe 路径注明为门禁产物；其余保留。
