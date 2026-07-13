# Design

## Ownership model

父任务只拥有源需求、依赖顺序、复用映射和最终集成验收，不直接承载产品代码。17 个子任务各自拥有一个可独立验证的结果；已有 Trellis 任务继续拥有其既有实现，新子任务通过引用和验收关闭审计缺口。

## Dependency shape

```text
CQ-02 mutation safety
  └─ CQ-01 thin VM ─ CQ-06 typed service results ─ CQ-07 contracts
       ├─ UX-01 frecency
       ├─ UX-02 central management ─ CQ-03 command XAML
       ├─ UX-03 quick create
       └─ UX-04 feedback ─ CQ-05 typed content
                              └─ V-03 dialog consistency

CQ-03 + CQ-01 ─ CQ-04 main XAML modularity ─ V-01 tokens ─ V-02 declutter
                                                        └─ V-04 accessibility
                                                        └─ V-06 responsive proof
                                                        └─ UX-05 empty state
```

顺序表达风险最小化，不要求人为制造接口依赖。某个后置任务若已由既有任务完成，只做证据审计和最小补缺。

## Cross-cutting contracts

- Persistence：所有磁盘写入最终汇聚到 `LauncherStore`; mutation 成功才发布新状态，保存失败恢复完整可观察状态。
- State ownership：service 拥有业务操作，ViewModel 组合可绑定状态和命令，不创建第二套缓存真相。
- UI ownership：稳定布局使用 XAML/DataTemplate；code-behind 只保留窗口生命周期、平台交互和无法合理声明的路由。
- Content：状态栏仍是权威结果面；就地反馈只强调上下文，不建立通知中心。
- Visual system：只提升重复值为 token；单次值保留局部，避免建立通用组件库。
- Verification：纯规则用单元/契约测试，绑定和结构用结构测试，像素与 DPI 风险用 GUI smoke/矩阵记录。

## Compatibility and migration

- 配置 schema 只能保持向后兼容；可选字段必须有旧配置默认行为。
- 不迁移用户数据到新位置，不改变 portable 路径解析和安全确认语义。
- UI 拆分期间保持现有命令和 AutomationProperties 契约，避免一次性重写。

## Rollback

每个子任务单独提交和归档。若集成门禁发现回退，回到拥有该契约的最小子任务修复；不通过回退整个现代化计划掩盖根因。
