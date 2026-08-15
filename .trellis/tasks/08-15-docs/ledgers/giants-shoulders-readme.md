# 台账：docs/giants-shoulders/README.md（验证日期 2026-08-15）

> 协议：.trellis/tasks/08-15-docs/verification-protocol.md。判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL / EXTERNAL。

| 声明 | 判定 | 证据（命令 + 结果） | 处理 |
| --- | --- | --- | --- |
| 文档无日期 | （格式） | 通读全文无日期字段 | 按协议在头部加 "Verified 2026-08-15" 标注 |
| 门禁命令 `powershell … -File .\tools\verify.ps1` | VERIFIED | `Test-Path tools/verify.ps1` → True | 保留 |
| 活跃权威源 = 当前仓库 + README.md + 原生门禁 | VERIFIED | `Test-Path README.md` → True；verify.ps1 存在 | 保留 |
| 条目链接（两个条目文件） | VERIFIED | `Test-Path docs/giants-shoulders/codexswitch-avalonia-native-aot.md` → True；`Test-Path docs/giants-shoulders/launcher-first-class-patterns.md` → True | 保留 |
| CodexSwitch 条目说明："可能比当前 WinForms/GDI 实现更适合下一代 WT Launcher" | STALE | WinForms 已退役：`Test-Path retired/design/winforms-launcher.md` → True；`Test-Path src/winforms-launcher` → False；`Test-Path 'WT Launcher.exe'` → False；git log aba9bcbf 2026-05-17 "Promote Avalonia launcher as the source mainline"；门禁 retired-paths-removed 强制 | 改写：条目是促成 Avalonia 主线的参考；WinForms 已退役 |
| 启动组条目："12-tool industry survey" | VERIFIED | launcher-first-class-patterns.md 表格 14 行 / 12 条产品教训，注释自述一致 | 保留 |
| 使用方式（git status --short + verify.ps1） | VERIFIED | 命令引用 tools/verify.ps1 存在 | 保留 |

## 处理总结
- VERIFIED：5 项；STALE：1 项（CodexSwitch 条目描述）；UNVERIFIABLE：0
- 重写点：条目表 CodexSwitch 一行改为已验证现实；头部加验证标注。

## 重写
文档已写回 docs/giants-shoulders/README.md。
