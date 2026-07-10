# Test Patterns

> How to write tests in WTLauncher.Tests.

---

## Test File Naming

```
<TypeUnderTest>Tests.cs                    # Single test file
<TypeUnderTest>Tests.<Area>Tests.cs        # Split across areas
```

**Examples:**
- `LaunchArgumentBuilderTests.cs` — single class, one file
- `MainWindowViewModelTests.DragTests.cs` — partial ViewModel, `Drag` area
- `MainWindowViewModelTests.PresetCommandTests.cs` — partial ViewModel, `PresetCommand` area
- `LauncherStoreLoadFailureTests.cs` — single class, persistence failure scenarios

---

## Fixture Setup Pattern

### Factory Methods

Each test shared file provides factory methods for creating test data:

```csharp
// LauncherPersistenceTestFixtures.cs
private static LauncherPresetFile PresetFile(string name, params string[] entryIds) => ...;
private static LaunchEntry Entry(bool selected = false) => ...;
```

**Rules:**
1. Factory methods are `private static` in the test class or shared file.
2. Accept only the parameters relevant to the test — override defaults sparingly.
3. Use Chinese names for test entries (matching real usage): `"演示"`, `"项目"`.

### Temp Directory Pattern

**Source:** `TestRepositoryPaths.cs`, `TempDirectory.cs`

```csharp
// TestRepositoryPaths creates isolated temp directories
// TempDirectory wraps IDisposable cleanup
```

**Rule:** Every test that touches file I/O uses a temp directory — never write to the real `config/`.

---

## Test Structure

### Arrange-Act-Assert

```csharp
[Fact]
public void Build_ValidEntry_ReturnsPlan()
{
    // Arrange
    var entry = Entry() with { Name = "演示", Directory = @"D:\demo", Command = "codex" };

    // Act
    var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false, requireConfirmation: false);

    // Assert
    Assert.Equal("wt.exe", plan.Executable);
    Assert.Contains("-d", plan.Arguments);
}
```

### Theory vs Fact

```csharp
[Fact]        // Single scenario
[Theory]      // Parameterized
[InlineData("cards")]
[InlineData("details")]
[InlineData("names")]
public void NormalizeId_ValidInput_ReturnsMatch(string input)
{
    var result = LauncherEntryViewModes.NormalizeId(input);
    Assert.Equal(input, result);
}
```

---

## Test Categories by Layer

### Layer 0 Tests (Core — no deps)

Test pure functions directly. No mocking needed.

**Examples:**
- `LaunchArgumentBuilderTests.cs` — Windows quoting rules
- `LauncherEntryLayoutCalculatorTests.cs` — grid math
- `LauncherEntryOrderTests.cs` — ordering rules
- `LauncherEntryVisibilityTests.cs` — filter/sort logic

### Layer 1-3 Tests (ViewModels)

Test ViewModel commands with mock `Func<>` delegates:

```csharp
// Inject mock dialog that returns canned result
viewModel.ConfirmLaunchAsync = confirmation => Task.FromResult(true);
viewModel.RequestEntryDraftAsync = entry => Task.FromResult<EntryDraft?>(draft);
```

**Examples:**
- `MainWindowViewModelTests.EntryTests.cs` — add/edit/delete flow
- `MainWindowViewModelTests.SelectionTests.cs` — checkbox selection
- `MainWindowViewModelTests.LaunchTests.cs` — single/batch launch
- `MainWindowViewModelTests.DragTests.cs` — drag reorder

### Layer 4 Tests (Views — extracted logic)

Test extracted pure functions, not Avalonia controls.

**Examples:**
- `MainWindowDragInsertDecisionTests.cs` — pure drop target logic
- `MainWindow.Drag.cs (inline)Tests.cs` — ghost creation
- `MainWindowEntryCardResolverTests.cs` — visual tree resolution
- `MainWindowReorderFlipAnimationTests.cs` — FLIP math (removed 2026-06-23; FLIP animation retired)

### Contract Tests

Verify structural contracts that span layers:

- `StatusTextContractTests.cs` — status text format contracts
- `MainWindowViewModelTests.CollectionContractTests.cs` — collection invariants (ordering, dedup, length bounds)
- `MainWindowViewModelTests.DialogBoundaryTests.cs` — dialog round-trip contracts (open / accept / cancel / validation)
- `MainWindowViewModelTests.PresetPersistenceTests.cs` — preset save/load round-trip

---

## Architecture Rules (ArchUnitNET)

**Source:** `tests/WTLauncher.Tests/ArchitectureRules.cs`

