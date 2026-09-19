Status: Active
Purpose: 全仓显隐分支证据矩阵，逐条区分实测确证与源码静态推断。
Update when: 门禁步骤、CI 流程、Trellis 更新逻辑、配置 schema 或关键分支证据变化时。

# 分支证据矩阵

本表数字是本次实测快照，不是结构基线的权威源；结构/门禁计数的唯一权威仍是 `AGENTS.md` 结构基线表。

## 判定口径

- **实测确证**：本 session 实际执行命令，记录到真实退出码或 receipt 数值。
- **源码静态推断**：仅从源码/配置可证明，本 session 未执行到该路径或未注入违规样本。
- **远端不可复现**：本机缺 .NET 10 SDK、GitHub 认证或 GitHub Actions 环境，只能在远端观测。

## 门禁层：45 步

本 session 实测 `npm run verify`（`CI=true` 与默认各一次）与 `npm test`。默认运行 receipt：`status=PASS`，`failures=0`，`advisory=1`，`skipped=5`，`checks=49`。

### Static：38 步

| # | 步骤 | 本次实测结果 | 判定 | 关键证据/缺口 |
| --- | --- | --- | --- | --- |
| 1 | line-ending-policy | PASS | 实测确证（通过路径） | 只读 `.gitattributes` 标记；违规分支未注入 |
| 2 | snow-ignored-local-state | PASS | 实测确证（通过路径） | `git ls-files -- .snow` 返回 0；无 tracked 分支实测 |
| 3 | retired-paths-removed | PASS | 实测确证（通过路径） | 4 个退役路径均不存在；出现路径的失败分支未实测 |
| 4 | no-bare-catch-exception | PASS，scannedFiles=168 | 实测确证（通过路径）+ 内建正/反自检 | 违规 `catch(Exception)` 失败分支未注入 |
| 5 | app-raw-hex-zero | PASS，scannedFiles=138 | 实测确证（通过路径）+ 内建正/反自检 | 裸色值失败分支未注入 |
| 6 | mainline-layout | PASS | 实测确证（通过路径） | 只验证 4 个路径存在 |
| 7 | readme | PASS | 实测确证（通过路径） | 6 标题 + 4 边界短语存在；缺失分支未注入 |
| 8 | github-snapshot-shape | PASS | 实测确证（通过路径） | 12 repo、verdict 计数、stars 下限全部实测通过；字段错误分支未注入 |
| 9 | launcher-store | PASS，items=11，presets=2 | 实测确证（通过路径） | 唯一 id、preset 引用、customEntryOrder 全量、template 引用均通过；重复/悬空分支未注入 |
| 10 | config-example-schema | PASS | 实测确证（通过路径） | example 零 entry、blank-manual locked、无绝对路径 |
| 11 | shortcut-backup-retirement | PASS，shortcuts=14，converted=2 | 实测确证（通过路径） | 依赖 launcher-store 的 id 集合；缺失转换 id 分支未注入 |
| 12 | config-required-fields | PASS，totalRequired=26 | 实测确证（通过路径） | 三份 config 均 ok；缺失字段分支未注入 |
| 13 | winforms-retired-design | PASS | 实测确证（通过路径） | 4 个标记存在 |
| 14 | hta-retired-config | PASS，items=2 | 实测确证（通过路径） | 只读退役配置；缺失字段分支未注入 |
| 15 | portable-installer-contract | PASS | 实测确证（通过路径） | 4 个标记存在 |
| 16 | update-manifest-generation | PASS | 实测确证（通过路径） | 只查 `update-manifest.json` 字符串存在 |
| 17 | test-structure-layering | PASS | 实测确证（通过路径） | 8 层目录均非空；缺层/空层分支未注入 |
| 18 | ci-fast-lane-no-publish-duplication | PASS | 实测确证（通过路径） | 只查 `npm run test:fast` 与禁止 publish 字符串；改名假绿是已知名字耦合 |
| 19 | aot-toolchain-owned-by-sdk | PASS + vitest | 实测确证（通过路径）+ 单测 | `root-gate.test.ts` 有 `not.toThrow()`；显式引用失败分支未实测 |
| 20 | no-stale-three-file-refs | PASS，scannedDocs=6 | 实测确证（通过路径）+ 内建正/反自检 | 旧三文件引用失败分支未注入 |
| 21 | task-ledger-citations-resolve | PASS，scannedDocs=17，citations=0 | 实测确证（扫描路径）+ 源码静态推断（引用=0 无法证明解析成功） | 当前无台账引用，解析成功分支未被真实触发 |
| 22 | no-hardcoded-gate-counts | PASS | 实测确证（通过路径）+ 5 模式自检 | 违规计数失败分支未注入 |
| 23 | settings-single-source | PASS，signatureCount=1 | 实测确证（通过路径） | 正则精确命中唯一装配点；若正则失效会假红而非假绿 |
| 24 | winget-config-schema | PASS | 实测确证（通过路径） | 4 个标记存在 |
| 25 | no-imperative-view-ui | PASS，viewsScanned=2 | 源码静态推断（近乎空转） | 排除全部 `.axaml.cs` 且白名单含不存在文件；实际仅扫 DialogService.cs/Panels.cs，未覆盖 DiscoverAgentsWindow.axaml.cs 的命令式 UI |
| 26 | dialog-service-no-func-action | PASS，scannedFiles=1 | 实测确证（通过路径）+ 内建自检 | 目录确只有 IDialogService.cs；Func/Action 失败分支未注入 |
| 27 | views-no-direct-core-business-calls | PASS，scannedFiles=24 | 实测确证（通过路径）+ 内建自检 | 24 个 View .cs 全部扫到 |
| 28 | security-audit-f1 | PASS | 源码静态推断 | 只查 `Path.GetFullPath(` 存在，不验证行为；存在性假绿风险低但无行为证明 |
| 29 | security-audit-f2 | PASS | 源码静态推断 | 只查 `knownEntryIds` 字符串存在，不验证调用链 |
| 30 | security-audit-f3 | PASS | 源码静态推断 | 只查 `LatinScript` 字符串存在，不验证 Parse 行为 |
| 31 | no-thin-wrapper-delegation | PASS，scannedFiles=196 | 实测确证（通过路径）+ 内建自检 | 196 个 .cs 全部扫到；违规分支未注入 |
| 32 | trinity-architecture-integrity | PASS | 实测确证（通过路径） | 4 组件存在；缺失分支未注入 |
| 33 | vtk-no-appstore-residue | PASS，scannedFiles=196 | 实测确证（通过路径）+ 内建自检 | 全 src 无 AppStore；违规分支未注入 |
| 34 | wiring-map-consistency | PASS，workflows=8 | 实测确证（通过路径） | 8 workflow + 8 port 名称均在；缺失分支未注入 |
| 35 | main-vm-line-budget | PASS，lines=1197/1200 | 实测确证（临界值） | 距预算仅 3 行，是本次发现的最脆弱可见分支 |
| 36 | workflow-no-callback-bag | PASS，scannedFiles=8 | 实测确证（通过路径）+ 内建自检 | 8 workflow 构造器均扫到；回调袋失败分支未注入 |
| 37 | no-active-powershell-scripts | PASS，scannedFiles=3016 | 实测确证（通过路径） | 全仓 3016 文件无 active .ps1；命中分支未注入 |
| 38 | structure-baseline-consistency | PASS | 实测确证（通过路径） | 8 行基线、765 属性、45 步全部一致；漂移分支未注入 |

