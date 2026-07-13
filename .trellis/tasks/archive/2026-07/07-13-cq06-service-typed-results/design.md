# Design

## Boundary

在 `WTLauncher.App.Features` 内增加最小封闭泛型结果：

```text
ServiceResult<T>
├─ Success(T Value)
├─ Canceled(string Status)
└─ Failure(string Status)
```

基类构造受限，三个具体分支为 sealed record。`Success` 拒绝 null payload，非成功分支拒绝空状态，从构造层排除 tuple 允许的非法组合。

## Payloads

- Entry add/clone/edit：`LaunchEntry`。
- Entry delete：小型删除批准载荷，只包含是否需要清除当前键盘焦点所需的 entry ID。
- Preset request-name：规范化后的非空 `string`。
- Preset confirm-delete：无数据成功载荷，使用单一 unit 值，避免退回 bool。
- Preset add/update/delete：各自的小型 payload record，携带更新后的 `LauncherPresetFile`、目标 preset 和删除 fallback ID（需要时）。
- `CreateEntryFromDraft` 同样返回 typed result，验证失败为 Failure；该内部规则不产生 Canceled。

## Data flow

Dialog 的 `null`/`false` 在 service 内翻译为 Canceled；捕获的 recoverable exception 翻译为 Failure 并保持原日志与 formatter；验证拒绝翻译为 Failure；有效数据翻译为 Success。ViewModel 对三个 sealed 分支进行 switch，成功后继续既有 snapshot/mutate/save/rollback 流程。

## Compatibility

- 不改配置或磁盘格式。
- 不改变既有状态字符串及何时展示。
- 不把持久化所有权移入 service；CQ-02 的 transaction boundary 保持唯一。
- 不改变 Core workflow 的 throw/null 契约；只在 App feature service 边界适配。

## Trade-offs

一个共享三分支结果比每个方法各造三种类型更少代码，同时仍是封闭且可穷尽的边界；它只服务本地 feature operations，不扩展为全仓错误分类框架。为不同成功形状保留明确 payload record，避免匿名 tuple 重新出现。

## Rollback

改动限于 feature result 模型、两个 service、其 ViewModel 调用方和测试。若迁移暴露行为差异，恢复接口签名前先由现有 ViewModel/service 测试定位具体分支，不触碰存储和配置。
