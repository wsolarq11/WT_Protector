using System.ComponentModel;
using System.Runtime.InteropServices;
using WTLauncher.App.Localization;

namespace WTLauncher.App.Platform;

/// <summary>
/// Minimal Win32 tray icon with Open/Quit context menu. Implemented locally so
/// the app does not depend on a third-party tray package under Native AOT.
/// </summary>
internal sealed class Win32TrayIcon : IDisposable
{
    private const uint WmApp = 0x8000;
    private const uint CallbackMessage = WmApp + 1;
    private const uint WmContextMenu = 0x007B;
    private const uint WmRButtonUp = 0x0205;
    private const uint WmLButtonDblClk = 0x0203;
    private const uint NinSelect = 0x0400;

    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NimSetVersion = 0x00000004;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NotifyIconVersion4 = 4;

    private const uint ImageIcon = 1;
    private const uint LrDefaultSize = 0x00000040;
    private const uint LrShared = 0x00008000;

    private const uint MfString = 0x00000000;
    private const uint TpmReturnCmd = 0x00000100;
    private const uint TpmRightButton = 0x00000002;
    private const uint TpmNoNotify = 0x00000080;

    private const uint OpenCommandId = 1;
    private const uint QuitCommandId = 2;
    private const string WindowClassName = "WTLauncherTrayWindow";

    private static readonly HashSet<string> RegisteredClasses = [];
    private static WndProcDelegate? _keepAliveDelegate;

    private readonly WndProcDelegate _wndProc;
    private readonly IntPtr _window;
    private readonly IntPtr _icon;
    private NOTIFYICONDATA _notifyData;
    private bool _disposed;

    public event Action? OpenRequested;
    public event Action? QuitRequested;

    public Win32TrayIcon(string tooltip)
    {
        _wndProc = WndProc;
        _keepAliveDelegate = _wndProc;
        RegisterWindowClass();
        _window = CreateWindowExW(
            0,
            WindowClassName,
            "WTLauncherTray",
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
            throw new Win32Exception(Marshal.GetLastWin32Error(), Win32PlatformDiagnostics.TrayWindowCreationFailed);

        _icon = ExtractApplicationIcon();
        _notifyData = new NOTIFYICONDATA
        {
            CbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            HWnd = _window,
            UID = 0,
            UFlags = NifMessage | NifIcon | NifTip,
            UCallbackMessage = CallbackMessage,
            HIcon = _icon,
            SzTip = string.IsNullOrWhiteSpace(tooltip) ? "WT Launcher" : tooltip,
        };
        if (!Shell_NotifyIconW(NimAdd, ref _notifyData))
            throw new Win32Exception(Marshal.GetLastWin32Error(), Win32PlatformDiagnostics.TrayIconAddFailed);

        _notifyData.UTimeoutOrVersion = NotifyIconVersion4;
        _ = Shell_NotifyIconW(NimSetVersion, ref _notifyData);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _ = Shell_NotifyIconW(NimDelete, ref _notifyData);
        if (_window != IntPtr.Zero)
            _ = DestroyWindow(_window);
        if (_icon != IntPtr.Zero)
            _ = DestroyIcon(_icon);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == CallbackMessage)
        {
            var trayMessage = (uint)lParam.ToInt64();
            if (trayMessage is WmContextMenu or WmRButtonUp)
                ShowContextMenu();
            else if (trayMessage is WmLButtonDblClk or NinSelect)
                OpenRequested?.Invoke();
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static IntPtr ExtractApplicationIcon()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            _ = ExtractIconExW(path, 0, out var large, out var small, 1);
            if (small != IntPtr.Zero)
                return small;
            if (large != IntPtr.Zero)
                return large;
        }

        return LoadImage(IntPtr.Zero, new IntPtr(32512), ImageIcon, 0, 0, LrDefaultSize | LrShared);
    }

    private void ShowContextMenu()
    {
        var point = new POINT();
        _ = GetCursorPos(ref point);

        var menu = CreatePopupMenu();
        _ = AppendMenuW(menu, MfString, OpenCommandId, LauncherLocalization.Format("tray.open"));
        _ = AppendMenuW(menu, MfString, QuitCommandId, LauncherLocalization.Format("tray.quit"));

        _ = SetForegroundWindow(_window);
        var command = TrackPopupMenu(menu, TpmReturnCmd | TpmRightButton | TpmNoNotify, point.X, point.Y, 0, _window, IntPtr.Zero);
        _ = DestroyMenu(menu);

        if (command == OpenCommandId)
            OpenRequested?.Invoke();
        else if (command == QuitCommandId)
            QuitRequested?.Invoke();
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
            throw new Win32Exception(Marshal.GetLastWin32Error(), Win32PlatformDiagnostics.TrayWindowClassRegistrationFailed);
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint CbSize;
        public IntPtr HWnd;
        public uint UID;
        public uint UFlags;
        public uint UCallbackMessage;
        public IntPtr HIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string SzTip;
        public uint DwState;
        public uint DwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string SzInfo;
        public uint UTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string SzInfoTitle;
        public uint DwInfoFlags;
        public Guid GuidItem;
        public IntPtr HBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
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
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(ref POINT lpPoint);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconExW(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);
}
