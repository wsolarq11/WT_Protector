# Design

## Test ownership

- `EntryServiceTests.cs`：service 的 dialog 翻译、draft validation、clone ID、delete approval 契约。
- `PresetServiceTests.cs`：service 的 name/confirmation 翻译、eligibility/query delegation、add/update/delete typed payload 契约。
- `LauncherCardGroupViewModelTests.cs`：组件公开属性、通知与 callback payload 契约。
- 既有 `MainWindowViewModelTests.*`：继续拥有 mutation/persistence/rollback 的跨层组合行为。

## Test shape

使用真实 service、真实 Core workflow 和轻量 `StubDialogService`。对于需要 `LauncherEntryViewModel` 的测试，复用或增加一个最小 factory，构造真实 owner；不 mock ViewModel，不使用反射触发命令，直接执行 CommunityToolkit 生成的 public command。

## Contract matrix

- Dialog returns value → Success payload normalized/snapshotted.
- Dialog returns null/false → Canceled with exact existing status.
- Dialog throws recoverable exception → Failure with exact formatter status.
- Domain validation rejects input or mutation target missing/locked → Failure and original aggregate remains unchanged.
- Component commands → callbacks receive immutable point-in-time lists in visible order.

## Compatibility and rollback

默认只改测试。若发现生产 bug，最小修复放在对应 service/component，并新增回归断言。任何依赖 XAML renderer、窗口消息循环或文件写入的断言都退回到现有 GUI smoke/集成测试所有者。
