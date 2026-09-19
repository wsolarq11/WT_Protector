# Research: F# Code Review — WTLauncher.Core.FSharp

- **Query**: Ultrareview F# & Analyzers — F# idiomatic code patterns, C# interop, edge cases
- **Scope**: Internal
- **Date**: 2026-07-26

## 问题

审查 `WTLauncher.Core.FSharp` 的 F# 惯用性、C# 互操作与边界情况。

## 结论

孤儿项目：`WTLauncher.Core.FSharp` 在 `.sln:16` 中但无任何 C# 项目引用，全仓 `.csproj`/`.cs` 零命中，整个代码库不可达且零测试。`quoteArgument` 与 C# `QuoteArgument` 逻辑等价（反斜杠计数、引号转义、尾部反斜杠翻倍一致），唯一差异是缺 null 守卫；5 个 record 与 C# 类型结构重复；AOT 下 FSharp.Core 增重（当前无消费者故非问题）。

## 关键证据

- `Library.fs:56-75` `let quoteArgument (value: string)` 无 null 守卫；F# 字符串默认非空，但 C# 互操作边界可传 null，`for ch in value` 会抛 `NullReferenceException`。对照 C# `LaunchArgumentBuilder.cs:124` 首行 `ArgumentNullException.ThrowIfNull(value)`。
- `Library.fs:9-52` 5 个 record（`LaunchEntry`/`LauncherWindowSettings`/`LauncherSettings`/`LauncherPreset`/`LauncherStoreFile`）与 `WTLauncher.Core` 同名 C# 类型结构一致，是第二份未用副本。
- `WTLauncher.Core.FSharp.fsproj:6` `<IsAotCompatible>true</IsAotCompatible>`；`string list`（`Microsoft.FSharp.Collections.FSharpList<T>`）依赖 FSharp.Core 并按元素链表分配，Native AOT 下增重。
- `.fsproj` 未定义 `<FSharpPreferNetFrameworkReferences>false</FSharpPreferNetFrameworkReferences>` 或 `<MicrosoftFSharpPackageVersion>` 固定，构建依赖宿主 SDK 选择的 FSharp.SDK 版本。
- 可复现命令：

```bash
rg "Core.FSharp" src/avalonia-launcher --glob "*.csproj"   # 0 命中
rg "FSharp" src/avalonia-launcher --glob "*.cs"            # 0 命中
```

## Caveats

无 F# 测试；无说明 F# 项目意图的架构文档；仓库无 F# 专用 `.editorconfig` 或编码规范。

## 关联路径

- `src/avalonia-launcher/src/WTLauncher.Core.FSharp/Library.fs`
- `src/avalonia-launcher/src/WTLauncher.Core.FSharp/WTLauncher.Core.FSharp.fsproj`
- `src/avalonia-launcher/WTLauncher.Avalonia.sln:16`
- `src/avalonia-launcher/src/WTLauncher.Core/LaunchArgumentBuilder.cs:122-158`（C# 规范实现，生产使用于 `:72`，测试于 `LaunchArgumentBuilderTests.cs:57,63`）
