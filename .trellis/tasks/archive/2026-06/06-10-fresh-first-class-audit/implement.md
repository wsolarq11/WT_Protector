# 执行计划

## 1. 预检门禁

```powershell
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

记录：HEAD hash、worktree dirty、test count、gate status。

## 2. 基准确认

- [ ] 读取当前 `config/config.json`（entries count, unique ids, directory existence）
- [ ] 读取当前 `config/presets.json`（presets count, schemaVersion, ref integrity）
- [ ] 读取当前 `config/settings.json`（所有字段，含新增 `searchHistory`、`commandTemplates`）
- [ ] GitHub 快照时效性：检查 `capturedAtUtc` 是否 ≤30 天

## 3. 功能逐项审计（必须从源码提取证据）

按以下顺序逐一审计，每个功能产出：星级 + 证据锚点 + 改造建议：

1. Launch entry object → `LaunchEntry.cs`, `LaunchArgumentBuilder.cs`, config tests
2. Existing-directory contract → `LaunchArgumentBuilder.cs`, `verify.ps1`
3. Search/filter → `LauncherEntryVisibility.cs`, `MainWindow.axaml.cs`, settings tests
4. Direct single launch → `LaunchArgumentBuilder.cs`, launch workflow tests
5. Checkbox selection & batch launch → config `selected`, batch preflight/confirmation/receipt tests
6. Named launch groups / presets → `presets.json`, preset store/gate tests
7. First-class preset actions → `MainWindowViewModel.Presets*.cs`
8. Preset-vs-selection preview → preset difference UI, apply/preview tests
9. Preset launch plan preview → batch preflight dry-run path
10. Add/edit/delete launcher buttons → `MainWindowViewModel.Entries.cs`
11. Custom order & drag/non-drag reorder → `customEntryOrder`, drag session/workflow tests
12. Sort modes → `LauncherEntryVisibility.cs` mode contract, `recent`sort
13. View modes: Cards / Details / Names → `MainWindow.axaml` 三视图
14. Adaptive card sizing & density → `LauncherEntryLayoutCalculator.cs`, `FastWrapPanel.cs`
15. Global launch confirmation → `settings.json` `requireConfirmationForLaunches`
16. Portable settings → `LauncherSettings.cs`, `LauncherSettingsStore.cs`
17. JSON persistence, backup & rollback → `LauncherJson.cs`, mutation snapshot tests
18. Core/App/Tests separation → solution layout, test count
19. Self-contained win-x64 publish & native gate → `build-release.ps1`, `verify.ps1`
20. Retired legacy surfaces → `retired/`, gate rejection of old paths
21. Design benchmark & giants-shoulders → `docs/design-benchmark-digest.md`, `docs/giants-shoulders/`
22. Command templates → `LauncherCommandTemplateResolver.cs`, `settings.json` `commandTemplates`
23. Design tokens → `LauncherDesignTokens.cs`, `App.axaml.cs`, XAML contract tests

## 4. 缺口分析

- [ ] 验证昨天审计的 5 个缺口是否仍然真实
- [ ] 发现新缺口（如有）
- [ ] 区分缺口类型

## 5. 撰写报告

- [ ] 写 `docs/feature-first-class-upgrade-audit.md`（覆盖旧版本）
- [ ] 更新审查日期为 2026-06-10
- [ ] 写入当前 HEAD commit hash
- [ ] 确保每项功能有源码文件:行号证据锚点

## 6. 收尾

- [ ] 更新 `README.md` 中对该文档的引用（如需要）
- [ ] 最终 `git diff --check`
- [ ] 报告摘要
