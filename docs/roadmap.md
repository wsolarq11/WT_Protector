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
- 重写代价（接受）：2026-10-14 历史重写后，重写前发布过的 commit 短哈希失效。规则：文档引用一律用 tag 或提交标题，不写裸短哈希。

## 历史重写记录（2026-10-14）

误提交的构建产物（`tools/uninstaller/bin|obj` 等 242 个可再生 blob，约 85MB 快照）经 `git filter-repo --strip-blobs-with-ids` 从历史剥离：

- `.git` 43MB → 5.4MB；1268 提交全保留；工作树哈希不变（前后均 `b574e745`）
- `--force-with-lease` 覆盖 `origin/main`（公开仓库；未走私有 push）
- 重写前完整历史存于仓库外 bundle（`WT_Protector-history-backup-20261014.bundle`）与本地分支 `backup/pre-rewrite`
- `.trellis/` 过程日志刻意保留——是证据，不是产物
- 首个版本 tag `v0.1.0` 打于重写后 HEAD，receipt 与 friend zip 随 release 留档（维护承诺的收口仪式首次执行）

## 不做的

- 多机同步（超出本地工具定位）
- 插件系统（复杂度远超收益）
- 切换回 WPF/WinForms（已归档 + 不可逆决策）