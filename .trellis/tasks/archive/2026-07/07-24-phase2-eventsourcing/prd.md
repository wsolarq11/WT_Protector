# Phase 2: Event Sourcing

## Goal

引入 append-only 事件日志作为持久化真相源,把"当前状态"变为事件的纯投影,使崩溃恢复可验证、状态变更可回放。

## Requirements

1. **EventJournal** — Append-only 事件日志,使用 `FileOptions.WriteThrough` 实现崩溃安全写入;写入完成前不返回。
2. **LauncherEvent ADT** — 覆盖所有状态变更的 14 个事件类型,作为 OneOf-based 代数数据类型。
3. **LauncherStoreProjector** — 纯函数:事件序列 → `LauncherStoreFile`,无副作用、无 I/O。
4. **EventJsonContext** — AOT-safe 的 source-generated JSON 序列化上下文。
5. **向后兼容** — 现有 `config/launcher.json` 仍可直接读取,旧用户无感知迁移。

## Acceptance Criteria

- [x] `EventJournal.Append` 在返回前把事件写入磁盘(`FileOptions.WriteThrough`)。
- [x] `LauncherStoreProjector` 能把事件序列正确投影为 `LauncherStoreFile`。
- [x] `dotnet build -warnaserror` 通过,无 IL2026/IL3050 警告。
- [x] `dotnet test` 全绿,无回归。
- [x] 现有 `launcher.json` 仍可加载,无需迁移脚本。

## Implementation Evidence

- Landing commit: `d9fa243` "feat: implement VTuber-Killer architecture (EventJournal + Grain + StoreGrain)"
- 关键文件: `WTLauncher.Core/Events/EventJournal.cs` (142 lines)、`LauncherEvent.cs` (123 lines)、`LauncherStoreProjector.cs` (72 lines)、`EventJsonContext.cs` (24 lines)。
- Journal #54 记录。

## Notes

- 与 Phase 3 (CQRS + Grain) 同次落地,事件日志被 `StoreGrain` 单线程消费。
- 无独立快照机制(snapshot);Phase 2 PRD 提及的 snapshot 支持作为后续可选优化,未在本次实现。
