# Implement — reorg docs

## Ordered checklist

### Phase A — 准备

- [ ] A1 `git rev-parse HEAD` 记录基线
- [ ] A2 `git status` 确认 working tree 状态（audit-pass 归档后的状态）
- [ ] A3 Read `audit-findings.md` D1 八项，确认理解

### Phase B — AGENTS.md 计数修正（Item 1–4）

- [ ] B1 Read `AGENTS.md` 完整内容
- [ ] B2 Edit `:18` `26文件` → `24文件`
- [ ] B3 Edit `:20` `34文件, 单 MainWindowViewModel + 9 partial` → `28文件, 单 MainWindowViewModel + 7 partial`
- [ ] B4 Edit `:21` `15文件` → `13文件`
- [ ] B5 Edit `:39` 表格行：`16 项检查` → `18 项检查`，括号内补全 18 项关键分类（含 `no-stale-three-file-refs`、`settings-single-source-buildstorefile`、`settings-json`）
- [ ] B6 `git diff AGENTS.md` 自查：tree 字符画 / 表格列宽未被破坏

### Phase C — README.md verifier 列表（Item 5）

- [ ] C1 Read `README.md:130-160` 区域
- [ ] C2 Edit `:138-150` 列表 → 方案 A（引用 `tools/verify.ps1` Add-Check 清单 + 18 项 summary）
- [ ] C3 `git diff README.md` 自查

### Phase D — tests/index.md（Item 6）

- [ ] D1 `grep -cE "\[(Fact|Theory)\]" src/avalonia-launcher/tests/WTLauncher.Tests/*.cs` 取实际 `[Fact]/[Theory]` 计数
- [ ] D2 Read `.trellis/spec/tests/index.md:1-15`
- [ ] D3 Edit `:9` `71 test files (approximately 410 [Fact]/[Theory])` → `61 test files (~N [Fact]/[Theory])`，N 为 D1 实测值
- [ ] D4 `git diff .trellis/spec/tests/index.md` 自查

### Phase E — 归档 PRD 措辞修正（Item 7）

- [ ] E1 Read `.trellis/tasks/archive/2026-07/07-09-composition-di-async-evaluation/prd.md:40-50`
- [ ] E2 Edit `:44` `8 partials total` → `1 main + 7 partials (8 files) total`
- [ ] E3 `git diff` 自查

### Phase F — Phantom 引用清理（Item 8）

- [ ] F1 `Grep "feedback_audit_driven_proposals"` 取完整文件清单（7 处）
- [ ] F2 对每个文件：Read → Edit 替换 phantom 路径为通用表述
  - 父任务 `07-09-project-wide-reorg/prd.md`
  - `07-09-reorg-audit-pass/prd.md`（已归档）
  - `07-09-reorg-audit-pass/design.md`（已归档）
  - `07-09-reorg-audit-pass/implement.md`（已归档）
  - `07-09-composition-di-async-evaluation/prd.md`（已归档）
  - `07-09-rename-evaluation/prd.md`（已归档）
  - `07-09-trivial-cleanups/prd.md`（已归档）
  - `07-09-07-09-residual-backlog-clearance/prd.md`（已归档）
- [ ] F3 `Grep "feedback_audit_driven_proposals"` 确认全仓零匹配
- [ ] F4 `git diff` 自查所有改动

### Phase G — dotnet build 警告核实（Item 9）

- [ ] G1 `dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror 2>&1 | tee build.log`
- [ ] G2 若零警告：标记 G1 完成，跳到 Phase H
- [ ] G3 若有警告：逐条分类
  - CS0168/CS0219/CS8019 → 删未用 using / 变量
  - 其他 → **停下**，升级为单独子任务（不在本轮处置）
- [ ] G4 改完后再跑一次 `dotnet build -warnaserror`，确认零警告

### Phase H — 全门禁自查

- [ ] H1 `git diff --stat retired/` 应为空
- [ ] H2 `git diff src/` 若非空，确认全部为机械清理（删未用 using 等），无行为变更
- [ ] H3 `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release` 全过
- [ ] H4 `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` 全绿（18/18）
- [ ] H5 AGENTS.md / README.md 视觉自查：表格、tree 字符画、链接完整性

### Phase I — Review gate

- [ ] I1 acceptance criteria 逐项核对（含 `git diff src/`、`retired/` 字节未变等）
- [ ] I2 把 `git diff --stat` 输出回报父任务，由父任务 owner 决定是否进入最终集成评审

## Validation commands

```bash
# 文档改动核查
git diff --stat
# 期望：AGENTS.md / README.md / .trellis/spec/tests/index.md / 多份归档 PRD / 父子任务 PRD
# 不期望：retired/ 任何文件、config/launcher.json、src/**/*.cs 行为变更

# Phantom 引用清零
grep -rn "feedback_audit_driven_proposals" .
# 期望：无匹配

# 编译零警告
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror

# 测试全过
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release

# 全门禁
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## Review gates

- Phase B6 / C3 / D4 / E3 / F4：每个文件改完立刻 `git diff` 自查，避免 Edit 牵动相邻行
- Phase G3：dotnet build 警告若涉及非机械项，**强制停下**升级子任务
- Phase H4：verify.ps1 必须全绿才能进 I

## Rollback points

- 任何 Phase 中若 verify.ps1 红：立即 `git checkout -- <file>` 还原该文件
- 若 phantom 引用清理误删了"审计准则"语义：恢复"严守审计准则：每条提案都是假设..."的通用表述
- README 方案 A 若用户反对：Phase C 改用方案 B（保留 bullet 列表）

## Expected output

- 1 次提交，含 8 文档机械修改 + 可选 src 机械清理
- `git diff --stat` 预期 ~10 文件，~30 行净改动（不计 phantom 引用清理的批量替换）
