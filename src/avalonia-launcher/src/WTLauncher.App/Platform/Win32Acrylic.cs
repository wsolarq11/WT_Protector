using System.Runtime.InteropServices;

namespace WTLauncher.App.Platform;

/// <summary>
/// Win32 SetWindowCompositionAttribute interop for acrylic blur effect.
/// Works on Windows 10 1803+ (build 17134+) — does not depend on Avalonia's
/// TransparencyLevelHint which requires newer builds for AcrylicBlur.
/// </summary>
internal static class Win32Acrylic
{
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_ENABLE_HOSTBACKDROP = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttribData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private const int WcaAccentPolicy = 19;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttribData data);

    /// <summary>
    /// Enable acrylic blur on the given window handle.
    /// </summary>
    /// <param name="hwnd">Native window handle (HWND).</param>
    /// <param name="tintColor">Tint color in 0xAABBGGRR format (Win32 COLORREF order). Default semi-transparent light gray-blue.</param>
    internal static void EnableAcrylic(IntPtr hwnd, uint tintColor = 0x40FCFAF8)
    {
        if (hwnd == IntPtr.Zero)
            return;

        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 0,
            GradientColor = tintColor,
            AnimationId = 0,
        };

        ApplyAccent(hwnd, accent);
    }

    /// <summary>
    /// Disable acrylic blur, restoring the window to solid rendering.
    /// </summary>
    internal static void DisableAcrylic(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_DISABLED,
        };

        ApplyAccent(hwnd, accent);
    }

    private static void ApplyAccent(IntPtr hwnd, AccentPolicy accent)
    {
        var structSize = Marshal.SizeOf<AccentPolicy>();
        var ptr = Marshal.AllocHGlobal(structSize);

        try
        {
            Marshal.StructureToPtr(accent, ptr, false);

            var data = new WindowCompositionAttribData
            {
                Attribute = WcaAccentPolicy,
                Data = ptr,
                SizeOfData = structSize,
            };

            _ = SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// Build a Win32 gradient color value from ARGB components.
    /// Win32 uses 0xAABBGGRR format (blue and red swapped vs. standard ARGB).
    /// </summary>
    internal static uint ToGradientColor(byte alpha, byte red, byte green, byte blue) =>
        (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
}
