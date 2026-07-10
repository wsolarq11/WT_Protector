# 改造 3：ArchUnitNET 架构不变量测试

## Goal

把 `.trellis/spec/core/quality-guidelines.md` 中的"非协商不变量"和"forbidden patterns"翻译成可执行架构测试，让 `dotnet test` 自动拦截违规。**从 prose 规则 → 编译期/测试期强制规则。**

## Background

当前 `quality-guidelines.md` 写了 24+ 条 forbidden pattern（如"Core 不能引用 Avalonia"、"catch (Exception) 不允许"、"static mutable state 禁止"），靠人肉 code review 兜底。新人/未来会话漏看规则的窗口很大。

`WTLauncher.Core.csproj` 零 PackageReference 已经在编译期锁定 Core 不引用 Avalonia/MVVM Toolkit，但其他规则（命名空间归属、类层级、catch 宽度等）没有强制。

## Requirements

### 功能要求

1. 新增 NuGet 包 `ArchUnitNET`（xUnit v3 兼容版本）到 `WTLauncher.Tests.csproj`
2. 新增 `ArchitectureRules.cs` 测试文件，编码至少以下规则：
   - **R1**: `WTLauncher.Core` 程序集不依赖 `Avalonia*` 任何命名空间
   - **R2**: `WTLauncher.Core` 程序集不依赖 `CommunityToolkit.Mvvm*`
   - **R3**: `WTLauncher.Core` 中所有 public 类型必须 sealed 或 static（值对象/纯函数原则）
   - **R4**: `WTLauncher.App.ViewModels` 命名空间内的类型不直接调用 `File.WriteAllText` / `File.WriteAllTextAsync`（持久化必须走 `LauncherJson`）
   - **R5**: `WTLauncher.Core` 中所有类型不得 `catch (Exception)` 不带 `when` 过滤
   - **R6**: `WTLauncher.App.ViewModels` 命名空间内的类型不得 `catch (Exception)` 不带 `when` 过滤（必须用 `ExceptionFilters.IsRecoverable`）
3. 验证现有代码全部通过（不能误报现有合规代码）

### 非功能要求

- 测试在 `dotnet test -c Release` 跑通
- 测试新增运行时间 < 2 秒（ArchUnitNET 反射开销）
- 不破坏现有 ~453 测试

## Acceptance Criteria

- [ ] `WTLauncher.Tests.csproj` 新增 `ArchUnitNET` 包引用
- [ ] 新增 `ArchitectureRules.cs` 包含至少 R1-R6 六条规则
- [ ] `dotnet test -c Release` 全部通过
- [ ] `tools/verify.ps1` 18 项检查全部 PASS

## Out of Scope

- 编写 24+ forbidden patterns 的完整覆盖（先做 R1-R6 这 6 条最关键）
- 自定义 Roslyn analyzer（ArchUnitNET 已经够用，写 analyzer 是另一个改造）
- Living documentation 生成

## Notes

- 参考：https://github.com/TngMVP/ArchUnitNET
- 架构测试是只读的（基于反射），不会影响生产代码
- ArchUnitNET 与 xUnit v3 兼容性需要验证（v3 较新，可能需要最新 ArchUnitNET 版本）
