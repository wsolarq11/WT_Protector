# 验证台账: retired/design/winforms-launcher.md

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 四个门禁标题均存在 | VERIFIED | read 文件 + 门禁模拟: Contains("Retired WinForms launcher design"/"Portable config discovery"/"Launch safety boundary"/"Successor directive") 全部 True | 保留（锚点未动） |
| Status: retired on 2026-05-17 | VERIFIED | git log --follow: 首提交 aba9bcbf 2026-05-17 | 保留（历史） |
| WT Launcher.exe / src/winforms-launcher/Program.cs 曾存在并已退役 | VERIFIED | git log --all -- WT Launcher.exe / src/winforms-launcher/Program.cs: aba9bcbf, d9e5d3de, a837beb9 均有记录; 门禁 retired-paths-removed 确认不得恢复 | 保留 |
| SHA-256/字节收据（43008/75324） | HISTORICAL | 文件已删除无法重算哈希; 收据为 2026-05-17 退役时点记录 | 保留（历史收据） |
| config/config.json 与 settings.json 曾为便携配置 | VERIFIED | git log --all -- config/config.json, config/settings.json: 3a65fe17, a076ab56, 4be3c8b5 等历史提交存在 | 保留（设计教训, 历史） |
| 未声称 HTA 仍存在 | VERIFIED | Select-String HTA|.hta: 仅第33行 "HTA-derived low-friction UX lessons"（历史教训引用, 非存在声明） | 保留 |
| Successor directive 与现实现一致（Core/App/Tests 分层、artifacts/ 忽略） | VERIFIED | AGENTS.md 5层架构 + CLAUDE.md publish 路径 src/avalonia-launcher/artifacts/ | 保留 |
