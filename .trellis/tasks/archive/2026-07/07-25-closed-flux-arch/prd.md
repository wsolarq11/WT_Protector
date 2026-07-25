# 斩根架构迁移 — Closed Flux Architecture 5 步跃迁

## Goal

从当前 5 层混合架构迁移到 Closed Flux Architecture (CFA)：单向不可变数据流 + 函数式核心 + 编译器强制守卫。目标是**永久剪除 Ultrareview 发现的 47 个问题的根因**，使其同类问题不可能再次出现。

## 问题根因映射

| 根因类别 | 引发缺陷数 | 剪除策略 | 对应步 |
|---------|-----------|---------|-------|
| 双设置源 (`_settings` vs `CurrentSettings`) | 7 | AppState 单源 + [AutoSelector] | Step 1 |
| 事件订阅永不取消 | 5 | ViewModelBase + CompositeDisposable | Step 3 |
| 异常被静默吞掉 (空 catch / 静默返回空) | 5 | ErrorOr→`union` + [MustHandleErrors] | Step 2 |
| 静态可变全局 (DefaultIO/DefaultPersistence) | 3 | DI 容器注入 | Step 1 |
| TOCTOU 竞态 (Dispatch 锁外保存) | 2 | Mutation 在 Grain 锁内原子化 | Step 1 |
| View 层混杂业务逻辑 | 2 | WT1007 禁止 View 层 `if`/`switch` | Step 5 |
| 未完成迁移 (ponytail 标记) | 8+ | 一次性完成所有 Phase 迁移 | Step 1-5 整体 |
| Vogen 双包装 | 4 | .NET 11 `record` 取代 | Step 4 |
| 死代码验证器从未被调用 | 1 | 构造器内联验证 | Step 4 |

## Requirements

1. **单向不可变数据流**: 整个应用状态必须集中在一个 `AppState sealed record`。不允许第二个可写副本
2. **纯 Reducer**: 状态迁移通过 `Reducer(AppState, Action) → AppState` 纯函数，在 Grain 锁内原子执行
3. **声明式投影**: ViewModel 属性通过 `[AutoSelector]` 编译时生成，无手动赋值、无事件订阅
4. **确定性生命周期**: 所有 Subscribe/事件订阅必须返回 `IDisposable` 且被 `CompositeDisposable` 追踪
5. **编译时错误处理**: 所有 fallible 操作返回 `union` 类型，编译器穷举检查所有匹配分支
6. **零静态可变**: 所有 I/O 依赖通过构造器注入，`static` 非 readonly 字段被 analyzer 禁止
7. **验证内联**: 所有 record 类型的有效性检查在构造器中完成，禁止游离的 `Validate()` 方法
8. **类型安全**: 所有 ID 字段使用强类型值对象，字符串仅出现在 JSON 边界

## Non-goals

- 不改 UI 布局、不改用户交互流程
- 不改变 `config/launcher.json` 的 schema
- 不删除 `StoreGrain`/`Grain<T>` —— 在其基础上完善而非替换
- 不引入 F# 或其他语言

## Constraints

- 保持 `-warnaserror` 合规
- 保持 AOT-safe 序列化（无 `JsonSerializerOptions` 重载）
- 保持 `catch (Exception)` 必须有 `when` 过滤器（WT1004）
- .NET 11 升级只能在正式发布后（2026-11）进行，在此之前保持 .NET 10 兼容的预备状态

## Acceptance Criteria

- [ ] `_settings` 字段被完全删除，所有设置读取通过 `storeGrain.Select(s => s.Settings)`
- [ ] 所有事件订阅返回 `IDisposable` 且被 `CompositeDisposable` 追踪，analyzer 验证
- [ ] 所有 fallible I/O 方法返回 `union` 类型，编译器穷举检查匹配
- [ ] `LauncherJson.DefaultIO` 和 `LauncherStore.DefaultPersistence` 被删除
- [ ] `StoreGrain.Dispatch` 的 reducer 在 `Grain.Mutate` 锁内执行，无 TOCTOU 窗口
- [ ] `LauncherPreset.Id/Name/EntryIds` 使用强类型值对象
- [ ] WT1007 `ViewNoBusinessLogic`、WT1008 `VmNoManualSetter` 等新增 analyzer 全部通过
- [ ] 现有测试套件全部通过 + 新增 10+ 条 ArchUnitNET 架构规则通过
- [ ] `ponytail` 标记从代码库消失（除合法兼容性场景）
