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
// LauncherPersistenceTestFixtures.cs (persistence-roundtrip helpers)
public static LaunchEntry Entry(string id) => new() { Id = id, Name = id, Directory = ..., Command = "pwsh" };
public static LauncherPresetFile PresetFile(params LauncherPreset[] presets) => new() { ... };
public static LauncherPreset Preset(string id, params string[] entryIds) => new() { ... };
```

ViewModel-area tests have their own private `Entry(id, name?, directory?, command?, selected?)` factory in `MainWindowViewModelTests.Shared.cs` for behavior-test ergonomics.

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
- `LauncherDragGhostLayoutCalculatorTests.cs` — ghost element layout math
- `MainWindowEntryCardResolverTests.cs` — visual tree resolution
- `MainWindowEntryRowDoubleTapTests.cs` — double-tap launch decision

### Contract Tests

Verify structural contracts that span layers:

- `StatusTextContractTests.cs` — status text format contracts
- `MainWindowViewModelTests.CollectionContractTests.cs` — collection invariants (read-only projection consistency, entry snapshot equivalence)
- `MainWindowViewModelTests.DialogBoundaryTests.cs` — dialog delegate boundary contracts (null-handler rejection on `RequestEntryDraftAsync` / `ConfirmLaunchAsync` / etc.)
- `MainWindowViewModelTests.PresetPersistenceTests.cs` — preset save/load round-trip

---

## Architecture Rules (ArchUnitNET)

**Source:** `tests/WTLauncher.Tests/ArchitectureRules.cs`

Architecture invariants that prose `quality-guidelines.md` rules translate into executable checks. Violations fail `dotnet test` immediately, not after human review.

**Packages:** `TngTech.ArchUnitNET` 0.13.3 + `TngTech.ArchUnitNET.xUnitV3` 0.13.3 (in `Directory.Packages.props`).

**Rules currently encoded (R1-R10):**
- R1: Core must not reference `Avalonia.*`
- R2: Core must not reference `CommunityToolkit.*`
- R3: Core must not depend on `WTLauncher.App.*` (dependency direction)
- R4: ViewModels must not depend on `System.IO.File` (persistence gateway rule)
- R5: Only `LauncherJson` + known readers (`LaunchArgumentBuilder`, `LauncherStore`) may depend on `System.IO.File` inside Core
- R6: Core must not reference `WTLauncher.App.ViewModels.*` (belt-and-suspenders to R3)
- R7: Preset feature services must not reference Views
- R8: Entry feature services must not reference Views
- R9: Preset feature services must not depend on `System.IO.File`
- R10: Entry feature services must not depend on `System.IO.File`

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

## Scenario: Feature-service and ViewModel-component contracts

### 1. Scope / Trigger

Use direct contract tests after extracting a feature service or a stable bindable component from `MainWindowViewModel`. The goal is to prove its public boundary without duplicating the existing persistence or GUI-smoke owners.

### 2. Signatures

```csharp
Task<ServiceResult<T>> FeatureService.OperationAsync(...);
ServiceResult<T> FeatureService.Mutate(...);
IAsyncRelayCommand BindableComponent.ActionCommand { get; }
```

Test files follow `<ProductionType>Tests.cs`, for example `EntryServiceTests.cs`, `PresetServiceTests.cs`, and `LauncherCardGroupViewModelTests.cs`.

### 3. Contracts

- Service tests use the real service, real Core workflow and `StubDialogService`; no mocking framework.
- Cover Success payload, user Canceled status, recoverable Failure status, validation rejection, and input aggregate non-mutation where applicable.
- Component tests execute generated public commands and assert bindable properties, `PropertyChanged`, callback payload values and visible order.
- Callback collections are point-in-time payloads: later component collection mutation must not rewrite an already-delivered list.
- MainWindowViewModel tests remain the owner of snapshot/save/rollback and status integration; GUI smoke remains the owner of XAML rendering.

### 4. Validation & Error Matrix

| Boundary event | Required assertion |
| --- | --- |
| Dialog returns data | Exact normalized/snapshotted Success payload |
| Dialog closes or rejects | Exact `ServiceCanceled<T>.Status` |
| Dialog throws recoverably | Exact `ServiceFailure<T>.Status`; no state mutation |
| Domain target is invalid/missing/locked | Failure and original aggregate unchanged |
| Component command runs | Callback receives IDs/items in current visible order |
| Bindable state changes | Property value and dependent notification both observed |

### 5. Good/Base/Bad Cases

- Good: delete service test asserts both snapshotted target and focus-cleanup ID.
- Base: group component test asserts `CountLabel`, expand icon and callback order without creating a Window.
- Bad: invoking a private command method through reflection or asserting XAML margin values in a unit test.

### 6. Tests Required

- Every public feature-service operation has at least one direct success or delegation test plus every applicable cancel/failure branch.
- Every operation-specific typed payload has field-level assertions.
- Every extracted bindable component has construction/property and command/callback tests.
- Full Release tests and `tools/verify.ps1` must remain green; focused tests do not replace the production gate.

### 7. Wrong vs Correct

```csharp
// Wrong: only a large owner test indirectly happens to cross the service.
await viewModel.AddEntryCommand.ExecuteAsync(null);
Assert.Equal(1, viewModel.TotalCount);

// Correct: direct boundary test names the outcome and its payload.
var result = await service.AddEntryAsync(...);
var success = Assert.IsType<ServiceSuccess<LaunchEntry>>(result);
Assert.Equal("one", success.Value.Id);
```

Do not change production visibility or add interfaces solely to make these tests convenient; use existing public commands and lightweight real owners/factories.

---

## Forbidden Patterns

- ❌ **Tests that write to the real `config/` directory.**
- ❌ **[Fact] that depends on file system order** — always sort before asserting.
- ❌ **Sleep/delay in tests** — use fixed clock injection for time-dependent logic.
- ❌ **Boolean assertions without failure message** — use `Assert.True(condition, "why it failed")`.
- ❌ **Tests that require a running Avalonia window** — extract pure logic, test that.
