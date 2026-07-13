# CQ01 薄 MainWindowViewModel

## Goal

把 `MainWindowViewModel` 从业务工作流所有者收敛为可绑定状态、命令入口和服务协调器，使 preset、selection、layout/filter 与 launch 等可独立规则不再因新增功能继续堆入 partial。

## Confirmed facts

- Dialog、Entry、Preset service 已存在，但 `MainWindowViewModel` 仍在构造函数中直接 `new EntryService/PresetService`，且服务契约仍泄漏 `ObservableCollection<LauncherEntryViewModel>` 等 UI 所有权。
- 当前最大 partial 是 Presets、Layout、Batch；热点包括 `SyncVisibleEntries`、`ApplyFilter`、entry CRUD orchestration、single/batch launch 和 selection transitions。
- `07-12-fma-p2-services` 与 `07-12-fma-p3-thin-vm` 是既有实现任务；本任务复用并验收它们，不创建平行架构。
- CQ-02 已建立共享 mutation transaction boundary；所有提取必须保持该失败回滚契约。

## Requirements

- R1：完成剩余 service extraction，但只提取具有稳定输入/输出契约的工作流；绑定属性、ObservableCollection 所有权和 RelayCommand 仍归 ViewModel。
- R2：优先复用现有 Core workflows/calculators；service 不复制 Core 规则，不直接写磁盘，不持有第二份长期状态。
- R3：通过构造函数注入 `IEntryService`、`IPresetService` 及本任务新增的服务；composition root 提供生产实现，测试可使用轻量 fake。
- R4：将 preset CRUD/selection preview、search-history 等可独立测试的决策移出 ViewModel；复用现有 `LauncherEntryQuery`、`LauncherLaunchWorkflow`、selection snapshot/preview owners，不创建只转发这些既有所有者的空服务。窗口/绑定刷新顺序留在协调层。
- R5：不得以继续新增 `MainWindowViewModel.*.cs` partial 作为“瘦身”；目标是减少所有权和调用复杂度，而非仅移动行。
- R6：保持所有 public bindable property、generated command、dialog behavior、status text、config schema、rollback 与启动安全行为兼容。
- R7：每个抽取后的 service 具有直接契约测试；架构测试阻止 service 依赖 Views 或直接 `System.IO.File`。

## Acceptance Criteria

- [x] AC1：生产 composition root 注入 Dialog、Entry、Preset 与 Launch workflow，`MainWindowViewModel` 不再直接构造具体 Entry/Preset feature service。
- [x] AC2：ViewModel 内不再包含可由现有 Core workflow/service 完整拥有的 preset CRUD、search-history、filter/sort 或 launch decision 实现；selection 与 collection 同步的保留理由记录在 `ownership-audit.md`。
- [x] AC3：没有新增 MainWindowViewModel partial；Presets/Layout/Batch/Launch 的业务热点均有明确 owner，且没有增加空转发 service。
- [x] AC4：变更后的 PresetService 具备 success、cancel、failure 与规则契约测试；完整测试集增长并保持通过。
- [x] AC5：Core 零 Avalonia、single store、CQ-02 rollback、status authority 和三视图绑定契约保持不变，R7–R10 新架构规则通过。
- [x] AC6：完整 `tools/verify.ps1` 通过，包括 self-contained publish 和 GUI smoke。

## Out of scope

- CQ-03/CQ-04 的 XAML 拆分。
- CQ-05/CQ-06 的全面状态文案或 typed-result 改造；只允许为当前 service contract 做最小兼容形状。
- 新框架、mediator/event bus、通用 repository、运行时插件或跨平台抽象。
