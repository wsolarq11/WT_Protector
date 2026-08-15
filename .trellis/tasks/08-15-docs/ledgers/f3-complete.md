# 验证台账: docs/archive/f3-complete.md

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 文档日期 | MISSING→HISTORICAL | 内容无日期; git log --follow 首提交 dd01f185 2026-07-23 | 添加 provenance 头（2026-07-23） |
| EntryId.cs / LauncherEntryIdsTests.cs 引用 | VERIFIED | git cat-file -e dd01f185:<path> 全部 True; 现行存在（EntryId.cs 在 Core/, 测试在 CoreUnit/） | 保留 |
| LatinScriptRanges / EnsureLatinScript 实现 | VERIFIED | Select-String EntryId.cs: 行31 LatinScriptRanges, 行148 EnsureLatinScript | 保留 |
| 677 测试通过, 0 警告 | HISTORICAL | 点状测试数（2026-07-23） | 保留（历史, 已有日期） |
