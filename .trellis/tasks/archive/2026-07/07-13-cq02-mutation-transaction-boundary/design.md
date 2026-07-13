# Design

## Minimum boundary

新增一个 App 层内部、无状态、同步的泛型 helper，用调用方提供的四个动作完成事务控制流：

```text
snapshot = capture()
mutate()
failure = persist()
if failure: restore(snapshot)
return failure
```

若 mutation 已在调用 helper 前完成，允许使用等价的 `Commit(snapshot, persist, restore)` 形状，以最小化现有命令重排风险。最终 API 以调用点最少且不降低正确性者为准。

## Ownership

- Helper：只拥有成功/失败分支顺序，不能理解 entries、presets、settings 或 UI 文案。
- Caller：拥有 snapshot 内容、mutation、restore、派生状态刷新和成功文案。
- `SaveStoreFile`：拥有调用 `LauncherStore.Save`、可恢复异常归类和错误日志。
- `LauncherStore`：继续独占序列化和磁盘原子提交。

## Migration seams

- Settings：`TryPersistSettingsChange` / `SaveSettingsChange`。
- Entries：`SaveEntryCollectionChanges` 与 entry snapshot restore。
- Presets：`SavePresetMutationAndApply` 与 preset file state restore。
- Selection：`SaveSelectionChangeOrRestore`。
- 其他 `SaveStoreFile` 调用逐一审计；管理编辑等若没有调用前 snapshot，不伪装成可回滚事务，留给相应 service extraction 补齐。

## Failure semantics

- `persist` 的失败值原样返回，使调用点继续显示具体错误。
- `restore` 不捕获或吞掉编程错误；恢复动作必须是内存操作，不执行第二次磁盘写入。
- 派生状态刷新属于 restore callback 的一部分，保证 UI 观察不到半回滚状态。

## Trade-offs

- 不建立通用 repository/unit-of-work：应用只有一个本地 JSON aggregate，泛化会增加样板而没有第二资源事务收益。
- 不在此任务引入 typed result：当前 `string?` 失败契约足以统一控制流，CQ-06 再在服务边界有证据地收紧类型。
- 不让 helper 捕获异常：异常分类已集中在持久化边界，重复捕获会模糊错误所有权。

## Rollback

改动仅涉及 helper、四类调用点和测试；若行为不等价，可逐调用点恢复原控制流而不触碰配置格式或磁盘数据。
