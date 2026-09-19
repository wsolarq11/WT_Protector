# Core Layer Development Guidelines

> `WTLauncher.Core` — 纯数据/契约/规则，零 UI 依赖。禁止引用 Avalonia、CommunityToolkit.Mvvm、`WTLauncher.App`。

## Guidelines Index

| Guide | 内容 |
|-------|------|
| [Directory Structure](./directory-structure.md) | 5 层架构与项目布局 |
| [Data Types](./data-types.md) | record / enum / design token / 值对象 |
| [Persistence](./persistence.md) | 单文件 JSON、原子写、备份轮转 |
| [Error Handling](./error-handling.md) | 校验、异常类型、状态文本 |
| [Quality Guidelines](./quality-guidelines.md) | 不变量、禁止模式、Review 清单 |

## Pre-Development Checklist

- [ ] 改 Core 前先读本层 5 份指南与 [Pitfalls](../guides/pitfalls.md) 第 1、2 章。
- [ ] 新类型是否零 UI 依赖？能否脱离 Avalonia 测试？能则留在 Core，否则上移 ViewModels。
- [ ] 新 JSON 字段：加 `[JsonPropertyName]` + `[JsonRequired]`，字符串默认 `""`。
- [ ] 新常量先 `grep` 确认是否已存在于 `LauncherDesignTokens`。

## Quality Check

- [ ] `dotnet test -c Release` 全绿；提交前 `npm run verify`。
- [ ] 不违反 `ArchitectureRules` R1-R10；Core 不引用 Avalonia / CommunityToolkit.Mvvm / App。
- [ ] 对照 `quality-guidelines.md`「Forbidden Patterns」逐条自检。

## Core Principle

> 数据结构优先于代码。能在 Core 测试的规则不下沉到 ViewModel。