### Local：1 步

| 步骤 | 默认实测 | CI=true 实测 | 判定 |
| --- | --- | --- | --- |
| local-entry-directories | PASS（11 entry 目录均存在） | SKIP（CI 覆盖 launcher.ci.json） | 实测确证两条环境分支 |

### Native：5 步

本机 `dotnet --list-sdks` 只有 `8.0.424`，而 `global.json` 钉 `10.0.301`；`detectDotnet10Sdk` 返回 null，5 步全部显式跳过。

| 步骤 | 本次结果 | 判定 |
| --- | --- | --- |
| release-lockfile-consistency | SKIP（requires .NET 10 SDK） | 远端不可复现 |
| avalonia-mainline-verify | SKIP | 远端不可复现 |
| coverage-gate | SKIP | 远端不可复现 |
| architecture-rule-check | SKIP | 远端不可复现 |
| analyzer-wiring | SKIP | 远端不可复现 |

### Snapshot：1 步

| 步骤 | 本次结果 | 判定 |
| --- | --- | --- |
| github-snapshot-freshness | ADVISORY（35 天 > 30 天） | 实测确证过期分支；刷新分支因 `gh auth login`/`GH_TOKEN` 未设置而远端不可复现 |

## npm 脚本入口实测

| 命令 | 退出码/结果 | 判定 |
| --- | --- | --- |
| npm test | 33/33 PASS | 实测确证 |
| npm run typecheck | PASS | 实测确证 |
| npm run verify | PASS，advisory 1 | 实测确证 |
| npm run verify:mainline | EXIT=1，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run test:fast | EXIT=-2147450725，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run test:coverage | EXIT=1，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run test:launch-args | EXIT=1，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run smoke | EXIT=1，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run apply:config:dry | EXIT=1，winget 不存在 | 实测确证失败路径（缺 winget） |
| npm run release:build | EXIT=1，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run release:contract | EXIT=1，SDK 解析失败 | 实测确证失败路径（缺 SDK） |
| npm run update:github-snapshot | EXIT=1，gh 未认证 | 实测确证失败路径（缺 GH_TOKEN） |
| npm run start / apply:config / install:portable / uninstall / package:friend / generate:icon | 未执行 | 源码静态推断（GUI/副作用/需交互） |

