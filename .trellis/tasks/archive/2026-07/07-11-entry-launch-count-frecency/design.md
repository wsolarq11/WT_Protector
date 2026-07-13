# Design — Entry launchCount + frecency 排序

## 架构与边界

本任务横跨 5 层架构中的 4 层（Core 数据 + Core sort + App mutation + 持久化快照 + 视图绑定）。所有改动遵循现有不变量，不引入新的可变状态。

| 层 | 改动 | 不变量保持 |
|----|------|------------|
| 0 — Core 数据 | `LaunchEntry.LaunchCount` 字段 | 仍是 init-only record；mutation 通过 `with` |
| 0 — Core sort | `LauncherEntryVisibility` 新增 `FrequentSortModeId` + `frequent` 分支 | `FilterAndSort` 仍是单一 sort 入口；time/name cache 模式不变 |
| 1 — Mutation | `RecordLaunchFromOwner` 同时更新 timestamp + count | 仍是 launch 记录的单一 mutation point |
| 2 — 持久化/回滚 | 无新逻辑；`LauncherEntryMutationSnapshot` 自动捕获 LaunchCount | 快照不可变性 |
| 4 — 视图 | sort ComboBox 新增选项 | 无新 projection bool；SelectedItem 直接绑定 |

## 数据契约

### `LaunchEntry.LaunchCount`（新字段）

```csharp
/// <summary>
/// 累积启动次数。frequent sort 模式按 frecency(count, recency) 评分。
/// Optional — 旧版本 JSON 无此字段时反序列化为 0（前向兼容）。
/// </summary>
[JsonPropertyName("launchCount")]
public int LaunchCount { get; init; }
```

- 不加 `[JsonRequired]`（保持 optional）。
- 默认 0（int 默认值，无需 `= 0`）。
- `JsonSerializerOptions.RespectRequiredConstructorParameters` 不影响 init-only 默认值。

### `LauncherSortMode` enum（新增 Frequent）

```csharp
public enum LauncherSortMode { Recent, Updated, Alphabetical, Custom, Frequent }
```

### `LauncherEntryVisibility` 常量（新增）

```csharp
public const string FrequentSortModeId = "frequent";
```

`NormalizeSortModeId` switch 增加 `FrequentSortModeId => FrequentSortModeId`。

## Frecency 评分公式

zoxide 风格时间衰减：

```
score(entry, now) = launchCount * decayBucket(now - launchedAtUtc)

decayBucket(delta):
  delta < 1h   → 4.0
  delta < 24h  → 2.0
  delta < 7d   → 0.5
  else         → 0.25
```

边界：
- `launchCount == 0` → score = 0（不论 bucket）。
- `launchedAtUtc == ""` → `ParseLaunchedAt` 返回 `DateTimeOffset.MinValue` → delta 巨大 → 落入 else 桶 → score = count * 0.25。若 count 也是 0，score = 0。
- now 在 `FilterAndSort` 入口一次性采样（避免排序过程中时间漂移导致 bucket 跳变）。

### Tiebreaker（与现有 recent/updated 一致）

score 相等时：
1. name OrdinalIgnoreCase asc
2. id Ordinal asc

### 单元测试矩阵

| Entry | count | launchedAtOffset | 期望 bucket | 期望 score |
|-------|-------|------------------|-------------|-----------|
| A | 10 | 30min ago | 1h | 40.0 |
| B | 3 | 10min ago | 1h | 12.0 |
| C | 5 | 2h ago | 24h | 10.0 |
| D | 20 | 3d ago | 7d | 10.0 |
| E | 1 | 10d ago | else | 0.25 |
| F | 0 | never | else | 0 |

排序顺序（desc）：A(40) > B(12) > C=D(10, name tiebreak) > E(0.25) > F(0)。

注意 C 和 D score 同为 10 → 走 name tiebreak → C 在 D 前（按 name asc）。

## FilterAndSort 新增分支

