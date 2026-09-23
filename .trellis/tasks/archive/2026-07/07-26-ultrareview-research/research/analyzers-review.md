# Research: Roslyn Analyzers Review — WTLauncher.Analyzers

- **Query**: Ultrareview F# & Analyzers — Roslyn analyzer pattern correctness, false positives/negatives, perf, wiring
- **Scope**: Internal
- **Date**: 2026-07-26

## 问题

审查 `WTLauncher.Analyzers`（WT1001–WT1006）的 Roslyn 模式正确性、误报/漏报、性能与构建接线。

## 结论

- 接线正确：`Directory.Build.props:16-20` 用 `ProjectReference OutputItemType="Analyzer" ReferenceOutputAssembly="false"` 接入所有项目并排除自身；Avalonia 12.1 自动把 `.axaml` 作为 `AdditionalFiles` 提供。
- 本地默认不执行：`RunAnalyzersDuringBuild` 仅在 `$(CI)=='true'` 或 `$(ForceAnalysis)=='true'` 时为真（`Directory.Build.props:12`），本地 Debug 零分析反馈（符合 pitfalls.md:1046 的设计意图）。
- 零自动化验证：WT1001–WT1006 没有任何 Roslyn 分析器单测。
- 文档/代码漂移：WT1002 被 `docs/completion-overview.md:53` 引用但从未实现；WT1003 allowlist 含死条目 `LazyInit`；`DispatchAsync` 被 CLAUDE.md/persistence.md 引用但代码中不存在。
- 其余发现均为低风险模式问题，当前代码库下不触发误报。

## 关键证据

可复现命令（验证零单测 + 死条目）：

```bash
rg "WT1001|WT1003|WT1004|WT1005|WT1006|CatchException|DispatchMust|LayerViolation|ResourceKey|JsonRegist" --glob "*Test*.cs"   # 0 命中
rg "LazyInit" src/avalonia-launcher/src/   # 仅命中分析器自身
```

| Analyzer | 结论 | 证据 |
|---|---|---|
| WT1001 LayerViolation | 正确；用 `.Contains()` 匹配命名空间（低风险子串误报）；Core 项目改名会静默失效；同时注册 using 与完全限定名两类 action（纵深防御正确） | `LayerViolationAnalyzer.cs:29,45,60` |
| WT1003 Dispatch-only-write | 模式正确（`RegisterCompilationStartAction` 包裹 `RegisterSyntaxNodeAction`）；allowlist 含死条目 `LazyInit`（方法不存在）；只按类短名 `StoreGrain` 匹配；只校验方法名 `Save`（新增 `Write`/`Persist` 会绕过） | `DispatchMustBeOnlyWritePathAnalyzer.cs:29,43,61-62` |
| WT1004 catch-when | 正确捕获 `catch {}` 与 `catch (Exception)`；用 `SymbolEqualityComparer.Default` 比对类型；缺类型时 null 早返回；`category="Correctness"` 无问题 | `CatchExceptionMustHaveWhenAnalyzer.cs:46-56` |
| WT1005 ResourceKey | 只匹配裸 `Resources["Key"]`（带 `this.`/`App.Current.` 不匹配；当前 `InjectDesignTokens` 是裸写法所以全命中）；regex `\w+` 不匹配含点/连字符键（当前 PascalCase 无影响）；XAML 注释会被误报（按原始文本而非解析节点扫描）；`GetText(default)` 用默认 CancellationToken | `ResourceKeyAnalyzer.cs:24,28,76,101,127` |
| WT1006 JsonRegistration | `[Obsolete]` 脚手架；`Initialize` 不注册任何 action，永不产出诊断 | `JsonRegistrationAnalyzer.cs:21-40` |

## 与 CLAUDE.md 不变量一致性

| 不变量 | 覆盖 | 状态 |
|---|---|---|
| `catch (Exception)` 禁止 | WT1004 | 一致 |
| 选中项两条写路径都可见（checkbox vs batch） | 无分析器 | 未覆盖 |
| 无 per-entry `confirm` 键 | 无分析器 | 未覆盖 |
| Fluxor 移除，`StoreGrain.Dispatch` 为写路径 | WT1003 | 一致 |
| Design tokens 唯一颜色/间距来源 | WT1005 | 一致 |

## Caveats

- 无分析器单测；本地构建默认跳过分析器；WT1002 仅文档存在、无实现与占位；WT1003 allowlist 含死条目 `LazyInit`。

## 关联路径

- `src/avalonia-launcher/src/WTLauncher.Analyzers/`
- `src/avalonia-launcher/Directory.Build.props`
- `src/avalonia-launcher/Directory.Packages.props`
- `.trellis/spec/core/quality-guidelines.md:76-79`（WT1005/WT1006 规则）
- `.trellis/spec/guides/pitfalls.md:1039-1047`（分析器不加载已知坑）
- `docs/completion-overview.md:53-57`（WT1002 文档漂移）