Architecture invariants that prose `quality-guidelines.md` rules translate into executable checks. Violations fail `dotnet test` immediately, not after human review.

**Packages:** `TngTech.ArchUnitNET` 0.13.3 + `TngTech.ArchUnitNET.xUnitV3` 0.13.3 (in `Directory.Packages.props`).

**Rules currently encoded (R1-R6):**
- R1: Core must not reference `Avalonia.*`
- R2: Core must not reference `CommunityToolkit.*`
- R3: Core must not depend on `WTLauncher.App.*` (dependency direction)
- R4: ViewModels must not depend on `System.IO.File` (persistence gateway rule)
- R5: Only `LauncherJson` + known readers (`LaunchArgumentBuilder`, `LauncherStore`) may depend on `System.IO.File` inside Core
- R6: Core must not reference `WTLauncher.App.ViewModels.*` (belt-and-suspenders to R3)

### Critical Gotcha: Type-Level vs Method-Level Rules

ArchUnitNET's `MethodMembers` collection **only contains methods defined in loaded assemblies**. External framework methods (`System.IO.File.WriteAllText`, `System.IO.File.Exists`, etc.) appear **only** as `MethodCallDependency` targets, never as `MethodMembers`.

A rule like this is a **silent no-op** — it matches an empty set and passes vacuously:

```csharp
// ❌ WRONG — vacuously true, never catches violations
Types().That().Are(CoreLayer)
    .Should().NotCallAny(
        MethodMembers().That().HaveFullName("System.IO.File.WriteAllText"))
    .Because("...");
```

Use the **type-level** formulation instead — it resolves via the `arch.Types` dependency edges and actually catches the violation:

```csharp
// ✅ Correct — type-level dependency check
Types().That().Are(CoreLayer)
    .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
    .Because("...");
```

The trade-off: type-level is coarser — it also catches read-only `File.Exists` callers, not just writers. If that matters, narrow with explicit per-type exemptions (see R5's `DoNotHaveFullName(...)` exclusions for `LaunchArgumentBuilder` and `LauncherStore`, which only do reads).

### Critical Gotcha: `IObjectProvider` Is a Stateful Builder

Each `[Fact]` method must build **fresh** predicates. ArchUnitNET's `IObjectProvider<T>` accumulates state across `That()` chains; reusing a static provider across multiple rules silently merges their conditions. Pattern in `ArchitectureRules.cs`: every test declares its own local `IArchRule rule = Types()...` inside the method body.

### Adding a New Rule

1. Add a new `[Fact]` method `R7_<Layer>_<Constraint>`.
2. Build the predicate inline (don't reuse static providers — see gotcha above).
3. Decide type-level vs method-level based on whether the target is in a loaded assembly.
4. If exempting specific types, add `.And().DoNotHaveFullName("...")` clauses with a `Because(...)` comment naming the reason.
5. Cross-reference the new rule from `quality-guidelines.md` invariants table.

---

## Mocking Strategy

**No mocking framework.** The project uses hand-rolled test doubles:

1. **Dialog injection:** Replace `Func<>` delegates on ViewModel.
2. **File I/O:** Use temp directories via `TestRepositoryPaths`.
3. **Time:** `MainWindowViewModel` accepts `Func<DateTimeOffset>` — tests inject fixed clock.

```csharp
var viewModel = new MainWindowViewModel(
    configPath, settingsPath, settings, launchWorkflow,
    utcNow: () => new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
```

---

## Test Fixture Files

| File | Purpose |
|------|---------|
| `LauncherPersistenceTestFixtures.cs` | Shared persistence test helpers (`Entry()`, `PresetFile()` factories) |
| `TestRepositoryPaths.cs` | Temp directory creation and path helpers |
| `TempDirectory.cs` | IDisposable temp directory wrapper |
| `MainWindowEntryCardTestFactory.cs` | Entry card factory for View tests |
| `MainWindowViewModelTests.Shared.cs` | ViewModel test shared setup (`Entry()`, default settings) |

**Rule:** If you write the same factory method in 3+ test files, move it to a Shared fixture.

---

## Forbidden Patterns

- ❌ **Tests that write to the real `config/` directory.**
- ❌ **[Fact] that depends on file system order** — always sort before asserting.
- ❌ **Sleep/delay in tests** — use fixed clock injection for time-dependent logic.
- ❌ **Boolean assertions without failure message** — use `Assert.True(condition, "why it failed")`.
- ❌ **Tests that require a running Avalonia window** — extract pure logic, test that.
