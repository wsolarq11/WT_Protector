# Phase A: 分析器重新接线 — 设计

## 当前状态

```xml
<!-- WTLauncher.App.csproj — 没有 Analyzer 引用 -->
<ProjectReference Include="..\WTLauncher.Core\WTLauncher.Core.csproj" />
```

## 目标状态

```xml
<!-- 在 Directory.Build.props（方案根目录）统一添加 -->
<ItemGroup>
  <ProjectReference Include="src\WTLauncher.Analyzers\WTLauncher.Analyzers.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## 分析器修复方案

### WT1003（DispatchMustBeOnlyWritePathAnalyzer）
- 当前：检查 `LauncherStore.Save` — 重构后真实写路径是 `IStorePersistence.Save`
- 修复：增加对 `IStorePersistence.Save` 的检查；保留对 `LauncherStore.Save` 的检查（向下兼容）

### WT1001（LayerViolationAnalyzer）
- 当前：只检查 `using` 指令
- 修复：增加 `QualifiedNameSyntax` 和 `MemberAccessExpressionSyntax` 注册，检测全限定名引用

### WT1005（ResourceKeyAnalyzer）
- 当前：`CollectXamlKeys` 返回空集合（scaffolding）
- 方案：补充正则解析 `.axaml` AdditionalTexts 文件提取 `{StaticResource KeyName}` 引用
- 失败方案：标记 `[Obsolete]` 并引用 `verify.ps1` 的 raw-hex-gate 作为替代

### WT1006（JsonRegistrationAnalyzer）
- 当前：Intentionally disabled（scaffolding）
- 方案：实现 `TypeOfExpressionSyntax` 注册检测 + `LauncherJsonContext` 中 `[JsonSerializable]` 属性对比
- 失败方案：标记 `[Obsolete]` 并引用已有的 `LauncherJsonContextRegistrationTests`

## 门禁

在 `verify.ps1` 添加：构建后验证分析器 DLL 实际加载。
