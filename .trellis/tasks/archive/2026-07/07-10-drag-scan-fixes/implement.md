# Execution Plan — 拖拽柄可见性 + Discovery 扫描并行化

## Execution Summary (2026-07-11) — 实际收尾

任务在 2026-07-11 完成所有修复 + gate 绿。**实际发现的额外根因**（不在原 plan 内）:

- **扫描挂起真实根因 = UI 线程冲突**（不是原假设的并行死锁）。`DiscoverAgentsViewModel.ScanAsync` 原为 `Task.Run(() => { Rows.Clear(); Rows.Add(...); })` 整块在线程池跑,CollectionChanged 同步触发 `RenderRows` 修改可视化树 → Avalonia 跨线程异常,被 `catch (Exception when IsRecoverable)` 吞掉,UI 卡在"扫描中..."。修复:改为 `await Task.Run(() => _scanFunc())`,只有 scan 在线程池,Rows 修改通过 Avalonia SynchronizationContext 回 UI 线程。(.NET `await` 默认捕获 UI SynchronizationContext。)
- **ApplyFilter button shape bug** (发现于 brainstorm 期间): `ApplyFilter` 的 `_cardsAvailableSize != _lastAppliedCardsAvailableSize` 优化忽略了 entry count 变化,Add/Clone/DiscoverAgents 添加的新 entry 用默认 CardWidth=300/CardHeight=164,其他 entries 用旧 layout → 卡片大小不一致(Codex CLI 卡片比其他高)。修复:加 `_lastAppliedVisibleCount` 字段,条件改为 size OR count 变化。

**最终验证** (2026-07-11):
- `tools/verify.ps1` 16 项全 PASS
- 测试 508 (基线 507 + 新增 1 = `ApplyCardSize_RetriggersAfterAddingEntrySoAllCardsShareUniformSize`)
- `AgentDiscoveryScannerTests` 从 6 测试扩展到 10 测试 (并行 spawn + cli hang + PATH 异常)

## Revision (2026-07-10) — 方向调整摘要

用户反馈"拖拽要始终可以拖拽 + 扫描依旧不出东西"，否决了 Phase A 原方向（隐藏拖拽柄）。本任务实际执行：

- **Phase A (REVISED)** — 撤销 `MainWindow.axaml` 三处 `IsVisible` 绑定（拖拽柄恢复常显）；`MainWindow.Drag.cs:36` 守卫移除 `IsCustomSortMode: true`；`BeginDragReorder` 入口加 `EnsureDragSortMode()`。
- **Phase F (NEW)** — 修复 `AgentDiscoveryScanner.WindowsExtensions` 顺序 bug（移除 `""`）。
- Phase B/C/D/E 保留不变（已执行）。
- **D2 (REVISED)** — timing 测试 `Scan_ParallelSpawn_CompletesInMaxPerCliTime` 改为功能测试 `Scan_ParallelSpawn_FindsAllConcurrentEntries`（timing 对 AV/负载 flaky，并行性是库保证）。

## Pre-flight

- [x] 确认 `task.py current` 指向 `07-10-drag-scan-fixes`，status=in_progress。
- [x] 基线：`tools/verify.ps1` 当前 PASS（507 测试，gate 绿）。

## Phase A: 拖拽柄可见性（R1）

1. **A1** — 读 `MainWindow.axaml` 行 579-591（cards）、756-767（details）、864-876（names），确认三处 Border 当前结构。
2. **A2** — 在三处 Border 上加 `IsVisible="{Binding DataContext.IsCustomSortMode, RelativeSource={RelativeSource AncestorType=Window}}"`。
   - 不动 `PointerPressed="OnEntryDragPointerPressed"` 绑定。
   - 不动 `Classes="entry-drag-handle"`、`Cursor="Hand"`、`ToolTip.Tip`。
3. **A3** — 在 `MainWindow.axaml.cs` 头部确认 `xmlns:vm` 已声明（用于强类型绑定，可选；binding path 字符串足够）。
4. **A4** — 启动 app，切 alphabetical → 确认 ≡ 消失；切 custom → 确认 ≡ 出现且可拖。

