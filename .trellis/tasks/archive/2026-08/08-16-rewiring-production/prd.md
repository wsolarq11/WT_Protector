# Rewiring production implementation

## Goal

将 docs/wiring-rewiring-plan.md 和 docs/wiring-rewiring-production-blueprint.md 全量落地为生产代码，使 MainWindowViewModel 从中心 hub 变成 facade，命令体进入显式 Command Workflow。

## Requirements

- 新增 IStatusSink 与 ICommandExecutionContext，workflow 只依赖窄上下文，不依赖 MainWindowViewModel。
- 按真实业务事务提取 Entry、Preset、Layout、Selection、Maintenance、Exchange 六个 workflow。
- LaunchOrchestrator 保留为唯一启动编排者，不迁移到薄壳。
- StoreGrain 仍是唯一状态 owner，所有规范配置写入仍经过 StoreGrain / StoreMutationSession。
- 不引入 DI 容器、服务定位器、全局 EventBus、薄包装或 Func/Action 回调包。
- 保留两条选择写入路径，投影同步仍只走 IProjectionSynchronizer。
- 新增 docs/wiring-map.md 端口台账和三条门禁检查。

## Acceptance Criteria

- [x] MainWindowViewModel*.cs 总行数 <= 1200（实测 1168）。
- [x] 6 个 Command Workflow 均有独立测试文件，覆盖成功、失败回滚、取消/空输入。
- [x] MainWindowViewModel 不再直接触碰 IConfigExchangeService / IUpdateManifestReader。
- [x] docs/wiring-map.md 与源码端口一致性由门禁校验。
- [x] 门禁 checks 从 40 升到 43：wiring-map-consistency、main-vm-line-budget、workflow-no-callback-bag。
- [x] dotnet build -warnaserror 0 警告；dotnet test 832 全绿；tools/verify.ps1 43 checks 全绿。
- [x] 结构基线同步更新（ViewModels 65、partials 15、Tests 129、attributes 749、checks 43）。

## Notes

- 每个阶段独立提交，任一阶段门禁不过则单独回退，不叠加修复。
- 行预算按 MainWindowViewModel*.cs 文件名统计，禁止用新建 partial 逃避。
- 蓝图状态：Production-ready blueprint v1。
