# Tests Layer Guidelines

> Testing patterns, fixtures, and conventions for `WTLauncher.Tests/`.

---

## Overview

The test project uses **xUnit v3** + **ArchUnitNET** for architecture invariants, covering all 5 layers. File/test counts evolve with the codebase — run `ls tests/WTLauncher.Tests/*.cs | wc -l` and `dotnet test -c Release` for current numbers. Tests are the mainline gate — must pass before any commit.

**Reference files:**
- `src/avalonia-launcher/tests/WTLauncher.Tests/Support/LauncherPersistenceTestFixtures.cs` — canonical shared fixtures (`Entry()`, `PresetFile()` factories)
- `src/avalonia-launcher/tests/WTLauncher.Tests/Support/TestRepositoryPaths.cs` — temp directory management
- `src/avalonia-launcher/tests/WTLauncher.Tests/Architecture/ArchitectureRules.cs` — ArchUnitNET architecture invariants (R1-R6)
- `src/avalonia-launcher/verify.ps1` — mainline gate script

---

## Guidelines Index

| Guide | Description |
|-------|-------------|
| [Test Patterns](./test-patterns.md) | Fixture setup, shared helpers, factory methods |
| [Test Quality](./test-quality.md) | What to test, naming, coverage expectations |
