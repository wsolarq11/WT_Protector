# Implement: TS 根门禁迁移

## Execution Order

1. Add `scripts/checks/common.ts`.
2. Add `scripts/checks/static.ts` for policy, docs, schema-static, code scans.
3. Add `scripts/checks/schema.ts` for GitHub and launcher/config contracts.
4. Add `scripts/checks/run.ts` for dynamic dotnet gates and structure baseline.
5. Add `scripts/verify.ts` and npm `verify` script.
6. Switch CI fast lane and full-gate to npm; retain PS for dual-track but point it at TS mainline/coverage.
7. Update AGENTS/CLAUDE/README/README-avalonia.

## Validation

- `npm run typecheck`
- `npm test`
- `npm run verify` (44 checks PASS)
- `tools/verify.ps1` (dual-track PASS)
- `git diff --check`
