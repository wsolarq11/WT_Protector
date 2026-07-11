namespace WTLauncher.Core;

/// <summary>
/// 命令管理 dialog → owner 的编辑意图 (command field 操作)。
/// ponytail: sealed discriminator union, owner pattern-match 处理。
/// </summary>
public abstract record CommandManagementEdit
{
    private CommandManagementEdit() { }

    public sealed record RenameCommand(string OldCommand, string NewCommand) : CommandManagementEdit;
    public sealed record MergeCommands(string LoserCommand, string KeeperCommand) : CommandManagementEdit;
    public sealed record DeleteCommand(string Command) : CommandManagementEdit;

    public sealed record UpdateTemplateKey(string OldKey, string NewKey, string ExpandedValue) : CommandManagementEdit;
    public sealed record UpdateTemplateValue(string Key, string NewExpandedValue) : CommandManagementEdit;
    public sealed record AddTemplate(string Key, string ExpandedValue) : CommandManagementEdit;
    public sealed record DeleteTemplate(string Key) : CommandManagementEdit;
}

/// <summary>
/// Owner 应用一次 edit 后的返回: 成败 + 状态消息 + 派生的最新 command/template rows。
/// VM 用 UpdatedXxxRows 刷新表格。
/// </summary>
public sealed record CommandManagementResult(
    bool Success,
    string StatusMessage,
    IReadOnlyList<CommandRow> UpdatedCommandRows,
    IReadOnlyList<TemplateRow> UpdatedTemplateRows);
