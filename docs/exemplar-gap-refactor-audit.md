# WT Launcher exemplar gap and refactor audit

Date: 2026-07-13

## Executive conclusion

WT Launcher is already unusually strong in its narrow-domain safety contract: typed Core rules, one atomic JSON store, rollback-aware mutations, launch preflight/confirmation, architecture tests, self-contained publish and GUI smoke. Its largest remaining gap is not missing platform scope. It is that the product's growing first-class features still converge into a few oversized UI/orchestration files, so visual consistency and routine feature work cost more than they should.

The correct exemplar is composite:

- **Code quality:** this repository's own five-layer/FMA target, with smaller services and a thin composition ViewModel.
- **Visual quality:** CodexSwitchUI's component/theme separation plus Windows/Fluent/WCAG principles already captured by `DESIGN.md`.
- **Operational quality:** first-class entry/group/settings/receipt patterns from the existing launcher/workspace benchmark library.

CodexSwitch is not an across-the-board code exemplar: its indexed `MainWindowViewModel.cs` is 5291 lines (as measured during this audit; external benchmark data — no second in-repo source). It is useful for component, theme, icon and packaging patterns, not for ViewModel size or product scope.

## Evidence baseline

### WT Launcher strengths (as of 2026-07-13)

- `WTLauncher.Core` stays independent of Avalonia and app ViewModels, enforced by ArchUnitNET.
- `config/launcher.json` is the single authority and uses atomic replace plus backup recovery.
- User mutations follow snapshot → mutate → save → rollback.
- Drag reorder uses pointer capture rather than OLE's nested message loop.
- Three view modes, keyboard alternatives, global confirmation policy, launch receipts and a full publish/smoke gate already exist.

> Note (2026-08-15 verification): the snapshot → mutate → save → rollback line describes the state as of this audit's date. The snapshot classes were removed on 2026-07-21; rollback now lives in `StoreGrain.Dispatch` (save-first-then-commit). The remaining strengths above were re-verified on 2026-08-15.

### Concentration hotspots (as of 2026-07-13)

| Surface | Evidence | Meaning |
| --- | --- | --- |
| Main window XAML | `MainWindow.axaml` is 992 lines | three modes, resources, templates and shell layout share one change surface |
| Command management view | `CommandManagementWindow.cs` is 432 lines; constructor alone spans lines 32–192 | visual tree is assembled imperatively and is harder to theme/test than XAML components |
| Preset orchestration | `MainWindowViewModel.Presets.cs` is 473 lines | one feature still owns CRUD, apply, preview, launch and UI state together |
| Layout orchestration | `MainWindowViewModel.Layout.cs` is 427 lines | filtering, sorting, sizing, modes, scroll and persistence remain coupled |
| Batch orchestration | `MainWindowViewModel.Batch.cs` is 338 lines | selection and launch workflow share one owner |
| Token drift | hardcoded margins, radii, font sizes and colors remain in XAML/C# | `LauncherDesignTokens` is not yet the complete visual source of truth |

> Note (2026-08-15 verification): all line counts above are measurements taken on the audit date (2026-07-13) and have since changed: `MainWindow.axaml` is now 341 lines; `CommandManagementWindow` was converted to `CommandManagementWindow.axaml` (168) + `.axaml.cs` (158); `MainWindowViewModel.Presets.cs` is 444 lines; `Layout.cs` is 226 lines; `Batch.cs` is 190 lines.

## Refactor matrix

