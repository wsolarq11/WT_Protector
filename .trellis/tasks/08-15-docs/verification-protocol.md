# 文档证据化验证协议 (2026-08-15)

> 目标：完全重构所有文档，逐项查证，禁止推断。本协议由每个验证代理强制执行。

## 铁律（违反即返工）

1. **每一句事实声明必须有工具证据。** 证据 = Test-Path / Get-ChildItem / Select-String / git log / 读文件。
2. **禁止推断、禁止"合理猜测"补全。** 查不到证据的声明 → 删除，或明确改写为"未验证"并删除具体断言。
3. **历史记录不"纠正"。** 带日期的点状记录（如"当时 716 测试"）是历史事实，保留并确保有日期；不得改成当前值。
4. **结构数字一律引用 AGENTS.md 结构基线**，不得在文档中硬编码可漂移计数；确需写数字时与基线一致。
5. **门禁保护内容一字不差保留**（见各代理提示中的"保护内容"节）。
6. **不要运行构建/测试/门禁**（verify.ps1、dotnet、publish）；只做只读验证。最终门禁由主代理统一跑。
7. 行尾 LF；不碰 AGENTS.md 的 TRELLIS 块；不碰 config/launcher.json。

## 证据工具速查（pwsh 只读）

- 存在性: `Test-Path '<path>'`
- 文件计数（排除构建产物）: `@(Get-ChildItem '<dir>' -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\|\\TestResults\\' }).Count`
- 类型/成员存在: `Select-String -Path '<src>\*.cs' -Pattern 'class <Name>' -Recurse`
- 提交哈希: `git log -1 --format='%h %cs %s' <hash>`（哈希无效则退出非零 = 证据不成立）
- 门禁检查项: `Select-String -Path 'tools/verify.ps1' -Pattern "Add-Check '<name>'"`
- 配置键: 读 config/launcher.json 与 config/launcher.example.json
- 文档自身历史: `git log --oneline --follow -- <doc>`

## 判定词（每个声明必须打标）

- `VERIFIED` — 有工具证据
- `STALE` — 证据与声明矛盾（记录正确事实）
- `UNVERIFIABLE` — 无证据可查 → 删除或标注
- `HISTORICAL` — 带日期的历史记录（保留）

## 交付物（每个文档）

1. **验证台账**: `.trellis/tasks/08-15-docs/ledgers/<slug>.md` — 表格: 声明 | 判定 | 证据(命令+结果) | 处理
2. **重写后的文档**: 直接写回原文件（只保留 VERIFIED/HISTORICAL 内容；STALE 改实；UNVERIFIABLE 删）

## 已知已核实事实（可直接引用，无需重复验证）

- 结构基线: Core 53 / ViewModels 54 / partials 15 / Features 16 / Views 24cs+20axaml / Analyzers 7 / Tests 121 / 测试属性 737 / 门禁 40
- HTA 于 2026-07-06 提交 825ecb5b 移除（retired/ 仅剩 config.json + design md）
- 快照类 LauncherEntryMutationSnapshot/LauncherSelectionMutationSnapshot 已于 2026-07-21 删除；回滚 = StoreGrain.Dispatch save-first-then-commit
- WTLauncher.Core.FSharp 为孤儿项目（fsproj 自标 _FSharpProjectIsOrphaned）
- 测试 8 层: Support/CoreUnit/Persistence/ViewModelBehavior/Analyzer/Architecture/UiContract/PropertyTest
- 门禁实测用例 820（737 属性展开）
