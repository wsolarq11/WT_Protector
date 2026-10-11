# Views Layer Development Guidelines

> `WTLauncher.App.Views/` — XAML 视图、code-behind、drag-drop、对话框、视觉行为。依赖 ViewModels 取状态、Core 取 token；永不含业务逻辑。code-behind 只做窗口化：对话框注入、键盘导航、drag 事件接线、窗口尺寸。

## Guidelines Index

| Guide | 内容 |
|-------|------|
| [Directory Structure](./directory-structure.md) | Views 文件组织 |
| [MVVM Pattern](./mvvm-pattern.md) | ViewModel↔View 契约、对话框注入 |
| [Drag & Drop](./drag-drop.md) | ghost 工厂、插入指示器 |
| [Dialogs](./dialogs.md) | 确认 / 编辑器对话框 |
| [Quality Guidelines](./quality-guidelines.md) | 标准、禁止模式 |

## Pre-Development Checklist

- [ ] 改 Views 前读本层 5 份指南 + [Pitfalls](../guides/pitfalls.md) §2.5 / §2.6。
- [ ] code-behind 是否只做窗口化？（含业务 `if` 即放错层）
- [ ] 新颜色/尺寸查 `LauncherDesignTokens` 是否已有；XAML 只用 `{StaticResource}`。
- [ ] 新对话框走 `typed param → ShowDialog<T>` 注入模式。

## Quality Check

- [ ] `dotnet test -c Release` 全绿；提交前 `npm run verify`（含 GUI smoke + `app-raw-hex-zero`）。
- [ ] 对照 `quality-guidelines.md`「Code Review Checklist」逐条自检。

## Core Principle

> Views 只拥有像素，不做决策。键盘快捷键、drag hit-test 是合法 code-behind；"是否确认启动"不是。
