# P4: FSM + CsCheck — 设计文档

## Finite State Machine (Stateless)

将拖拽工作流建模为显式状态机：

```
States: Idle → Preparing → Dragging → Committing → Idle
                                   → Canceling → Idle
```

使用 `dotnet-state-machine/stateless` 库，使非法转换成为运行时错误：
- `Preview` in Idle state → 自动转换到 Dragging
- `Commit` in Idle state → no-op（而非 NRE）
- `Cancel` in Committing → 不允许（需要先完成）

## Property-Based Testing (CsCheck)

添加 CsCheck NuGet 包，实现 property-based test：

| 测试目标 | Property | 输入范围 |
|---------|----------|---------|
| `RotateBackups` | 备份文件数 ≤ maxBackups | 任意文件系统状态 |
| `MigrateFromLegacyFiles` | 所有有效 entry 出现在结果中 | 任意 legacy 文件组合 |
| `LaunchArgumentBuilder.Build` | 往返一致性 | 任意有效参数 |
| `LauncherJson.Save + Load` | 往返一致性 | 任意有效 LauncherStoreFile |
| `AppStore.Dispatch` | 并发安全性 | 随机 CRUD 操作序列 |

## Debug.Assert 不变量

核心路径添加 Debug.Assert：
- `AppStore.Dispatch`: 操作后 state 完整性
- `LauncherStore.Save`: 文件写入后验证
- `EntryListViewModel.ReplaceEntries`: ID 唯一性
