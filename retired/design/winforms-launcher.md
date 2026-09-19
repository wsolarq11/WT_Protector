# Retired WinForms launcher design

- Status: Archived
- Purpose: 保存 WinForms 主线删除前的设计教训与源码收据
- Update when: 不更新，仅追溯

退役时间：2026-05-17。活动主线已迁入 `src/avalonia-launcher/`。

## 源码收据

| Artifact | Former path | SHA-256 | Bytes |
| --- | --- | --- | ---: |
| 可执行文件 | `WT Launcher.exe` | `A3A575648D93679C0106A6D654BA91BE4A39CE4AFAF0464C87414C7BA1426C11` | 43008 |
| 源码 | `src/winforms-launcher/Program.cs` | `40A2609F8D49F5A18811B50857AE2EB08EF7910DCD825214CE00DFC145145F1F` | 75324 |

## Portable config discovery

仓库根 `config/config.json` 优先，可执行文件本地配置兜底；UI 偏好存 sibling `settings.json`，不写 AppData/registry。启动契约：`id`、`name`、`directory`、`command`、`confirm`、`selected`；`selected` 是跨重启真实状态。

## Launch safety boundary

先验证 `directory` 存在；拒绝空命令；集中引用 `wt.exe -d ... pwsh -NoExit -Command ...`；危险命令保持确认。

低摩擦 UX 教训：减 chrome、卡片可读、长文本省略、悬停不移动卡片、批量选择可见且持久。验证习惯：根门禁应是可交互少的原生脚本，证明配置解析、危险命令硬化、启动参数规则、发布/构建可行性。

## 退役原因

单文件 75 KB `Program.cs` 混合配置 IO、启动引用、渲染、布局、主题、对话框、批量状态与持久化；GDI 手绘让小改都需命令式重绘；生成式根 exe 使源码所有权不清。

## Successor directive

Avalonia 主线沿用上述契约，但按 Core/App/Tests 分层；发布物放 ignored `artifacts/`。除非 Avalonia 主线失败同样的原生门禁且做出明确回滚决定，否则不恢复。
