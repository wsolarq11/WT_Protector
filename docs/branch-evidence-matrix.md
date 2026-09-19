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

本 session 已装 .NET 10.0.401 SDK（`C:\Program Files\dotnet`），实测默认 `npm run verify` 全量 PASS：`dotnetSdk=10.0.401`、`failures=0`、`advisory=1`、`skipped=0`；Avalonia 主线 870 个测试全部通过。winget 尝试安装失败：本机 Windows 10 19045 的 Appx 模块不可用（0x80131539），`apply:config` 分支仍不可执行。

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
| 12 | config-required-fields | PASS，totalRequired=21 | 实测确证（通过路径） | 三份 config 均 ok；缺失字段分支未注入 |
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
| 25 | no-imperative-view-ui | PASS，viewsScanned=20 | 实测确证（通过路径） | 已改为扫描 `.axaml.cs`；仅 `DiscoverAgentsWindow.axaml.cs` 显式白名单为有意命令式 UI |
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
| 38 | structure-baseline-consistency | PASS | 实测确证（通过路径） | 8 行基线、787 属性、45 步全部一致；漂移分支未注入 |

### Local：1 步

| 步骤 | 默认实测 | CI=true 实测 | 判定 |
| --- | --- | --- | --- |
| local-entry-directories | PASS（11 entry 目录均存在） | SKIP（CI 覆盖 launcher.ci.json） | 实测确证两条环境分支 |

### Native：5 步

本机已装 .NET 10.0.401，`global.json` 的 `10.0.301` + `rollForward: latestFeature` 正常解析到 10.0.401，5 个 native 步骤全部执行。

| 步骤 | 本次结果 | 判定 |
| --- | --- | --- |
| release-lockfile-consistency | PASS，lockedMode=true，aotMarker=EnableNativeAot（本 session 因 SDK 10.0.401 漂移用 `--force-evaluate` 重生成 lock 后转绿） | 实测确证 |
| avalonia-mainline-verify | PASS，tests=870，nativeAot=true，worktreeDirty=true | 实测确证 |
| coverage-gate | PASS，criticalPaths=9 | 实测确证 |
| architecture-rule-check | PASS | 实测确证 |
| analyzer-wiring | PASS | 实测确证 |

### Snapshot：1 步

| 步骤 | 本次结果 | 判定 |
| --- | --- | --- |
| github-snapshot-freshness | ADVISORY（35 天 > 30 天） | 实测确证过期分支；刷新分支因 `gh auth login`/`GH_TOKEN` 未设置而远端不可复现 |

## npm 脚本入口实测

| 命令 | 退出码/结果 | 判定 |
| --- | --- | --- |
| npm test | 34/34 PASS | 实测确证 |
| npm run typecheck | PASS | 实测确证 |
| npm run verify | PASS，advisory 1，skipped 0，dotnetSdk 10.0.401 | 实测确证 |
| npm run verify:mainline | 已由 verify 的 avalonia-mainline-verify 执行：PASS，tests=848 | 实测确证 |
| npm run test:fast | EXIT=0 | 实测确证 |
| npm run test:coverage | EXIT=0，写 coverage-gate-receipt.json | 实测确证 |
| npm run test:launch-args | EXIT=0 | 实测确证 |
| npm run smoke | EXIT=0，process-booted-main-window + window-title | 实测确证 |
| npm run apply:config:dry | 仍 EXIT=1：winget 缺失，且 Appx 模块不可用（0x80131539） | 实测确证环境不可执行 |
| npm run release:build | 由 release:contract 执行：PASS，AOT publish 27,681,792 bytes | 实测确证 |
| npm run release:contract | EXIT=0，status=PASS，worktreeClean=true | 实测确证 |
| npm run update:github-snapshot | EXIT=1，gh 未认证 | 实测确证失败路径（缺 GH_TOKEN） |
| npm run make | EXIT=0：dirty-tree advisory + release-build PASS + critical-journey launch/title PASS + friend zip 19,235,792 bytes | 实测确证 |
| npm run package:friend | EXIT=0，zip 含 exe、`av_libglesv2.dll`、`libHarfBuzzSharp.dll`、`libSkiaSharp.dll`、`使用说明.txt`、`config/launcher.example.json`；不含 `receipt.json` / `update-manifest.json` | 实测确证 |
| exe 单独运行 | 失败：`DllNotFoundException: libSkiaSharp`，证明原生 DLL 是运行依赖 | 实测确证 |
| npm run start | EXIT=1（boot 后主动杀进程），窗口标题 `WT Launcher`，进程正常退出残留为 0 | 实测确证 boot 路径 |
| npm run apply:config:dry | EXIT=1：winget 缺失，且 Appx 模块不可用（0x80131539） | 实测确证环境不可执行 |
| npm run install:portable | EXIT=0（临时目录 + `--skip-shortcuts`）；安装目录只含 exe、三个原生 DLL、`install-manifest.json`、`uninstaller.exe`，已排除 `receipt.json`/`update-manifest.json` | 实测确证 |
| npm run uninstall | EXIT=0，删除安装目录 | 实测确证 |
| npm run generate:icon | EXIT=0，`app.ico` 重生成多尺寸（16/24/32/48/64/128/256） | 实测确证 |

