# P4: Drop redundant ExitCode line before Environment.Exit

## Goal

Remove the redundant `Environment.ExitCode = 2;` assignment in
`Program.cs:31-32` of the `AppDomain.UnhandledException` handler. The next
line calls `Environment.Exit(2)` which terminates the process immediately with
exit code 2 — the prior assignment is dead code and reads as if there were two
distinct exit paths.

## Requirements

- Delete the `Environment.ExitCode = 2;` line, keeping only `Environment.Exit(2)`.
- No behavioral change: the process still exits with code 2 on an unhandled
  domain exception.

## Out of scope

- Refactoring the unhandled-exception handler shape.
- Touching the `TaskScheduler.UnobservedTaskException` branch (which correctly
  uses only `e.SetObserved()`).

## Acceptance criteria

- [ ] `Program.cs` has a single `Environment.Exit(2)` call in the
      `AppDomain.UnhandledException` handler.
- [ ] `dotnet build -c Release` 0/0.
- [ ] `dotnet test -c Release` 453/453 pass.
