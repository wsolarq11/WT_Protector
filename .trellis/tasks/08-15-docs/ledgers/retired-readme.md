# 验证台账: retired/README.md

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 目录为退役归档（archive-only, never restore） | VERIFIED | git status/AGENTS.md 仓库地图: retired/ 为退役归档; 门禁 retired-paths-removed 禁止恢复 | 保留 |
| `config/config.json` — HTA-local config 存在 | VERIFIED | Test-Path retired/config/config.json -> True | 保留（路径改为完整 retired/config/config.json 以更精确） |
| HTA launcher 快照于 2026-07-06 (commit 825ecb5b) 移除 | VERIFIED | git log -1 825ecb5b -> 2026-07-06; git cat-file: 父提交含 retired/WT Launcher.hta (True), 该提交不含 (False) — 确被删除 | 保留 |
| config.json 是仅剩的 HTA 时代产物 | VERIFIED | Get-ChildItem retired -Recurse: 仅 README.md + config/config.json + design/winforms-launcher.md（后者为 WinForms 时代） | 保留 |
| design/winforms-launcher.md 存在且为退役 WinForms 设计 | VERIFIED | Test-Path retired/design/winforms-launcher.md -> True; 内含 SHA-256 收据 | 保留 |
| retired/config/config.json 非活动配置、非后备 | VERIFIED | 门禁 hta-retired-config 仅校验其静态存在与哈希; 活动配置为 config/launcher.json | 保留 |
| 活动配置为仓库根 config/launcher.json（entries+presets+settings+defaultPresetId 原子单元） | VERIFIED | 读取 config/launcher.json: keys = schemaVersion, settings, entries, presets, defaultPresetId, runHistory | 保留 |
