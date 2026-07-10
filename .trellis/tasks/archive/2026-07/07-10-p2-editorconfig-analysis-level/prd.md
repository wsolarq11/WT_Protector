# P2: Add .editorconfig + explicit AnalysisLevel

## Goal

Provide a baseline code-style + analyzer enforcement surface so CI and local builds apply
the same .NET analyzer rules. Current state: SDK 10 default analyzers run implicitly, but no
explicit `AnalysisLevel` declaration and no `.editorconfig`.

## Requirements

- Add `.editorconfig` at repo root with:
  - `root = true`
  - C# latest language version target
  - `.NET` core analyzer severity (latest-recommended baseline)
  - file-scoped namespace preference, `var` preference, pattern matching preference
  - naming rule: private fields `_camelCase`
- Update `src/avalonia-launcher/Directory.Build.props` to set:
  - `<AnalysisLevel>latest-recommended</AnalysisLevel>`
  - `<EnableNETAnalyzers>true</EnableNETAnalyzers>`
  - `<EnforceCodeStyleInGenerated>true</EnforceCodeStyleInGenerated>`
- Must not introduce new warnings (build stays 0/0).

## Out of scope

- Third-party analyzers (Sonar, Roslynator).
- Per-method style rules (repo-wide baseline only).

## Acceptance criteria

- [ ] `.editorconfig` exists at repo root, parsed by `dotnet build` without error.
- [ ] `Directory.Build.props` has the three new properties.
- [ ] `dotnet build -c Release` 0 warnings / 0 errors.
- [ ] `dotnet test -c Release` 453/453 pass.
