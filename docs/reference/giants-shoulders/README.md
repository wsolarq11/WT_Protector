Status: External
Purpose: 外部项目教训库入口与准入规则。
Update when: 新增/移除教训条目或准入规则变化时。

# 巨人肩膀库

把高信号外部项目教训放在本仓库一步之外，供技术选型与 UX 决策从已核对的引用出发，不重新发现。
这是参考库，不是权威层；WT Launcher 的事实源仍是本仓库 `README.md` 与 `npm run verify`。

## 准入规则

每篇条目记录：
1. reviewed source：上游 URL、commit/tag/date、关键文件。
2. observed stack：项目实际使用，不是猜测。
3. 值得借用的教训：能映射到 WT Launcher 的小模式。
4. 不照抄的复杂度/依赖/产品范围/许可/维护负担。
5. 下一步本地验证：能证明该教训是否有用的最小 spike/gate。

## 现有条目

| 条目 | 为什么收录 |
| --- | --- |
| [codexswitch-avalonia-native-aot.md](./codexswitch-avalonia-native-aot.md) | 支持把 Avalonia/.NET 线提升为主线的外部参照；旧单文件 WinForms/GDI 线已退役（`retired/design/winforms-launcher.md`），不得恢复 |
| [launcher-first-class-patterns.md](./launcher-first-class-patterns.md) | 12 工具行业调研提炼为 entries/presets/settings 与未来状态边界；命名启动组切片已落入 `config/launcher.json` |

## 使用方式

1. 重读相关条目与来源链接。
2. 用 `git status --short` 与 `npm run verify` 核对当前状态。
3. 定义不替换当前可执行文件、直到证明一阶收益的 spike。
4. 对照验收标准：更快/更简单/更自然 UX；相同或更强配置与启动安全；相同或更便宜验证。
5. spike 在同一门禁下胜出才采纳，否则归档 spike、保持现行路径。
