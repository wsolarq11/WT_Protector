# Quality Guidelines — Views Layer

> 代码标准、禁止模式、review 清单（`WTLauncher.App/Views/`）。

## Code-Behind 边界

允许：对话框注入、键盘事件路由（`OnWindowKeyDown` / Ctrl+F / Escape）、drag 事件接线、窗口生命周期（SizeChanged / Closing / Opened）、右键 `ContextMenu.Open()`。
禁止：业务决策（如"该条目是否确认启动"）、直接变异 VM 集合、调持久化方法、含数据有效性条件逻辑。

## XAML 标准

- 资源引用：`{StaticResource PrimaryBrush}` / `{StaticResource CardBorderRadius}`；禁 `#2563EB` / `CornerRadius="13"` 字面量。`WTLauncher.App` 生产 `.cs` / `.axaml` 禁 `#RRGGBB` / `#AARRGGBB`；先复用 token，加 Core token 仅限独特稳定语义角色；框架值（空阴影）在 `App.InjectDesignTokens()` 建一次。根门禁 `app-raw-hex-zero` 拒绝临时 raw-hex fixture，禁引入白名单绕过所有权规则。
- 类驱动视觉态：`Classes.drag-insert="{Binding IsDragInsertBefore}"`；类名定义于 `MainWindowEntryClasses` 等静态容器；不用 inline `Setter`。
- 三视图：Cards/Details/Names 同时只显示一个（`MainWindowEntriesRootSelection.Select`）。
- 主窗动作层级：高频直接可见（launch selected / add entry / discover / preset apply-preview-launch）；维护动作入标记 overflow 菜单（select/clear/refresh、command/address 管理、preset save/update/delete）。overflow 触发器 = 标准文本 `Button` + `ContextMenu` + automation name + 单一 view-only click handler；菜单项绑现有 VM 命令，禁代理命令/复制业务逻辑。

## 性能规则

- 应用内 reorder 用 Pointer capture，禁 OLE `DragDrop.DoDragDropAsync`（嵌套 Win32 消息循环优先输入消息，降级渲染回调 → 卡顿）。Pointer 事件走 Avalonia 正常渲染循环；配 `ForceCleanup()`（`PointerReleased` + `PointerCaptureLost` 双路径释放，全部 handler try-catch 兜底）。
- drag 热路径零 layout、零视觉树遍历：移动元素用 `RenderTransform`（`TranslateTransform`），禁 `Canvas.SetLeft/SetTop`（触发 InvalidateArrange）与影响测量的 `IsVisible` 绑定；用 render 属性（`Opacity`）。
- 性能诊断先消除后优化：先隐藏目标组件（`IsVisible=false`）测 delta；隐藏 450+ 元素零差异 → 瓶颈不在渲染，查消息循环 / I/O / GC；不优化未证实的瓶颈（pitfalls §2.6）。
- 卡片布局：Core 整数数学 O(n)；尺寸委托 `CardLayoutProvider` 单一源，无每 VM 可变态、无 memoization（计算廉价）。不变量：`CardWidth` / `CardHeight` / `NameTileWidth` / `NameTileHeight` 是 `LauncherEntryViewModel` 只读属性委托 `CardLayoutProvider`；provider 由 `LayoutManager` 拥有经 `IEntryCardHost.CardLayout` 暴露；`Recalculate(availableSize, count)` 是唯一 mutation（`ApplyFilter()` / `UpdateCardsAvailableSize()` 调用）；禁把 ephemeral View 尺寸存为带默认的 `[ObservableProperty]`（staleness 风险）。

## 测试 Views

测提取的纯函数，非 Avalonia 控件：`LauncherDragGhostLayoutCalculatorTests`、`MainWindowDragInsertDecisionTests`、`MainWindowEntryCardResolverTests`、`MainWindowEntryCardContextMenuTests`、`MainWindowEntryRowDoubleTapTests`、`MainWindowViewModelTests.ModesTests`。范式：决策逻辑抽静态方法测试；XAML 绑定/视觉由 `verify.ps1` GUI smoke 验证。

## 视图状态保持

三视图切换重建视觉树，滚动位置若不显式保存即丢失。范式：VM 持有滚动态 `Dictionary<LauncherEntryViewMode,double> _scrollOffsets`（`SaveScrollOffset` / `GetScrollOffset`），切换时存取；禁 code-behind 字段缓存（模板切换即垃圾）。

## Preflight 失败确认（E8, 2026-06-23）

`StartBatch` 不再因 preflight 部分失败无条件跳过有效条目；失败时始终弹确认让用户决定（仅 clean preflight + 关闭确认时跳过）。

## Code Review Checklist

- [ ] code-behind 无业务逻辑。
- [ ] XAML 无硬编码颜色/尺寸，全 `{StaticResource}`。
- [ ] 新对话框走 `typed param → ShowDialog<T>`。
- [ ] 新 drag 视觉用 `drag-target` class。
- [ ] DragOver 不触发全列表刷新。
- [ ] drag 操作有键盘替代。
- [ ] 对话框布局复用 `LauncherDialogButtons` / `LauncherDialogText`。
- [ ] XAML TimeSpan 显式 `h:m:s.ms`，禁裸整数（`"150"` = 150 天非 150ms，用 `"0:0:0.150"`）。
- [ ] `MeasureOverride` / `ArrangeOverride` 用 `double.IsFinite` / `IsNaN` 守卫可用尺寸。
- [ ] 破坏性确认对话框 Cancel `isDefault:true`；仅安全/肯定对话框 Primary 为默认。
- [ ] 异常解包 while 循环带 `for i<16` 上限。
