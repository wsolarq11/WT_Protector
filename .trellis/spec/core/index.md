# Core Layer Development Guidelines

> `WTLauncher.Core` — pure data, contracts, and rules with zero UI dependencies.

---

## Overview

The Core layer (`src/avalonia-launcher/src/WTLauncher.Core/`) is the foundation of the launcher. It must never reference Avalonia, CommunityToolkit.Mvvm, or any UI framework. Every type here is either a data record, a pure function, or a static helper with no side effects beyond file I/O (in persistence helpers).

**Reference files:**
- `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs` — canonical data record shape
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs` — JSON persistence facade (delegates to LauncherJsonSerializer + FileLauncherJsonIO)
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherJsonSerializer.cs` — Zone 0 pure JSON serializer (no I/O)
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreValidator.cs` — Zone 0 pure schema validator
- `src/avalonia-launcher/src/WTLauncher.Core/ILauncherJsonIO.cs` — Zone 0 boundary port for JSON I/O
- `src/avalonia-launcher/src/WTLauncher.Core/IStorePersistence.cs` — Zone 0 boundary port for store I/O
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherDesignTokens.cs` — single source of design constants

---

## Guidelines Index

| Guide | Description |
|-------|-------------|
| [Directory Structure](./directory-structure.md) | 5-layer architecture and project layout |
| [Data Types](./data-types.md) | Records, enums, design tokens, immutable data |
| [Persistence](./persistence.md) | JSON persistence, atomic writes, .bak rotation |
| [Error Handling](./error-handling.md) | Validation, exception types, status text patterns |
| [Quality Guidelines](./quality-guidelines.md) | Code standards, forbidden patterns, invariants |

---

## Core Principle

**"Bad programmers worry about the code. Good programmers worry about data structures."**

The Core layer exists so that business rules are testable without spinning up a window. Every time a rule creeps into a ViewModel that could live in Core, ask: "Can I test this without Avalonia?" If yes, move it down to Core.

---

**Language**: All documentation in **English**.
