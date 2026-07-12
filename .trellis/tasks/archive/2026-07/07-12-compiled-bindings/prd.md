# P1: 启用 CompiledBindings (Avalonia 编译时绑定)

## Goal

开启 `AvaloniaUseCompiledBindingsByDefault=true`，为所有 DataTemplate 添加 `x:DataType` 指令，让 XAML 绑定从运行时反射解析变为编译时类型安全检测。

## Requirements

1. `WTLauncher.App.csproj` 中 `AvaloniaUseCompiledBindingsByDefault` 设为 `true`
2. `MainWindow.axaml` 中所有 `<DataTemplate>` 都有正确的 `x:DataType` 指令
3. MainWindow 根元素添加 `x:DataType="vm:MainWindowViewModel"` 用于顶层绑定
4. 处理 `{Binding !IsVisibleEmpty}` 等特殊绑定模式的兼容性

## Acceptance Criteria

- [ ] `dotnet build -c Release --no-restore -warnaserror` 通过（无 AVLN 错误）
- [ ] `dotnet test -c Release` 全部通过（400+ tests）
- [ ] `tools/verify.ps1` 通过（19项检查）
- [ ] 所有 DataTemplate 中的绑定都正确解析
