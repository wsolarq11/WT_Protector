using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// 单一不可变状态树：整个应用运行时状态的唯一事实来源。
///
/// 包含持久化存储数据（LauncherStoreFile）和 UI 瞬时状态（筛选/排序/视图模式）。
/// 所有变更通过 StoreGrain.Dispatch 以 reducer 模式产生新实例。
/// ViewModel 通过 StoreGrain.Select 读取投影，不持有本地可变副本。
///
/// 设计原则：
/// - sealed record + init-only 属性 →  每次变更产生新实例，零可变共享状态
/// - Store 字段包装 LauncherStoreFile → 保持与 config/launcher.json 序列化兼容
/// - UI 瞬时状态字段在此层级 →  消除 MainWindowViewModel._settings 双源问题
/// </summary>
public sealed record AppState
{
    /// <summary>持久化存储：条目、设置、预设。</summary>
    public LauncherStoreFile Store { get; init; } = new();

    /// <summary>当前搜索筛选文本。</summary>
    public string FilterText { get; init; } = "";

    /// <summary>排序模式 ID。</summary>
    public string SortModeId { get; init; } = LauncherEntryQuery.CustomSortModeId;

    /// <summary>条目视图模式 ID。</summary>
    public string ViewModeId { get; init; } = LauncherEntryViewModes.CardsId;

    /// <summary>卡片分组键。</summary>
    public string CardsGroupKey { get; init; } = LauncherCardsGroupKeys.None;

    /// <summary>启动是否需确认。</summary>
    public bool RequireConfirmationForLaunches { get; init; } = true;

    /// <summary>自定义条目顺序（拖拽时的工作副本）。</summary>
    public ImmutableList<string> CustomEntryOrder { get; init; } = ImmutableList<string>.Empty;

    /// <summary>命令模板字典。</summary>
    public ImmutableDictionary<string, string> CommandTemplates { get; init; }
        = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
}