## Avalonia 源码层

结构基线：Core 51、ViewModels 64（partials 15）、Features 15、Views 24、Analyzer 7、Tests 129、测试属性 765。本机无 .NET 10 SDK，C# 测试未运行，以下 C# 证据是“测试存在 + 源码匹配”，不是本 session 运行结果。

| 层/分叉点 | 语义 | 判定 | 证据 |
| --- | --- | --- | --- |
| `DirectoryPath.Parse` 空/无效/UNC/设备路径 | 目录路径安全边界 | 测试存在（本机未跑） | `EntryDirectoryTests.cs:13-68` |
| `EntryId.Parse` 保留名/路径符/非拉丁同形字 | ID 安全边界 | 测试存在（本机未跑） | `LauncherEntryIdsTests.cs:129-152` |
| `LaunchArgumentBuilder.Build` 空字段/32767 上限/转义 | 启动参数边界 | 测试存在（本机未跑） | `LaunchArgumentBuilderTests.cs`、`CsCheckPropertyTests.cs` |
| `LauncherCommandTemplateResolver` `$$`/`$key`/缺 key | 命令模板解析 | 测试存在（本机未跑） | `LauncherCommandTemplateResolverTests.cs` |
| `LauncherEntryOrder` 同步/移动/去重 | 排序状态机 | 测试存在（本机未跑） | `LauncherEntryOrderTests.cs`、PBT |
| `LauncherStoreValidator` 版本 >2/<1 | schema 升级边界 | 测试存在（本机未跑） | `LauncherStoreValidatorPbtTests.cs` |
| `StoreGrain.Dispatch` 并发 save+state | 并发写一致性 | 测试存在（本机未跑） | `ConcurrencyStressTests.cs:18-49` |
| `StoreGrain.ReloadFromDisk` 锁外读盘 + `StateChanged` 锁外触发 | 重载与通知竞态 | 源码静态推断（无直接竞态测试） | `StoreGrain.cs:55-84`；研究文件 M6/M7 仍指向此处 |
| `MainWindowViewModel` 15 个 partial | 单写状态聚合 | 实测确证（门禁行预算） | `main-vm-line-budget` lines=1197/1200，余量 3 行 |
| `DiscoverAgentsWindow.axaml.cs` 动态构建 UI | 有意命令式 UI 例外 | 实测确证（修正后） | `no-imperative-view-ui` 白名单唯一命令式视图 |
| 其余 View `.axaml.cs` | 禁止命令式建 UI | 实测确证（修正后扫 20 文件） | `checkNoImperativeViewUi` viewsScanned=20 |
| Analyzer 告警转错 | 编译期规则 | 远端不可复现（本机缺 SDK） | `analyzer-wiring` native skip |

