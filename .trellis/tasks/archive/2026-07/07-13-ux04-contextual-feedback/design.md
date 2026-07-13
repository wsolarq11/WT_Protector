# Design — UX04 Contextual Feedback

## Boundary

`MainWindowViewModel` 继续拥有一个权威状态值。最小新增一个枚举型语义属性（neutral/success/warning/error），与 `StatusText` 同步更新；View 只把语义映射为样式类或伪类。

不建立 message object collection、event bus、toast service 或 timer。CQ-05 后续可将现有 formatter 返回值升级为包含 text/severity 的类型，但 UX-04 不要求先完成全量文案迁移。

## Data Flow

```text
operation/service result
  -> existing status formatter text
  -> owner applies text + severity atomically
  -> MainWindow status region renders existing tokens
  -> next operation replaces both values
```

## Mapping

- success：已保存、已启动、已完成、已新增/修改/删除。
- warning：部分成功、跳过、预检存在失败但仍可继续、用户取消。
- error：验证失败、dialog failure、save/load failure、launch failure。
- neutral：ready/loaded、筛选排序、选择状态、进行中状态。

取消使用 warning 而非 error：操作未发生但系统没有失败。

## Compatibility and Rollback

无 schema、服务接口或持久化变化。回滚只需移除 severity 属性、映射 helper 和状态区样式；`StatusText` 行为保持兼容。
