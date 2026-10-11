# 上游跟进 SOP

> 触发条件：上游项目发布新版本或安全公告。
> 红线：禁止在生产配置上直接测试上游预览版；一切改动先在 `.snow/` 隔离验证。

## 流程

1. **触发源**：GitHub release watch（AvaloniaUI/Avalonia、dotnet/sdk、microsoft/terminal）
2. **评估**：读 changelog，标记 breaking changes
3. **隔离验证**：在 `.snow/upstream-verify/` 中构建测试分支
4. **门禁**：跑 `npm run verify` 全量 + 手动 smoke
5. **合并**：全绿后更新 `global.json` / NuGet 引用，写 receipt

## 状态基线

| 上游 | 当前版本 | 锁定方式 | 上次跟进 |
| --- | --- | --- | --- |
| .NET SDK | 10.0.301 | `global.json` + `rollForward: latestFeature` | 2026-10 |
| Avalonia | 11.x | NuGet package ref | 2026-10 |
| Windows Terminal | N/A（读取外部配置） | 无锁定 | N/A |