关键结论：Core 层边界测试密度高；Persistence 的 `ReloadFromDisk`/`StateChanged` 竞态仍是源码静态推断，没有直接测试钉死；Views 的命令式 UI 检查刚从 2 个文件收紧到 20 个文件。

Top 无覆盖分叉（源码静态推断，按静默错误风险降序）：

1. `StoreGrain.cs:108-125` 初始化读失败静默回退空 store，后续 Dispatch 可能覆盖原本可读配置。
2. `LegacyMigration.cs:33-85` 解析失败/改名 `.bak` 失败被吞，整条迁移链零测试。
3. `FileLauncherJsonIO.cs:71-143` 备份轮转失败吞异常、恢复失败回抛、无 `.bak` 回抛均零测试。
4. `ErrorOrExtensions.cs:25-56` `IsRecoverable` 解包循环与分类零运行时测试，全局影响 catch-when。
5. `EntryId.cs:68/71`、`DirectoryPath.cs:36/43` 保留名、路径分隔符、`..`、设备路径、无效字符拒绝零测试。
6. `MainWindowViewModel.cs:292-304` `OnStoreReloaded` 验证失败部分恢复与 preset 重载 catch 零测试。
7. `EntryListViewModel.cs:231-305` 删除/重命名保存失败 catch 零测试。
8. `UpdateCheckService.cs:17-26` 真实 HTTP/File/非 2xx/超时全盲区。
9. `ImportConfigViewModel.cs:36-87` 导入合并/告警与保存失败回滚零测试。
10. `ResourceKeyAnalyzer.cs` 与 `CatchExceptionMustHaveWhenCodeFixProvider.cs` 两个分析器/CodeFix 完全零测试。

## CI 工作流层

`.github/workflows/verify.yml` 四个 job 的本地可复现性：

| job/分支 | 行为 | 判定 | 证据 |
| --- | --- | --- | --- |
| build-and-test | setup-dotnet 10.0.301、NuGet cache、`--locked-mode` restore、`-warnaserror` build、Node `npm ci`、`typecheck && npm test`、`test:fast` | 远端不可复现 | `verify.yml:10-64`；本机缺 10.0.301 |
| nuget-audit | `--locked-mode` restore + `dotnet list --vulnerable --include-transitive` | 远端不可复现 | `verify.yml:66-83` |
| release-contract | `npm ci && npm run release:contract`，上传 publish 产物 | 远端不可复现 | `verify.yml:85-116` |
| full-gate | `needs` 前三 job；`if` PR/main/master；`Copy-Item launcher.ci.json launcher.json`；`npm ci && npm run verify`；上传 coverage | 远端不可复现（除本地 `CI=true` 已实测 local skip） | `verify.yml:118-155` |
| SDK 分支 | `global.json` 钉 `10.0.301`；本机 `dotnet --list-sdks` 只有 `8.0.424` | 实测确证（缺 SDK 失败路径） | 多个 npm 入口 EXIT=1/`-2147450725` |
| `CI=true` local tier | `local-entry-directories` 显式跳过，与 CI 覆盖一致 | 实测确证 | `CI=true npm run verify` receipt |
| `update:github-snapshot` | 需 `gh auth`；本机 `gh 2.98.0` 未认证，EXIT=1 | 实测确证（缺认证失败路径） | 命令输出 `gh auth login` 提示 |
| `apply:config` | 需 `winget`；本机无 winget，`apply:config:dry` EXIT=1 | 实测确证（缺工具失败路径） | 命令输出 `winget is not recognized` |
| GUI/副作用入口 | `start`、`install:portable`、`uninstall`、`package:friend`、`generate:icon`、`apply:config` | 源码静态推断（未执行） | `package.json:11-25` |

远端才能观测：dotnet 真实 restore/build/test/publish 分支、NuGet 漏洞审计、artifact 上传、`release:contract` 的 AOT publish 与 clean-tree 断言。

本地不可复现风险序：native 5 步只在 CI 执行、`launcher.ci.json` 让 CI 看不见真实 config、`nuget-audit` 的漏洞退出码语义、`release:contract` 的 worktree-clean 断言、`-warnaserror` 双点、`needs` 链前置失败会跳过 full-gate、三处 `upload-artifact always()` 行为、NuGet cache key 命中、`setup-node lts/*` 版本、fork PR 的 `if` 求值。

