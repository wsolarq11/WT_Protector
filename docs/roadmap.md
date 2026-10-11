# 路线图

> 向前看：「接下来做什么」。已收敛项目，无大特性规划。

## 维护事项

- [ ] 响应 Avalonia / .NET SDK 版本升级
- [ ] 响应 Windows Terminal 配置格式变更
- [ ] 按需扩展 preset 能力（用户反馈驱动）

## 技术债务（known + accepted）

- Avalonia Native AOT 发布体积是 WPF 的 ~3x——这是 runtime 代价，已接受
- `config/launcher.json` 单文件：并发窗口写可能丢更新的上限 ≥2 窗口——概率极小，暂不解决

## 不做的

- 多机同步（超出本地工具定位）
- 插件系统（复杂度远超收益）
- 切换回 WPF/WinForms（已归档 + 不可逆决策）