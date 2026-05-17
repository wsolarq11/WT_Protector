
# CodexSwitch: Avalonia + .NET 10 + Native AOT

Reviewed on: 2026-05-17  
Upstream repository: <https://github.com/AIDotNet/CodexSwitch>  
Reviewed upstream commit: `6152418fd1de7779010d4786b79060e651649109`  
Reviewed UI submodule commit: `cbb5aaae69b498e5f6b324d8c9632a55bc2dbd29` (`CodexSwitchUI`)  
Local verification used: `git ls-remote https://github.com/AIDotNet/CodexSwitch.git HEAD refs/heads/master`

## Why this belongs here

WT Launcher retired its former single-file WinForms/GDI line and promoted the Avalonia/.NET line into `src/avalonia-launcher/`. This note remains useful as the reference that justified that migration and as a guard against blindly copying unrelated CodexSwitch product scope.

CodexSwitch is useful because it shows a modern C# desktop route without moving to Electron, WebView2, or Tauri:

```text
.NET 10
Avalonia 12
XAML UI
MVVM / CommunityToolkit.Mvvm
CodexSwitchUI component library
Lucide.Avalonia icons
xUnit tests
self-contained Native AOT publish
multi-platform CI packaging
```

The lesson is not "copy CodexSwitch". The lesson is that a C# desktop app can get modern layout, styling, components, testable state, and release discipline without WinForms hand drawing.

## Observed stack evidence

| Area | Observed in CodexSwitch | Source |
| --- | --- | --- |
| Desktop UI framework | `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter` | [`CodexSwitch.csproj`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch/CodexSwitch.csproj) |
| Runtime target | `TargetFramework` = `net10.0`; `OutputType` = `WinExe` | [`CodexSwitch.csproj`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch/CodexSwitch.csproj) |
| Native publish posture | `PublishAot=true`, `TrimMode=full`, `--self-contained true` in CI publish | [`CodexSwitch.csproj`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch/CodexSwitch.csproj), [`ci.yml`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/.github/workflows/ci.yml) |
| MVVM support | `CommunityToolkit.Mvvm`; `MainWindowViewModel` uses generated observable properties/commands | [`Directory.Packages.props`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/Directory.Packages.props), [`MainWindowViewModel.cs`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch/ViewModels/MainWindowViewModel.cs) |
| Icon system | `Lucide.Avalonia` | [`Directory.Packages.props`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/Directory.Packages.props) |
| UI structure | `Views/`, `ViewModels/`, `Controls/`, `Styles/`, XAML pages/dialogs/shell | [`MainWindow.axaml`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch/Views/MainWindow.axaml), [`HomePage.axaml`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch/Views/Pages/HomePage.axaml) |
| Component library | `CodexSwitchUI` submodule with controls, themes, tokens, docs, tests | [`CodexSwitchUI` submodule](https://github.com/AIDotNet/CodexSwitchUI/tree/cbb5aaae69b498e5f6b324d8c9632a55bc2dbd29) |
| Test discipline | xUnit project covers config, protocol, usage, i18n, updates, UI infrastructure | [`CodexSwitch.Tests.csproj`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/CodexSwitch.Tests/CodexSwitch.Tests.csproj), [`README.zh-CN.md`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/README.zh-CN.md) |
| Release packaging | Windows Inno Setup, Linux AppImage, macOS DMG/signing/notarization paths | [`Package-ReleaseArtifact.ps1`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/build/Package-ReleaseArtifact.ps1), [`ci.yml`](https://github.com/AIDotNet/CodexSwitch/blob/6152418fd1de7779010d4786b79060e651649109/.github/workflows/ci.yml) |

## Lessons worth borrowing for WT Launcher

### 1. Keep UI out of hand-drawn WinForms

The retired WinForms line proved that cards, hover, truncation, stats, and dialogs become expensive when manually painted. Avalonia lets a launcher card become an actual component with layout, binding, styles, and theme resources.

Current local translation:

```text
src/avalonia-launcher/
  src/WTLauncher.App/       Avalonia shell, pages, dialogs, styles
  src/WTLauncher.Core/      config parsing, launch argument builder, safety policy
  tests/WTLauncher.Tests/   xUnit tests for core behavior
```

### 2. Use MVVM for launcher state

Instead of keeping UI state as form fields and event handlers, model it as a view model:

```text
LauncherViewModel
  Entries
  FilterText
  SelectedEntries
  LastLaunched
  LaunchCommand
  EditCommand
  DeleteCommand
  BatchLaunchCommand
```

This maps directly to launcher behavior and makes command logic testable without opening a GUI.

### 3. Treat design tokens/components as first-class

CodexSwitch has a component/theme layer. WT Launcher should not copy that whole library, but it should copy the principle:

- `LauncherCard`
- `CommandPreview`
- `EditEntryDialog`
- `BatchBar`
- `StatusBadge`

If those are components, UX iteration does not require editing launch logic.

### 4. Keep release proof close to the app

CodexSwitch uses `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet publish`. WT Launcher keeps a local PowerShell front door:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

Native AOT should remain a later option, not a day-one requirement. The current mainline first proves normal self-contained `win-x64` publish.

## What not to copy blindly

| CodexSwitch trait | Why not copy directly into WT Launcher |
| --- | --- |
| Local AI proxy, protocol adapters, OAuth, usage dashboard | WT Launcher is a local command launcher; these are unrelated product scope. |
| Full multi-platform packaging matrix | WT Launcher is currently Windows-first; start with `win-x64` only. |
| Native AOT from the first spike | AOT/trimming can add avoidable friction. Prove normal self-contained publish first. |
| Large sidebar/dashboard shape | The HTA lesson says WT Launcher wants less chrome, not more. |
| Submodule/component-library dependency | Useful pattern, but WT Launcher should begin with local minimal components. |
| Upstream code copying | The upstream README states no license file was declared at review time; use ideas and public docs, not copied source. |

## Current local state

The former candidate spike has been promoted:

```text
src/avalonia-launcher/
  WTLauncher.Avalonia.sln
  src/WTLauncher.App/
  src/WTLauncher.Core/
  tests/WTLauncher.Tests/
```

The retired WinForms line is documented in `retired/design/winforms-launcher.md`; the old root `WT Launcher.exe` and `src/winforms-launcher/` should not return unless there is an explicit rollback decision.

## Decision lens versus other candidates

| Stack | Local conclusion after studying CodexSwitch |
| --- | --- |
| Retired WinForms | Keep only the design lessons; do not restore as a parallel source tree. |
| WebView2 | Fastest route to HTA-like HTML/CSS feel, but introduces JS bridge and web/runtime boundary. |
| Avalonia | Current mainline candidate for a long-lived C# native desktop launcher with modern UI and testable state. |
| Tauri | Strong long-term web/Rust route, but heavier migration for this small Windows launcher. |
| WPF / WinUI 3 | Better than WinForms, but less compelling than Avalonia for cross-platform modern .NET and component reuse. |
| Electron | UI-friendly but too heavy for a small local launcher. |

## Official references for future study

- Avalonia overview: <https://docs.avaloniaui.net/docs/overview/what-is-avalonia>
- Avalonia getting started: <https://docs.avaloniaui.net/docs/get-started/test-drive/introduction>
- CommunityToolkit.Mvvm: <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/>
- .NET Native AOT deployment: <https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/>
- .NET self-contained deployment: <https://learn.microsoft.com/en-us/dotnet/core/deploying/>
- xUnit: <https://xunit.net/>
