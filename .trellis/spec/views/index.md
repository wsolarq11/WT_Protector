# Views Layer Development Guidelines

> `WTLauncher.App.Views/` — XAML views, code-behind, drag-drop, dialogs, and visual behavior.

---

## Overview

The Views layer (Layer 4) is the outermost shell. It depends on ViewModels for state and Core for design tokens, but never contains business logic. View code-behind handles **windowing concerns only**: dialog injection, keyboard navigation, drag-drop event wiring, and window size.

**Reference files:**
- `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs` — primary code-behind
- `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs` — drag-drop handlers
- `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs` — design token injection

---

## Guidelines Index

| Guide | Description |
|-------|-------------|
| [Directory Structure](./directory-structure.md) | Views file organization |
| [MVVM Pattern](./mvvm-pattern.md) | ViewModel ↔ View contract, dialog injection |
| [Drag & Drop](./drag-drop.md) | Ghost factory, FLIP animation, insert indicators |
| [Dialogs](./dialogs.md) | Confirmation windows, entry editor, preset editor |
| [Quality Guidelines](./quality-guidelines.md) | Code standards, forbidden patterns |

---

## Core Principle

**Views own pixels, not decisions.** If a code-behind has an `if` statement about business logic, it's in the wrong file. Keyboard shortcuts and drag-drop hit-testing are acceptable code-behind; "should we launch with confirmation?" is not.

---

**Language**: All documentation in **English**.
