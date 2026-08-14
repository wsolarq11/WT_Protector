# 语义化 UI 契约测试 - PRD

## Goal

移除脆弱的 XAML 字符串断言，改用结构化/语义化契约检查。

## Requirements

- 不再用 `Assert.Contains` 匹配整段 XAML 字符串。
- 保留现有 UI 结构约束的能力。
- 不依赖真实窗口/E2E 完成契约检查。

## Acceptance

- UiContract 测试改为 XML/控件语义断言或稳定常量断言。
- 重构 XAML 格式不会造成误失败。
- 门禁仍能发现菜单、按钮、自动化名称退化。