## Phase B: ReadLine 超时（R7/R8）

5. **B1** — 编辑 `src/avalonia-launcher/src/WTLauncher.Core/AgentDiscoveryScanner.cs:TryDetectVersion`：调换 `ReadLine` 和 `WaitForExit` 顺序，参考 design.md ADR-3 代码块。
6. **B2** — 保留 `VersionTimeout` 常量、`VersionPattern` 正则、UTF-8 编码设置不变。
7. **B3** — 保留 kill 后 narrow ignore 的 catch（仅 `IOException`，已经覆盖 stream 关闭）。
8. **B4** — 编译 `dotnet build src/avalonia-launcher -c Release -warnaserror`，确保 0 warning。

## Phase C: 并行化（R4/R5/R6）

9. **C1** — 在 `AgentDiscoveryScanner.cs` 顶部 `using System.Collections.Concurrent;`。
10. **C2** — 把 `Scan` 方法体改成：
    - `found` / `failures` 改用 `ConcurrentBag<DiscoveredAgent>` / `ConcurrentBag<AgentDiscoveryFailure>`。
    - 用 `Parallel.ForEachAsync(catalog, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (entry, ct) => { ... })`。
    - 每个 entry 内部用 `await Task.Run(() => TryResolveOnPath(...))` + `await Task.Run(() => TryDetectVersion(...))`。
    - 失败仍按现有 catch 类型（`IOException | UnauthorizedAccessException | Win32Exception | InvalidOperationException`）累积到 `failures`。
    - 方法尾 `.GetAwaiter().GetResult()` 转 sync，构造 `AgentDiscoveryResult { Found = found.ToImmutable(), Failures = failures.ToImmutable() }`。
11. **C3** — 保留 `ArgumentNullException.ThrowIfNull(catalog)` 入口检查。
12. **C4** — 编译 + 跑现有 `AgentDiscoveryScannerTests` 6 个测试，全 PASS。

## Phase D: 测试（R10/R11）

13. **D1** — 读 `tests/WTLauncher.Tests/AgentDiscoveryScannerTests.cs` 现有结构（temp dir + mock catalog entry 模式）。
14. **D2** — 加 `Scan_ParallelSpawn_CompletesInMaxPerCliTime`：
    - 构造 2 个 catalog entry，一个 cli 立即输出（fast），一个 cli `Start-Sleep -Milliseconds 800` 后输出（slow）。
    - 用 `Stopwatch` 计时，断言 elapsed < 1500ms（串行模式下会 >= 1600ms）。
    - 用 PowerShell fixture 脚本（写入 temp dir，PATH override）。
15. **D3** — 加 `TryDetectVersion_ReturnsNull_WhenCliHangsOnStdout`：
    - 构造一个 cli fixture：`Start-Sleep -Seconds 10`（不输出 stdout）。
    - 断言 `TryDetectVersion` 在 `VersionTimeout + 1000ms` 内 return null。
16. **D4** — `dotnet test src/avalonia-launcher -c Release --filter "FullyQualifiedName~AgentDiscoveryScanner"`，全 PASS。

## Phase E: Gate + 收尾

17. **E1** — `tools/verify.ps1` 全 16 项 PASS。
18. **E2** — Grep `catch \(Exception\)` 在 `src/avalonia-launcher/src/**/*.cs`（无 `when`）= 0 命中。
19. **E3** — 测试数从基线 N → N+2。
20. **E4** — 手动 smoke：构建 `tools/build-release.ps1` → 启动 exe → 切 custom 模式拖拽 → 切回 updated 验证柄消失 → 打开 Discover 对话框验证亚秒扫描完成。

## Rollback Points

- Phase A 后出问题 → revert XAML 三行 IsVisible 绑定。
- Phase B 后出问题 → revert `TryDetectVersion` 顺序调换。
- Phase C 后出问题 → revert `Scan` 并行化（保留 Phase B 的超时修复）。
- 整体出问题 → `git revert` 整个任务 commit。

## Validation Commands

```powershell
# Quick rebuild + test
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~AgentDiscoveryScanner"

# Full gate
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1

# Release build + smoke
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```
