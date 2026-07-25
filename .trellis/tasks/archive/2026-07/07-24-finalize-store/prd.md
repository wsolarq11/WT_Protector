# Finalize: AppStore removal & migration

## Goal

关闭 VTuber-Killer 迁移的最后一公里:确认 `AppStore` 完全退役、`StoreGrain` 接管全部状态管理、文档与 spec 同步更新。

## Requirements

1. **`AppStore` 物理删除** — 文件从仓库移除,不再保留为 deprecated shim。
2. **`pitfalls.md` 同步** — 把 StoreGrain 迁移踩坑(`Dispatch` 后必须 `SetState`、async void handler 保护模式)写入 spec,供未来参考。
3. **`async void` handler 保护** — 4 个 dialog 共 11 个 async void handler 全部加 `try-catch(ExceptionFilters.IsRecoverable)`。
4. **bare catch 清零** — 4 处违规全部修复(`DirectoryPath.cs`、`AgentDiscoveryScanner.cs`、`PresetManager.cs`、`MainWindowViewModel.Batch.cs ×2`)。
5. **2 个新 analyzer 落地** — `WT1005 ResourceKeyAnalyzer` + `WT1006 JsonRegistrationAnalyzer`。

## Acceptance Criteria

- [x] `AppStore.cs` 已从仓库删除(`d9fa243` 中 -165 lines)。
- [x] `pitfalls.md` 新增 §2.5.5(StoreGrain Dispatch 后必须 SetState)与 §2.5.6(async void 保护)。
- [x] 11 个 async void handler 全部保护到位。
- [x] 4 处 bare catch 清零,`WT1004` 同时检测 `catch {}` 与 `catch (Exception)`。
- [x] `WT1005` / `WT1006` analyzer 进 `.analyzer/WTLauncher.Analyzers/`。
- [x] `dotnet build -warnaserror` 通过,无 analyzer 告警。

## Implementation Evidence

- Pitfalls 提交: `fd9a1bd` "docs: add StoreGrain migration pitfalls and async void handler pattern"。
- Analyzer 提交: `1b6dc51` "feat: add Roslyn analyzers (WT1005 resource-keys, WT1006 JSON registration)"。
- Bare catch + async void 修复提交: `075682b` "fix: eliminate bare catch violations and protect async void handlers"。
- Journal #54 记录。

## Notes

- 本 PRD 文件在归档时回填(2026-07-25),原始 PRD 是 TBD 模板;真实验收标准据上述 3 个提交 + journal #54 还原。
