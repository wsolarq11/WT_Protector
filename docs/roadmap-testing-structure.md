# 测试体系结构改进路线图

状态：已完成（P0-P7 已实现并归档）
适用范围：`src/avalonia-launcher` 测试体系、CI、Avalonia 门禁

> 创建日期: 2026-08-15（提交 1d8dbadd）；P0-P7 任务已归档于 `.trellis/tasks/archive/2026-08/`。
> 文中指标为 2026-08-14 基线（120 测试源文件 / 721 属性 / 804 用例，与 `docs/test-structure.md` 基线表一致）。
> 复核（2026-08-15）：当前基线以 AGENTS.md 结构基线为准 — 127 测试文件 / 746 个 `[Fact]`+`[Theory]` 属性 / 门禁实测 829 用例。

## 目标

把当前"单测试项目 + 804 个测试 + 全量跑"的结构升级为分层、快速、可维护的测试体系，同时保持 `tools/verify.ps1` 的完整验收能力。

## 现状基线

- 804 个测试集中在 `WTLauncher.Tests`，没有测试层级标记。
- Core 单元、ViewModel 行为、持久化、架构规则、analyzer、XAML 契约、属性测试混在同一个项目。
- 没有真正的关键旅程 E2E；现有 Avalonia smoke 只验证窗口能打开。
- XAML 契约测试以字符串匹配为主，重构脆弱。
- CI 的 `build-and-test` 与 `full-gate` 会重复跑全量测试。
- 已引用 coverlet，但没有覆盖率报告、阈值或关键路径策略。

## 实施后结果

- 804 个测试按 7 个业务分类 + `Support` 物理分层，带 xUnit `Category` Trait。
- XAML 契约改为 XML 语义断言；关键旅程冒烟固定在 2 条。
- 覆盖率门禁覆盖 9 条高风险路径；CI 快速层与完整门禁去重。
- 最终指标：120 个测试源文件、721 个 `[Fact]`/`[Theory]` 方法、804 个用例；快速层约 12.4s、全量约 16.7s。

## 原则

- 先分类，再分层：不能靠文件猜测试级别。
- 优先用目录/命名空间分层，不急着拆项目。
- E2E 只保护关键旅程，数量必须少。
- 覆盖率用于关键路径，不设全仓 100% 目标。
- 任何阶段结束时，`tools/verify.ps1` 必须仍能 PASS。

## 阶段

### ✅ P0 测试地图与基线

目标：让仓库有一份权威测试地图和可对比基线。

交付物：
- 建立测试分类：`CoreUnit`、`Persistence`、`ViewModelBehavior`、`Analyzer`、`Architecture`、`UiContract`、`PropertyTest`。
- 输出 `docs/test-structure.md`：每个分类的目录、命名空间、测试目标、允许/禁止内容。
- 记录基线：测试文件数、测试数、全量耗时、CI 耗时。

验收：
- 每个现有测试文件都能归入一个分类。
- 基线数据写入文档，后续阶段可对比。

### ✅ P1 测试层级标记与快速反馈

目标：PR 阶段可以只跑快速层，完整门禁再跑全量。

交付物：
- 为测试类增加 `[Trait("Category", "CoreUnit|Persistence|ViewModelBehavior|Analyzer|Architecture|UiContract|PropertyTest")]`。
- 提供 `tools/test-fast.ps1`：只跑 `CoreUnit`、`PropertyTest` 等快速层。
- CI 的 `build-and-test` 改为 build + 快速层；`full-gate` 跑完整 verify。

验收：
- `dotnet test --filter Category=CoreUnit` 和 `--filter Category=ViewModelBehavior` 均能独立运行。
- 快速层耗时明显低于全量，CI 至少有一个"秒级/分钟级"反馈通道。

### ✅ P2 目录/命名空间分层

目标：在同一个测试项目内建立物理边界，避免混合。

建议结构：

```text
tests/WTLauncher.Tests/
├── Support/          # TempDirectory、StubDialogService、TestRepositoryPaths
├── CoreUnit/         # Core 纯逻辑
├── Persistence/      # 保存、加载、回滚、损坏恢复
├── ViewModelBehavior/ # VM 命令、状态、选择、批启动
├── Analyzer/         # Roslyn analyzer
├── Architecture/     # ArchUnitNET / 架构规则
├── UiContract/       # XAML/UI 语义契约
└── PropertyTest/     # CsCheck 属性测试
```

只允许把测试文件移动到新目录并调整命名空间；不改变被测代码。

验收：
- 目录结构与测试地图一致。
- `dotnet test --filter` 与目录筛选结果一致。
- 移动后全量测试仍 804+，`tools/verify.ps1` PASS。

### ✅ P3 语义化 UI 契约

目标：把脆弱的 XAML 字符串断言改成稳定、语义化的 UI 契约。

