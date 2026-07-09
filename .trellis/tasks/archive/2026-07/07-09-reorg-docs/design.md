# Design — reorg docs

## Boundary

- **输入**：`audit-findings.md` D1 八项 + 当前 `HEAD` 仓库状态
- **输出**：8 处文档机械修改 + 可选 `dotnet build -warnaserror` 警告清理；最终落在 1 次提交内
- **不触碰**：`retired/`、`config/launcher.json`、`src/**/*.cs` 行为、`src/**/*.axaml`、`tools/*.ps1`（除可能的机械清理）

## Edit strategy per item

### Item 1–4：AGENTS.md 计数 + 检查名

**风险**：tree 字符画对齐 + 表格列宽。

**策略**：
- 用 `Read` 取 AGENTS.md 完整内容
- 用 `Edit` 改具体行（不用 `replace_all`，避免误伤）
- 改完后 `git diff AGENTS.md` 自查：确认只动了目标字符，tree 字符画（`│` `├──` `└──`）与表格 `|` 列宽未被破坏

**具体改动**：
- `:18` `26文件` → `24文件`
- `:20` `34文件, 单 MainWindowViewModel + 9 partial` → `28文件, 单 MainWindowViewModel + 7 partial`
- `:21` `15文件` → `13文件`
- `:39` `16 项检查（...）` → `18 项检查（...）`，括号内补全当前 18 项的关键分类

### Item 5：README.md "verifier currently checks"

**风险**：列表项与实际 verify.ps1 检查脱钩导致未来再漂移。

**策略**（推荐方案 A，备选 B）：
- **方案 A（推荐）**：把 `:138-150` 的 bullet 列表替换为单行引用："The verifier currently enforces 18 checks — see the `Add-Check` calls in `tools/verify.ps1` for the canonical list. Summary: line-ending policy, retired paths, mainline layout, README/schema/claim-boundary integrity, GitHub snapshot freshness, Avalonia restore/test/publish, GUI smoke (Windows)."
  - 优点：单一权威源是 `verify.ps1`，未来增减检查自动同步
  - 缺点：丢失当前"每个 bullet 一行解释"的可读性
- **方案 B（备选）**：保留 bullet 列表，按 `verify.ps1` 当前 18 项重写
  - 优点：可读性高
  - 缺点：下次 verify.ps1 加减检查还会漂移

**决策**：用方案 A。若用户反对再回退到 B。

### Item 6：`.trellis/spec/tests/index.md`

**策略**：
- `:9` `71 test files (approximately 410 [Fact]/[Theory] attributes)` → `61 test files (~N [Fact]/[Theory] attributes)`
- `N` 用 `grep -cE "\[(Fact|Theory)\]"` 在 `src/avalonia-launcher/tests/WTLauncher.Tests/*.cs` 取实际值后填入

### Item 7：归档 PRD `composition-di-async-evaluation/prd.md:44`

**策略**：
- `:44` `MainWindowViewModel 8 partials total **2305 lines** (main 333 + Drag 151 + ...)` → `MainWindowViewModel 1 main + 7 partials (8 files) total **2305 lines** (main 333 + Drag 151 + ...)`
- 2305 行总数与分项加总都用 `wc -l` 重新核实，确认仍精确匹配（已在上一步核实）

### Item 8：7 处 phantom 引用

**策略**：
- `Grep` 全仓 `feedback_audit_driven_proposals` 拿到完整文件清单（已知 7 处）
- 每处把引用替换为等价通用表述：
  - 原：`严守 .trellis/spec/guides/feedback_audit_driven_proposals_verify_each_source.md：每条提案都是假设，必须用当前代码/文档验证后才决定动作`
  - 新：`严守审计准则：每条提案都是假设，必须用当前代码/文档验证后才决定动作`
- 已归档 PRD 的引用同样改（归档不等于只读；措辞修正属于事实订正）

### Item 9：dotnet build 警告核实

**策略**：
- `dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror 2>&1 | tee /tmp/build.log`
- 若零警告：跳过
- 若有警告：分析每条
  - CS0168/CS0219/CS8019（未用 using/变量）→ 删
  - CS0618（obsolete API）→ **停下升级为单独子任务**
  - 其他 → 按 Ponytail ultra 判断：机械清/停下

## Data flow

```
audit-findings.md (D1 八项)
        │
        ├─ Edit AGENTS.md (4 处)
        ├─ Edit README.md (1 处, 方案 A)
        ├─ Edit .trellis/spec/tests/index.md (1 处 + 核实)
        ├─ Edit 归档 PRD composition-di-async-evaluation (1 处)
        ├─ Edit 7 处 phantom 引用 (7 文件)
        └─ dotnet build -warnaserror (条件触发)
              │
              ▼
        git diff 自查 + verify.ps1 全绿
              │
              ▼
        一次提交 (D1 八项 + 可选 build 清理)
```

## Tradeoffs

- **方案 A vs B（README）**：方案 A 牺牲当前可读性换未来零漂移。Ponytail ultra 倾向 A——单一权威源是真省事。若用户认为 README 应保持"human-readable 长列表"，回退到 B。
- **改归档 PRD**：归档目录在理论上应是只读，但 phantom 引用是事实错误，留着会误导未来读者。修正措辞而不重写历史是正确动作。
- **不补建 phantom 文件**：补建 `feedback_audit_driven_proposals_verify_each_source.md` 等于为了 7 处引用造一份新文档，反而增加维护面。删除引用是更懒的正确做法。

## Compatibility / rollback

- 全部为文档与可选机械代码清理
- Rollback：`git revert <commit>` 单次提交回退
- 不影响 `tools/verify.ps1` 检查逻辑（除非 Item 9 发现的警告涉及 verify.ps1 自身，但 verify.ps1 是 PowerShell 不是 C#，不会被 dotnet build 检查）

## Open questions

- README 方案 A vs B：design 中决策用 A，但留作 1.4 review gate 时用户可推翻
