using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 方案 C：在 Unity 透明窗口正下方为每张卡片垫一块原生 Windows 系统亚克力小窗（DWM 真实模糊背后桌面）。
    /// 不抓屏、不用 WDA_EXCLUDEFROMCAPTURE，截图/录屏都正常。点击穿透、不抢焦点、不进任务栏。
    /// </summary>
    internal static class XqAcrylicUnderlay
    {
        delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public uint cbSize; public uint style; public IntPtr lpfnWndProc;
            public int cbClsExtra; public int cbWndExtra; public IntPtr hInstance;
            public IntPtr hIcon; public IntPtr hCursor; public IntPtr hbrBackground;
            public string lpszMenuName; public string lpszClassName; public IntPtr hIconSm;
        }
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }
        [StructLayout(LayoutKind.Sequential)] struct AccentPolicy { public int AccentState, AccentFlags, GradientColor, AnimationId; }
        [StructLayout(LayoutKind.Sequential)] struct WinCompAttrData { public int Attribute; public IntPtr Data; public int SizeOfData; }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassExW(ref WNDCLASSEX wc);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateWindowExW(int exStyle, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
        [DllImport("user32.dll")] static extern IntPtr DefWindowProcW(IntPtr h, uint m, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint flags);
        [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr h, IntPtr rgn, bool redraw);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref POINT p);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] static extern int GetWindowLong(IntPtr h, int idx);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")] static extern IntPtr FindWindowW(string cls, string title);
        [DllImport("user32.dll")] static extern int SetWindowCompositionAttribute(IntPtr h, ref WinCompAttrData d);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
        [DllImport("gdi32.dll")] static extern IntPtr GetStockObject(int i);
        [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);

        const string ClassName = "XqAcrylicUnderlay";
        const uint WS_POPUP = 0x80000000;
        const int WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOPMOST = 0x8;
        const uint SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40, SWP_NOMOVE = 0x2, SWP_NOSIZE = 0x1;
        // 淡薰衣草亚克力染色（ABGR）：约 27% 的 (250,242,255)
        const int Tint = 0x44FFF2FA;

        sealed class Slot { public IntPtr hwnd; public int x, y, w, h, r; public bool shown; public int frame; }

        static readonly Dictionary<int, Slot> _slots = new Dictionary<int, Slot>();
        static WndProcDelegate _proc;
        static bool _registered, _quitHooked, _failed;
        static IntPtr _unity;
        static int _frame;

        public static bool Supported
        {
            get { return !_failed && Application.platform == RuntimePlatform.WindowsPlayer; }
        }

        static IntPtr UnityHwnd()
        {
            if (_unity != IntPtr.Zero && IsWindow(_unity)) return _unity;
            _unity = FindWindowW("UnityWndClass", "小祈");
            if (_unity == IntPtr.Zero) _unity = FindWindowW("UnityWndClass", null);
            return _unity;
        }

        static IntPtr Proc(IntPtr h, uint m, IntPtr w, IntPtr l)
        {
            if (m == 0x84) return new IntPtr(-1);   // WM_NCHITTEST → HTTRANSPARENT（点击穿透）
            if (m == 0x21) return new IntPtr(3);    // WM_MOUSEACTIVATE → MA_NOACTIVATE
            return DefWindowProcW(h, m, w, l);
        }

        static bool EnsureClass()
        {
            if (_registered) return true;
            _proc = Proc;
            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_proc),
                hInstance = GetModuleHandleW(null),
                hbrBackground = GetStockObject(4), // BLACK_BRUSH：GDI 黑色在亚克力上视为透明
                lpszClassName = ClassName
            };
            RegisterClassExW(ref wc);
            _registered = true;
            if (!_quitHooked) { _quitHooked = true; Application.quitting += DestroyAll; }
            return true;
        }

        static IntPtr Create(int x, int y, int w, int h)
        {
            var hu = UnityHwnd();
            bool top = hu != IntPtr.Zero && (GetWindowLong(hu, -20) & WS_EX_TOPMOST) != 0;
            int ex = WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | (top ? WS_EX_TOPMOST : 0);
            var hwnd = CreateWindowExW(ex, ClassName, "", WS_POPUP, x, y, w, h, IntPtr.Zero, IntPtr.Zero, GetModuleHandleW(null), IntPtr.Zero);
            if (hwnd == IntPtr.Zero) return IntPtr.Zero;

            int noRound = 1; // DWMWCP_DONOTROUND：圆角交给 SetWindowRgn
            DwmSetWindowAttribute(hwnd, 33, ref noRound, 4);

            var acc = new AccentPolicy { AccentState = 4, AccentFlags = 2, GradientColor = Tint, AnimationId = 0 };
            int sz = Marshal.SizeOf(acc);
            IntPtr p = Marshal.AllocHGlobal(sz);
            try
            {
                Marshal.StructureToPtr(acc, p, false);
                var d = new WinCompAttrData { Attribute = 19, Data = p, SizeOfData = sz };
                SetWindowCompositionAttribute(hwnd, ref d);
            }
            finally { Marshal.FreeHGlobal(p); }
            return hwnd;
        }

        /// <summary>按 UI 元素在屏幕上的实际像素位置，同步一块系统亚克力底窗。</summary>
        public static void Sync(int id, VisualElement el)
        {
            if (!Supported || el == null || el.panel == null) return;
            try
            {
                var hu = UnityHwnd();
                if (hu == IntPtr.Zero) return;
                var root = el.panel.visualTree.worldBound;
                var r = el.worldBound;
                if (root.width < 8f || root.height < 8f || float.IsNaN(r.x) || r.width < 4f || r.height < 4f) { Hide(id); return; }

                RECT cr;
                if (!GetClientRect(hu, out cr)) return;
                float cw = cr.right - cr.left, ch = cr.bottom - cr.top;
                if (cw < 8f || ch < 8f) return;
                float sx = cw / root.width, sy = ch / root.height;
                var o = new POINT();
                ClientToScreen(hu, ref o);
                int x = o.x + Mathf.RoundToInt((r.x - root.x) * sx);
                int y = o.y + Mathf.RoundToInt((r.y - root.y) * sy);
                int w = Mathf.Max(4, Mathf.RoundToInt(r.width * sx));
                int h = Mathf.Max(4, Mathf.RoundToInt(r.height * sy));
                int rad = Mathf.RoundToInt(el.resolvedStyle.borderTopLeftRadius * sx);

                EnsureClass();
                Slot s;
                if (!_slots.TryGetValue(id, out s)) { s = new Slot(); _slots[id] = s; }
                if (s.hwnd == IntPtr.Zero || !IsWindow(s.hwnd))
                {
                    s.hwnd = Create(x, y, w, h);
                    if (s.hwnd == IntPtr.Zero) { _failed = true; return; }
                    s.w = s.h = -1; s.shown = false;
                }

                bool sizeChanged = w != s.w || h != s.h || rad != s.r;
                bool moved = x != s.x || y != s.y;
                bool reassert = (++s.frame % 30) == 0;
                if (sizeChanged)
                {
                    var rgn = CreateRoundRectRgn(0, 0, w + 1, h + 1, rad * 2, rad * 2);
                    SetWindowRgn(s.hwnd, rgn, true);
                }
                if (sizeChanged || moved || reassert || !s.shown)
                {
                    SetWindowPos(s.hwnd, hu, x, y, w, h, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    s.x = x; s.y = y; s.w = w; s.h = h; s.r = rad; s.shown = true;
                }
            }
            catch (Exception e)
            {
                _failed = true;
                Debug.LogWarning("XQ_ACRYLIC sync failed: " + e.Message);
            }
        }

        public static void Hide(int id)
        {
            Slot s;
            if (_slots.TryGetValue(id, out s) && s.shown && s.hwnd != IntPtr.Zero)
            {
                ShowWindow(s.hwnd, 0);
                s.shown = false;
            }
        }

        public static void HideAll()
        {
            foreach (var kv in _slots)
            {
                var s = kv.Value;
                if (s.shown && s.hwnd != IntPtr.Zero) { ShowWindow(s.hwnd, 0); s.shown = false; }
            }
        }

        static void DestroyAll()
        {
            foreach (var kv in _slots)
                if (kv.Value.hwnd != IntPtr.Zero) DestroyWindow(kv.Value.hwnd);
            _slots.Clear();
        }
    }
}