## Avalonia 源码层

结构基线：Core 51、ViewModels 64（partials 15）、Features 15、Views 24、Analyzer 7、Tests 135、测试属性 787。本机已装 .NET 10.0.401，`avalonia-mainline-verify` 实测 870 个测试全部通过；下表“实测确证”以该 receipt 为准。

| 层/分叉点 | 语义 | 判定 | 证据 |
| --- | --- | --- | --- |
| `DirectoryPath.Parse` 空/无效/UNC/设备路径 | 目录路径安全边界 | 实测确证（870 tests） | `EntryDirectoryTests.cs:13-68` |
| `EntryId.Parse` 保留名/路径符/非拉丁同形字 | ID 安全边界 | 实测确证（870 tests） | `LauncherEntryIdsTests.cs:129-152` |
| `LaunchArgumentBuilder.Build` 空字段/32767 上限/转义 | 启动参数边界 | 实测确证（870 tests） | `LaunchArgumentBuilderTests.cs`、`CsCheckPropertyTests.cs` |
| `LauncherCommandTemplateResolver` `$$`/`$key`/缺 key | 命令模板解析 | 实测确证（870 tests） | `LauncherCommandTemplateResolverTests.cs` |
| `LauncherEntryOrder` 同步/移动/去重 | 排序状态机 | 实测确证（870 tests） | `LauncherEntryOrderTests.cs`、PBT |
| `LauncherStoreValidator` 版本 >2/<1 | schema 升级边界 | 实测确证（870 tests） | `LauncherStoreValidatorPbtTests.cs` |
| `StoreGrain.Dispatch` 并发 save+state | 并发写一致性 | 实测确证（870 tests） | `ConcurrencyStressTests.cs:18-49` |
| `StoreGrain.ReloadFromDisk` 锁外读盘 + `StateChanged` 锁外触发 | 重载与通知竞态 | 源码静态推断（无直接竞态测试） | `StoreGrain.cs:55-84`；研究文件 M6/M7 仍指向此处 |
| `MainWindowViewModel` 15 个 partial | 单写状态聚合 | 实测确证（门禁行预算） | `main-vm-line-budget` lines=1197/1200，余量 3 行 |
| `DiscoverAgentsWindow.axaml.cs` 动态构建 UI | 有意命令式 UI 例外 | 实测确证（修正后） | `no-imperative-view-ui` 白名单唯一命令式视图 |
| 其余 View `.axaml.cs` | 禁止命令式建 UI | 实测确证（修正后扫 20 文件） | `checkNoImperativeViewUi` viewsScanned=20 |
| Analyzer 告警转错 | 编译期规则 | 实测确证 | `analyzer-wiring` PASS |

关键结论：Core 层边界测试密度高；Persistence 的 `ReloadFromDisk`/`StateChanged` 竞态仍是源码静态推断，没有直接测试钉死；Views 的命令式 UI 检查刚从 2 个文件收紧到 20 个文件。

Top 无覆盖分叉（源码静态推断，按静默错误风险降序；本 session 已关闭前五项主路径，剩余为部分分支）：

1. `StoreGrain.cs:106-126` 初始化读失败静默回退空 store：已新增可读失败与 FileNotFound 两条测试；后续 Dispatch 覆盖原本可读配置的完整攻击链仍属静态推断。
2. `LegacyMigration.cs:33-85`：已新增四测覆盖迁移成功、统一 store 跳过、settings/presets 损坏降级；改名 `.bak` 失败分支仍静态推断。
3. `FileLauncherJsonIO.cs:71-104`：已新增备份恢复失败回抛、主文件缺失从备份链恢复；备份轮转失败吞异常仍静态推断。
4. `ErrorOrExtensions.cs:25-56`：已新增 Try/TryAsync 可恢复与非可恢复四测。
5. `EntryId.cs`/`DirectoryPath.cs` 保留名、路径分隔符、`..`、设备路径：已新增测试补齐。
6. `MainWindowViewModel.cs:272-304` `OnStoreReloaded` 验证失败部分恢复与 preset 重载 catch：已新增 Refresh 损坏主文件无备份时报错分支；验证失败部分恢复分支仍静态推断。
7. `EntryListViewModel.cs:231-305`：已新增删除保存失败保留条目并报错；重命名保存失败分支仍静态推断。
8. `UpdateCheckService.cs:17-26`：已新增本地文件读取与非 2xx 报错；真实网络超时/非 2xx 的 HTTP 延迟分支仍静态推断。
9. `ImportConfigViewModel.cs`：已新增 Summary/BuildPlan 重复、缺失、无效引用分支；保存失败回滚仍静态推断。
10. `ResourceKeyAnalyzer.cs` 已新增未定义 StaticResource 诊断测试；`CatchExceptionMustHaveWhenCodeFixProvider.cs` 仍零测试。

