# Phase D: 构建加速

## 实施列表

### D1: MSBuild.CompilerCache
在 Core/App/Tests 项目中添加 MSBuild.CompilerCache NuGet 引用。
配置环境变量 MSBuildCompilerCache_RootPath=D:\.buildcache

### D2: Central Package Management
创建 Directory.Packages.props 统一版本。

### D3: CI NuGet 缓存
在 .github/workflows/verify.yml 添加 nuget 缓存步骤。

## 验收标准
- dotnet build 无代码变更时 <10s
- dotnet test 全部通过
- verify.ps1 通过
