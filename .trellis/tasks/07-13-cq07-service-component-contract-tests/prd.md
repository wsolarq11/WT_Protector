# CQ07 Service 与组件契约测试

## Goal

为 CQ-01/CQ-06 抽取后的 feature services 与稳定 ViewModel 组件建立直接、可维护的公共行为契约测试，使重构不再只能依赖大型 MainWindowViewModel 间接测试发现回退。

## Background

- `LauncherLaunchWorkflow` 已有独立完整测试，Entry/Preset service 已有基础测试，ArchitectureRules 已禁止 service 依赖 Views 与直接文件 IO。
- `EntryService` 的 clone/edit/delete/validation/ID generation 分支和 `PresetService` 的确认、validation、update/delete 失败分支仍主要由 MainWindowViewModel 测试间接覆盖。
- `LauncherCardGroupViewModel` 是已抽取、由 XAML 绑定的稳定组件，但尚无直接测试其属性、展开状态和 callback payload。
- XAML 样式与像素表现继续由 GUI smoke/后续视觉矩阵负责，不在单元测试中复制框架渲染。

## Requirements

- R1：以每个生产类一个测试文件的现有模式，补齐 `EntryService` 和 `PresetService` 公共方法的成功、取消、验证失败、可恢复异常与关键 payload 契约。
- R2：直接测试 `LauncherCardGroupViewModel` 的构造快照、`CountLabel`、展开图标通知、LaunchAll 与 SaveAsPreset callback 数据。
- R3：测试必须断言外部可观察行为，不复刻私有实现或通过脆弱反射调用私有方法。
- R4：复用 `StubDialogService`、现有 fixture/factory 与真实 Core workflow；不新增 mocking 框架或测试依赖。
- R5：若测试发现生产缺陷，只修复契约所有者的最小根因；不扩展 UI 或产品范围。
- R6：保留现有 MainWindowViewModel 回滚、持久化和结构测试，避免用小测试替换跨层证据。

## Acceptance Criteria

- [x] AC1：`EntryServiceTests` 覆盖所有公共方法与 Success/Canceled/Failure 的适用分支。
- [x] AC2：`PresetServiceTests` 覆盖所有公共方法、确认分支、mutation 成功/失败及 fallback payload。
- [x] AC3：新增 `LauncherCardGroupViewModelTests`，证明绑定属性和两个 callback 的稳定公共契约。
- [x] AC4：没有新增生产抽象、测试依赖或 XAML 样式断言；ArchitectureRules 继续通过。
- [x] AC5：`git diff --check` 与完整 `tools/verify.ps1` 通过，包括 publish 和 GUI smoke。

## Verification evidence

- 24 focused Entry/Preset/component contract tests passed.
- Full Release suite passed with 610 tests, including ArchitectureRules.
- `tools/verify.ps1` PASS with win-x64 self-contained publish and hidden-temp-config GUI smoke.
- No production source or dependency change was required.

## Out of scope

- CQ-03/CQ-04 后续 XAML 组件的测试（在组件实际抽取后由各自任务拥有）。
- 像素快照、视觉基线和 DPI 矩阵。
- 重新测试 Core 已充分覆盖的纯 workflow 实现细节。
