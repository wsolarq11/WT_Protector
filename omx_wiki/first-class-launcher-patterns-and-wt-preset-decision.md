---
title: "First-class launcher patterns and WT preset decision"
tags: ["launcher", "presets", "giants-shoulders", "portable-config"]
created: 2026-05-17T09:23:29.683Z
updated: 2026-05-17T09:23:29.683Z
sources: ["docs/giants-shoulders/launcher-first-class-patterns.md", "config/presets.json", "tools/verify.ps1"]
links: []
category: pattern
confidence: high
schemaVersion: 1
---

# First-class launcher patterns and WT preset decision

## Decision

Promote repeated checkbox selections into named portable launch groups while keeping the HTA-compatible `selected` checkbox state as a bridge.

Local shape:

```text
config/config.json    launch entries + persisted checkbox bridge
config/presets.json   named launch groups referencing entry ids
config/settings.json  portable UI preferences
```

## Why

The industry survey in [docs/giants-shoulders/launcher-first-class-patterns.md](../docs/giants-shoulders/launcher-first-class-patterns.md) showed a recurring first-class pattern across mature tools: durable named objects beat transient UI state. VS Code uses scoped settings/workspaces, Windows Terminal uses profiles/defaults/fragments, Docker Compose uses profiles, JetBrains uses run configurations, and Kubernetes separates desired spec from observed status.

## WT rule

A preset may replace checkbox state, but it must not launch by selection alone. Launch remains an explicit `一键启动已勾选` action.

The UI should show the delta before replacement: matched preset entries, missing entries that applying would add, and extra checked entries that applying would remove. This keeps presets understandable without adding an editor or automation graph.

## Local gate

`tools/verify.ps1` must reject unknown preset entry ids, duplicate preset ids, duplicate entry references, and missing `config/presets.json`.

## Current first slice

- Add `config/presets.json`.
- Add Core preset load/save/path support.
- Add Avalonia selector plus explicit `应用启动组`.
- Add live preset-vs-selection feedback so users can see the effect before applying.
- Add xUnit + root verifier coverage.

## Future reflection

Do not add a workflow graph, plugin runtime, hidden status DB, or automation engine until the current preset model fails the same native gate or a smaller first-order benefit is proven.
