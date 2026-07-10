# Implement — 重新构建发布产物并按需求矩阵全量验证

## 执行顺序与 review gate

> Review gate 在每步结束时自检；不通过不进下一步。

### Step 0: 预检（避免后期红）

- [ ] `git status` 干净
- [ ] `git rev-parse HEAD` 记录 commit hash
- [ ] `Get-Process WTLauncher.App -ErrorAction SilentlyContinue` 无输出（若有进程，kill）
- [ ] 读 `config/launcher.json` 每个 entry 的 `directory`，在本机 `Test-Path` 全部存在；缺一即修
- [ ] skim `.trellis/spec/guides/pitfalls.md` §1（工具层坑）+ §3（NuGet/test 层）

**Review gate 0**：以上全 ✅，否则停在预检。

---

### Step 1 (Stage A): 重新构建发布产物

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

- [ ] 输出含 `receipt.json` JSON
- [ ] `publishedExe` 指向 `src\avalonia-launcher\artifacts\publish\win-x64\WTLauncher.App.exe`
- [ ] `bytes`/`sha256`/`builtAt` 字段非空
- [ ] 记录到 report Stage A 段

**Review gate 1**：build 成功且 receipt 字段完整。
**Rollback point**：build 失败 → 进 Step 5 (Stage E)。

---

### Step 2 (Stage B): 全量 gate

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

- [ ] 整体末行 JSON `status: PASS`
- [ ] `checks` 数组 18 项（root verify 顶层 Add-Check 数）
- [ ] `avalonia-mainline-verify` 段 `status: PASS`、`tests` 段全 PASS、`guiSmoke: PASS`（Windows）
- [ ] 抓取 xUnit 测试统计（通过/失败/跳过）→ report Stage B

如果需要测试详细输出：

```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --logger "console;verbosity=normal"
```

**Review gate 2**：status=PASS 全绿。
**Rollback point**：任意 check 红 → 进 Step 5 (Stage E)。

---

### Step 3 (Stage C): 21-feature 实机矩阵

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

启动后按 `design.md` 的 21-feature 表逐项操作。每条立即在 report 草稿记录：

- # 实际操作步骤简述
- 实际观察
- 判定 ✅/⚠️/❌
- 若 ❌ → 进 Step 5 (Stage E)

操作完成后关 app，确认 `config/launcher.json` 被合法持久化（手动 diff 一次）。

**Review gate 3**：21 条都有结论；任何 ❌ 都已进 Step 5。
**Rollback point**：操作意外改坏了 launcher.json → `git checkout -- config/launcher.json`。

---

### Step 4 (Stage D): 社区佐证（可后台并行 Step 1-3）

WebFetch + WebSearch 调研，按 `design.md` 的来源与判定阈值表：

- [ ] D.1 GitHub Releases 页（WebFetch `https://github.com/<USER>/WT_Protector/releases`）
- [ ] D.2 Avalonia 12.1 release notes + 已知 issues（WebSearch）
- [ ] D.3 coverlet 10 / Test.Sdk 18.7（WebSearch）
- [ ] D.4 12 个相似项目近 30 天动态（WebFetch snapshot.json 里各 repo）

每条摘要 ≤ 5 行，给 citation URL。

**Review gate 4**：4 个子项都有结论 + URL。

---

### Step 5 (Stage E): 失败根因（仅条件触发）

**触发条件**：Step 1/2/3/4 任一非全绿。

按 `design.md` 的诊断决策树：

- [ ] 写下症状 + 复现命令
- [ ] 定位 文件:行号
- [ ] 决议：fix（独立 commit）/ defer-with-trigger（写进 `.trellis/spec/`）/ 已知 wontfix
- [ ] 若 fix → 回 Step 1 重跑

**Review gate 5**：根因 + 决议都入 report Stage E。

---

### Step 6 (Stage F): 撰写最终报告

文件：`.trellis/tasks/07-10-release-rebuild-matrix-verify/release-validation-report.md`

按 `design.md` 的报告 schema 写完整。

- [ ] TL;DR 含 PASS/PARTIAL/FAIL
- [ ] Stage A/B/C/D/E 各段填齐
- [ ] AC1-AC9 逐项 ✅/❌
- [ ] 引用清单 commit/URL/文件:行号

**Review gate 6**：报告结构齐全，每条判定都有 evidence。

---

### Step 7: 收尾验证

- [ ] 再跑一次 `tools/verify.ps1` 确认 AC9（独立 PASS，非侥幸）
- [ ] `git status` 干净（或仅有新增的 .trellis/tasks/07-10-... 文档）
- [ ] `git diff retired/` 字节级为空
- [ ] 在 `.trellis/workspace/<USER>/journal-N.md` 追加 session 记录
- [ ] （用户决定是否 commit）

---

## 验证命令快速参考

| 命令 | 用途 |
|------|------|
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1` | Stage A |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` | Stage B / AC9 |
| `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release` | 测试详细输出 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1` | Stage C |
| `git -C . rev-parse HEAD` | 拿 commit hash |
| `git -C . status --porcelain` | 干净度检查 |

## Review gate 标准汇总

| Gate | 通过条件 |
|------|---------|
| 0 | 预检 5 项全 ✅ |
| 1 | build receipt 字段完整 |
| 2 | verify status=PASS，18 checks 全绿，测试全过 |
| 3 | 21-feature 矩阵全有结论 |
| 4 | D.1-D.4 4 项调研全有 URL+摘要 |
| 5 | 触发时：根因 + 决议入报告 |
| 6 | 报告 schema 齐全，AC1-AC9 逐项核对 |
| 7 | verify 二次 PASS + working tree 干净 + retired/ 字节不变 |
