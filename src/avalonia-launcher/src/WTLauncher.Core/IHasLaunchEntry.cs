namespace WTLauncher.Core;

/// <summary>
/// Minimal interface for types that carry a <see cref="LaunchEntry"/>, enabling
/// generic sorting and filtering without depending on a specific ViewModel type.
/// </summary>
public interface IHasLaunchEntry
{
    LaunchEntry Entry { get; }
}
