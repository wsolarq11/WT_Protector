# 巨人肩膀库

Purpose: keep high-signal external project lessons one step away from this repository, so future stack and UX decisions can start from checked references instead of re-discovery.

This is a reference library, not an authority layer. The active WT Launcher source of truth remains the current repository, its `README.md`, and its native gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
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
| [CodexSwitch: Avalonia + .NET 10 + Native AOT](./codexswitch-avalonia-native-aot.md) | A modern C# desktop app shape that may fit a next-generation WT Launcher better than the current WinForms/GDI implementation. |

## How to use before changing WT Launcher

1. Re-read the relevant entry and its source links.
2. Re-check the current WT Launcher state with `git status --short` and `tools\verify.ps1`.
3. Define a spike that does **not** replace the active executable until it proves first-order benefit.
4. Compare against the active acceptance standard: quicker, simpler, more natural UX; same or stronger config/launch safety; same or cheaper verification.
5. Adopt only if the spike wins under the same gate; otherwise archive the spike and keep the active path unchanged.