```csharp
case FrequentSortModeId:
{
    var now = DateTimeOffset.UtcNow;  // 单次采样
    var scoreCache = new Dictionary<TEntry, double>(filtered.Count);
    nameCache = new Dictionary<TEntry, string>(filtered.Count);
    foreach (var entry in filtered)
    {
        var launchEntry = selectEntry(entry);
        RequireEntryId(launchEntry);
        var launchedAt = ParseLaunchedAt(launchEntry.LaunchedAtUtc);
        scoreCache[entry] = ComputeFrecency(launchEntry.LaunchCount, now - launchedAt);
        nameCache[entry] = EntryName.RequireNonEmpty(launchEntry.Name);
    }
    comparison = (a, b) => CompareEntries(
        a, b, selectEntry, LauncherSortMode.Frequent,
        scoreCache: scoreCache, nameCache: nameCache);
    break;
}
```

`CompareEntries` 签名扩展（新增可选 `scoreCache` 参数）：

```csharp
private static int CompareEntries<TEntry>(
    TEntry a, TEntry b,
    Func<TEntry, LaunchEntry> selectEntry,
    LauncherSortMode sortKind,
    IReadOnlyDictionary<string, int>? customOrder = null,
    IReadOnlyDictionary<TEntry, string>? nameCache = null,
    IReadOnlyDictionary<TEntry, DateTimeOffset>? timeCache = null,
    IReadOnlyDictionary<TEntry, double>? scoreCache = null)  // 新增
```

switch 内新增分支：

```csharp
LauncherSortMode.Frequent => scoreCache![b].CompareTo(scoreCache![a]),  // desc
```

`ComputeFrecency` 静态私有方法：

```csharp
private static double ComputeFrecency(int count, TimeSpan delta)
{
    if (count <= 0) return 0;
    var decay = delta switch
    {
        var d when d < TimeSpan.FromHours(1) => 4.0,
        var d when d < TimeSpan.FromHours(24) => 2.0,
        var d when d < TimeSpan.FromDays(7) => 0.5,
        _ => 0.25,
    };
    return count * decay;
}
```

## RecordLaunchFromOwner 扩展

```csharp
internal string RecordLaunchFromOwner(string launchedAtUtc)
{
    if (launchedAtUtc.Length > 0 && !DateTimeOffset.TryParseExact(launchedAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        throw new ArgumentException("Invalid ISO 8601 timestamp format (expected 'O' round-trip format).", nameof(launchedAtUtc));
    var previous = _entry.LaunchedAtUtc;
    _entry = _entry with
    {
        LaunchedAtUtc = launchedAtUtc,
        LaunchCount = _entry.LaunchCount + 1,  // 新增
    };
    return previous;
}
```

返回值仍是 `previous LaunchedAtUtc`（保留原签名，避免触发上游调用点签名变更）。launchCount 增量内部完成，无需调用方关心。

## UI 改动

`MainWindow.axaml` sort 模式 ComboBox 已绑定 `SortModeOptions`（在 `MainWindowViewModel.cs` 构造）。新增一项即可。

`SortModeOptions` 当前形态（按现有 pattern 推断）：

```csharp
public IReadOnlyList<LauncherSortModeOption> SortModeOptions { get; } = new[]
{
    new LauncherSortModeOption("自定义", LauncherEntryVisibility.CustomSortModeId),
    new LauncherSortModeOption("按最近使用", LauncherEntryVisibility.RecentSortModeId),
    new LauncherSortModeOption("按更新时间", LauncherEntryVisibility.UpdatedSortModeId),
    new LauncherSortModeOption("按名称", LauncherEntryVisibility.AlphabeticalSortModeId),
    // 新增:
    new LauncherSortModeOption("按使用频率", LauncherEntryVisibility.FrequentSortModeId),
};
```

（确切形态待实施时按现有代码 verify；可能与上面略有差异。）

## 兼容性与迁移

### 前向兼容（旧 JSON 加载）

