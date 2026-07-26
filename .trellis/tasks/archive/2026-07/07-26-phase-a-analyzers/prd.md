# Phase A: Roslyn 分析器重新接线

## 根因

WTLauncher.Analyzers 项目包含 5 个 Roslyn 分析器，已加入 Solution 编译但未被任何消费项目引用——编译器从未加载分析器 DLL。

## 验收标准

1. `WTLauncher.Core.csproj` 和 `WTLauncher.App.csproj` 通过 `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` 引用 Analyzers
2. WT1004（CatchExceptionMustHaveWhenAnalyzer） 在构建时对裸 `catch(Exception)` 报 Error
3. WT1001（LayerViolationAnalyzer） 在构建时对 Core→App 引用报 Error，且补充全限定名检测
4. WT1003（DispatchMustBeOnlyWritePathAnalyzer） 更新为检查 `IStorePersistence.Save` 而非 `LauncherStore.Save`
5. WT1005（ResourceKeyAnalyzer）`CollectXamlKeys` 补充完整实现或标记 `[Obsolete]` 明确未完成
6. WT1006（JsonRegistrationAnalyzer）补充完整实现或标记 `[Obsolete]` 明确未完成
7. `verify.ps1` 添加分析器接线门禁——构建日志中验证分析器实际加载
8. `dotnet build -warnaserror` 通过，无退化

## 实施顺序

1. 修复 `.csproj` 引用 → 2. 修复 WT1003 过时检查 → 3. 修复 WT1001 全限定名检测 → 4. 修复 WT1005/WT1006 → 5. verify.ps1 门禁 → 6. 验证
