# Implement — Entry launchCount + frecency 排序

## 实施顺序（按依赖）

每步完成后立即跑该步的验证命令；失败停下，不堆积。

### Step 1 — Core 数据层：LaunchEntry + sort mode

**文件**：
- `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs`
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryVisibility.cs`

**改动**：
1. `LaunchEntry` 添加 `[JsonPropertyName("launchCount")] public int LaunchCount { get; init; }`（无 `[JsonRequired]`）。
2. `LauncherSortMode` enum 新增 `Frequent` 项。
3. `LauncherEntryVisibility` 新增 `public const string FrequentSortModeId = "frequent";`。
4. `NormalizeSortModeId` switch 增加 `FrequentSortModeId => FrequentSortModeId`。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 2 — Core sort 分支：ComputeFrecency + FilterAndSort

**文件**：
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryVisibility.cs`

**改动**：
1. 添加 `private static double ComputeFrecency(int count, TimeSpan delta)` 静态方法（公式见 design.md）。
2. `FilterAndSort` 新增 `case FrequentSortModeId:` 分支：now 单次采样、构造 scoreCache + nameCache、调用 CompareEntries。
3. `CompareEntries` 签名扩展 `IReadOnlyDictionary<TEntry, double>? scoreCache = null` 参数；switch 内新增 `LauncherSortMode.Frequent => scoreCache![b].CompareTo(scoreCache![a])`。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 3 — Core 单元测试

**文件（新增）**：
- `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryVisibilityFrecencyTests.cs`

**测试用例**（每条 `[Fact]` 或 `[Theory]`）：
1. `ComputeFrecency_DecayBuckets`（Theory 数据：count/delta/expected-score，覆盖 4 个 bucket + count=0）
2. `FilterAndSort_Frequent_OrdersByScoreDesc`（构造 6 条 entry：A=10/30min, B=3/10min, C=5/2h, D=20/3d, E=1/10d, F=0/never → 期望 A>B>C>D>E>F，注意 C=D score 同为 10 走 name tiebreak）
3. `FilterAndSort_Frequent_NameTiebreak_WhenScoreEqual`
4. `FilterAndSort_Frequent_ZeroCount_AlwaysScore0`（即使 launchedAt 是 1 分钟前）
5. `FilterAndSort_Frequent_NeverLaunched_FallsInElseBucket`
6. `NormalizeSortModeId_AcceptsFrequent_LowercaseAndMixedCase`（"frequent" / "FREQUENT" / "Frequent"）
7. `NormalizeSortModeId_RejectsUnknown_StillThrows`

**验证**：
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~LauncherEntryVisibilityFrecency"
```

### Step 4 — App mutation：RecordLaunchFromOwner +1 count

**文件**：
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryViewModel.cs`

**改动**：
- `RecordLaunchFromOwner` 内部 `_entry with { LaunchedAtUtc = ..., LaunchCount = _entry.LaunchCount + 1 }`。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 5 — App 单元测试

**文件**：
- `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.LaunchCountTests.cs`（新增）

**测试用例**：
1. `RecordLaunchFromOwner_IncrementsCount`（直接构造 VM，调用方法，断言 `LaunchCount` +1）
2. `SingleLaunch_PersistsIncrementedCount`（走完整 LaunchAsync 路径，验证 `_entries[0].LaunchCount == 1`，再启动一次 → == 2）
3. `BatchLaunch_IncrementsCountForEachSelected`（选 2 条 → 启动 → 都 +1）
4. `Rollback_OnSaveFailure_PreservesOldCount`（注入失败，断言 count 回滚到原值）
5. `FrequentSortMode_PersistsAcrossSaveLoad`（save → reload → 仍按 frequent 排序）

**验证**：
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~LaunchCount"
```

### Step 6 — UI：sort ComboBox 新增项

**文件**：
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`（`SortModeOptions` 列表初始化处）
- 或 `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Layout.cs`（若 SortModeOptions 在那里）

**改动**：
- `SortModeOptions` 添加 `new LauncherSortModeOption("按使用频率", LauncherEntryVisibility.FrequentSortModeId)`。

XAML 无需改：ComboBox 已 `ItemsSource={Binding SortModeOptions}` + `DisplayMemberBinding=Display`。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 7 — 配置文件兼容性验证

**手动验证**：
1. 不改 `config/launcher.json`（无 `launchCount` 字段）。
2. 启动应用：所有 entry 显示正常，sort 模式可切到"按使用频率"，所有 entry score=0 → 按 name asc 排序。
3. 启动 1 条 entry → 重启 → 该 entry `launchCount=1`，sort 模式切到 frequent 后该 entry 排首位。

**自动化验证**：在 Step 5 测试 5 中已覆盖。

### Step 8 — 完整 gate

**命令**：
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File ./tools/verify.ps1
```

**期望**：16 检查全绿，含 GUI smoke。测试数应至少增加 12 条（Step 3 的 7 条 + Step 5 的 5 条）。

## 风险点与回滚锚

| 风险 | 触发条件 | 回滚 |
|------|---------|------|
| `ComputeFrecency` 公式 typo | Step 3 测试失败 | 对照 design.md 公式逐项核对 |
| `CompareEntries` 签名扩展破坏现有调用 | Step 2 build 失败 | 检查所有 CompareEntries 调用点（应只有 FilterAndSort 内 4 处） |
| `SortModeOptions` 实际位置不在 VM 主类 | Step 6 找不到符号 | grep 定位 |
| 现有 JSON schema 校验拒绝新字段 | Step 8 gate config-schema 检查失败 | 检查 `LauncherStore` schema validator 是否白名单字段；若是，添加 `launchCount` 到白名单 |
| 反序列化 `int` 默认值与 nullable 冲突 | build warning CAxxxx | 字段用 `int`（非 nullable），默认 0，符合前向兼容 |

## 完成检查清单

- [x] Step 1-8 全部绿（实现随 `ac675a4` 落地，后续重命名为 `LauncherEntryQuery`）。
- [x] frecency 与 launch-count 聚焦测试全过。
- [x] 当前完整测试数 610。
- [x] GUI smoke 通过。
- [x] `git diff --check` 与调试输出审计通过。
- [x] 实现已由 `ac675a4 feat(modernize): ... frecency ...` 提交。

## 跟进检查（实施后）

- [ ] 归档后由 Trellis 自动更新父任务进度。
- [x] zoxide 公式与 ADR 已记录在本任务 design。
- [x] 后续任务已由生产现代化父计划统一排序。
