# MVVM Pattern — Views Layer

> View↔ViewModel 契约、对话框注入、design token 流。

## Wiring Contract（2026-08-15，取代旧 Func/Action 注入）

- `IDialogService` 打开窗口；对话框契约禁暴露 `Func<...>` / `Action<...>` 桥。
- `IProjectionSynchronizer.SynchronizeProjections(ProjectionSyncMode)` 是唯一投影重建入口（`VisibleOnly` / `Mutation` / `FullReload`）；调用方禁直接调私有 `ApplyFilter` / `RebuildVisibleGroups`。
- `ILaunchCoordinatorContext : IProjectionSynchronizer`；`SaveConfig(LaunchSaveResultKind)` 返回 `LaunchSaveResult`（区分外部启动副作用后的保存失败）。
- `IConfigExchangeService` 包导入/导出文件 IO；`IUpdateManifestReader` 包 HTTP(S)/本地 manifest 读取。
- `MainWindowViewModel` 接收 `StoreGrain`、`IDialogService`、`IEntryService`、`IPresetService`、`Func<DateTimeOffset>`、可选 `IConfigExchangeService`。

## 组合根（App.axaml.cs）

`InjectDesignTokens()`（LauncherDesignTokens→XAML Resources）→ `OnFrameworkInitializationCompleted()`：`ResolveConfigPath → Load → new LaunchWorkflow → new DialogService → new EntryService(dialogService) → new PresetService(dialogService) → new MainWindowViewModel(interfaces) → new MainWindow(vm) → dialogService.Initialize(mainWindow)`。

## Feature-Service 构造与 VM 所有权

契约：`App.axaml.cs` 是生产组合根，构造具体服务；VM 只收接口，禁 `new EntryService` / `new PresetService`；feature service 是无状态 workflow 所有者，不拥有可观察聚合、不直接持久化；VM 保留可绑定状态、命令、CQ-02 事务应用、状态/回执映射、属性通知顺序；已有 Core 所有者或注入 workflow 直接复用，不加只转发一个方法的服务。矩阵：注入服务 null → 构造抛 `ArgumentNullException`；服务返回校验/取消状态 → VM 只发布状态不变异不持久化；服务返回更新不可变状态 → VM 经 mutation 事务应用；Store 提交失败 → VM 恢复完整快照并保留保存错误；提取需整个 VM/回调袋 → 保持编排，seam 不稳定。测试：服务直测覆盖成功/取消/可恢复失败；R7-R10 防 Entry/Preset 服务引用 Views 或 `System.IO.File`。

## Dialog Injection

VM 永不 `new Window()`；`MainWindow` 构造注入 delegate 包 `ShowDialog<>`。理由：VM 可测（mock delegate 返回固定结果）、View 拥有窗口生命周期、单一组合点。旧 Func/Action 桥已由 `IDialogService` 取代（保留为历史）。

## Design Token Flow

`LauncherDesignTokens(Core const) → App.axaml.cs InjectDesignTokens()`（`Color.Parse` / `CornerRadius` / `Thickness`）→ `Application.Resources` → XAML `{StaticResource PrimaryBrush}`。规则：XAML 禁硬编码颜色/尺寸，一律引用 token 资源键。

## ViewModelBase + 确定性生命周期

- 每个 VM 必须继承 `ViewModelBase`（非裸 `ObservableObject`）。
- 所有事件/IObservable 订阅经 `AddDisposable` 注册，Dispose 自动取消；禁匿名 lambda 永不解绑。
- 替换 ObservableCollection 中的 VM 时先 Dispose 旧条目（`foreach vm.Dispose(); _entries.Clear();`）。
- 单源状态：VM 禁持有 `StoreGrain.Select()` 副本；只读投影派生（`CurrentSettings => _storeGrain.Select(...)`）；禁 `_settings` 字段复制。

## ViewModel↔View 绑定

单向 `{Binding StatusText}` / `{Binding LaunchCommand}`；双向 `{Binding FilterText, Mode=TwoWay}`。Selection：`LauncherEntryViewModel.IsSelected` setter 调 `_owner.UpdateSelection()` 立即持久化。

