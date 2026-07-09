# 修门禁 verify.ps1 三文件→单文件

## Goal

修复项目主门禁 `tools/verify.ps1`:它当前在干净的 main 分支也失败,因为 L275-419 检查 config.json + presets.json + settings.json 三文件,但项目已迁移到 launcher.json 单文件。这是后续所有 child 验证的前置条件。

## Background

stash 后在干净 main 跑 verify.ps1 复现:`Missing config: config/config.json` at config-lib.ps1:84。

**根因**:commit `3c394b3 refactor: single-file store architecture — 3 JSON files → 1` 把三文件合并为 launcher.json,但 verify.ps1 + config-lib.ps1 没同步更新。这是三文件→单文件重构的遗漏。

## Requirements

### R1: config-lib.ps1 简化
- 删 `Get-LauncherConfigPath`(L71-73,过时)
- 删 `Get-LauncherSettingsPath`(L75-77,过时)
- 保留 `Get-LauncherStorePath`(L67-69,正确,返回 launcher.json)
- `Read-ConfigItems` 函数(L83-99)保留,但调用方式改变(由 verify.ps1 传入从 launcher.json 提取的 entries)

### R2: verify.ps1 三文件段(L275-419)重写
- 读单个 launcher.json
- 从中提取 entries(等价旧 config.json)、presets、settings、defaultPresetId
- 保持原有验证逻辑(字段必填、id 唯一、selected 是 bool、preset 引用合法 entryId、commandTemplates 校验等)
- 保持 `$seenIds`、`$configItems`、`$settings` 等变量名,减少下游代码改动

### R3: 接受 launcher.json schema
- 顶层 schemaVersion=2(不是 1)
- settings.schemaVersion=2
- presets 段无独立 schemaVersion
- settings 字段:lastFilterText、sortMode、entryViewMode、requireConfirmationForLaunches、customEntryOrder、commandTemplates、searchHistory、window
- entries 字段:id、name、directory、command、selected、updatedAtUtc、launchedAtUtc

### R4: preset schemaVersion 检查放宽
- 当前 verify.ps1 L321 `if ([int]$presetDoc.schemaVersion -ne 1)` —— 这是 presets.json 时代的检查
- 改为:launcher.json 顶层 schemaVersion=2,presets 段无独立 schemaVersion

### R5: 不破坏现有 avalonia mainline verify
- L455-480 调用 `src/avalonia-launcher/verify.ps1`(独立的子 verify 脚本)—— 这个不动
- 只动根 verify.ps1 和 config-lib.ps1

## Acceptance Criteria

- [ ] `tools/verify.ps1` 在干净 main + 我的改动上 PASS(最终 receipt `status: PASS`)
- [ ] 不引入新的"忽略检查"——所有原有检查项保留逻辑,只换数据源
- [ ] config-lib.ps1 不再导出 `Get-LauncherConfigPath` / `Get-LauncherSettingsPath`(grep 返回空)
- [ ] receipt 的 `checks` 数组保留所有原有检查名(json、config-schema、presets-schema、settings-json、shortcut-backup-retirement 等)
- [ ] 如果 launcher.json 缺字段或字段非法,verify.ps1 仍然 throw(不能为了通过而放松校验)

## Notes

- 不动 `src/avalonia-launcher/verify.ps1`(那是 Avalonia 项目自己的 verify,独立)
- 不动 README 中关于 verify.ps1 检查项的描述(已经准确)
- 单次 commit 完成
