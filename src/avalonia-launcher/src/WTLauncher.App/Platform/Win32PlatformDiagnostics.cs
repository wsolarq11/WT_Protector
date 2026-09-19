namespace WTLauncher.App.Platform;

/// <summary>Win32 platform diagnostic originals. These are not localized;
/// they are stable diagnostic text, sanitized by the presentation layer
/// before any user-facing display.</summary>
internal static class Win32PlatformDiagnostics
{
    public const string TrayWindowCreationFailed = "Failed to create the tray message window.";
    public const string TrayIconAddFailed = "Failed to add the system tray icon.";
    public const string TrayWindowClassRegistrationFailed = "Failed to register the tray message window class.";
    public const string HotkeyWindowCreationFailed = "Failed to create the global hotkey message window.";
    public const string HotkeyRegistrationFailed = "Global hotkey registration failed; the combination may already be in use.";
    public const string HotkeyWindowClassRegistrationFailed = "Failed to register the global hotkey message window class.";
}