## 声明式管理窗口 + 模态编辑器路由

稳定布局/行渲染放编译 XAML（`ItemsSource` / `SelectedItem` / 类型化 `DataTemplate`）；集合 mutation 替换后通知空态投影，code-behind 禁 clear/rebuild 控件；code-behind 可开 owner 相对模态编辑器并把返回值路由到 VM 命令；公共无参构造仅为 Avalonia 加载 XAML，生产用类型化构造。矩阵：tab/选择/删除命令/可见性 = VM 绑定；行渲染与选中边框 = XAML/DataTemplate；双击/工具栏开编辑器 = code-behind；校验/mutation/持久化/回滚 = VM/Core。Bad：`RenderRows()` 清 StackPanel 重建子节点。

## ObservableObject

VM 继承 `ObservableObject`（CommunityToolkit.Mvvm）；`[ObservableProperty]` 生成属性 + 通知，`[RelayCommand]` 生成命令。规则：CommunityToolkit.Mvvm 是 ViewModels 唯一允许的第三方依赖。

## 类型化权威状态内容

`LauncherStatusContent(string Text, LauncherStatusTone Tone)` 是 MainWindow 唯一可写状态权威；text/tone/indicator/boolean XAML 投影只读。`ServiceCanceled<T>(LauncherStatusContent Status)` / `ServiceFailure<T>(...)` 原样传输类型化内容。workflow formatter 拥有精确文案 + 显式 tone，生产代码禁从中文关键字推断 tone。CQ-02 save helper 内部可保留字符串错误传输，MainWindow 边界显式包为 Error。禁 timer/queue/history/持久化字段/后台任务。矩阵：空文案 → 构造抛；取消 → Warning；部分成功/跳过 → Warning；保存/加载/对话框/校验/启动失败 → Error；完成 → Success；就绪/加载/预览/进行中 → Neutral。Bad：`Classify(StatusText)` 扫描"失败/已保存"改 tone。

## AutoCompleteBox 搜索

搜索框用 `AutoCompleteBox`（非 TextBox）。属性：`ItemsSource={Binding SearchHistoryItems}`、`MinimumPrefixLength=1`、`MinimumPopulateDelay=0:0:0.150`、`IsTextCompletionEnabled=False`。规则：`ItemsSource` 绑 `IReadOnlyList<string>`（内置 StartsWith 大小写不敏感）；`IsTextCompletionEnabled=False` 防内联自动补全；`MinimumPopulateDelay` 用显式 `h:m:s.ms`（裸 `150` = 150 天）；XAML 不设 `FilterMode`。差异：AutoCompleteBox 无 `SelectAll()`（Ctrl+F 只 `Focus()`）；`IsDropDownOpen` 区分下拉打开/关闭。

## Commit-on-Enter

搜索历史只在 Enter 写入：`OnWindowKeyDown` Enter + source=SearchTextBox + `!IsDropDownOpen` → `RecordSearchTerm()` → `LauncherSearchHistory.Record()`（Normalize + 去重 + 置顶）→ `MaximumTerms`(20) → snapshot/rollback 持久化。不在 `OnFilterTextChanged` 写（每次按键触发；`LastFilterText` 持久化是有意设计，搜索历史是"确认"语义）。

## StoreGrain save-first-then-commit

签名：`Dispatch(Func<LauncherStoreFile,LauncherStoreFile>)`、`DispatchErrorOr(...)`；失败后 VM `RestoreVmStateFromStore()`。契约：reducer 结果先经 `IStorePersistence`（`FileStorePersistence` → `FileLauncherJsonIO`，temp + `File.Replace` + `.bak`）落盘，再在 `Grain<TState>` 锁内更新内存；保存失败内存不变、异常传播（或 `DispatchErrorOr` 转 `ErrorOr`）。失败后缓存本地态（`_settings` / `_list.MutableEntries`）必须 `RestoreVmStateFromStore()`：重读 `_settings`、presets、entries，再 `EnsureCustomOrderContainsEntries()` + `RefreshAllProjections()`。历史：`AppStore.Mutate`（快照前→revert）已被 2026-07-24 VTK 迁移移除。矩阵：reducer 成功 + 保存成功 → 盘先更新内存后更新，订阅者通知；reducer 成功保存失败 → 内存不变、`DispatchErrorOr` 返回 `LauncherErrors.ConfigSaveFailed`、VM restore；reducer 抛 → 立即传播，内存文件不变。

