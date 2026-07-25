# Phase 1: Zone 0 纯函数化 — LauncherJson/LauncherStore 拆分 + CsCheck PBT

## Goal

将 Core 层的 `LauncherJson.cs` 和 `LauncherStore.cs` 从"IO + 逻辑混合"拆分为"纯函数 + IO 接口"，
消除根因 #1（命令式突变）在持久化层的所有表现。同时为所有纯函数添加 CsCheck 属性基测试，
建立 Zone 0 的验证基础。

## Background

Trinity 架构要求 Zone 0（Pure Domain Core）的所有函数为 `static (Input) → Output`：
无状态、无依赖、无副作用。当前：

- `LauncherJson.cs`（275 行）混合序列化逻辑、文件 IO、备份轮转、临时文件清理
- `LauncherStore.cs`（261 行）混合 schema 验证、路径解析、旧格式迁移、文件 IO

这违反了 Zone 0 的纯函数原则。两个文件中的 IO 操作必须提取到接口，
纯逻辑部分保留在 Zone 0。

## Requirements

### REQ-1: LauncherJson 拆分为纯序列化器 + IO 接口
- `LauncherJsonSerializer`（Zone 0）：纯序列化/反序列化函数，无 IO，无副作用
- `ILauncherJsonIO`（Zone 1 接口）：Load/Save/Cleanup/RotateBackups 抽象
- `FileLauncherJsonIO`（Zone 1 实现）：原 LauncherJson 的 IO 代码
- 现有 API 兼容：`LauncherJson.Load<T>()` / `Save<T>()` 保持签名不变，内部委托给新组件

### REQ-2: LauncherStore 拆分为纯验证器 + 存储接口
- `LauncherStoreValidator`（Zone 0）：纯 schema 验证，无 IO
- `IStorePersistence`（Zone 1 接口）：Load/Save/ResolveStorePath 抽象
- `FileStorePersistence`（Zone 1 实现）：原 LauncherStore 的 IO 代码
- `LegacyMigration`（Zone 1）：旧格式迁移逻辑（本质上是一次性 IO 操作）
- 现有 API 兼容：`LauncherStore.Load()` / `Save()` 保持签名不变

### REQ-3: 新增 CsCheck 属性基测试覆盖所有纯函数
- `LauncherJsonSerializer` 所有函数
- `LauncherStoreValidator.Validate`
- 每条测试至少 1000 次随机输入

### REQ-4: 现有测试不受影响
- 所有现有 700 测试继续通过
- 不修改现有测试的断言逻辑
- 仅在需要时调整 using/namespace

## Non-goals

- 不改变 launcher.json 的 schema 或行为
- 不修改 LauncherStoreFile / LaunchEntry 等数据记录
- 不改动现有 VM / View 层代码
- 不引入 DynamicData（属于 Phase 2）
- 不修改 Stateless 状态机（属于 Phase 2）

## Acceptance Criteria

- [ ] `LauncherJsonSerializer` 在 Core 层，纯静态类，无文件 IO 引用
- [ ] `ILauncherJsonIO` + `FileLauncherJsonIO` 在 App 层（Infrastructure）
- [ ] `LauncherStoreValidator` 在 Core 层，纯静态类，无文件 IO 引用
- [ ] `IStorePersistence` + `FileStorePersistence` 在 App 层（Infrastructure）
- [ ] `LegacyMigration` 独立文件
- [ ] 原始 `LauncherJson` 和 `LauncherStore` 类保持为 facade 委托给新组件
- [ ] `dotnet build -warnaserror` 通过
- [ ] `dotnet test -c Release` 全绿（700+ tests）
- [ ] CsCheck 属性基测试 >= 6 条，每条 >= 1000 次随机输入
- [ ] `tools/verify.ps1` 全绿（28 gates）
