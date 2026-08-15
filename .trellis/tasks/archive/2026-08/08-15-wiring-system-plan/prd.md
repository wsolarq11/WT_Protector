# 调研规划：理顺每个接线系统方案

## Goal

对当前 5 层架构下的每个业务接线系统做调研与方案规划，产出一份可执行、可分阶段落地、且不破坏现有 38 项门禁和 811 测试的接线理顺计划。本任务只做规划，不进入实现。

## 已确认事实

- 当前统一接线已记录在 `docs/business-wiring-boundaries.md`：共 21 条业务路径，统一持久化边界为 `StoreGrain.Dispatch → FileStorePersistence → config/launcher.json`。
- 当前架构不是历史文档中的 Fluxor/AppStore：实际为 5 层 + `StoreGrain` 单一状态容器 + `Dispatch/DispatchErrorOr` + `MainWindowViewModel` 编排。
- 已知脆弱接线点：
  - `CommandManagement` 模板 edit 内层 `SaveStoreFile` + 外层 `ApplyCommandManagementEdit` 再保存，且内层返回值被忽略。
  - `EntryDetailsWindow` code-behind 直接调用 Core `LauncherEntryDrafts`，与服务层存在双重校验路径。
  - `StoreGrain.ReloadFromDisk` 不通知订阅者，刷新依赖 VM 手动全量重同步。
  - 单/批启动先 `Process.Start` 再 `SaveConfig`；保存失败时外部副作用无法回滚，内存运行历史/启动次数已变化。
  - `LaunchOrchestrator` 通过大量 `Func/Action` 桥接到 `MainWindowViewModel`，缺少编译期端口契约。
  - 多处 mutation 各自手写 `BuildStoreFile → SaveStoreFile → RestoreVmStateFromStore`，没有统一事务辅助。

## 调研结论（初始）

- 不建议引入 Fluxor/消息总线/use-case 框架来“解决”全部接线；当前规模下应先固化契约，再按证据决定是否加层。
- 可按 8 个接线系统分治：状态持久化、Entry/Preset mutation、启动、Dialog/Service、投影/布局、选择/拖拽、平台/外部 IO、错误回滚。
- 推荐路径：先做低风险“接线契约化”，包括统一事务保存、删除双保存、消除 View code-behind 业务规则、Reload 通知、启动后保存边界；更重度的 use-case/command 层留作未来扩展，不作为本轮默认方案。

## Requirements

- 将 21 条业务路径归纳为清晰的接线系统清单，每个系统给出：现状、问题、目标接线、边界、失败语义、验证方式。
- 对每个已知脆弱点给出至少一个方案选项，并标出推荐项、风险、回滚点。
- 产出分阶段实施计划：每阶段必须有独立可验证的收尾标准，不能一次大改。
- 明确“不做”清单，防止为规划而引入不必要复杂度。
- 实施阶段必须保持 `git diff --check` + `tools/verify.ps1` 全绿，文档与代码同步更新。

## Acceptance Criteria

- [ ] `design.md` 包含接线系统地图、目标接线原则、方案选项与推荐项。
- [ ] `implement.md` 包含分阶段计划、每阶段验收标准、回滚点。
- [ ] 规划不修改产品代码，不启动实现。
- [ ] 后续实施每个阶段跑通 `dotnet test` 与根门禁。

## Open Decisions

- 已确认（2026-08-15）：按档位 2 接线契约化规划；档位 1 作为前置阶段，档位 3 只作为远期边界，不进入本轮默认实施范围。

## Notes

- 本 PRD 是规划任务，不授予实现许可。
- 证据基础：`docs/business-wiring-boundaries.md`、`src/avalonia-launcher/`、`.trellis/spec/`、`docs/architecture-*` 历史提案。