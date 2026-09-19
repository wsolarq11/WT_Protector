# 巨人肩膀库

> Verified 2026-08-15 — 依据 .trellis/tasks/archive/2026-08/08-15-docs/verification-protocol.md 逐项查证后重写；本仓库相关声明均已对照工作树核实。

Purpose: keep high-signal external project lessons one step away from this repository, so future stack and UX decisions can start from checked references instead of re-discovery.

This is a reference library, not an authority layer. The active WT Launcher source of truth remains the current repository, its `README.md`, and its native gate:

```bash
npm run verify
```

## Admission rule

Every entry in this library should record:

1. **Reviewed source** — upstream URL, commit/tag/date, and important files.
2. **Observed stack** — what the project actually uses, not what we guessed.
3. **Lessons worth borrowing** — small patterns that map to WT Launcher.
4. **What not to copy** — complexity, dependencies, product scope, licensing, or maintenance burden that does not fit.
5. **Next local proof** — the smallest spike/gate that can prove whether the lesson helps this project.

## Current entries

| Entry | Why it matters |
| --- | --- |
| [CodexSwitch: Avalonia + .NET 10 + Native AOT](./codexswitch-avalonia-native-aot.md) | The external reference that justified promoting the Avalonia/.NET line into the current mainline `src/avalonia-launcher/`; the former single-file WinForms/GDI line is retired (`retired/design/winforms-launcher.md`) and must not be restored. |
| [First-class launcher patterns](./launcher-first-class-patterns.md) | 12-tool industry survey (reviewed 2026-05-17) distilled into WT Launcher entries, presets, settings, and future status boundaries; the named launch-group slice has since landed in `config/launcher.json`. |

## How to use before changing WT Launcher

1. Re-read the relevant entry and its source links.
2. Re-check the current WT Launcher state with `git status --short` and `npm run verify`.
3. Define a spike that does **not** replace the active executable until it proves first-order benefit.
4. Compare against the active acceptance standard: quicker, simpler, more natural UX; same or stronger config/launch safety; same or cheaper verification.
5. Adopt only if the spike wins under the same gate; otherwise archive the spike and keep the active path unchanged.
