# Phase A: 分析器重新接线 — 实施计划

## 实施步骤

### Step 1: `Directory.Build.props` + `.csproj` 修复
- [ ] 在 `src/avalonia-launcher/` 创建 `Directory.Build.props`
- [ ] 添加 Analyzer ProjectReference 指向 Analyzers 项目
- [ ] CI 环境变量控制：`$(CI)=='true'` 时全量分析器，本地 Debug 零分析器

### Step 2: WT1003 修复（DispatchMustBeOnlyWritePathAnalyzer）
- [ ] `AnalyzeInvocation` 增加 `IStorePersistence.Save` 检测
- [ ] 保留 `LauncherStore.Save` 旧检测
- [ ] 验证：故意添加 `_persistence.Save(...)` 绕过 → 构建失败

### Step 3: WT1001 修复（LayerViolationAnalyzer）
- [ ] `RegisterSyntaxNodeAction` 增加 `QualifiedNameSyntax` 注册
- [ ] 增加 `MemberAccessExpressionSyntax` 注册
- [ ] 验证：Core 中写 `new WTLauncher.App.ViewModels.X()` → 构建失败

### Step 4: WT1005 修复（ResourceKeyAnalyzer）
- [ ] 检查 App.csproj 中 `.axaml` 是否已为 `AdditionalTexts`
- [ ] 如无则添加：`<AdditionalFiles Include="**/*.axaml" />`
- [ ] `CollectXamlKeys` 用正则 `\{StaticResource\s+(\w+)\}` 提取 XAML 引用
- [ ] 验证：删除一个 Resources 定义 → 构建产生 WT1005 错误

### Step 5: WT1006 修复（JsonRegistrationAnalyzer）
- [ ] `RegisterSyntaxNodeAction` 注册 `TypeOfExpressionSyntax`
- [ ] 收集 `typeof(SomeType)` 在 `LauncherJson.Save<T>/Load<T>` 中的使用
- [ ] 对比 `LauncherJsonContext` 的 `[JsonSerializable(typeof(T))]` 属性
- [ ] 验证：对未注册类型调用 `LauncherJson.Save<T>()` → 构建失败

### Step 6: verify.ps1 门禁
- [ ] 添加 `Add-Check 'analyzer-wired'`：`dotnet build` 后查 msbuild 日志含分析器加载事件
- [ ] 确认 WT1004 在构建时实际触发

### Step 7: 全量验证
- [ ] `dotnet build -warnaserror` 通过
- [ ] `dotnet test` 全部通过
- [ ] `powershell -File tools\verify.ps1` 通过
