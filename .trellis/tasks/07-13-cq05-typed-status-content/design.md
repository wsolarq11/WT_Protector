# Design — CQ05 Typed Status Content

## Authority

MainWindowViewModel 持有一个 LauncherStatusContent。StatusText、StatusTone、StatusIndicator 和 IsStatus* 都是只读投影；不存在第二个可写 tone。

~~~text
workflow formatter
  -> LauncherStatusContent(Text, Tone)
  -> ServiceCanceled/ServiceFailure or direct ViewModel operation
  -> MainWindowViewModel.StatusContent
  -> existing status XAML bindings
~~~

## Contract

LauncherStatusContent 是 App/ViewModel 层的小值对象：Text 不得为空白，Tone 是现有 LauncherStatusTone。提供按 tone 命名的 factory，调用方不直接猜 marker。

ServiceResult 的取消/失败状态改为该类型；成功结果保持不携带状态。保存失败等 legacy string 边界在唯一入口显式包装为 Error，而不引入隐式 string 转换。

## Compatibility

XAML 继续绑定 StatusText 和 IsStatus*，视觉结构不变。现有文本应逐字保持；测试从比较 string 改为比较 Status.Text 或整个值。LauncherStatusPresentation 只保留 IndicatorFor。

## Rollback

先建立类型和值投影，再迁移 service results，最后迁移 formatter/call sites。每个阶段立即 build；若发现未分类的动态状态，先在 owning workflow formatter 增加方法，不恢复 marker heuristic。
