# 台账：docs/reference/distribution-signing-strategy.md（验证日期 2026-08-15）

> 协议：.trellis/tasks/08-15-docs/verification-protocol.md。判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL / EXTERNAL。

| 声明 | 判定 | 证据（命令 + 结果） | 处理 |
| --- | --- | --- | --- |
| 文档无日期 | （格式） | 通读全文无日期字段 | 按协议在头部加 "Verified 2026-08-15" 标注 |
| config/launcher.json 唯一配置权威 | VERIFIED | AGENTS.md 核心规则 + config/launcher.json 存在；verify.ps1 行363 json 检查 canonicalKey | 保留 |
| tools/build-release.ps1 发布 win-x64 AOT exe | VERIFIED | build-release.ps1：`dotnet publish $app -c Release -r win-x64 -p:EnableNativeAot=true -o $publishDir`；config-lib.ps1 行63-64 Get-AvaloniaPublishDir = artifacts\\publish\\win-x64（= src/avalonia-launcher/artifacts/publish/win-x64，与 AGENTS.md 一致） | 保留（发布产物目录本身 git 忽略，未构建时不落盘——非断言） |
| 写入 update-manifest.json（version/fileName/sha256/bytes/publishedAtUtc/notes） | VERIFIED | build-release.ps1 updateManifest 块：schemaVersion=1, version, fileName='WTLauncher.App.exe', sha256=Get-FileSha256, bytes, publishedAtUtc, notes | 保留 |
| tools/install-portable.ps1 安装到 %LOCALAPPDATA%\\Programs\\WTLauncher | VERIFIED | install-portable.ps1 param 默认 InstallDir = `Join-Path $env:LOCALAPPDATA 'Programs\\WTLauncher'` | 保留 |
| 创建开始菜单（可选桌面）快捷方式 | VERIFIED | install-portable.ps1：Start Menu\\Programs\\WT Launcher\\WT Launcher.lnk；`-Desktop` 开关时创建桌面快捷方式 | 保留 |
| 写 uninstall.ps1 | VERIFIED | install-portable.ps1 生成 `uninstall.ps1` 至 InstallDir（模板含 config 备份） | 保留 |
| 卸载时保留用户 config 文件夹 | VERIFIED | 卸载路径将 `$InstallDir\\config` 备份到 `Documents\\WTLauncher-config-backup\\config-<时间戳>` 后再删 | 保留 |
| 应用暴露 管理 -> 检查更新 | VERIFIED | LauncherControlBar.axaml 行109-126：Button Content="管理" 的 ContextMenu 含 MenuItem Header="检查更新" | 保留 |
| 经 LauncherUpdateManifestParser 解析 update-manifest.json | VERIFIED | `Select-String 'LauncherUpdateManifestParser' src` → Core/LauncherUpdateManifest.cs 行81 定义；App/Features/Updates/UpdateCheckService.cs 行49 调用 | 保留 |
| 报告 newer/up-to-date/error，不下载不覆盖配置 | VERIFIED | UpdateCheckViewModel：Status = 检查失败/发现新版本/当前已是最新版本；UpdateCheckService 仅抓取清单文本（DefaultFetch: HttpClient 或 File.ReadAllText），无 exe 下载/写盘代码 | 保留 |
| 无证书 → 未签名 + "print the existing SmartScreen guidance" | STALE（措辞） | 全仓 'SmartScreen' → 仅 README.md 行153（"exe is unsigned — 使用说明.txt 记载 更多信息 → 仍要运行"）与本文件；应用代码无 SmartScreen 字符串 | 改写：未签名发布，SmartScreen 绕过指引记录于 README.md/使用说明.txt；构建仍须通过（门禁） |
| 有证书 → CI 用 signtool sign 签名 WTLauncher.App.exe 后写签名哈希 | UNVERIFIABLE（现状无实现） | `Select-String 'signtool|Sign' tools/*.ps1 + .github/workflows/*.yml` → 无签名相关命中；build-release.ps1 写的是构建产物 sha256（未签名哈希） | 改写为"建议/未来决策"，明确无本地实现证据 |
| 推荐信任签名路径：Azure Trusted Signing / EV 证书（Sectigo/SSL.com/GlobalSign） | EXTERNAL | 外部采购/环境决策；仓库无本地证据 | 保留为外部建议，标注不可本地验证 |
| update-manifest schema { schemaVersion:1, version, fileName, sha256, bytes, publishedAtUtc, notes } | VERIFIED | LauncherUpdateManifest.cs record：SchemaVersion=1 默认且 Parser 强制 SchemaVersion==1 + 必填字段校验；build-release.ps1 写入同构 | 保留 |
| settings.updateManifestUrl 可为 HTTP(S) URL 或本地文件路径 | VERIFIED | UpdateCheckService.DefaultFetch：http/https → HttpClient.GetAsync；否则 File.ReadAllTextAsync | 保留 |
| 为空 → 检查对话框报告缺失配置而非猜测远端 | VERIFIED | UpdateCheckService：url 空白 → ErrorMessage "未配置更新清单地址；可在 launcher.json 的 settings.updateManifestUrl 设置。"；config/launcher.json 现 updateManifestUrl="" | 保留 |
| 更新动作显式：检测并展示新版本，不自动下载/替换/迁移配置 | VERIFIED | UpdateCheckService/UpdateCheckViewModel 无下载与文件替换代码；门禁 update-manifest-generation（verify.ps1 行547）与 portable-installer-contract（行537）只约束清单生成与安装契约 | 保留 |
| Out of Scope（多平台安装器/云更新/注册表 AppData 配置） | VERIFIED（现状一致） | 无此类代码/脚本；README/AGENTS 声明无 AppData/registry | 保留 |

## 处理总结
- VERIFIED：14 项；STALE：1 项（SmartScreen 措辞）；UNVERIFIABLE：1 项（signtool 现状）；EXTERNAL：1 项（签名厂商建议）
- 重写点：头部验证标注、SmartScreen 措辞改实、signtool 段明确为"建议且无本地实现"；其余保留。

## 重写
文档已写回 docs/reference/distribution-signing-strategy.md。