## 类型化 feature-service 结果

`ServiceResult<T>` = `ServiceSuccess<T>` / `ServiceCanceled<T>` / `ServiceFailure<T>`。契约：`ServiceSuccess<T>.Value` 非空；Cancel/Failure 的 `Status` 非空 `LauncherStatusContent` 显式 tone；对话框 null/false → Canceled，可恢复异常/校验拒绝 → Failure，合法数据 → Success；VM 匹配三分支 + 不可达 default；持久化/快照/mutation/保存/回滚留在服务外；操作特定 payload 用不可变 sealed record 非匿名元组。Bad：`(LaunchEntry? Entry, string? Status)` 双可空。

## MainWindow XAML 模块窄 owner 契约

`LauncherControlBar` / `LauncherEntriesView`：模块用 `MainWindowViewModel` 作 `x:DataType` 继承 DataContext，不镜像 VM 状态；公共控件只读访问，限于已证窗口行为；子 code-behind 只转发原始 sender/event args，MainWindow 保留 launch/context-menu/drag eligibility/scroll 决策；禁跨模块字符串 `FindControl`。Bad：`FindControl<ItemsControl>("CardsEntriesItemsControl")`；正确：类型化只读访问器。

## 协作类 + 桥接委托

职责域独立但依赖 MWVM 内部方法时，提取为 `ObservableObject` 协作类，经桥接委托通信。方向：MWVM→协作类 = `internal Func<>` / `Action<>`；协作类→MWVM = `PropertyChanged` 订阅；协作类→协作类 = `event Action?` 自定义事件（MWVM 中转）。构造顺序：1) new 协作类（委托全 null）→ 2) 设初始属性（`OnXxxChanged` 内 `if (Xxx is null) return;` 跳过）→ 3) 赋值桥接委托 → 4) 订阅事件。规则：`[ObservableProperty]` 的 `OnXxxChanged` 必须 null 守卫跳过构造期；禁 `SetXxxDirect(bool)`（触发 MVVMTK0034）。检查清单：协作类确需独立（>3 相关私有字段 + 操作）；桥接只暴露 MWVM 已有方法；遵循构造顺序；CA1001 误报用 `#pragma warning disable` + 解释确定性释放。

Design Decision：保留 partial 文件作为编排层（RelayCommand 必须定义在 partial 类上被 source generator 识别）。区分：Manager 协作类 = 状态 + 业务 + 事件输出；MWVM partial = 编排（命令路由 + CanExecute + 事件处理 + 持久化协调）。partial 只应含编排；>5 行业务判断 → 下移 Manager/Core。

## 状态单一源（2026-07-24）

VM 禁持有 StoreGrain 副本；读走 `StoreGrain.Select`，写走 `Dispatch` / `DispatchErrorOr`。过渡态 `_settings` 仅构造期工作副本。编译期强制 WT1002/1003。

## Drag 工作流 FSM（2026-07-24）

`Stateless` 建模：`Idle → Dragging → Committing → Idle`，`Dragging ↔ Idle`（Preview 重入 / Cancel）。非法转换（Idle 下 Commit）FSM 禁止；守卫调用 `LauncherDragReorderSession` 逻辑。

## Forbidden Patterns

- ❌ VM `new Window()`；对话框注入。
- ❌ XAML 硬编码颜色/尺寸；用 token `{StaticResource}`。
- ❌ code-behind 业务 `if/else`；委托 VM。
- ❌ View 引用 Core 类型作数据契约；只绑 VM 属性。
- ❌ SearchTextBox 上 `SelectAll()`（AutoCompleteBox 无此方法）；只 `Focus()`。
- ❌ VM 持有 `_settings`；用 `CurrentSettings`。
- ❌ 手写状态机守卫；用 `Stateless` `StateMachine<TState,TTrigger>`。
