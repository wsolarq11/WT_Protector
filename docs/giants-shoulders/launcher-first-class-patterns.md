Status: Active
Purpose: 12 工具行业调研提炼的一等启动器对象与边界。
Update when: 调研结论、命名启动组模型或门禁参与关系变化时。

# 成熟工具的一等启动器模式

Reviewed on: 2026-05-17

保留 12 工具行业调研的设计教训，避免以后重新发现同样的 UX 思路。这是决策素材，不是抄任何产品的授权。
本地验收门禁：`npm run verify`。

## 本地结论

旧 `selected` 勾选状态贴近退役 HTA 的一键手感，但不是最佳长期对象。成熟工具通常把重复选择提升为
命名、便携、可查的一等对象：

```text
entry / command / profile       可运行的东西
preset / group / workspace      命名子集或语境
settings / defaults             用户与 UI 偏好
status / receipt                观察结果，非期望配置
```

本地翻译（`config/launcher.json`，键：schemaVersion、settings、entries、presets、defaultPresetId、runHistory）：

```text
config/launcher.json = 启动条目 + 命名启动组（引用 entry id，轻量 CRUD）+ 便携 UI 偏好 + defaultPresetId
```

preset 只重写勾选状态（`ApplyPreset` → `SetSelectionFromOwner`）；真正启动仍需显式「一键启动已勾选」
（`LauncherControlBar.axaml` 的 `LaunchSelectedCommand`），组选择与命令执行保持独立安全边界。

## 12 工具借用的设计模式

| 工具族 | 借用的一等对象 | WT Launcher 翻译 | 来源 |
| --- | --- | --- | --- |
| VS Code Settings | 分层、人可读的设置作用域 | 配置保持便携可审查，避免隐藏 AppData 状态 | <https://code.visualstudio.com/docs/configure/settings> |
| VS Code Workspaces | 已保存的多根上下文 | 命名启动组可表达工作上下文 | <https://code.visualstudio.com/docs/editing/workspaces/workspaces> |
| Windows Terminal Profiles | profile 对象 + 默认值 | 条目稳定、按 id 寻址（`EntryId`/`PresetId` 值类型） | <https://learn.microsoft.com/en-us/windows/terminal/customize-settings/profile-general> |
| Windows Terminal JSON Fragments | 外部配置片段 | 未来导入/导出可增量叠加，而非单一巨文件 | <https://learn.microsoft.com/en-us/windows/terminal/json-fragment-extensions> |
| JetBrains Run/Debug Configurations | 命名的可运行配置 | 启动行为应是命名对象，不只是按钮状态 | <https://www.jetbrains.com/help/idea/run-debug-configuration.html> |
| Raycast Quicklinks/Snippets | 带标签与占位符的可搜索命令 | 启动器卡片保持快速、可搜索的命令对象 | <https://manual.raycast.com/quicklinks>, <https://manual.raycast.com/snippets> |
| Raycast Extensions | 可发现的可安装命令 | 未来扩展/导入不得削弱本地配置契约 | <https://manual.raycast.com/extensions> |
| Alfred Workflows | 可见的输入/动作/输出流 | 未来自动化应是显式图式步骤，非隐藏副作用 | <https://www.alfredapp.com/workflows/> |
| Obsidian Workspaces/Bookmarks | 命名的已保存布局与链接 | 把用户意图存为命名语境，而非瞬时视图状态 | <https://help.obsidian.md/Plugins/Workspaces>, <https://help.obsidian.md/plugins/bookmarks> |
| GitHub CLI Extensions | 感觉原生的自定义命令 | 自定义启动条目若共享同一命令契约即可原生 | <https://docs.github.com/en/github-cli/github-cli/creating-github-cli-extensions> |
| Docker Compose Profiles | 命名激活子集 | `launcher.json` presets 按 id 选择条目子集（`presets[].entryIds`） | <https://docs.docker.com/compose/how-tos/profiles/> |
| Kubernetes Objects | 期望 spec 与观察 status 分离 | 启动配置/presets 与最后运行回执分离；`runHistory` 在统一存储，不进隐藏库 | <https://kubernetes.io/docs/concepts/abstractions/overview/> |
| Apple Shortcuts | 带变量/自动化的动作列表 | 自动化是未来范围；不用勾选隐藏自动化 | <https://support.apple.com/guide/shortcuts/apdd2b316022/ios>, <https://support.apple.com/guide/iphone/shortcuts-iph47e1c9d7d/ios> |
| Figma Components/Variants | 带变体/属性的可复用组件 | 仅当重复证明必要时才把卡片/动作做成可复用组件 | <https://help.figma.com/hc/en-us/articles/360038662654-Guide-to-components-in-Figma>, <https://help.figma.com/hc/en-us/articles/360056440594-Create-and-use-variants> |

注：调研 12 个产品教训但 14 行，因 VS Code 与 Windows Terminal 各贡献两个独立模式。本地决策收敛为一项：命名启动组。

## 有意不抄

- 暂不引入插件市场/扩展运行时。
- 无隐藏状态库（`runHistory` 在 `config/launcher.json`，不在 AppData/registry）。
- 无工作流图引擎。
- 无新依赖。
- 选择 preset 不自动启动（应用只重写勾选；启动需显式「一键启动已勾选」）。

## 已落地的本地证据

1. `launcher.json` 的 presets 是统一存储里人可读的命名启动组区段；示例 schema 在 `config/launcher.example.json`。
2. preset id 与 entry 引用在 xUnit 与门禁双验证：`scripts/verify.ts` `presets-schema` +
   `security-audit-f2-preset-entryid-validate`；xUnit：`LauncherPresetIdsTests`、`LauncherPresetWorkflowTests`、
   `PresetServiceTests`、`MainWindowViewModelTests.PresetSelectionTests`。
3. UI 选择器 + 显式「应用启动组」：`LauncherPresetSelectionPreview.DefaultApplyButtonText`；
   `MainWindowViewModel.Presets.cs` `ApplyPreset`/`CanApplyPreset`。
4. 一等操作「保存为新启动组 / 更新当前启动组 / 删除当前启动组」：`Views/LauncherControlBar.axaml` 菜单 +
   `SaveSelectionAsPresetCommand` / `UpdateSelectedPresetCommand` / `DeleteSelectedPresetCommand`。
5. 保留 `selected` 勾选作为 HTA 兼容桥：`Views/LauncherEntriesView.axaml` 的
   `CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}"`。

回滚点（不变）：从 `config/launcher.json` 删 presets 与 defaultPresetId，删 `LauncherPreset*` 与 preset
UI/测试；旧勾选流保持完整。
