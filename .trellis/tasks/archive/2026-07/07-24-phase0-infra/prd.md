# Phase 0: Infrastructure Setup

## Requirements

1. **OneOf NuGet package** — Add to `Directory.Packages.props` (central) and `WTLauncher.App.csproj` (project)
   - Version 4.0.x latest stable
2. **Immutype NuGet package** — Add to `WTLauncher.Core.csproj` for compile-time immutability code generation
   - Version 1.0.x latest stable  
3. **Roslyn Analyzer project skeleton** — Create `.analyzer/WTLauncher.Analyzers/` project with:
   - `ResourceKeyAnalyzer.cs` — detects undefined `{StaticResource}` keys in XAML
   - `JsonRegistrationAnalyzer.cs` — detects unregistered `[JsonSerializable]` types
   - `BareCatchAnalyzer.cs` — detects `catch (Exception)` without `when` filter
   - Project reference from `WTLauncher.App.csproj` as analyzer
4. **Common base classes** — Create `WTLauncher.Core/Errors/AppError.cs` (error ADT hierarchy)
5. **ErrorSupervisor base** — Create `WTLauncher.App/Infrastructure/ErrorSupervisor.cs`

## Acceptance Criteria

- [ ] `dotnet restore` passes with OneOf + Immutype
- [ ] `dotnet build -warnaserror` passes
- [ ] Analyzer project compiles and produces valid analyzer DLL
- [ ] `AppError` type hierarchy defined with `OneOfBase`
- [ ] `ErrorSupervisor` class with circuit-breaker pattern implemented
