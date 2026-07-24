# OnPropertyChanged 声明式替换 — 跳过

## Ponytail 决策: 不做

49 处 `OnPropertyChanged` 分布在 9 个文件中。迁移到 `[NotifyPropertyChangedFor]` 是纯抛光，无功能增益。

**理由**:
- 现有代码能正常工作，无相关 bug 报告
- 手动 OnPropertyChanged 显式且可调试
- 源生成器转换在复杂依赖链中有遗漏风险
- 与用户"平稳落地"目标一致: 交付可工作代码，不添加不必要抛光

## 如仍需实施

- 社区工具包 MVVM 8.4.2 已安装
- 目标文件: CommandManagementViewModel.cs, DiscoverAgentsViewModel.cs, LauncherEntryViewModel.cs, LayoutManager.cs, MainWindowViewModel.cs 等 9 个文件
- 策略: 每个属性逐个替换为 `[ObservableProperty]` + `[NotifyPropertyChangedFor]`