## CI 工作流层

`.github/workflows/verify.yml` 单 job 顺序链的本地可复现性：

| job/分支 | 行为 | 判定 | 证据 |
| --- | --- | --- | --- |
| 单 job 顺序链 | checkout → setup-dotnet → setup-node → `npm ci` → `typecheck && npm test` → `test:fast` → NuGet audit → prepare launcher store → `npm run verify` → `npm run release:contract` → upload release artifacts | 源码静态推断（本地未跑 GitHub Actions） | `.github/workflows/verify.yml:1-56` |
| SDK 分支 | `global.json` 钉 `10.0.301`，`rollForward: latestFeature` | 实测确证 | 本机解析到 10.0.401 |
| `CI=true` local tier | `local-entry-directories` 显式跳过，与 CI 覆盖一致 | 实测确证 | `CI=true npm run verify` receipt |
| `update:github-snapshot` | 需 `gh auth`；本机 `gh 2.98.0` 未认证，EXIT=1 | 实测确证（缺认证失败路径） | 命令输出 `gh auth login` 提示 |
| `apply:config` | 需 `winget`；本机无 winget，且 Appx 模块不可用 | 实测确证环境不可执行 | Appx 加载失败 0x80131539 |
| GUI/副作用入口 | `start`、`install:portable`、`uninstall`、`generate:icon`、`apply:config` | 实测确证（本 session 已执行；`apply:config` 因 winget 缺失失败） | `package.json:11-25` |
| 便携好友包 | `package:friend` 只打 exe + 三个原生 DLL + 示例配置 + 使用说明，不含 `receipt.json` / `update-manifest.json` | 实测确证（本次展开 zip 复核） | `scripts/package-friend.ts:31-36` |

已消除的远端独占分支：多 job `needs` 链、PR/main/master `if` 求值、三处 `upload-artifact if: always()`、`build-and-test` 与 `release-contract` 分 runner 的 lock 隔离。当前 CI 只剩 GitHub Actions 平台本身不可本地复现的调度事实。

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
| `schemaVersion` | TS 侧必须 `2`；C# `ExpectedSchemaVersion=2`、`Minimum=1` | 有测试覆盖（870 tests 实测） | `schema.ts:235`；`LauncherStoreValidatorPbtTests.cs` 覆盖 >2 与 <1 |
| `entries[].id/name/directory/command` | `[JsonRequired]`，四字段非空 | 有测试覆盖 + 门禁实测 | `LaunchEntry.cs`；`common.ts readConfigItems` |
| `DirectoryPath.Parse` 空值/无效字符/UNC/设备路径 | 抛 `FormatException` | 有测试覆盖（870 tests 实测） | `DirectoryPath.cs:24-48`；`EntryDirectoryTests.cs:13-60` |
| `DirectoryPath.ParseAndValidateExists` | 不存在目录抛 `DirectoryNotFoundException` | 有测试覆盖（870 tests 实测） | `EntryDirectoryTests.cs:30` |
| `EntryId.Parse` 空/保留名/路径符/非拉丁同形字/控制符 | 抛 `FormatException` | 有测试覆盖（870 tests 实测） | `EntryId.cs:45-77`、`150-183`；`LauncherEntryIdsTests.cs:129-152` |
| `LaunchArgumentBuilder.Build` 空名/空命令/超 32767 | 抛异常或生成计划 | 有测试覆盖（870 tests 实测） | `LaunchArgumentBuilder.cs:59-78`；`LaunchArgumentBuilderTests.cs`、PBT |
| `LauncherCommandTemplateResolver` `$$` 转义/`$key`/缺 key | 正确解析/抛错 | 有测试覆盖（870 tests 实测） | `LauncherCommandTemplateResolverTests.cs` |
| `LauncherEntryOrder.SyncWithEntries/MoveRelative/BuildIndex` | 去重、保持合法、相对移动 | 有测试覆盖（870 tests 实测） | `LauncherEntryOrderTests.cs`、`LauncherEntryOrderPbtTests.cs` |
| `LauncherEntryQuery.FilterAndSort` 各 sortMode | 正确排序与过滤 | 有测试覆盖（870 tests 实测） | `LauncherEntryQueryTests.cs` |
| TS `assertPositiveNumber/assertOnlyJsonKeys/readConfigItems` | 正数/白名单/必填 | 实测确证 | `root-gate.test.ts`、`config-lib.test.ts` 全绿 |
| 三份 config 的 `[JsonRequired]` 字段齐全 | 21 个唯一必填字段，按 LauncherStoreFile/Entry/Preset/Settings/Window/RunHistory 分类全量强制 | 实测确证 | `npm run verify` receipt `config-required-fields` totalRequired=21，三份均 ok |
| `customEntryOrder` 全量唯一、preset 引用解析、command template 引用 | 无悬空引用 | 实测确证 | `checkLauncherStore` 通过；失败分支未注入 |