行动：
- 先用 spike 验证 Avalonia Headless / UI Automation 能否在测试内加载控件树。
- 将 `LauncherControlBarDeclutterContractTests`、`DialogConsistencyContractTests`、`MainWindowXamlModularityContractTests` 等改为断言控件树、命令绑定、自动化名称或稳定契约对象。
- 无法语义化的规则移入 `tools/verify.ps1`，不进测试文件。

验收：
- 测试中不再以 `Assert.Contains` 直接匹配整段 XAML 字符串。
- 重构 XAML 格式不会误伤契约测试，行为退化仍能失败。

### ✅ P4 关键旅程 E2E 冒烟

目标：用极少量测试覆盖用户核心旅程。

候选旅程：
- 启动应用并加载临时 `launcher.json`。
- 主窗口出现且条目标题可见。
- 关键命令路径（选择、预设、批启动）由 ViewModel 集成测试覆盖；E2E 只做"应用能真实启动并进入可用状态"。

行动：
- 新建独立 E2E 目录或项目，不混入单元测试。
- 复用现有 hidden-temp-config 冒烟，扩展为可断言的窗口/条目状态。
- 如果 UIAutomation/FlaUI 被证明稳定，再加一条真实点击关键路径；否则保留为文档化冒烟，不强行堆浏览器式 E2E。

验收：
- 发布后的 exe 能以临时配置启动并进入主界面。
- E2E 数量不超过 3 条，且可以在 CI 独立 job 运行。

### ✅ P5 覆盖率与质量门禁

目标：覆盖率用于关键路径，不做无意义的全仓指标。

行动：
- 配置 coverlet 输出 Cobertura/OpenCover，并上传 CI artifact。
- 为 Core 纯逻辑和持久化关键路径设置阈值。
- 为启动、导入、回滚等高风险路径建立"必须覆盖"清单。
- 评估 Stryker.NET 突变测试；若 .NET 10/AOT 不支持则记录为暂缓。

验收：
- CI 能生成并上传覆盖率报告。
- 关键路径清单在门禁中可执行，新增关键路径未覆盖会失败。
- 不把全仓总覆盖率设为硬性目标。

### ✅ P6 CI 与 verify 去重

目标：一次完整门禁只跑一轮全量测试。

行动：
- `build-and-test` 只做 build、快速层测试、结果上传。
- `full-gate` 作为唯一完整验收阶段，复用已上传的 build/TRX。
- 评估 `tools/verify.ps1` 内部是否可传 `-SkipMainlineTests` 给 `src/avalonia-launcher/verify.ps1`，避免本地重复跑测试。

> 复核（2026-08-15）：`-SkipMainlineTests` 参数未加入 `src/avalonia-launcher/verify.ps1`；CI 去重由 `build-and-test` 快速层（`test-fast.ps1`）+ `ci-fast-lane-no-publish-duplication` 门禁检查实现。

验收：
- CI 日志中全量测试只执行一轮。
- 本地 `tools/verify.ps1` 仍是一次完整验收。

### ✅ P7 文档与验收

目标：把新结构写进规格，完成整体验收。

交付物：
- 更新 `.trellis/spec/tests/test-quality.md`。
- 更新 `README.md` 的测试章节。
- 记录最终测试文件数、测试数、快速层/全量耗时。

验收：
- `dotnet build -warnaserror` PASS。
- `dotnet test -c Release` PASS。
- `tools/verify.ps1` PASS。
- 路线图所有阶段已归档或标记完成。

## 依赖关系

```text
P0 -> P1 -> P2 -> P3/P4 -> P5 -> P6 -> P7
             \        /
              P3,P4 可并行
```

P1 必须在 P6 之前完成，因为 CI 去重依赖快速层筛选。
P2 在 P1 之后做，先有分类再做物理移动。

## 建议 Trellis 任务拆分

规划时建议以下 8 个任务（实际实现并归档为 `08-14-` 前缀，见 `.trellis/tasks/archive/2026-08/`）：

1. `08-14-test-map-baseline`
2. `08-14-test-level-traits-fast-path`
3. `08-14-test-directory-layering`
4. `08-14-ui-contract-semantics`
5. `08-14-critical-journey-smoke`
6. `08-14-coverage-gates`
7. `08-14-ci-verify-dedupe`
8. `08-14-test-roadmap-acceptance`

## 风险与对策

- 批量加 Trait 会产生大量文件改动：用脚本机械化处理，并单独提交。
- 拆目录会碰到 `InternalsVisibleTo`、测试辅助类引用：保留 `Support/`，不拆项目。
- Avalonia E2E 可能 flaky：数量压到 1-3 条，CI 独立 job，不做浏览器式全量 E2E。
- 覆盖率阈值可能制造假安全感：只对关键路径设阈值，全仓覆盖率只报告不强制。
- XAML 契约测试迁移可能发现"规则本身不明确"：先把规则写进 `docs/test-structure.md`，再写测试。
