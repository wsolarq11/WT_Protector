# Test Quality Standards

> 测什么、覆盖预期、质量门禁。测契约不测实现：行为变化时破，重构时不破。

## 分层目录

| 层 | 目录 |
|----|------|
| CoreUnit | `CoreUnit/` |
| Persistence | `Persistence/` |
| ViewModelBehavior | `ViewModelBehavior/` |
| Analyzer | `Analyzer/` |
| Architecture | `Architecture/` |
| UiContract | `UiContract/` |
| PropertyTest | `PropertyTest/` |
| Support | `Support/`（fixture，非测试） |

每测试类带 `[Trait("Category", TestCategories.<Layer>)]`。两类主导：逻辑测试（Core 纯函数输入→输出）与行为测试（VM 命令副作用 + 状态文本）。

## 必测场景

| 场景 | 必测 |
|------|------|
| 新 Core 纯函数 | ✅ |
| 新持久化路径 | ✅ 成功 + 回滚 |
| 新 VM 命令 | ✅ happy + cancel + error |
| 新对话框 | ✅ 返回值（接受/取消/校验） |
| 新 drag-drop | ✅ 决策逻辑 + ghost 布局 |
| XAML-only 样式 | ❌ 视觉 smoke |
| Ctrl+F / 快捷键 | ❌ 逻辑复杂才手测 |

持久化测试：正常 round-trip（`Save`→`Load`）+ 保存失败（快照恢复 entries + settings + presets + custom order + selection）。单文件原子保存无部分失败路径（`File.Replace` 全有或全无）；回滚由 `AppStore.Mutate` 内部 + VM `RestoreVmStateFromStore()`。VM 命令测试：成功 / 取消 / 对话框错误 / 校验失败四路径。

## 行尾契约

持久化 JSON LF：`LauncherJson.NormalizeLineEndings` 在 `Save` 写时强制；`line-ending-policy` 检查 `.gitattributes`（非 `launcher.json` 字节）。

## 门禁

本地（提交前）：`dotnet build ... -c Release --no-restore -warnaserror`、`npm run test:fast`、`dotnet test ... -c Release --no-restore`、`npm run test:coverage`。全量（声称完成前）：`npm run verify`（restore → 全测 → publish → temp-config 冒烟 → coverage → 架构/分析器）。CI：`build-and-test` 跑 `test:fast`（CoreUnit + PropertyTest + Architecture + Analyzer）；`full-gate` 跑 `npm run verify`。E2E 封顶：`npm run smoke` 只断言主窗启动 + `WT Launcher` 标题，始终临时 `--config`。

视觉 token 变更额外证明：`app-raw-hex-zero` 零 raw hex；临时 `#RRGGBB` fixture 被拒；`VisualTokenContractTests` 钉 token 所有权、资源注入、生产消费者、重复 XAML `12/14/16` 字体字面量移除。

主窗动作层级变更：结构契约钉直接可见动作、迁入标记菜单的命令、菜单触发器 automation name、卡片"launch+more"边界；发布 GUI smoke 仍必需。

对话框一致性变更：`DialogConsistencyContractTests` 钉三 token（`DialogTitleFontSize` / `DialogOuterPadding` / `DialogFooterTopMargin`）在 Core 且 `App.axaml.cs` 注入；`Title` 用 token；`CommandManagementWindow.axaml` 消费三 `{StaticResource}`；10 个程序化对话框用 token 外 padding 与 footer 上 margin（无 12/14/16/18/24 字面量）；`ConfirmLaunchWindow` 用 `Footer(0,...)`（父 Grid `RowSpacing=14`）；破坏性对话框 `Cancel(isDefault:true)` + 初始焦点 Cancel，Delete `Destructive(isDefault:false)` 不聚焦。

## 测试计数预期

新增 Core helper +1 测试文件；新增 VM 命令 +1 场景；新增可持久化类型 +1 持久化测试（round-trip + failure）；bug 修复 +1 先失败回归测试。

## Forbidden Patterns

- ❌ 无断言测试（至少一个 `Assert`）。
- ❌ 名不副实的通过测试。
- ❌ `Thread.Sleep()` / `Task.Delay()`；用固定时钟注入。
- ❌ 依赖执行顺序；每测试独立临时目录。
- ❌ `[Fact(Skip=...)]` 累积；修或删。
