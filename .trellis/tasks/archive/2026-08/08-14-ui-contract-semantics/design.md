# 语义化 UI 契约测试 - Design

## 方案

- 用 `XDocument` 解析 `.axaml`，按元素名、`x:Class`、`AutomationProperties.Name`、`Command` 绑定做结构化断言。
- 语义断言抽取到 `UiContract/XamlContractAssertions.cs`。
- 对无法语义化的规则迁移到 `tools/verify.ps1` 或明确删除。

## 优点

- 不依赖 XAML 空白/引号格式。
- 保留“菜单不能退化”约束。
- 不需要引入 Avalonia Headless 依赖。
