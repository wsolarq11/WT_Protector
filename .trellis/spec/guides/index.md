# Thinking Guides — WT_Protector

> **Purpose**: Expand your thinking to catch things you might not have considered in this C# Avalonia MVVM desktop application.

---

## Why Thinking Guides?

**Most bugs come from "didn't think of that"**, not from lack of skill:
- Didn't think about config/presets/settings atomic save → one file corrupt, others fine
- Didn't think about drag state leaking across sort/过滤 changes → ghost card for wrong entry
- Didn't think about `LauncherDesignTokens` as single source → hardcoded color in XAML diverges

---

## Available Guides

| Guide | Purpose | When to Use |
|-------|---------|-------------|
| [Code Reuse Thinking Guide](./code-reuse-thinking-guide.md) | Identify patterns, reduce duplication | When you notice repeated patterns or are about to add a new utility |
| [Cross-Layer Thinking Guide](./cross-layer-thinking-guide.md) | Think through data flow across 5 layers | Features spanning Core + ViewModels + Views |
| [Pitfalls — 跨会话踩坑实录](./pitfalls.md) | 50+ 真实 session 取证的坑与预防规则 | 每次开始编码前速查；遇到诡异失败先看本文是否已有记录；踩完新坑追加到本文 |

---

## Quick Reference: Thinking Triggers

### When to Think About Cross-Layer Issues

- [ ] Feature touches 3+ layers (Core → ViewModel persistence → ViewModel orchestration → View)
- [ ] Data format changes between layers (JSON ↔ record ↔ ViewModel property ↔ XAML binding)
- [ ] Multiple consumers need the same data (e.g., selection state used by launch + batch + display)
- [ ] You're not sure where to put some logic (Core pure function? ViewModel helper? Code-behind?)
- [ ] Config/settings/presets must be atomically consistent
- [ ] New JSON field added to `LaunchEntry` — must update serialization, validation, normalization, and tests

→ Read [Cross-Layer Thinking Guide](./cross-layer-thinking-guide.md)

### When to Think About Code Reuse

- [ ] You're writing similar validation to something in `LauncherConfigStore`
- [ ] You see the same status text pattern in 3+ places
- [ ] You're creating a new `const` — does `LauncherDesignTokens` already have it?
- [ ] Two ViewModel commands follow the same snapshot→mutate→save→rollback pattern
- [ ] Multiple views need the same visual state class

→ Read [Code Reuse Thinking Guide](./code-reuse-thinking-guide.md)

---

### When to Check Pitfalls

- [ ] Starting any new coding session → skim [Pitfalls](./pitfalls.md) section 1 (工具层坑)
- [ ] Encountering a weird error → search pitfalls.md for keywords before deep debugging
- [ ] Just learned a hard lesson → append it to pitfalls.md with session-id evidence
- [ ] About to do a batch rename/refactor → read pitfalls.md §3.4, §3.5
- [ ] Debugging any network/service issue → read pitfalls.md §M4 (诊断顺序)
- [ ] Third time hitting the same blocker → read pitfalls.md §3.3 (标 blocked)

→ Read [Pitfalls — 跨会话踩坑实录](./pitfalls.md)

---

## WT_Protector-Specific Gotchas

> 完整跨会话踩坑见 [pitfalls.md](./pitfalls.md)。此处仅保留最频繁触发的 5 条项目特有坑。

### 1. Triple-File Atomicity

`config.json` + `presets.json` + `settings.json` must stay consistent. A save that succeeds on config but fails on presets MUST roll back config. Never save files independently — always through the persistence pipeline (`LauncherEntryMutationPersistence.Save()`).

### 2. Design Token Drift

XAML hardcoded `#2563EB` will diverge from `LauncherDesignTokens.PrimaryColor` the next time the token changes. Always use `{StaticResource PrimaryBrush}`.

### 3. Drag State Leak

Changing sort mode or applying a filter while dragging must cancel the drag session first. `MainWindowViewModel.Refresh()` starts with `DiscardDragReorderPreview()`.

### 4. Single Source of Truth

Read-only projections (`IsCompactLayout`, `IsCardsViewMode`, etc.) are computed from a single writable field. Never store the projection as a second field — they WILL drift.

### 5. Never `catch (Exception)` — Narrow It

`TrySaveWindowBounds` 原本 catch 裸 `Exception`，吞掉了不应出现的异常。现窄化为 `IOException | UnauthorizedAccessException | SecurityException`。裸 `Exception` 掩盖 bug，已知异常才处理。

---

## Pre-Modification Rule

> **Before changing ANY value, ALWAYS search first!**

```bash
# Search for the constant/value you're about to change
grep -r "CardBorderRadius" src/
grep -r '"selected"' src/
```

---

**Core Principle**: 30 minutes of thinking saves 3 hours of debugging. In a desktop app with 5 layers and atomic persistence, "just change it" is never the right first step.
