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
- `LauncherConfigStoreTests.ConfigPersistenceTests.cs` — `ConfigPersistence` area
- `LauncherConfigStoreTests.PathTests.cs` — `Path` area
- `MainWindowViewModelTests.DragTests.cs` — partial ViewModel, drag area

---

## Fixture Setup Pattern

### Factory Methods

Each test shared file provides factory methods for creating test data:

```csharp
// LauncherConfigStoreTests.Shared.cs
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
- `MainWindowDragDropDecisionTests.cs` — pure drop target logic
- `MainWindowDragGhostFactoryTests.cs` — ghost creation
- `MainWindowEntryCardResolverTests.cs` — visual tree resolution
- `MainWindowReorderFlipAnimationTests.cs` — FLIP math

### Contract Tests

Verify structural contracts that span layers:

- `RepositoryConfigContractTests.*.cs` — config/schema/settings integrity
- `StatusTextContractTests.cs` — status text format contracts
- `DialogLayoutContractTests.cs` — dialog layout contracts

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
| `LauncherPersistenceTestFixtures.cs` | Shared persistence test helpers |
| `TestRepositoryPaths.cs` | Temp directory creation and path helpers |
| `TempDirectory.cs` | IDisposable temp directory wrapper |
| `MainWindowEntryCardTestFactory.cs` | Entry card factory for View tests |
| `LauncherConfigStoreTests.Shared.cs` | Config test factories and assertions |

**Rule:** If you write the same factory method in 3+ test files, move it to a Shared fixture.

---

## Forbidden Patterns

- ❌ **Tests that write to the real `config/` directory.**
- ❌ **[Fact] that depends on file system order** — always sort before asserting.
- ❌ **Sleep/delay in tests** — use fixed clock injection for time-dependent logic.
- ❌ **Boolean assertions without failure message** — use `Assert.True(condition, "why it failed")`.
- ❌ **Tests that require a running Avalonia window** — extract pure logic, test that.
