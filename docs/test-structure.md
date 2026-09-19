Status: Active
Purpose: 测试层级定义与文件归属规则。
Update when: 测试目录、分类或验证命令变化时。

# 测试结构地图

每个测试文件必须能归入一个测试层级。文件计数与属性数以 `AGENTS.md` 结构基线表为准，本文不写死。

## 测试层级（8 层）

| 分类 | 目标 | 判定规则 | 典型文件 |
| --- | --- | --- | --- |
| Support | 共享 fixture/辅助类型，不是测试 | 文件名为 TempDirectory、StubDialogService、TestRepositoryPaths 等 | Support/ 目标目录 |
| CoreUnit | Core 纯逻辑：搜索、排序、分组、启动参数、历史、更新清单 | 只依赖 Core，不碰文件系统与 VM | LauncherEntryQueryTests、LauncherEntryOrderTests、LaunchArgumentBuilderTests |
| Persistence | 保存/加载/回滚/损坏恢复/JSON 契约 | 涉及临时文件、LauncherStore、FileLauncherJsonIO | FileLauncherJsonIOTests、LauncherStoreLoadFailureTests、LauncherJsonContextRegistrationTests |
| ViewModelBehavior | VM 命令、状态、选择、批启动、对话框边界 | 引用 App ViewModel 和 Stub 服务 | MainWindowViewModelTests.*、LauncherLaunchWorkflowTests |
| Analyzer | Roslyn analyzer 规则 | 文件名 *AnalyzerTests | CatchExceptionAnalyzerTests、LayerViolationAnalyzerTests |
| Architecture | ArchUnitNET/架构不变量 | 文件名 ArchitectureRules 或明确架构规则 | ArchitectureRules.cs |
| UiContract | XAML/UI 结构契约（XML 语义断言） | 文件名含 Contract、Xaml、Window、StatusPresentation | LauncherControlBarDeclutterContractTests、DialogConsistencyContractTests |
| PropertyTest | 属性/随机测试 | 文件名含 Pbt 或使用 CsCheck | CsCheckPropertyTests、LauncherEntryOrderPbtTests |

## 文件归属规则

- 一个文件只属于一个分类。
- 辅助类型不写测试属性，不进业务分类。
- 同时覆盖 Core 与 VM 时按被测行为主要层级归入 VM 分类。
- 命名冲突由本文判定。

## 目录

```text
tests/WTLauncher.Tests/
├── Support/
├── CoreUnit/
├── Persistence/
├── ViewModelBehavior/
├── Analyzer/
├── Architecture/
├── UiContract/
└── PropertyTest/
```

例外：`ArchitectureRules.cs` 位于 `Architecture/` 但保留根命名空间，避免遮蔽 ArchUnitNET 类型。

## 验证命令

```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore
```

```bash
npm run test:fast
```

快速层含 `CoreUnit`、`PropertyTest`、`Architecture`、`Analyzer`，不含临时文件/VM 集成层。

## 关键旅程 E2E 冒烟

```bash
npm run smoke
```

冒烟固定 2 条断言：进程启动进入主窗口、窗口标题为 `WT Launcher`；用临时 `launcher.json` 和 `--config`，
不触碰真实配置。`npm run verify:mainline` 已接入该冒烟。exe 为门禁 publish 产物（`artifacts/` 为 git-ignored，
仓库不落盘），需先运行门禁 publish 步骤生成。

## 覆盖率门禁

```bash
npm run test:coverage
```

关键路径清单在 `tools/coverage-gate.json`。全仓总覆盖率只报告、不设硬目标；`npm run verify` 执行该门禁。

## CI 反馈分层

- `build-and-test`：build + 快速层 + 上传 TRX，不做完整 publish。
- `full-gate`：唯一完整验收阶段，运行 `npm run verify`。