最危险缺口：TS schema 校验的非法值分支（重复 id、悬空 preset、template 引用缺失、window 非正数）当前没有专用 vitest 注入样本，只有真实 config 的通过路径；这些失败分支属于源码静态推断。

运行态与门禁分裂（源码静态推断，GUI 运行态未实测）：

| 缺口 | 证据/后果 |
| --- | --- |
| 运行态只校验 `schemaVersion`，不校验重复 id/悬空 preset/blank-manual 锁/默认 preset/模板引用/window 正数 | `LauncherStoreValidator.cs` 仅查 `>2` 与 `<1`；业务规则只在 `checkLauncherStore` 门禁里 |
| `checkConfigRequiredFields` 死提取 | 已修正：现按 LauncherStoreFile/Entry/Preset/Settings/Window/RunHistory 全量强制 21 个唯一必填字段 | `schema.ts` 重写；`npm run verify` totalRequired=21，三份 ok |
| 恢复覆盖会静默丢手改 | `config/` 已存在 `.bak/.bak.1/.bak.2`；`LoadWithRecovery` 用备份 `File.Replace` 覆盖主文件 |
| 非法 `DirectoryPath/EntryId/EntryName` 抛 `FormatException`，不在任何 catch 列表 | 启动即崩且无恢复 |
| 未知键 loose read 跳过，下次 Save 被 source-gen 清除 | 手改字段静默消失 |
| `launcher.ci.json`/`launcher.example.json` 不受 `checkLauncherStore` 业务规则约束 | 只有 `config-required-fields` 检查它们 |
| `settings.window` 运行态缺 `[JsonRequired]` → null，门禁却强制 | 两套契约不一致 |
| `asArray` 单值包装让门禁比运行态宽容 | `common.ts:69-74` |
| `schemaVersion=1` 运行态接受，门禁拒绝，且无升级路径 | `LauncherStoreValidator.cs:13-14` |

## 视觉系统层

| 分支 | 行为 | 判定 | 证据 |
| --- | --- | --- | --- |
| 画布大气渐变 | `BackgroundBrush` 从暖纸色渐变到冷石板色，替换纯色 | 实测确证 | `App.axaml.cs`；`smoke` boot 通过 |
| 焦点光/地平线 | `CanvasLightBrush` + `HorizonBrush` 压在主窗口内容后 | 实测确证 | `MainWindow.axaml`；`smoke` boot 通过 |
| 全局控件皮肤 | Button 悬停/按下位移、TextBox/ComboBox/AutoCompleteBox 焦点描边、ListBoxItem 选中、ContextMenu/MenuItem/ToolTip 圆角 | 实测确证 | `App.axaml`；`dotnet build -warnaserror` PASS |
| 全局 destructive 按钮 | 从 DialogSharedStyles 移到 App.axaml，所有窗口共享 | 实测确证 | `App.axaml`；build PASS |
| 控件阴影/圆角令牌 | `ControlBoxShadow`、`ControlCornerRadius`、`DropdownCornerRadius`、`DialogCornerRadius` | 实测确证 | `LauncherDesignTokens.cs`；`App.axaml.cs` |
| 响应式密度 | 窗口尺寸变化仍由 `CardLayoutProvider` 切 Normal/Compact/Micro，不缩放文字 | 实测确证（既有逻辑） | `LauncherEntryViewModel.Layout.cs`；`verify` PASS |
| reduced-motion / drag 过渡抑制 | 既有 `reduced-motion` 与 `suppress-drag-transitions` 样式保留 | 实测确证（路径存在） | `MainWindow.axaml` |

视觉改动已把裸色值从 App 层移回 Core 令牌层，`app-raw-hex-zero` 复验 PASS。