| ID | Priority | Dimension | Gap and evidence | Minimum refactor | Feasibility / risk | Existing task |
| --- | --- | --- | --- | --- | --- | --- |
| CQ-01 | P0 | Architecture | The partial split reduced file size but did not reduce ownership; preset/layout/batch partials remain 338–473 lines. | Finish service extraction, then leave `MainWindowViewModel` as state composition and command delegation. Do not add more partials. | High feasibility; medium regression risk because state/persistence ordering must remain exact. | `07-12-fma-p2-services`, `07-12-fma-p3-thin-vm` |
| CQ-02 | P0 | Persistence boundary | Service extraction can accidentally duplicate snapshot/save/rollback behavior. | Centralize each mutation family behind one service contract while keeping `LauncherStore` the only disk authority. | High feasibility; high data-integrity importance. | `07-12-fma-p2-services` |
| CQ-03 | P1 | View separation | `CommandManagementWindow` builds a large visual tree in C#, including rows and tool buttons. | Convert stable layout to XAML/DataTemplates; retain code-behind only for window/dialog routing. | High feasibility; low runtime risk, medium visual-regression risk. | Follow-up under `07-11-centralize-declutter` |
| CQ-04 | P1 | XAML modularity | The 992-line main XAML combines shell, three entry views, styles and templates. | Extract local `UserControl`/resource files only at proven seams: toolbar, cards view, details view, names view, status/receipt area. | High feasibility; medium binding/resource risk. Avoid a general component library. | New child after FMA |
| CQ-05 | P1 | Status/content ownership | A large static status-text surface and multiple workflows make copy changes cross-cutting. | Keep one message owner, but group by workflow and expose typed formatter methods instead of call-site string assembly. | High feasibility; low risk. | `07-12-fma-p4-quality` |
| CQ-06 | P1 | Error model | Recoverable exceptions are centralized, but normal failure still often travels through exceptions and boolean saves. | Introduce typed results only at service boundaries where callers already branch on success/failure; do not rewrite Core wholesale. | Medium feasibility; medium diff/risk. | `07-12-fma-p4-quality` |
| CQ-07 | P1 | Test architecture | Contract coverage is strong, but component boundaries are difficult to test while UI is monolithic. | Add contract tests for extracted service/component public behavior; keep XAML styling under GUI smoke. | High feasibility; low risk. | FMA children + quality child |
| CQ-08 | P2 | Performance evidence | Layout math is optimized, but no standing latency budget protects startup/search/filter/launch-preflight. | Add a small stopwatch-based diagnostic or benchmark only for a demonstrated regression; first record current timings in verify receipts. | Medium feasibility; avoid flaky hard thresholds. | `07-12-fma-p4-quality` |
| V-01 | P1 | Visual system | Hardcoded `Margin`, `Padding`, `CornerRadius`, `FontSize` and colors remain beside token resources. | Promote only repeated values to existing resource ownership and delete one-off drift; add a gate for raw hex outside approved transparent/shadow cases. | High feasibility; low code risk, visual review needed. | New quality child |
| V-02 | P1 | Visual hierarchy | Top controls and action density still compete; the product already has a declutter plan. | Complete management-panel migration and card action reduction before adding any new chrome. | High feasibility; high user benefit. | `07-11-centralize-declutter` |
| V-03 | P1 | Component consistency | Programmatic dialogs/windows can diverge in spacing, typography and button placement. | Reuse shared dialog primitives and XAML styles; make destructive/default-button semantics uniform. | High feasibility; low risk. | FMA/declutter follow-up |
| V-04 | P1 | Accessibility | Keyboard alternatives exist, but accessibility evidence is mostly structural rather than an explicit UI audit. | Audit focus order, accessible names, tooltip-only content, contrast, high DPI and reduced motion; add structural tests where stable. | High feasibility; medium manual-verification cost. | `07-12-fma-p4-quality` |
| V-05 | P2 | Icons and scanability | Text-heavy secondary actions reduce quick recognition, while copying a full icon library would be excessive. | Use a tiny local icon set only for repeated universal actions (launch/edit/delete/more/search); keep labels/tooltips. | Medium feasibility; avoid a new dependency unless current Avalonia assets cannot cover it. | Defer until declutter is measured |
| V-06 | P2 | Responsive proof | Responsive rules exist, but acceptance is concentrated in calculations and one GUI smoke size. | Add a small screenshot/manual matrix for narrow, default, wide and high-DPI states; keep calculation tests as the release gate. | High feasibility; low risk. | `07-12-fma-p4-quality` |
| UX-01 | P0 | Frequent work | Finding commonly used entries still depends on manual groups/recent sorting. | Complete launch-count/frecency sorting without displaying analytics noise. | High feasibility; low schema risk because the field is optional. | `07-11-entry-launch-count-frecency` |
| UX-02 | P1 | Central management | Commands/directories are first-class but management is visually separate and implementation-heavy. | Finish the central management flow and make add/edit/merge/delete outcomes immediately visible in the main list. | High feasibility; medium integration risk. | `07-11-centralize-declutter` |
| UX-03 | P1 | Fast creation | Reusing an existing directory and command template still takes too many form steps. | Complete the discovery "existing directory → command template → create entry" path and reuse the existing add-entry mutation service. | High feasibility; low conceptual risk. | `07-11-directory-scan-wizard` |
| UX-04 | P1 | Feedback | Status text is strong but competes with transient actions and can be missed. | Preserve the status bar as authority; add restrained in-context success/error emphasis, never a notification center. | High feasibility; low risk. | Quality/visual child |
| UX-05 | P2 | First-run discoverability | A portable JSON-first tool assumes users understand entries, groups and confirmation immediately. | Add a small empty-state/onboarding path with "add entry", "discover", config path and one sentence about groups. | High feasibility; low maintenance cost. | New lightweight child |
| UX-06 | P2 | Receipts/history | Current transient receipts are honest, but users cannot revisit a missed result. | Keep transient receipts unless real support cases prove history is needed; then add a bounded in-memory/session list before persisting anything. | Defer; persistence would increase scope and privacy surface. | No task yet |

## Recommended sequence

### Now

1. Complete FMA Phase 2–3; this unlocks safer UI extraction.
2. Complete central management, frecency and directory-scan tasks already planned.
3. Convert `CommandManagementWindow` to declarative XAML and remove repeated raw visual values.

### Next

4. Split `MainWindow.axaml` at the four proven view/shell seams.
5. Run the explicit accessibility/responsive audit and add the smallest stable checks.
6. Tighten typed failure results at newly extracted service boundaries.

### Later only with evidence

7. Add a tiny icon set, first-run onboarding or bounded session receipts if user observation shows those gaps matter.

## Explicitly rejected or deferred

- No plugin marketplace or extension runtime.
- No cross-platform packaging project.
- No new database or hidden AppData state.
- No full CodexSwitchUI dependency/submodule.
- No Native AOT requirement until self-contained publish has a measured problem.
- No fourth view mode, workflow graph, dashboard shell or persistent analytics screen.

These changes would copy the benchmark's scope rather than solve WT Launcher's narrow user job.

## Feasibility summary

The highest-value program is feasible with the current stack and mostly already represented by active Trellis tasks. The main technical risk is not framework capability; it is preserving atomic mutation and single-owner state while extracting collaborators. The safest route is therefore service extraction first, declarative view extraction second, visual polish third. A large redesign before FMA completion would increase binding churn and duplicate work.