## Trellis 工作流/更新层

实测 `trellis 0.6.17` 与 `.trellis/.version` 一致；`trellis update --dry-run` 输出 137 unchanged、0 其他，`Already up to date!`，git 对 `.trellis`/`.dsh`/`AGENTS.md` 零写入。

| 路径/分支 | 行为 | 判定 | 证据 |
| --- | --- | --- | --- |
| `.trellis/workspace/`、`tasks/`、`spec/`、`.developer`、`.current-task` | 永不写入、不迁移、不 safe-delete、不 hash、不 backup | 实测+静态 | dry-run `User data (preserved)`；`update.ts:130-136`、`1581-1596`、`1271-1288` |
| 上述例外 | 破坏性 release 会在 `tasks/` 建迁移任务；`--migrate` 会 `renameTracesToJournal` 写 `workspace/` | 静态 | `update.ts:2781-2886`、`2070-2095` |
| `.trellis/config.yaml` 整文件重写 | 被 `update.skip` 剔除，普通 update 不写 | 静态+实测 | `update.ts:943-959`；本仓 config.yaml skip 块 |
| `.trellis/config.yaml` additive 段 | 因 skip 使模板 map 缺 config.yaml，未来新 config 段不自动追加 | 静态 | `update.ts:609-655`、`2694-2711` |
| `.trellis/workflow.md` | skip 保护，普通 update 不写 | 静态+实测 | dry-run 未列为 changed |
| `.dsh/DSH.md` 与 3 个技能 | skip 保护，普通 update 不写 | 静态+实测 | `dist/configurators/dsh.js:48-63`；dry-run 未列出 |
| `AGENTS.md` | 非 skip；只替换 `<!-- TRELLIS:START/END -->` 块，块外保留 | 静态+实测 | `update.ts:242-250`、`160-219`；hash 同步 |
| hash 三分支 | 内容==模板→unchanged；hash==存储→autoUpdate 静默写；hash 不等→changedFiles 询问 | 静态 | `update.ts:977-1048` |
| `-f / -s / -n` | changedFiles 分别覆盖/跳过/写 `.new` | 静态 | `update.ts:1175-1234`、`2543-2559` |
| `--dry-run` | 不写模板/hash/gitattributes/迁移；孤儿 manifest 剪除未传 `persist:false`，理论可落盘 | 静态+实测 | `update.ts:2537-2541`、`2213-2228`；本次 git 干净 |
| safe-file-delete | 普通 update 即执行；存在 + allowed_hashes 匹配 + 非 protected + 非 skip 四重门控 | 静态 | `update.ts:337-407`、`2260-2268` |
| breakingBypass | `--migrate && 升级 && breaking && recommendMigrate` 绕过 skip；仅「已改文件」仍询问 | 静态 | `update.ts:2238-2245`、`871-872` |
| migration 硬闸 | 无 `--migrate` 时 breaking rename/delete 直接 `exit(1)` | 静态 | `update.ts:2325-2361` |
| backup | 全量备份 managed dirs + `AGENTS.md`，排除用户数据 | 静态 | `update.ts:1239-1353` |
| `update.skip` 解析 | 行级脆弱解析，格式破坏即 fail-open | 静态 | `update.ts:490-551` |

最高风险：未来破坏性 release 执行 `trellis update --migrate -f` 会绕过 skip 整体覆盖自撰文件。普通 `trellis update` 与交互式 `--migrate` 安全。

## 配置 schema 层

