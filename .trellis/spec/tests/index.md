# Tests Layer Guidelines

> Testing patterns, fixtures, and conventions for `WTLauncher.Tests/`.

---

## Overview

The test project uses **xUnit** with 71 test files (approximately 410 `[Fact]`/`[Theory]` attributes) covering all 5 layers. Tests are the mainline gate — `dotnet test -c Release` must pass before any commit.

**Reference files:**
- `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherConfigStoreTests.Shared.cs` — canonical shared fixtures
- `src/avalonia-launcher/tests/WTLauncher.Tests/TestRepositoryPaths.cs` — temp directory management
- `src/avalonia-launcher/verify.ps1` — mainline gate script

---

## Guidelines Index

| Guide | Description |
|-------|-------------|
| [Test Patterns](./test-patterns.md) | Fixture setup, shared helpers, factory methods |
| [Test Quality](./test-quality.md) | What to test, naming, coverage expectations |
