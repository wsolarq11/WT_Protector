# Design benchmark digest

Last refreshed: 2026-05-20

Purpose: convert 12 external first-class design benchmarks into WT Launcher acceptance rules. This document is not a dependency list and does not authorize importing a foreign design system. It is a source-tracked yardstick for the repo-local `DESIGN.md`, Avalonia XAML, ViewModels, and release gates.

## The 12 benchmark inputs

1. [Microsoft Windows app design](https://learn.microsoft.com/en-us/windows/apps/design/) — Windows-first behavior should feel native, accessible, and consistent across input types and form factors.
2. [Microsoft Fluent 2](https://fluent2.microsoft.design/) — component foundations and accessibility resources should reduce local token drift rather than add ornamental chrome.
3. [Apple Human Interface Guidelines: Feedback](https://developer.apple.com/design/human-interface-guidelines/feedback) — user actions need timely, proportional, accessible feedback; critical interruptions must stay rare.
4. [Apple Human Interface Guidelines: Accessibility](https://developer.apple.com/design/human-interface-guidelines/accessibility/) — interactions should remain perceivable, adaptable, and available through alternatives to gestures alone.
5. [Material Design adaptive layout](https://developer.android.com/codelabs/adaptive-material-guidance) — layout should adapt to usable breakpoints and continue flexing between them instead of locking to rigid sizes.
6. [WCAG 2.2](https://www.w3.org/TR/WCAG22/) — accessibility claims must be testable: perceivable, operable, understandable, and robust enough for keyboard, focus, contrast, target size, and dragging alternatives.
7. [IBM Carbon](https://carbondesignsystem.com/) — design systems should combine working code, design tools, guidelines, and contribution discipline; local tokens need owners.
8. [Atlassian Design System](https://atlassian.design/) — cohesive product UI depends on shared foundations: accessibility, content, spacing, grid, color, typography, iconography, elevation, borders, and radius.
9. [GitHub Primer](https://primer.style/) — product UI should separate product components, brand components, primitives, and accessibility instead of mixing every concern in one layer.
10. [Shopify Polaris web components](https://shopify.dev/docs/api/polaris/using-polaris-web-components) — admin-style tools benefit from semantic components, built-in keyboard/focus behavior, responsive values, and tokenized tone/spacing.
11. [Adobe Spectrum 2](https://s2.spectrum.adobe.com/) — mature professional tools should be contextual, cohesive, performant, adaptive, inclusive, and detailed without becoming noisy.
12. [GOV.UK Design System](https://design-system.service.gov.uk/) and [NN/g usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/) — reuse proven accessible patterns, minimize recall, keep status visible, use user language, and prefer aesthetic-minimal interfaces.

## WT Launcher acceptance translation

- Native-first: keep the implementation in Avalonia/MVVM/XAML and Windows release gates; external systems are reference standards, not new dependencies.
- One source per state: view modes, sort mode, drag session, drag cue, card density, available card area, and selection mutation paths must each have a single writable owner.
- One source per visual token: drag insertion cue color/radius/opacity/hit-test behavior belongs to `Border.drag-insert`; future visual tokens need a similarly named owner or should reuse existing styles.
- Feedback is proportional: status bar feedback handles routine saves/launches; modal confirmation is reserved for explicit launch/delete decisions.
- Gesture alternatives: drag reorder is an accelerator, while custom sort state, context menus, buttons, double-click row launch, and visible handles preserve non-drag paths.
- Adaptive density: cards may shrink only until readability/recognition limits; after that the scroll fallback is intentional and must stay covered by tests.
- View mode separation: Cards optimize readable action, `详细列表` optimizes dense field scanning, and `名称墙` optimizes overview. Do not force all jobs into one view.
- Minimal chrome: prefer layout, typography, and one accent over additional cards, gradients, badges, or decorative layers.
- Token restraint: use the existing 8 / 13 / 21 rhythm and local styles before adding a new token. A new token must remove duplication or encode a product invariant.
- Testable design: each benchmark-derived rule needs at least one of: XAML contract test, ViewModel unit test, calculator unit test, verify/build receipt, or explicit documented gap.

## Current evidence in repo

- `DESIGN.md` is the canonical local design contract.
- `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` owns the visual surface and XAML style tokens.
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherDragGhostLayoutCalculator.cs` owns drag ghost size and pointer-follow geometry.
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` owns view state, layout inputs, selection, presets, and entry mutation orchestration.
- `src/avalonia-launcher/tests/WTLauncher.Tests/*` locks core contracts for drag preview, context-menu reorder alternatives, details/names/cards modes, sizing fallback, and persistence.
- `tools/verify.ps1` and `tools/build-release.ps1` remain the release judge; screenshots/manual inspection are useful receipts but not replacements for the gate.

## Optional visual receipts

- Manual visual inspection of live drag ghost alignment and insert cue animation remains useful after visual-heavy changes, but it is no longer a blocking benchmark gap: current source-tracked tests cover drag cue structure, insert token ownership, ghost pointer-follow geometry, FLIP subscription safety, and GUI smoke startup.