| 字段/校验分支 | 期望 | 判定 | 证据 |
| --- | --- | --- | --- |
| `schemaVersion` | TS 侧必须 `2`；C# `ExpectedSchemaVersion=2`、`Minimum=1` | 有测试覆盖（本机未跑 .NET） | `schema.ts:235`；`LauncherStoreValidatorPbtTests.cs` 覆盖 >2 与 <1 |
| `entries[].id/name/directory/command` | `[JsonRequired]`，四字段非空 | 有测试覆盖 + 门禁实测 | `LaunchEntry.cs`；`common.ts readConfigItems` |
| `DirectoryPath.Parse` 空值/无效字符/UNC/设备路径 | 抛 `FormatException` | 有测试覆盖（本机未跑 .NET） | `DirectoryPath.cs:24-48`；`EntryDirectoryTests.cs:13-60` |
| `DirectoryPath.ParseAndValidateExists` | 不存在目录抛 `DirectoryNotFoundException` | 有测试覆盖（本机未跑 .NET） | `EntryDirectoryTests.cs:30` |
| `EntryId.Parse` 空/保留名/路径符/非拉丁同形字/控制符 | 抛 `FormatException` | 有测试覆盖（本机未跑 .NET） | `EntryId.cs:45-77`、`150-183`；`LauncherEntryIdsTests.cs:129-152` |
| `LaunchArgumentBuilder.Build` 空名/空命令/超 32767 | 抛异常或生成计划 | 有测试覆盖（本机未跑 .NET） | `LaunchArgumentBuilder.cs:59-78`；`LaunchArgumentBuilderTests.cs`、PBT |
| `LauncherCommandTemplateResolver` `$$` 转义/`$key`/缺 key | 正确解析/抛错 | 有测试覆盖（本机未跑 .NET） | `LauncherCommandTemplateResolverTests.cs` |
| `LauncherEntryOrder.SyncWithEntries/MoveRelative/BuildIndex` | 去重、保持合法、相对移动 | 有测试覆盖（本机未跑 .NET） | `LauncherEntryOrderTests.cs`、`LauncherEntryOrderPbtTests.cs` |
| `LauncherEntryQuery.FilterAndSort` 各 sortMode | 正确排序与过滤 | 有测试覆盖（本机未跑 .NET） | `LauncherEntryQueryTests.cs` |
| TS `assertPositiveNumber/assertOnlyJsonKeys/readConfigItems` | 正数/白名单/必填 | 实测确证 | `root-gate.test.ts`、`config-lib.test.ts` 全绿 |
| 三份 config 的 `[JsonRequired]` 字段齐全 | 21 个唯一必填字段，按 LauncherStoreFile/Entry/Preset/Settings/Window/RunHistory 分类全量强制 | 实测确证 | `npm run verify` receipt `config-required-fields` totalRequired=21，三份均 ok |
| `customEntryOrder` 全量唯一、preset 引用解析、command template 引用 | 无悬空引用 | 实测确证 | `checkLauncherStore` 通过；失败分支未注入 |

最危险缺口：TS schema 校验的非法值分支（重复 id、悬空 preset、template 引用缺失、window 非正数）当前没有专用 vitest 注入样本，只有真实 config 的通过路径；这些失败分支属于源码静态推断。

运行态与门禁分裂（源码静态推断，本机未跑 .NET）：

| 缺口 | 证据/后果 |
| --- | --- |
| 运行态只校验 `schemaVersion`，不校验重复 id/悬空 preset/blank-manual 锁/默认 preset/模板引用/window 正数 | `LauncherStoreValidator.cs` 仅查 `>2` 与 `<1`；业务规则只在 `checkLauncherStore` 门禁里 |
| `checkConfigRequiredFields` 提取 26 个 `[JsonRequired]`，实际只校验 settings 4 + window 2 | `schema.ts:436-533`；entry/preset/runHistory/顶层必填是死提取 |
| 恢复覆盖会静默丢手改 | `config/` 已存在 `.bak/.bak.1/.bak.2`；`LoadWithRecovery` 用备份 `File.Replace` 覆盖主文件 |
| 非法 `DirectoryPath/EntryId/EntryName` 抛 `FormatException`，不在任何 catch 列表 | 启动即崩且无恢复 |
| 未知键 loose read 跳过，下次 Save 被 source-gen 清除 | 手改字段静默消失 |
| `launcher.ci.json`/`launcher.example.json` 不受 `checkLauncherStore` 业务规则约束 | 只有 `config-required-fields` 检查它们 |
| `settings.window` 运行态缺 `[JsonRequired]` → null，门禁却强制 | 两套契约不一致 |
| `asArray` 单值包装让门禁比运行态宽容 | `common.ts:69-74` |
| `schemaVersion=1` 运行态接受，门禁拒绝，且无升级路径 | `LauncherStoreValidator.cs:13-14` |
