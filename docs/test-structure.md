# WT Launcher 测试结构地图

> 单一事实来源：每个测试文件必须能归入一个测试层级。
> 目标目录在 P2 落地；分类在 P1 以 xUnit Trait 落地。

## 测试层级

| 分类 | 测试目标 | 判定规则 | 典型文件 |
|---|---|---|---|
| CoreUnit | Core 纯逻辑：搜索、排序、分组、启动参数、历史、更新清单 | 只依赖 Core，不碰文件系统和 VM | `LauncherEntryQueryTests`、`LauncherEntryOrderTests`、`LaunchArgumentBuilderTests` |
| Persistence | 保存/加载/回滚/损坏恢复/JSON 契约 | 涉及临时文件、LauncherStore、FileLauncherJsonIO | `FileLauncherJsonIOTests`、`LauncherStoreLoadFailureTests`、`LauncherJsonContextRegistrationTests` |
| ViewModelBehavior | VM 命令、状态、选择、批启动、对话框边界 | 引用 App ViewModel 和 Stub 服务 | `MainWindowViewModelTests.*`、`LauncherLaunchWorkflowTests` |
| Analyzer | Roslyn analyzer 规则 | 文件名为 `*AnalyzerTests` | `CatchExceptionAnalyzerTests`、`LayerViolationAnalyzerTests` |
| Architecture | ArchUnitNET/架构不变量 | 文件名为 `ArchitectureRules` 或明确架构规则 | `ArchitectureRules.cs` |
| UiContract | XAML/UI 结构契约（XML 语义断言） | 文件名含 `Contract`、`Xaml`、`Window`、`StatusPresentation` | `LauncherControlBarDeclutterContractTests`、`DialogConsistencyContractTests` |
| PropertyTest | 属性测试/随机测试 | 文件名含 `Pbt` 或使用 CsCheck | `CsCheckPropertyTests`、`LauncherEntryOrderPbtTests` |
| Support | 共享 fixture/辅助类型，不是测试 | 文件名为 `TempDirectory`、`StubDialogService`、`TestRepositoryPaths` 等 | `Support/` 目标目录 |

## 文件归属规则

- 一个文件只属于一个测试分类。
- 辅助类型不写测试属性，不进入业务分类。
- 一个测试文件同时覆盖 Core 与 VM 时，按被测行为主要层级归入 VM 分类。
- 命名冲突时由 `docs/test-structure.md` 判定，不靠临时决定。

## 目标目录（已落地）

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

例外：`ArchitectureRules.cs` 位于 `Architecture/` 但保留根命名空间，避免子命名空间 `Architecture` 遮蔽 ArchUnitNET 类型。

## 基线（2026-08-14）

> 复核（2026-08-15）：当前基线以 AGENTS.md 结构基线为准 — **121 测试源文件 / 728 个 `[Fact]`+`[Theory]` 属性 / 门禁实测 811 用例**。下表 120/721/804 为 2026-08-14 时点记录（历史基线）。

| 指标 | 值 |
|---|---|
| 测试源文件 | 120 |
| `[Fact]`/`[Theory]` 方法 | 721 |
| 实际测试用例 | 804 |
| 快速层耗时 | 约 12.4s（Release no-build） |
| 本地全量测试耗时 | 约 16.7s（Release no-build） |
| 测试项目 | 1（WTLauncher.Tests） |
| 分类标记 | 已补齐（P1） |
| 物理分层 | 已落地（P2） |
| E2E/关键旅程 | 已补齐（P4） |
| 覆盖率报告 | 已生成并门禁（P5） |

## 验证命令

```powershell
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-fast.ps1
```

快速层包含 `CoreUnit`、`PropertyTest`、`Architecture`、`Analyzer`，不包括需要临时文件/VM 集成层的测试。

## 关键旅程 E2E 冒烟

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\smoke-critical-journey.ps1 -Exe .\src\avalonia-launcher\artifacts\verify-publish\win-x64\WTLauncher.App.exe
```

冒烟固定在 2 条断言（进程启动进入主窗口、窗口标题为 `WT Launcher`），使用临时 `launcher.json` 和 `--config`，不触碰真实配置。`src/avalonia-launcher/verify.ps1` 已接入该脚本。

> 注：命令中的 exe 路径为 verify 门禁的发布产物（`artifacts/` 为 git-ignored 构建输出，仓库中不落盘）；需先运行门禁的 publish 步骤生成该 exe。

## 覆盖率门禁

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-coverage.ps1
```

关键路径清单在 `tools/coverage-gate.json`，覆盖保存、原子写入、序列化、校验、启动参数、ID 安全、导入导出、文件持久化与回滚边界。全仓总覆盖率只报告、不设硬性目标；`tools/verify.ps1` 会执行该门禁。

## CI 反馈分层

- `build-and-test`：build + 快速层 + 上传 TRX，不做完整 publish。
- `full-gate`：唯一完整验收阶段，运行 `tools/verify.ps1`，包含全量测试、发布、关键旅程冒烟和覆盖率门禁。
