# Design — CQ03 CommandManagementWindow XAML

## Boundary

新增 `CommandManagementWindow.axaml` 与精简后的 `.axaml.cs`。ViewModel 与 Core edit/result contracts 不变。XAML拥有稳定视觉结构；code-behind 只把按钮点击路由到需要 owner Window 的 modal editor，再调用现有 ViewModel command。

## View Structure

```text
Window
  Grid
    Title + subtitle
    tab buttons (ActiveTab commands/classes)
    command panel
      toolbar + ItemsControl<CommandRow> + empty state
    template panel
      toolbar + ItemsControl<TemplateRow> + empty state
    status + close
```

命令与模板 DataTemplate 使用现有 row records。若 row 本身没有 ViewModel command，则按钮事件携带 `CommandParameter`，code-behind 仅打开对应 modal；mutation 仍由 `CommandManagementViewModel` 公共方法/command执行。

## Compatibility

- `DialogService.OpenCommandManagementAsync` 构造方式保持不变。
- 不改变 `CommandManagementEdit`, `CommandManagementResult`, `CommandRow`, `TemplateRow`。
- 不改变窗口尺寸、中文文案、reference count、危险操作禁用规则。

## Rollback

迁移无 schema 或业务变化；可整提交 revert 回旧 code-behind 窗口。若 compiled binding 暴露缺失 projection，先补只读 ViewModel projection，不在 code-behind 建镜像。
