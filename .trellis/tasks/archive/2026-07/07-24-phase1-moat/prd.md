# Phase 1: Compile-time Moat + Error Kernel

## Requirements

1. **Fix all remaining bare catches** — Every catch without when in src/ must be fixed.
2. **Complete ResourceKeyAnalyzer** — Read .axaml files, extract StaticResource references, verify against InjectDesignTokens().
3. **OneOf-based error handling** — Replace legacy ErrorOr/try-catch with OneOf<T, AppError>.
4. **async void event handlers** — Protect all 11 async void handlers in dialog code-behind.

## Acceptance Criteria

- [ ] dotnet build -warnaserror passes with 0 warnings
- [ ] WT1004: no bare catch (Exception) in src/
- [ ] All async void handlers have try-catch(ExceptionFilters.IsRecoverable)
