using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PersonalAssistant.Helpers;

public static class WindowHelper
{
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public static void SetClickThrough(Window window, bool enable)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (enable)
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
    }

    public static void SetToolWindow(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
    }

    public static void MakeTopmostSticky(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    public static void HideFromTaskbar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        extendedStyle |= WS_EX_TOOLWINDOW;
        extendedStyle &= ~WS_EX_APPWINDOW;
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
    }
}
