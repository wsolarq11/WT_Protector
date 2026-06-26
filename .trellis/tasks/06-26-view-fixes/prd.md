# View/XAML 修复
## Requirements
- [ ] R1: FastWrapPanel NaN 守卫
- [ ] R2: 键盘焦点 visibleCount==0 越界修复
- [ ] R3: Ghost 失败加 ErrorLog
- [ ] R4: SizeChanged/Opened/KeyDown 在 OnClosing 解注册
- [ ] R5: Details ScrollViewer null 加 ErrorLog
- [ ] R6: 滚动版本计数器溢出防护
## Acceptance
- [ ] dotnet test MainWindow 相关 PASS