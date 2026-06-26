# Implement — UltraReview 2026-06-26 全部发现清缴

## Execution Order

修复按依赖关系排序：Core 库 → ViewModels → Views → Persistence → 配置 → 文档 → 测试。每个子任务完成后运行测试验证无回归。

### Phase A: 子任务执行（可部分并行）

#### A1. core-fixes (独立，无上游依赖)
- [ ] 修复代理对截断 off-by-one (LauncherEntryIds.cs:25-27, LauncherPresetIds.cs:44-45)
- [ ] 统一 Entry/Preset ID 规范化为 FormKC (LauncherEntryIds.cs:18)
- [ ] 修复排序时冗余 DateTime 解析 — 预解析到投影数组 (LauncherEntryVisibility.cs:124)
- [ ] 修复 FilterAndSort 空 filter 时跳过 ToList() (LauncherEntryVisibility.cs:42)
- [ ] 补齐 Ghost 布局计算器窗口限界 (LauncherDragGhostLayoutCalculator.cs)
- [ ] 修复 Entry/Preset ID 256 字符 ZWJ 截断注释 (LauncherEntryIds.cs)
- [ ] `dotnet test --filter "FullyQualifiedName~WTLauncher.Tests.LauncherEntryIdsTests|WTLauncher.Tests.LauncherPresetIdsTests|WTLauncher.Tests.LauncherEntryVisibilityTests"`

#### A2. view-fixes (独立，无上游依赖)
- [ ] 修复 FastWrapPanel NaN 守卫 (FastWrapPanel.cs:40)
- [ ] 修复键盘焦点 visibleCount==0 越界 (MainWindow.axaml.cs:152-209)
- [ ] 修复 Ghost 创建失败静默 — 加 ErrorLog 警告 (MainWindow.Drag.cs:208-210)
- [ ] 修复 SizeChanged/Opened/KeyDown 在 OnClosing 中解注册 (MainWindow.axaml.cs)
- [ ] 修复 Details ScrollViewer null 回退加 ErrorLog (MainWindow.axaml.cs:283-291)
- [ ] 修复滚动恢复版本计数器溢出防护 (MainWindow.axaml.cs:244)
- [ ] `dotnet test --filter "FullyQualifiedName~WTLauncher.Tests.MainWindow"`

#### A3. viewmodel-fixes (依赖 A1 通过)
- [ ] 修复 LaunchAsync 先 SaveConfig 再 RecordLaunch (MainWindowViewModel.Launch.cs:90-94)
- [ ] 添加单次启动防重复点击守卫 (MainWindowViewModel.Launch.cs:76-111)
- [ ] 修复 ApplyCardSize 同时更新所有条目（可见+不可见）(LauncherEntryLayoutPresentation.cs:40)
- [ ] 修复 SelectVisible 加 CanExecute 守卫 (MainWindowViewModel.Selection.cs:18)
- [ ] 修复 CSS 类重叠 — IsCompactLayout 在 Micro 时为 false (LauncherEntryViewModel.Layout.cs:16)
- [ ] 修复 FailureIndex 命名一致性问题 (LauncherBatchLaunchReport.cs)
- [ ] 补齐 _filterText 旁路文档注释 (MainWindowViewModel.cs:88)
- [ ] 修复快照 Entries getter 缓存避免重复深拷贝 (LauncherEntryMutationSnapshot.cs:24)
- [ ] `dotnet test --filter "FullyQualifiedName~WTLauncher.Tests.MainWindowViewModelTests|WTLauncher.Tests.LauncherEntry"`

#### A4. persistence-fixes (独立)
- [ ] 扩展 CleanupStaleTempFiles 清理 .stage-*/.bak-tmp-*/.recovery-* (LauncherJson.cs:107-132)
- [ ] 在 Load 恢复路径中加 ErrorLog.Write 记录原始损坏 (LauncherConfigStore.cs, LauncherPresetStore.cs, LauncherSettingsStore.cs)
- [ ] 在持久化 catch 块中加 PathTooLongException (LauncherEntryMutationPersistence.cs, LauncherJson.cs)
- [ ] 移除 LauncherJson.Save 中冗余的 FileStream Open+Flush (LauncherJson.cs:53-56)
- [ ] 加 Preset 添加时数量上限检查 — 在 AddPreset 入口处 (LauncherPresetWorkflow.cs:7-30)
- [ ] 为 presets.json/settings.json 加预反序列化文件大小检查
- [ ] `dotnet test --filter "FullyQualifiedName~WTLauncher.Tests.LauncherConfigStoreTests|WTLauncher.Tests.LauncherEntryMutationPersistenceTests|WTLauncher.Tests.LauncherPreset"`

#### A5. config-fixes (独立)
- [ ] 删除或改为 `<NuGetAudit>true</NuGetAudit>` (Directory.Build.props:6)
- [ ] 验证并修正 NuGet 包版本 (Directory.Packages.props)
- [ ] 修正 config.json 中硬编码路径 — 改为占位符或文档说明
- [ ] `dotnet restore && dotnet build --no-restore`

#### A6. doc-fixes (独立)
- [ ] README.md: 修正 "拒绝未知键" → "未知键被静默跳过（向前兼容）"
- [ ] drag-drop.md: 修正 `Border.drag-insert` → `drag-target`
- [ ] pitfalls.md: 修正 M10 搜索历史"已删除"为当前状态
- [ ] DESIGN.md: 确认 8/13/21 节奏或移除 21 引用

#### A7. test-fixes (依赖 A1-A6 通过)
- [ ] 删除脆弱测试: MainWindowXamlContractTests（5 项）, DialogLayoutContractTests, RepositoryConfigContractTests.FeatureAuditTests, DesignDigestTests
- [ ] 重构反射测试为行为断言（3 项）
- [ ] 新增 14 项测试覆盖缺口
- [ ] `dotnet test` 全量通过

### Phase B: 集成验证

- [ ] `dotnet test` 全量通过（零失败、零跳过）
- [ ] `tools/verify.ps1` PASS
- [ ] 无新增编译 warning
- [ ] 置信度评估 → 100%

## Validation Commands

```powershell
# 每个子任务完成后
dotnet test --no-restore

# Phase B 最终门禁
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## Risky Files

- `LauncherEntryIds.cs:18` — FormKC 变更可能影响已有 ID
- `Directory.Build.props:6` — NuGetAudit 开启可能暴露包漏洞
- `Directory.Packages.props` — 包版本修正可能导致 restore 失败
