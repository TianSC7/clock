using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PersonalAssistant.Helpers;

public static class TopmostManager
{
    private static readonly List<(IntPtr hwnd, int priority)> _windows = new();
    private static readonly object _lock = new();
    private static DispatcherTimer? _timer;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    public static void Register(Window window, int priority)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        Register(hwnd, priority);
    }

    public static void Register(IntPtr hwnd, int priority)
    {
        lock (_lock)
        {
            var existing = _windows.FirstOrDefault(w => w.hwnd == hwnd);
            if (existing.hwnd != IntPtr.Zero)
            {
                _windows.Remove(existing);
            }
            _windows.Add((hwnd, priority));
            EnsureTimerRunning();
        }
    }

    public static void Unregister(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        Unregister(hwnd);
    }

    public static void Unregister(IntPtr hwnd)
    {
        lock (_lock)
        {
            var existing = _windows.FirstOrDefault(w => w.hwnd == hwnd);
            if (existing.hwnd != IntPtr.Zero)
            {
                _windows.Remove(existing);
            }
            StopTimerIfEmpty();
        }
    }

    private static void EnsureTimerRunning()
    {
        if (_timer != null && _timer.IsEnabled)
            return;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private static void StopTimerIfEmpty()
    {
        if (_windows.Count == 0 && _timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    private static void OnTimerTick(object? sender, EventArgs e)
    {
        List<(IntPtr hwnd, int priority)> snapshot;
        lock (_lock)
        {
            snapshot = _windows.OrderBy(w => w.priority).ToList();
        }

        foreach (var (hwnd, _) in snapshot)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
    }
}