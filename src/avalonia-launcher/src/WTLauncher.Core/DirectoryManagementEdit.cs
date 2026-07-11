namespace WTLauncher.Core;

/// <summary>
/// 地址管理 dialog → owner 的编辑意图 (directory 字段操作)。
/// ponytail: sealed discriminator union, owner pattern-match 处理。
/// </summary>
public abstract record DirectoryManagementEdit
{
    private DirectoryManagementEdit() { }

    public sealed record RenameDirectory(string OldDirectory, string NewDirectory) : DirectoryManagementEdit;
    public sealed record MergeDirectories(string LoserDirectory, string KeeperDirectory) : DirectoryManagementEdit;
    public sealed record DeleteDirectory(string Directory) : DirectoryManagementEdit;
}

/// <summary>
/// Owner 应用一次 edit 后的返回: 成败 + 状态消息 + 派生的最新 directory rows。
/// </summary>
public sealed record DirectoryManagementResult(
    bool Success,
    string StatusMessage,
    IReadOnlyList<DirectoryRow> UpdatedDirectoryRows);
