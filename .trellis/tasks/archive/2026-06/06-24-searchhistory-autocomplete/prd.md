# searchHistory autocomplete 下拉

## Goal

激活 `settings.json` 中已收集但从未渲染的 `searchHistory` 字段——用户在搜索框输入时显示历史匹配下拉，点击填充搜索词。

## 当前状态

- `LauncherSettings.SearchHistory` (ImmutableList&lt;string&gt;) 已存在于数据模型、已持久化到 `settings.json`、已通过 `LauncherSettingsStore.RequireSearchHistory` 规范化加载
- 但：**从未写入新词条**，**从未渲染给用户**——是 JSON 文件里的死数据

## Requirements

1. **搜索词提交时写入历史**：用户按 Enter 或搜索框失焦时（filterText 非空），追加到 `SearchHistory` 并持久化到 `settings.json`。去重：重复词条移到最前而非重复存储。上限 20 条。
2. **输入时显示匹配下拉**：用户在搜索框输入时，从 `SearchHistory` 匹配当前输入（前缀匹配，大小写不敏感），以 Popup/ListBox 显示在搜索框正下方。
3. **点击填充**：点击下拉项 → 填充 `FilterText` → 关闭下拉 → 立即触发 ApplyFilter。
4. **键盘导航**：↓↑ 在下拉项间移动，Enter 选中，Escape 关闭下拉。
5. **空输入/无匹配/已精确匹配时不显示下拉**：`FilterText` 为空或所有历史项都不匹配当前输入时隐藏下拉；当前输入已精确匹配唯一历史项时也不显示。

## 数据结构

- 单一 owner: `LauncherSettings.SearchHistory` — 已在 `config/settings.json`
- ViewModel 新增: `SearchSuggestions` (ReadOnlyObservableCollection&lt;string&gt;), `IsSearchDropDownOpen` (bool)
- 不新增第二个状态文件

## Acceptance Criteria

- [x] 输入文本 → 匹配历史项出现在搜索框下方的下拉列表中
- [x] 点击下拉项 → 填充搜索框，触发过滤
- [x] 按 Enter 提交搜索词 → 写入 searchHistory，持久化到 settings.json
- [x] 重复搜索词 → 移到最前而非重复
- [x] 上限 20 条 → 超出时移除最旧
- [x] Escape 关闭下拉但不影响 FilterText
- [x] ↓↑ 键盘导航 + Enter 选中（AutoCompleteBox 内置）
- [x] 空输入/无匹配时不显示下拉（AutoCompleteBox 内置）
- [x] 不破坏现有搜索/过滤/排序/持久化行为
- [x] 新增测试覆盖：写入历史、去重、上限、匹配逻辑

## Out of Scope

- 不做 fuzzy 匹配——前缀匹配足够
- 不做 X 按钮删除单个历史项
- 不做"清空搜索历史"按钮
- 不做鼠标 hover 高亮（用键盘导航即可）