现有 `config/launcher.json`（无 `launchCount` 字段）→ 所有 entry `LaunchCount = 0`（int 默认值 + 可选反序列化）。frequent sort 模式下所有 score=0 → 退化为按 name asc 排序。无破坏。

### 后向兼容（新 JSON 在旧版本加载）

旧版本 `LauncherJson` 已"未知字段静默跳过"。新版本写入 `launchCount` 字段 → 旧版本忽略 → 行为不变。

### Schema version

不变（仍 schemaVersion=2）。`launchCount` 是 entry 级 optional 字段，不影响 store file schema。

## 关键 ADR

### ADR-1：frecency 公式采用 zoxide 标准而非自创

**决策**：4.0/2.0/0.5/0.25 四档衰减。

**为何**：zoxide 是社区验证过的 frecency 实现，公式经过大量真实使用调参。自创公式无数据支撑，且用户从 zoxide 迁移心智模型无缝。

**权衡**：未来若需调参，公式写死在 `ComputeFrecency` 单一函数，改一处即可。无需配置化（YAGNI）。

### ADR-2：launchCount 字段加在 entry，而非独立 metrics 表

**决策**：直接放 `LaunchEntry` 上，与 `LaunchedAtUtc` 并列。

**为何**：entry 是 launch 历史的天然归属；独立 metrics 表引入 join 复杂度，且原子写入已覆盖整 JSON 文件，分表无收益。

**权衡**：若未来要做"跨 entry 频率分析"（如命令维度统计），需要 group-by，但当前 YAGNI。

### ADR-3：now 在 FilterAndSort 入口单次采样

**决策**：所有 entry 共享同一 `now`，不每条 entry 取 `DateTimeOffset.UtcNow`。

**为何**：避免排序过程中 bucket 跳变（例如排序到一半时 59min → 1h 边界跨越，导致同次排序前后 score 不一致）。

**权衡**：入口快 1ms 与出口慢 1ms 的差异 < 排序耗时，不构成问题。

### ADR-4：score=0 的 entry 不特殊处理，按 tiebreak 排

**决策**：count=0 的 entry score=0，与 score>0 的混排时排在后面（double desc 自然结果）。

**为何**：避免引入"零频专区"概念；frecency score=0 已经隐含表达"无使用"，sort 顺序正确。

**权衡**：UI 上 zero-score entry 会和低-score entry 混排，可能看起来不直观。但 ponytail：UI 排序只该反映 score，加专区是新概念。

### ADR-5：frequent 不进 IsCustomSortMode 投影，不参与拖拽自定义排序

**决策**：frequent 与 recent/updated/alphabetical 一样是非 custom 模式；拖拽排序仅在 custom 模式生效（已有不变量）。

**为何**：现有架构 already enforces this；frequent 自然落入"非自定义排序"族，无需特殊代码。

### ADR-6：RecordLaunchFromOwner 返回值不变（保持 LaunchedAtUtc）

**决策**：方法签名不变，仅内部多 mutate 一个字段。

**为何**：避免连锁修改两处调用点（`Launch.cs:99` + `Batch.cs:166`）的接收变量。launchCount 增量是方法内部细节，调用方不需要知道。

## 回滚

无新逻辑需回滚。schema 字段加错可立即删除（前向兼容使旧版本可读新 JSON，反向亦然）。frequent sort 模式可立即从 ComboBox 移除（旧设置文件 `sortMode="frequent"` 在移除后会抛 `UnknownLauncherSortMode` —— 若要 graceful 退化，可在 `NormalizeSortModeId` 把 frequent 映射到 recent，但当前选择 hard-fail，让用户感知）。

## 操作考量

- 重启应用后 frecency 排序结果稳定（同一 now 采样窗口内）。
- 时间漂移：用户跨时区/改系统时间不影响相对 delta 计算（都用 UTC offset-aware DateTimeOffset）。
- 性能：N=20 entries，frecency 计算 O(N)，不构成瓶颈。
