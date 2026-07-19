# Feature-Modular Shell — 目标架构设计

## 核心思路

按功能域封装 Feature Module，Shell 只协调不拥有，消除神类 ViewModel 和状态投影漂移。

## 目标 Layer 图

```
┌─────────────────────────────────────────────────────┐
│                    Shell Layer                       │
│  MainWindow + MainWindowViewModel (thin)             │
│  只负责：布局协调、窗口生命周期、DI 组装              │
└────────────────────┬────────────────────────────────┘
                     │ 持有引用
┌────────────────────▼────────────────────────────────┐
│              Feature Modules Layer                    │
│  ┌─────────────┐  ┌──────────┐  ┌────────────────┐  │
│  │ Entries      │  │ Presets  │  │ Management      │  │
│  │ Feature      │  │ Feature  │  │ Features        │  │
│  │              │  │          │  │ (Command/Dir)   │  │
│  │ EntryListVM  │  │PresetList│  │                 │  │
│  │ EntryService │  │VM        │  │ MgmtVM          │  │
│  └──────┬───────┘  └────┬─────┘  └────────┬────────┘  │
│         │               │                  │           │
│  ┌──────▼───────────────▼──────────────────▼────────┐  │
│  │           Cross-cutting Services                  │  │
│  │  IDialogService  │  ILaunchService  │  IStoreSvc  │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## 组件详解

### Shell Layer

```csharp
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IEntryListViewModel _entries;
    private readonly IPresetListViewModel _presets;
    private readonly ILaunchCoordinator _launch;
    
    private string _filterText = "";
    private LauncherEntryViewMode _viewMode;
    
    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries 
        => _entries.VisibleEntries;
}
```

### Feature Module Pattern

每个 Feature 自包含，不引用 Shell 或其他 Feature：

```
IEntryListViewModel    ← 对外接口
EntryListViewModel     ← 实现，在 Features/Entries/ 内
EntryService           ← 业务逻辑，不再接收集合参数
```

跨 Feature 通信通过 `WeakReferenceMessenger`。

### 根因消除

| 根因 | 方案 |
|------|------|
| 神类 VM | Feature Module 拆分 |
| 异常处理不一致 | ExceptionBoundary.RunAsync |
| 状态投影漂移 | NotifyPropertyChangedFor + RefreshProjections |
| 生命周期 | 消息总线 + Dispose |
| AOT 序列化 | 注册验证测试 |
