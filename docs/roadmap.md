# 路线图

> 向前看：「接下来做什么」。已收敛项目，无大特性规划。

## 维护承诺（唯一源）

2026-10-14 拍板：继续维护。维护不是无限保证，而是一组可触发、可闭合、可退出的承诺：

**触发条件**（事件驱动，不是日历驱动，只有这三类会启动一次维护轮）：

1. Avalonia / .NET SDK 安全修复或 LTS 切换
2. Windows Terminal（`wt.exe`）参数或配置格式变更导致启动链路失效
3. 可复现的真实用户故障报告（含复现步骤与配置样例）

**每轮维护的收口仪式**（不可省略）：

1. 修复 → `npm run verify` 全绿
2. `npm run release:contract` → 产出 receipt
3. `npm run package:friend` → 最终交付物为 friend zip
4. 打 tag（receipt 随 tag 留档），更新本文件勾选与 `docs/handoff.md` 状态句

**明确不承诺**：

- 响应时限（个人项目，无 SLA）
- 「不做的」清单之外的新功能；特性扩展只由真实用户反馈驱动
- 无触发时长期零 commit——这是正常状态，不是弃坑

**退出条件**：若连续 24 个月无上述三类触发且 CI 保持绿，在本文件显式宣布降级为冻结态，而非默认无限承诺。

## 维护事项

- [ ] 响应 Avalonia / .NET SDK 版本升级
- [ ] 响应 Windows Terminal 配置格式变更
- [ ] 按需扩展 preset 能力（用户反馈驱动）

## 技术债务（known + accepted）

- Avalonia Native AOT 发布体积是 WPF 的 ~3x——这是 runtime 代价，已接受
- `config/launcher.json` 单文件：并发窗口写可能丢更新的上限 ≥2 窗口——概率极小，暂不解决
- 历史中提交过构建产物（`tools/uninstaller/bin|obj` 等，约 85MB 快照），`.gitignore` 已止血但历史仍占 ~43MB。本地实测 `git filter-repo --strip-blobs-with-ids` 剥离后 `.git` 可降至 ~5.5MB（含 `.trellis/` 时 ~3.9MB），工作树哈希不变（`9618a81c`）。**未执行**：需 force-push 公开仓库且已引用的 commit 短哈希会失效。前提已就绪：`docs/spec/` 的短哈希引用（原本就无法解析的 phantom reference）已改为提交标题/任务名引用，重写不会再损坏文档引用。剩余决策点仅「是否接受公开仓库 force-push」。
- 仓库 0 tag / 0 release：维护承诺的收口仪式要求「打 tag、receipt 随 tag 留档」，尚未执行。

## 不做的

- 多机同步（超出本地工具定位）
- 插件系统（复杂度远超收益）
- 切换回 WPF/WinForms（已归档 + 不可逆决策）