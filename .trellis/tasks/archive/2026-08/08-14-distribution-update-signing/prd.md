# 分发与更新：安装器 / 签名 / 更新通道

## Goal

从“zip 手分发 + SmartScreen 引导”升级为更可信的分发形态：可选安装器、签名策略、可验证更新通道，同时不破坏便携配置语义。

## Background（已确认事实）

- 当前分发为 zip：AOT exe + 使用说明 + 示例配置；exe 未签名，README 用“更多信息 → 仍要运行”引导绕过 SmartScreen。
- `docs/feature-reality-north-star-audit.md` 把 installer/update/signing 标为 distribution-scope，不是启动器正确性缺口。
- `docs/giants-shoulders/codexswitch-avalonia-native-aot.md` 记录了 Inno Setup/AppImage/DMG/signing 的外部路线，可作为调研起点。
- 架构不变量：启动数据仍保持便携单文件，安装器/更新器不得引入 AppData/registry 作为配置第二真相。

## Requirements

- R1 安装器：研究并选型（Inno Setup/NSIS/WiX 等），支持安装、开始菜单/桌面入口、卸载时保留用户配置。
- R2 签名：记录证书来源/成本/CI 签名方案；在无证书时保持现有文档引导，不阻塞未签名开发构建。
- R3 更新通道：定义版本清单（当前 exe 已有版本信息），应用可检查更新；更新动作显式，不强制自动覆盖用户数据。
- R4 更新器/安装器不得迁移或改写 `config/launcher.json`；升级后配置路径语义不变。
- R5 打包门禁：`tools/build-release.ps1` / 新增脚本应验证安装包结构、版本、签名状态（有证书时）和配置保留行为。

## Acceptance Criteria

- [ ] 安装器可安装/卸载，安装后 exe 可启动；卸载后用户配置仍保留在便携位置。
- [ ] 有证书时产物签名且签名校验通过；无证书时构建仍成功并输出明确未签名提示。
- [ ] 更新清单可生成、可解析；应用能检测新版本并提示用户，更新后配置不丢失。
- [ ] 安装/更新过程不新增 AppData/registry 配置权威源。
- [ ] 新增测试/脚本检查覆盖打包结构、版本、签名和配置保留；`tools/verify.ps1` 保持绿。

## Out of Scope

- 多平台安装（只做 win-x64）。
- 云端更新后端、强制更新、遥测。
- 把配置迁移到安装器管理的注册表/AppData。

## Open Questions

- 分发托管位置和更新清单 URL（本地测试/私有源/GitHub Releases）。
- 代码签名证书预算与 CI 签名方式。
- 更新策略：手动检查 vs 可关闭的自动检查。