namespace WTLauncher.Core;

// ── Load errors ───────────────────────────────────────────

public abstract record StoreLoadError
{
    public abstract string Message { get; }

    public record FileNotFound(string Path) : StoreLoadError
    {
        public override string Message => $"找不到配置文件: {Path}";
    }

    public record FileTooLarge(long SizeBytes) : StoreLoadError
    {
        public override string Message => $"配置文件过大 ({SizeBytes / 1024 / 1024} MB)，超过 50 MB 限制。";
    }

    public record SchemaTooNew(int Version) : StoreLoadError
    {
        public override string Message => $"配置文件版本 {Version} 过新，请升级 WT Launcher。";
    }

    public record JsonError(string Details) : StoreLoadError
    {
        public override string Message => $"配置文件 JSON 格式错误: {Details}";
    }

    public record ValidationError(string Details) : StoreLoadError
    {
        public override string Message => $"配置数据验证失败: {Details}";
    }

    public record IoError(string Details) : StoreLoadError
    {
        public override string Message => $"文件读取失败: {Details}";
    }
}

// ── Save errors ───────────────────────────────────────────

public abstract record StoreSaveError
{
    public abstract string Message { get; }

    public record IoError(string Details) : StoreSaveError
    {
        public override string Message => $"文件保存失败: {Details}";
    }

    public record SerializationError(string Details) : StoreSaveError
    {
        public override string Message => $"数据序列化失败: {Details}";
    }

    public record AccessDenied(string Path) : StoreSaveError
    {
        public override string Message => $"没有写入权限: {Path}";
    }
}
