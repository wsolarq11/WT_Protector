# Test Patterns

> 如何写 `WTLauncher.Tests` 测试。

## 命名

`<TypeUnderTest>Tests.cs`（单文件）或 `<TypeUnderTest>Tests.<Area>Tests.cs`（分区）。例：`LaunchArgumentBuilderTests.cs`、`MainWindowViewModelTests.DragTests.cs`。

## Fixture

工厂方法：`LauncherPersistenceTestFixtures.cs` 提供 `Entry(id)` / `PresetFile(...)` / `Preset(...)`；VM 区测试在 `MainWindowViewModelTests.Shared.cs` 有私有 `Entry(...)`。规则：工厂 `private static`；只收测试相关参数；中文测试名（`"演示"` / `"项目"`）。

临时目录：`TestRepositoryPaths.cs` / `TempDirectory.cs`（IDisposable）。规则：碰文件 I/O 的测试必用临时目录，禁写真实 `config/`。`TestRepositoryPaths` 以活跃单一标记 `config/launcher.json` + `README.md` + Avalonia 源根定位仓库；禁锚定退役 `config.json` / `settings.json` / `presets.json`。

## 结构

Arrange-Act-Assert；`[Fact]` 单场景、`[Theory]` + `[InlineData]` 参数化。

## 分层类别

- Layer 0（Core）：纯函数直测（`LaunchArgumentBuilderTests`、`LauncherEntryLayoutCalculatorTests`、`LauncherEntryOrderTests`、`LauncherEntryVisibilityTests`）。
- Layer 1-3（VM）：mock `Func<>` 注入（`ConfirmLaunchAsync = _ => Task.FromResult(true)`）。
- Layer 4（Views）：测提取纯函数（`MainWindowDragInsertDecisionTests`、`LauncherDragGhostLayoutCalculatorTests`、`MainWindowEntryCardResolverTests`、`MainWindowEntryRowDoubleTapTests`）。
- 契约：`StatusTextContractTests`、`MainWindowViewModelTests.CollectionContractTests`、`DialogBoundaryTests`、`PresetPersistenceTests`。

## 架构规则（ArchUnitNET）

包：`TngTech.ArchUnitNET` 0.13.3 + `TngTech.ArchUnitNET.xUnitV3` 0.13.3。R1-R10：
R1 Core 禁引 `Avalonia.*`；R2 禁引 `CommunityToolkit.*`；R3 禁依赖 `WTLauncher.App.*`；R4 ViewModels 禁依赖 `System.IO.File`；R5 仅 `LauncherJson` + 已知读者（`LaunchArgumentBuilder`、`LauncherStore`）可依赖 `System.IO.File`（Core 内）；R6 Core 禁引 `WTLauncher.App.ViewModels.*`；R7 / R8 Preset / Entry 服务禁引 Views；R9 / R10 Preset / Entry 服务禁依赖 `System.IO.File`。

Gotcha 1（type-level vs method-level）：`MethodMembers` 只含已加载程序集方法，外部框架方法（`System.IO.File.WriteAllText` 等）只作 `MethodCallDependency` 目标。用 `MethodMembers()` 匹配 = 空集静默通过。用 type-level：`.Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")`。代价是更粗（连读调用 `File.Exists` 也抓），按需用 R5 的显式豁免。

Gotcha 2（`IObjectProvider` 有状态）：每个 `[Fact]` 建 fresh predicate；跨规则复用静态 provider 会静默合并条件。

新增规则：1) 新 `[Fact]` `R7_<Layer>_<Constraint>`；2) inline 构造谓词；3) 依目标是否已加载决定 type/method-level；4) 豁免加 `.And().DoNotHaveFullName(...)` + `Because(...)` 注明理由；5) 回写 `quality-guidelines.md` 不变量表。

## Mock 策略

无 mock 框架，手写 test double：对话框注入替换 `Func<>`；文件 I/O 用临时目录；时间注入 `Func<DateTimeOffset>`。

## Fixture 文件

`LauncherPersistenceTestFixtures.cs`、`TestRepositoryPaths.cs`、`TempDirectory.cs`、`MainWindowEntryCardTestFactory.cs`、`MainWindowViewModelTests.Shared.cs`。规则：同一工厂 3+ 文件出现即移入 Shared。

## Feature-service / VM 组件契约测试

服务测试用真实服务 + 真实 Core workflow + `StubDialogService`（无 mock 框架）；覆盖 Success payload、用户 Canceled、可恢复 Failure、校验拒绝、输入聚合非变异。组件测试执行生成公共命令，断言可绑定属性、`PropertyChanged`、回调 payload、可见顺序；回调集合是 point-in-time payload（后续 mutation 不改已交付列表）。MWVM 测试仍是 snapshot/save/rollback/status 集成所有者；GUI smoke 是 XAML 渲染所有者。Bad：反射调私有命令方法 / 单测断言 XAML margin。测试要求：每个公共 feature-service 操作至少一直接成功或委托测试 + 适用 cancel/failure 分支；每个 payload 字段级断言；每个提取组件有构造/属性 + 命令/回调测试；完整 Release 测试 + `tools/verify.ps1` 必须绿（聚焦测试不替代生产门禁）。

## Forbidden Patterns

- ❌ 写真实 `config/`。
- ❌ `[Fact]` 依赖文件系统顺序；断言前排序。
- ❌ Sleep/Delay；用固定时钟注入。
- ❌ 无失败消息的布尔断言；`Assert.True(cond, "why")`。
- ❌ 依赖运行 Avalonia 窗口；抽纯逻辑测。
