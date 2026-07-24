# P2: Roslyn 分析器 — 设计文档

## 分析器列表

| ID | 规则 | 严重性 | 实现成本 |
|----|------|--------|----------|
| WT1001 | Service_Must_Not_Reference_ViewModel_Namespace | Error | ~150 行 |
| WT1002 | MWVM_Field_Must_Not_Duplicate_Store_State | Error | ~100 行 |
| WT1003 | Dispatch_Must_Be_Only_Write_Path | Error | ~120 行 |
| WT1004 | Catch_Exception_Must_Have_When | Error | ~80 行 |
| WT1005 | UI_Thread_Only_Attribute | Warning | ~100 行 |
| WT1006 | Delegate_Must_Be_Initialized | Error | ~150 行 |

## 实现方案

使用 `Microsoft.CodeAnalysis.CSharp` 编写自定义 DiagnosticAnalyzer，打包为 NuGet，从 `Directory.Build.props` 引用。
