using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// XQ(WB) B1 第二轮 #14/#15：界面尺寸与摆位的唯一依据 = 她所在那块显示器（DPI + 可用区），不再看人物体形。
    /// 以前 PanelSettings.scale = 人物外框高/364（近景外框几千像素 → 顶到 2.0），设置页、近景左栏、菜单跟着放大遮人。
    /// 这里只给两样东西：UiScale()（显示器 DPI/96，夹 1~2）和 WorkArea()（她所在显示器的可用区，换成窗口屏幕坐标，原点左下）。
    /// 失败一律退回 scale=1、整个窗口当可用区。不改任何穿透/拖动判断。
    /// </summary>
    public static class XqMonitor
    {
        [StructLayout(LayoutKind.Sequential)] struct WRECT { public int left, top, right, bottom; }
        [StructLayout(LayoutKind.Sequential)] struct WPOINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] struct WMONINFO { public int cbSize; public WRECT rcMonitor; public WRECT rcWork; public uint dwFlags; }
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")] static extern IntPtr FindWindowW(string cls, string title);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out WRECT r);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(WPOINT pt, uint flags);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr mon, ref WMONINFO mi);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint dx, out uint dy);

        static IntPtr _hwnd;
        static float _scale = 1f;
        static float _scaleAt = -10f;
        static Rect _work;
        static bool _hasWork;
        static int _workFrame = -10;
        static Vector2 _workKey;

        static IntPtr Hwnd()
        {
            if (_hwnd == IntPtr.Zero) _hwnd = FindWindowW("UnityWndClass", "小祈");
            if (_hwnd == IntPtr.Zero) _hwnd = FindWindowW("UnityWndClass", null);
            return _hwnd;
        }

        static bool Mon(Vector2 screenPt, out WMONINFO mi, out WRECT wr, out IntPtr hm)
        {
            mi = new WMONINFO { cbSize = Marshal.SizeOf(typeof(WMONINFO)) };
            wr = default(WRECT);
            hm = IntPtr.Zero;
            try
            {
                var h = Hwnd();
                if (h == IntPtr.Zero || !GetWindowRect(h, out wr)) return false;
                var pt = new WPOINT { x = wr.left + Mathf.RoundToInt(screenPt.x), y = wr.top + Mathf.RoundToInt(Screen.height - screenPt.y) };
                hm = MonitorFromPoint(pt, 2);
                return hm != IntPtr.Zero && GetMonitorInfo(hm, ref mi);
            }
            catch (Exception) { return false; }
        }

        /// <summary>界面缩放：她所在显示器的有效 DPI/96，夹在 1~2。半秒缓存。</summary>
        public static float UiScale(Vector2 screenPt)
        {
            if (Time.unscaledTime - _scaleAt < 0.5f) return _scale;
            _scaleAt = Time.unscaledTime;
            float s = 1f;
            try
            {
                WMONINFO mi; WRECT wr; IntPtr hm;
                if (Mon(screenPt, out mi, out wr, out hm))
                {
                    uint dx = 0, dy;
                    if (GetDpiForMonitor(hm, 0, out dx, out dy) == 0 && dx > 0) s = dx / 96f; // 0 = MDT_EFFECTIVE_DPI
                }
            }
            catch (Exception) { s = 1f; }
            _scale = Mathf.Clamp(s, 1f, 2f);
            return _scale;
        }

        /// <summary>她所在显示器的可用区（任务栏除外），换成窗口屏幕坐标（原点左下）。取不到就是整个窗口。</summary>
        public static Rect WorkArea(Vector2 screenPt)
        {
            if (_hasWork && Time.frameCount - _workFrame < 30 && (screenPt - _workKey).sqrMagnitude < 40000f) return _work;
            Rect r = new Rect(0f, 0f, Screen.width, Screen.height);
            try
            {
                WMONINFO mi; WRECT wr; IntPtr hm;
                if (Mon(screenPt, out mi, out wr, out hm))
                {
                    float l = mi.rcWork.left - wr.left;
                    float rr = mi.rcWork.right - wr.left;
                    float yTop = Screen.height - (mi.rcWork.top - wr.top);
                    float yBot = Screen.height - (mi.rcWork.bottom - wr.top);
                    var w = Rect.MinMaxRect(l, yBot, rr, yTop);
                    // 只认「有交集」的结果，再和窗口求交（窗口比显示器小时别画到窗口外）
                    float xl = Mathf.Max(w.xMin, 0f), xr = Mathf.Min(w.xMax, Screen.width);
                    float yb = Mathf.Max(w.yMin, 0f), yt = Mathf.Min(w.yMax, Screen.height);
                    if (xr - xl > 200f && yt - yb > 200f) r = Rect.MinMaxRect(xl, yb, xr, yt);
                }
            }
            catch (Exception) { }
            _work = r; _hasWork = true; _workFrame = Time.frameCount; _workKey = screenPt;
            return r;
        }
    }
}
