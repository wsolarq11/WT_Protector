# 拆 MainWindowViewModel god class (Stage 2)

## Goal

按 Stage 1 PRD(scope 已规划 9 partial,Stage 1 已交付 Drag + Reorder),继续把剩余 6 个 partial 落地。纯机械搬移,不改逻辑。

## Scope

| Partial | Cluster | 主文件行号(参考) |
|---------|---------|--------------------|
| `.Entries.cs` | Add/Clone/Edit/Delete commands + entry snapshot helpers | L329-602 |
| `.Launch.cs` | single launch (ShowDryRun, LaunchAsync, ConfirmSingleLaunch, RefreshLaunchConfirmation) | L647-775 |
| `.Presets.cs` | preset CRUD + apply/preview | L1166-1635 |
| `.Batch.cs` | selection + batch launch | L1666-2010 |
| `.Layout.cs` | filter/sort/view/scroll | L775-973, L1111-1148 |
| `LauncherStatusText.cs` | extract status text classes (LauncherEntryStatus / LauncherPresetStatus / etc) | L2025-2230 |

## Requirements

- R1: 新 partial 文件 `public sealed partial class MainWindowViewModel`(partial 自动继承 sealed)
- R2: 新 partial 文件需有正确 using directives
- R3: 主文件保留所有共享 state(`_settings`, `_presetFile`, `_entries`)和共享 helper(`SaveStoreFile`, `BuildStoreFile`, `ApplyFilter`)
- R4: 451 tests 必须全部通过
- R5: verify.ps1 必须通过
- R6: 不改变任何逻辑——纯搬移
- R7: LauncherStatusText 抽取为独立文件,保留为嵌套类(避免大范围 namespace 改动)

## Out of Scope

- Composition 重构(把 VM 拆成独立 collaborator 类)—— 现代最佳实践,但需要单独 design,不在本任务 scope
- 改变任何 command 行为或公开 API

## Acceptance Criteria

- [ ] 6 个新 partial/extracted 文件存在,各包含相应 cluster 全部成员
- [ ] 主文件减少约 1500+ 行(从 2587 → ~1000)
- [ ] `dotnet test -c Release` 仍 451 passing
- [ ] `tools/verify.ps1` 仍 PASS
- [ ] grep 验证:各 cluster 方法名在新 partial 文件中,主文件中无残留

## Notes

- 每个 partial 一个 commit(便于回滚)
- 不动 spec(directory-structure.md 现已说明 3 文件 layout,Stage 2 完成后再更新)
