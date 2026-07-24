# 三剪架构落地 — 剪除一切问题源头

## 目标

基于 20 代理 Ultrareview + 5 路 Deep Research 的发现，实施"三剪"架构方案，从根源上消除所有已发现的系统性缺陷，无限逼近 99.99% 置信度。

## 验收标准

### Phase 1 — 状态单源化
- [ ] MWVM 不再持有 `_settings` 字段，所有读取通过 `_appStore.Select(s => s.Settings)`
- [ ] DragWorkflow 不再持有 `_settings` 字段
- [ ] `TrySaveWindowBounds` 路由到 `AppStore.Dispatch`，不直接调 `LauncherStore.Save`
- [ ] 删除 `_appStore.Subscribe` 中无条件覆盖 `_settings` 的逻辑
- [ ] 全部 410+ 测试通过，`tools/verify.ps1` 门禁通过

### Phase 2 — Roslyn 分析器
- [ ] WT1001/WT1002/WT1003/WT1004/WT1005/WT1006 — 6 条规则编译时强制
- [ ] IDE 红波浪线 = 架构违反

### Phase 3 — 持久化加固
- [ ] RotateBackups 顺序修正 + SaveLock 保护
- [ ] Load<T> 读锁 + 恢复链搜索 `.bak.1`/`.bak.2`

### Phase 4 — FSM + Property 测试
- [ ] 拖拽工作流用 Stateless FSM 建模
- [ ] CsCheck property-based 测试 + Debug.Assert 不变量

### Phase 5 — F# Core 试验（可选）
- [ ] 侧边项目验证 F# 与 Native AOT 兼容性
