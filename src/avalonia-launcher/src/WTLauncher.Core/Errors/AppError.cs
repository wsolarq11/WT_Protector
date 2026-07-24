using OneOf;

namespace WTLauncher.Core.Errors;

/// <summary>
/// Global error ADT (Algebraic Data Type) for all launcher operations.
/// Every fallible operation returns a <c>OneOf&lt;TResult, AppError&gt;</c>
/// instead of throwing. This makes every error path visible at the type level
/// and eliminates entire classes of "forgot to handle the error" bugs.
///
/// When a new error variant is added, every <c>.Match</c> / <c>.Switch</c>
/// call site that handles <c>OneOf&lt;..., AppError&gt;</c> will fail to
/// compile until the new variant is handled — no runtime surprises.
/// </summary>
public abstract record AppError
{
    private AppError() { }

    /// <summary>Configuration file failed to load.</summary>
    public sealed record ConfigLoadFailed(string Path, string Detail) : AppError;

    /// <summary>Configuration file failed to save.</summary>
    public sealed record ConfigSaveFailed(string Path, string Detail) : AppError;

    /// <summary>An entry with the given ID was not found.</summary>
    public sealed record EntryNotFound(string EntryId) : AppError;

    /// <summary>The store file is corrupted and could not be recovered.</summary>
    public sealed record StoreCorrupted(string Path, string Detail) : AppError;

    /// <summary>A validation check failed.</summary>
    public sealed record ValidationFailed(string Field, string Reason) : AppError;

    /// <summary>A required resource (file, directory) was not found.</summary>
    public sealed record ResourceNotFound(string ResourceType, string Identifier) : AppError;

    /// <summary>An I/O operation failed (disk full, access denied, etc.).</summary>
    public sealed record IoFailed(string Operation, string Path, string Detail) : AppError;

    /// <summary>An unexpected but recoverable error occurred.</summary>
    public sealed record Unexpected(string Context, string Detail) : AppError;
}
