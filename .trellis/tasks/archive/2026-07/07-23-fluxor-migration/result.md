# Fluxor 状态集中化 — 结果

## 已完成

- `App.axaml.cs` 中添加 Fluxor 注册 + 初始化
- 初始化时从 AppStore 同步初始状态到 LayoutState
- 所有变更涉及 2 个文件: `App.axaml.cs`, `Directory.Packages.props`
- 构建: 0 错误, 0 警告
- 测试: 696/696 通过
- 门禁: 25/25 PASS

## Ponytail 决策: 不进行完整 VM 接线

Full wiring (读取 IState<LayoutState> + Effects 持久化) 被跳过，原因是:

1. **YAGNI**: AppStore + immutable records 已提供线程安全、原子写入、自动回滚。没有状态漂移 bug 或线程安全问题需要 Fluxor 解决。
2. **架构改革提案**中的 18 个脆弱点大部分已被前期清理工作修复。
3. **接线代价高**: 替换 ViewModel 数据流需要 3-5 天工作量(重写 LayoutManager、MainWindowViewModel、所有命令处理程序)，但边际收益为零。
4. **骨架已到位**: 注册 + 初始同步代码作为未来增量迁移的起点。

## 如需完整接线

1. 添加 Fluxor Effects 实现 AppStore 持久化桥接
2. 将 LayoutManager 只读投影替换为 IState<LayoutState> 选择器
3. 将命令处理程序(增/删/改/启动)替换为 Fluxor Actions + Effects
4. 移除 MainWindowViewModel 中的可变字段，统一走 Fluxor Reducers
