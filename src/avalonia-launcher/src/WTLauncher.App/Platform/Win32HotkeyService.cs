using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WTLauncher.App.Platform;

/// <summary>
/// Registers a process-wide global hotkey through a hidden message-only window.
/// The hotkey event is raised on the Win32 UI thread, so callers should marshal
/// to Avalonia's UI thread before touching windows.
/// </summary>
internal sealed class Win32HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x5A44;
    private const string WindowClassName = "WTLauncherQuickLaunchHotkeyWindow";

    private static readonly HashSet<string> RegisteredClasses = [];
    private static WndProcDelegate? _keepAliveDelegate;

    private readonly WndProcDelegate _wndProc;
    private readonly IntPtr _window;
    private bool _registered;

    public event Action? HotkeyPressed;

    public Win32HotkeyService(uint modifiers, uint key)
    {
        _wndProc = WndProc;
        _keepAliveDelegate = _wndProc;
        RegisterWindowClass();
        _window = CreateWindowExW(
            0,
            WindowClassName,
            "WTLauncherQuickLaunchHotkey",
            0,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            GetModuleHandleW(null),
            IntPtr.Zero);
        if (_window == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), Win32PlatformDiagnostics.HotkeyWindowCreationFailed);

        _registered = RegisterHotKey(_window, HotkeyId, modifiers, key);
        if (!_registered)
            throw new Win32Exception(Marshal.GetLastWin32Error(), Win32PlatformDiagnostics.HotkeyRegistrationFailed);
    }

    public void Dispose()
    {
        if (_registered)
        {
            _ = UnregisterHotKey(_window, HotkeyId);
            _registered = false;
        }

        if (_window != IntPtr.Zero)
        {
            _ = DestroyWindow(_window);
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmHotkey && wParam == HotkeyId)
            HotkeyPressed?.Invoke();

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void RegisterWindowClass()
    {
        if (!RegisteredClasses.Add(WindowClassName))
            return;

        var windowClass = new WNDCLASSW
        {
            LpfnWndProc = _wndProc,
            HInstance = GetModuleHandleW(null),
            LpszClassName = WindowClassName,
        };
        if (RegisterClassW(ref windowClass) == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), Win32PlatformDiagnostics.HotkeyWindowClassRegistrationFailed);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSW
    {
        public uint Style;
        public WndProcDelegate? LpfnWndProc;
        public int CbClsExtra;
        public int CbWndExtra;
        public IntPtr HInstance;
        public IntPtr HIcon;
        public IntPtr HCursor;
        public IntPtr HbrBackground;
        public string? LpszMenuName;
        public string? LpszClassName;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassW(ref WNDCLASSW lpWndClass);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);
}
