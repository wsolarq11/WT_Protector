# 测试修复与补充
## Requirements
### 删除 (7项脆弱测试)
- [ ] MainWindowXamlContractTests (5项), DialogLayoutContractTests, FeatureAuditTests
### 重构 (3项反射测试)
- [ ] 卡密度/拖拽/排序状态测试→行为断言
### 新增 (14项覆盖缺口)
- [ ] 并发、拖拽+视图切换、最后预设删除、大数据集、Unicode边界、UNC、损坏.bak、
  快速拖拽、0条目预设、全角注入、模板循环、NaN/Infinity、跨256字符、临时文件清理
## Acceptance
- [ ] dotnet test 全量 PASS 零失败零跳过