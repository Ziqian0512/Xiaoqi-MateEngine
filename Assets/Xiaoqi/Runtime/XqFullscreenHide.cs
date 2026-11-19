using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Kirurobo;
using UnityEngine;

/// <summary>
/// P1-2d: default topmost, and hide with SW_HIDE while another window covers a whole monitor.
/// Maximized windows (taskbar still visible) do not count. Hit-test math is unchanged; it is only paused while hidden.
/// </summary>
public class XqFullscreenHide : MonoBehaviour
{
    const int SW_HIDE = 0;
    const int SW_SHOWNOACTIVATE = 4;
    const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    // XQ: P1-6 托盘「隐藏小祈」。和「全屏躲开」共用同一套隐藏/恢复，但手动隐藏优先，且不因退出全屏而自动恢复。
    // 不持久化：重启后她一定是显示的（启动就看不见会让人以为坏了）。
    public static bool ManualHidden { get; private set; }

    public static void SetManualHidden(bool on)
    {
        ManualHidden = on;
        UnityEngine.Debug.Log("XQ_TRAY manual_hidden=" + on);
        var h = UnityEngine.Object.FindFirstObjectByType<XqFullscreenHide>();
        if (h != null) h._nextCheck = 0f; // 下一帧就处理，不等 1 秒
    }

    static UniWindowController.HitTestType _savedHitTest = UniWindowController.HitTestType.Opacity;
    static bool _hasSavedHitTest;

    bool _hidden;
    IntPtr _benchHwnd;
    float _nextCheck;
    uint _selfPid;

    void Awake()
    {
        _selfPid = (uint)Process.GetCurrentProcess().Id;
    }

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + (_hidden ? 0.25f : 1f);
        if (ManualHidden)
        {
            if (!_hidden) HideSelf();
            return;
        }
        if (Xiaoqi.Mode.XqCloseCamera.IsActive && !_hidden) return;
        var cfg = XiaoqiRuntime.Config;
        if (cfg != null && !cfg.hide_on_fullscreen)
        {
            if (_hidden) ShowAgain();
            return;
        }
        bool full = ForegroundIsFullscreen();
        if (full && !_hidden) HideSelf();
        else if (!full && _hidden) ShowAgain();
    }

    void HideSelf()
    {
        var hwnd = SelfWindow();
        if (hwnd == IntPtr.Zero) return;
        SetHitTest(false);
        ShowWindow(hwnd, SW_HIDE);
        // XQ: P3 全屏躲开 × 工作台：网页窗口跟她一起藏
        _benchHwnd = Xiaoqi.Mode.XqBenchWindow.Hwnd;
        if (_benchHwnd != IntPtr.Zero) ShowWindow(_benchHwnd, SW_HIDE);
        _hidden = true;
        UnityEngine.Debug.Log("XQ_FS hide");
    }

    void ShowAgain()
    {
        var hwnd = SelfWindow();
        if (hwnd != IntPtr.Zero) ShowWindow(hwnd, SW_SHOWNOACTIVATE);
        if (_benchHwnd != IntPtr.Zero) { ShowWindow(_benchHwnd, SW_SHOWNOACTIVATE); _benchHwnd = IntPtr.Zero; } // XQ
        SetHitTest(true);
        _hidden = false;
        UnityEngine.Debug.Log("XQ_FS show");
    }

    static void SetHitTest(bool on)
    {
        var uw = UnityEngine.Object.FindFirstObjectByType<UniWindowController>();
        if (uw == null) return;
        if (!on)
        {
            if (uw.hitTestType != UniWindowController.HitTestType.None)
            {
                _savedHitTest = uw.hitTestType;
                _hasSavedHitTest = true;
            }
            uw.hitTestType = UniWindowController.HitTestType.None;
            uw.isHitTestEnabled = false;
        }
        else
        {
            if (_hasSavedHitTest) uw.hitTestType = _savedHitTest;
            uw.isHitTestEnabled = true;
        }
    }

    bool ForegroundIsFullscreen()
    {
        var fg = GetForegroundWindow();
        if (fg == IntPtr.Zero) return false;
        var self = SelfWindow();
        if (fg == self) return false;
        GetWindowThreadProcessId(fg, out uint pid);
        if (pid == _selfPid) return false;

        var cls = new StringBuilder(256);
        GetClassName(fg, cls, cls.Capacity);
        var name = cls.ToString();
        if (name == "Progman" || name == "WorkerW" || name == "Shell_TrayWnd") return false;

        if (!GetWindowRect(fg, out RECT wr)) return false;
        var mon = MonitorFromWindow(fg, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (mon == IntPtr.Zero || !GetMonitorInfo(mon, ref info)) return false;
        // 只看她所在的那块屏：另一块屏上的全屏窗口不该把她藏掉（主人 2026-11-16 定的规则，待确认）
        if (self != IntPtr.Zero)
        {
            var selfMon = MonitorFromWindow(self, MONITOR_DEFAULTTONEAREST);
            if (selfMon != IntPtr.Zero && selfMon != mon) return false;
        }
        var m = info.rcMonitor;
        const int tol = 8;
        int ww = wr.right - wr.left;
        int wh = wr.bottom - wr.top;
        int mw = m.right - m.left;
        int mh = m.bottom - m.top;
        if (ww < mw - tol || wh < mh - tol) return false;
        if (wr.left > m.left + tol || wr.top > m.top + tol) return false;
        if (wr.right < m.right - tol || wr.bottom < m.bottom - tol) return false;
        return true;
    }

    static IntPtr SelfWindow()
    {
        var hwnd = FindWindow("UnityWndClass", "小祈");
        if (hwnd == IntPtr.Zero) hwnd = FindWindow("UnityWndClass", null);
        return hwnd;
    }
}
