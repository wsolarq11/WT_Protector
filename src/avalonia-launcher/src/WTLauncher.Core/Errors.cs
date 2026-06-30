namespace WTLauncher.Core;

// ── Id errors ──────────────────────────────────────────────

public abstract record IdError
{
    public abstract string Message { get; }

    public record Empty : IdError
    {
        public override string Message => "ID 不能为空。";
    }

    public record ReservedName(string Value) : IdError
    {
        public override string Message => $"ID \"{Value}\" 是 Windows 保留名称。";
    }

    public record PathSeparator(string Value) : IdError
    {
        public override string Message => $"ID \"{Value}\" 包含路径分隔符或 \"..\"。";
    }

    public record TooLong(int Length) : IdError
    {
        public override string Message => $"ID 长度 {Length} 超过 256 字符限制。";
    }
}

// ── Name errors ────────────────────────────────────────────

public abstract record NameError
{
    public abstract string Message { get; }

    public record Empty : NameError
    {
        public override string Message => "名称不能为空。";
    }

    public record TooLong(int Length, int MaxLength) : NameError
    {
        public override string Message => $"名称长度 {Length} 超过 {MaxLength} 字符限制。";
    }
}

// ── Directory errors ───────────────────────────────────────

public abstract record DirectoryError
{
    public abstract string Message { get; }

    public record Empty : DirectoryError
    {
        public override string Message => "目录不能为空。";
    }

    /// <summary>Fullwidth % (U+FF05) was normalized to ASCII %, which would trigger
    /// unintended environment-variable expansion. Reject the input before expansion.</summary>
    public record PercentInjection : DirectoryError
    {
        public override string Message => "目录包含规范化后引入 % 的字符，不允许。";
    }

    public record UncPath(string Path) : DirectoryError
    {
        public override string Message => "目录不允许使用网络路径 (UNC)。";
    }

    public record DevicePath(string Path) : DirectoryError
    {
        public override string Message => "目录不允许使用设备路径。";
    }

    public record NotFound(string Path) : DirectoryError
    {
        public override string Message => $"目录不存在: {Path}";
    }
}

// ── Shell errors ───────────────────────────────────────────

public abstract record ShellError
{
    public abstract string Message { get; }

    public record NotFound(string Path) : ShellError
    {
        public override string Message => $"未找到 shell 可执行文件: {Path}。请安装 PowerShell Core (pwsh) 或配置其他 shell。";
    }
}
