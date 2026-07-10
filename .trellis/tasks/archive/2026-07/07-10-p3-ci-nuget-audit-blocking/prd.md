# P3: Make CI nuget-audit blocking

## Goal

Make the CI NuGet security audit fail the pipeline when known-vulnerable
dependencies are detected. Current state: `.github/workflows/verify.yml` runs
the audit job with `continue-on-error: true`, so vulnerabilities are reported
but never block CI. The job also uses `dotnet restore --audit`, which is not a
valid .NET 10 CLI flag (audit is driven by MSBuild properties, not a CLI switch).

## Requirements

- Drop the invalid `--audit` flag from the CI restore command (audit still runs
  because the project sets `NuGetAudit=true`).
- Remove `continue-on-error: true` from the `nuget-audit` job so a reported
  vulnerability fails CI.
- Explicitly declare `<NuGetAudit>true</NuGetAudit>` and
  `<NuGetAuditMode>all</NuGetAuditMode>` in
  `src/avalonia-launcher/Directory.Build.props` so the audit posture is
  self-documenting rather than relying on SDK defaults.
- Keep `dotnet list package --vulnerable` as the human-readable fallback for
  local debugging (documented in PRD notes; no CI change needed).

## Out of scope

- Adding an third-party SBOM generator (CycloneDX, Syft).
- Pinning to a specific `nuget.org` audit feed beyond the default.

## Acceptance criteria

- [ ] `nuget-audit` job no longer has `continue-on-error`.
- [ ] `Directory.Build.props` declares `NuGetAudit=true` and
      `NuGetAuditMode=all`.
- [ ] CI restore command is `dotnet restore <sln>` (no `--audit`).
- [ ] Local `dotnet restore` exits 0 with current pinned versions.
