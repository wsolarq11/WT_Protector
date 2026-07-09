# LauncherEntryVisibility rename evaluation

## Goal

Evaluate whether to rename `LauncherEntryVisibility` → `LauncherEntryQuery` (or similar) per misc-cleanup audit deferred item.

## Background

`LauncherEntryVisibility` is a static utility class in `WTLauncher.Core/launcherEntryVisibility.cs` containing:
- Sort mode ID constants (Custom/Recent/Updated/Alphabetical)
- `NormalizeSortModeId`
- `FilterAndSort<TEntry>` (core method, ~85 lines)
- `MatchesFilter`, `ValidateCustomOrder`, `ParseTimestamp` helpers

The class name suggests "visibility" (show/hide) semantics, but the actual contents are query/filter/sort operations. The audit proposed `LauncherEntryQuery` as a more accurate name.

## Investigation (2026-07-09)

### Occurrence count
- **85 occurrences** across **10 .cs files** (verified 2026-07-09)
- **31 files total** when including .md docs and archived task artifacts
- Test file `LauncherEntryVisibilityTests.cs` (44 occurrences) would also need renaming

### ROI assessment

| Dimension | Rename | Keep + comment |
|-----------|--------|----------------|
| Naming accuracy | Improved (class does query, not visibility) | Same |
| Code churn | 85 refs across 10 files + file rename | 1 comment line |
| Risk | Low (mechanical) but non-zero (typos, missed refs) | Zero |
| Single-user project consideration | Familiarity > academic accuracy | N/A |
| Bug fix | None | None |
| Build/test impact | Need full rebuild + 453 tests re-run | None |
| Time cost | ~30 min mechanical work + verification | ~2 min |

## Decision: DEFER (NO-ACTION with documentation)

The rename is correctly identified (name is semantically inaccurate) but the ROI doesn't justify the churn at this time:

1. **No functional bug** — the class works correctly under its current name
2. **No blocking issue** — no developer has been confused by the name (no related bug reports, questions, or design discussion)
3. **Single-user project** — naming familiarity trumps academic accuracy; the user has worked with `LauncherEntryVisibility` for months
4. **85 refs churn** vs **0 LOC functional improvement** — pure cost, no functional benefit
5. **Natural opportunity** — if this class gets refactored later (e.g., splitting FilterAndSort into a separate class), the rename naturally happens as part of that work

## Action (1-line ponytail comment)

Add a `ponytail:` comment above the class declaration documenting the evaluation, to prevent future audits from re-proposing:

```csharp
// ponytail: class name says "Visibility" but contents are query/filter/sort ops.
// Renaming to LauncherEntryQuery considered 2026-07-09 (task 07-09-rename-evaluation)
// and deferred — 85 refs / 10 files, no functional bug, single-user familiarity wins.
// Revisit when this class is naturally refactored (e.g., FilterAndSort split out).
```

## Acceptance Criteria

- [x] ROI evaluation documented in this PRD
- [x] ponytail: comment added to `LauncherEntryVisibility.cs`
- [ ] Build PASS
- [ ] 453/453 tests PASS
- [ ] verify.ps1 18/18 PASS
- [ ] Update `feedback_audit_driven_proposals_verify_each_source.md` or spec with the "85 refs / single-user / no bug = defer mechanical rename" rule
