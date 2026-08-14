# 测试层级标记与快速反馈 - Design

## 分类

- `CoreUnit`：Core 纯逻辑。
- `Persistence`：文件读写、回滚、损坏恢复、JSON 契约。
- `ViewModelBehavior`：VM 命令、选择、批启动、状态。
- `Analyzer`：Roslyn analyzer。
- `Architecture`：ArchUnitNET/架构规则。
- `UiContract`：XAML/UI 契约。
- `PropertyTest`：CsCheck 属性测试。

## 实现

- 新增 `TestCategories.cs` 常量。
- 用批量脚本按文件模式插入 Trait。
- `tools/test-fast.ps1` 使用 `--filter "Category=CoreUnit|Category=PropertyTest|Category=Architecture|Category=Analyzer"`。
