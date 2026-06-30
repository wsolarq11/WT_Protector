# 类型驱动架构重构——从根本上消除 UltraReview 发现的根因

## Goal

将 WT_Protector 从"字符串为基础的手动验证"架构重构为"类型驱动的解析式验证"架构，
从根本上消除 UltraReview 2026-06-29 发现的 ~80 个问题的 5 个根因，
而非逐一打补丁。

## Background

### UltraReview 根因分析

| 根因 | 引发的问题 |
|------|-----------|
| **验证逻辑跨层重复** | C1(FormKC注入检测失效)、C3(预设验证缺口)、H5(命令行长度off-by-N) |
| **字符串为基础的数据模型** | C1、H1(JSON大小写丢数据)、H6(pwsh硬编码) |
| **手写崩溃恢复协议** | C2(CommitJournal死循环)、H7(保存失败StatusText不更新) |
| **JSON裸存 + 人工编辑风险** | H1(静默数据丢失)、H8(文档/UI过期文件名) |
| **异常用于控制流** | H2(IsRecoverable过宽)、H3(裸catch致命异常) |

### 架构调研结论

通过深度全网调研，确定核心架构重构方向：
1. **Parse, Don't Validate** — 类型构造即验证，非法状态不可表示
2. **强类型 Value Object** — 消除"字符串原始类型偏执"
3. **LiteDB 嵌入式数据库** — 替代手写 JSON+journal 持久化
4. **Result<T, Error> 类型** — 替代异常控制流

### 现有代码模式（不可违背）

- Core 层零 UI 依赖（见 `.trellis/spec/core/data-types.md`）
- `sealed record` + `init`-only 属性 = 不可变数据载体
- `ImmutableList<T>` / `ImmutableDictionary<K,V>` 用于集合属性
- 枚举+字符串ID双重表示（JSON 持久化兼容）
- `LauncherDesignTokens` 是设计常量的唯一来源
- 状态文本使用中文，集中在静态类中

## Requirements

### 阶段一：类型加固（增量、低风险）

- **R1.1** 创建 `EntryId` 强类型 value object（`readonly record struct`），构造时完成 FormC 规范化
- **R1.2** 创建 `EntryName` 强类型，构造时完成 FormKC 规范化
- **R1.3** 创建 `EntryDirectory` 强类型，构造时完成 FormKC 规范化 + 原始输入 % 注入检测 + `Path.GetFullPath`
- **R1.4** 创建 `PresetId` 强类型，构造时完成 FormC + 小写规范化
- **R1.5** 创建 `PresetName` 强类型，构造时完成 FormKC 规范化 + 256字符限制
- **R1.6** 创建 `ShellCommand` 类型，支持可配置的 shell（消除 H6 硬编码 pwsh）
- **R1.7** 将 `LaunchEntry` 的 string 属性逐步替换为强类型
- **R1.8** 将 `LauncherPreset` 的 string 属性逐步替换为强类型
- **R1.9** 规范化逻辑从 `LauncherEntryIds`、`LauncherEntryText`、`LauncherPresetIds`、`LaunchArgumentBuilder` 收归到各自的 value object 构造器中，删除旧的静态辅助类中的规范化方法
- **R1.10** 保持 `store.json` 格式不变，value object 的 JSON 序列化/反序列化透明

### 阶段二：Result 类型（中等风险）

- **R2.1** 引入 `Result<T, Error>` 或 `OneOf<T, Error>` 类型
- **R2.2** 改造 `LauncherJson.Save/Load` 返回 `Result`，消除 `try-catch` 控制流
- **R2.3** 改造 `LauncherStore.Save/Load` 返回 `Result`
- **R2.4** 改造 ViewModel 层的操作回调返回 `Result`，消除 `ExceptionFilters.IsRecoverable` 分支
- **R2.5** 异常仅用于真正不可恢复的场景（OOM、StackOverflow等）

### 阶段三：持久化替换（高风险，需一次性切换）

- **R3.1** 引入 LiteDB NuGet 包（纯 C#，零原生依赖）
- **R3.2** 实现 `IStore` 接口，LiteDB 作为实现
- **R3.3** 提供 JSON → LiteDB 一次性迁移工具
- **R3.4** 删除手写崩溃恢复代码：`CommitJournal`、`LauncherJson` 的手写 temp-file/backup-rotation
- **R3.5** store 的所有读写操作在 LiteDB 事务内完成——自动 ACID 保证


## Acceptance Criteria

### 阶段一验收

- [ ] 所有强类型 value object 编译通过，无 NRT 警告
- [ ] 现有单元测试全部通过（store.json 格式不变）
- [ ] `EntryDirectory.TryParse` 正确检测全角 % 注入（C1 修复验证）
- [ ] `PresetName` 构造拒绝 >256 字符（C3 修复验证）
- [ ] 旧的规范化静态方法标记为 `[Obsolete]` 或删除
- [ ] `ShellCommand` 支持从配置读取 shell 可执行文件路径（H6 修复验证）

### 阶段二验收

- [ ] 所有 `LauncherJson.Save/Load` 调用方改为处理 `Result`
- [ ] `ExceptionFilters.IsRecoverable` 调用减少 ≥80%
- [ ] 裸 `catch (Exception)` 在 ViewModel 层减少至零（仅保留基础设施层必要的）

### 阶段三验收

- [ ] JSON → LiteDB 迁移工具成功运行，数据零丢失
- [ ] `CommitJournal.cs` 文件删除
- [ ] `LauncherJson.cs` 中手写 temp-file/backup-rotation 代码删除
- [ ] 所有现有测试通过，且新增 LiteDB 相关测试

## Out of Scope

- **TOML 配置格式**（Phase 4 取消——无明确用户需求，YAGNI）
- 不改变 Avalonia UI 层架构（MVVM 模式不变）
- 不改变 `LauncherDesignTokens` 设计
- 不改变 `wt.exe` 启动逻辑（`LaunchArgumentBuilder` 的安全措施保持）
- 不引入第三方容器/DI 框架（保持当前显式构造函数注入模式）
- 不改变 XAML 绑定层

## Technical Notes

- Core 层零 UI 依赖不可变式必须保持
- `readonly record struct` 优先于 `record class`（value object 无堆分配）
- JSON 序列化兼容：value object 需要 `JsonConverter` 或在序列化时展开为原始 string
- LiteDB 的 BSON 序列化天然支持 `record struct`，迁移成本低于 SQLite

## Decisions Made

- **持久化引擎：LiteDB**（纯 C#，零原生依赖，BSON 文档模型匹配现有 JSON 结构）
- **值对象实现：手写 `readonly record struct`**（6 个类型 ≈300 行，不值得引入 source generator 依赖）
- **Result 类型：手写 `Result<T, E>`**（~25 行，零依赖，Core 层保持零 NuGet 包）
- **JSON→LiteDB 迁移：首次启动自动导入 + JSON 保留为 .bak**（透明升级，不双写）
- **旧代码处理：标记 `[Obsolete]` 委托给新类型**，Phase 3 再删除
