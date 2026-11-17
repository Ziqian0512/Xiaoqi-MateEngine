using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // P3-3 工作台外侧方钮（G59-P3 §3.2 的 10/11 号，派工 WB-G69 §3）。「近景」= XqBenchWindow.Close() + XqMode.Set(Close)；
    // 「对话」灰着、悬停「还没开放」、点了只写日志。100% 下 40×40、离外沿 14、上沿 = 窗上沿 +6、间距 8，像素随 DPI 等比，
    // 右边距不足 100 DIP 换左外侧。坐标只走 XqPanelXf.FromScreen、点击用 worldBound 几何命中、方钮上 RegisterMenuBlocker
    // + SetUiBlocksMovement 挡拖动。只新增本文件与 Resources/XqBenchChrome.uss；-xq-bench-chrome-always（默认关）不开也画。
    [DefaultExecutionOrder(100)]
    public class XqBenchChrome : MonoBehaviour
    {
        const float BtnDip = 40f, GapDip = 14f, InsetDip = 6f, SpacingDip = 8f, RoomDip = 100f;
        const float TipDip = 92f, TipHDip = 30f, TipGapDip = 8f, WordDip = 15f, RadiusDip = 14f;
        const uint Nearest = 2;

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }
        [StructLayout(LayoutKind.Sequential)] struct MONINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint dpiX, out uint dpiY);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr h, uint f);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr m, ref MONINFO mi);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")] static extern IntPtr FindWindowW(string cls, string title);

        static XqBenchChrome _i;
        static bool _ussAdded, _always;
        VisualElement _host, _btnExit, _btnChat, _tip;
        readonly Vector2[] _pts = new Vector2[3];
        bool _built, _shown, _over, _hover, _blocks, _blockerSet;
        string _side = "";
        float _nextDoc;
        int _clickFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_i != null) return;
            _always = Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-bench-chrome-always") >= 0;
            var go = new GameObject("XqBenchChrome");
            DontDestroyOnLoad(go);
            go.AddComponent<XqBenchChrome>();
        }

        void Awake() { _i = this; }
        void OnDestroy() { if (_i == this) _i = null; }

        void Update()
        {
            try { Tick(); } catch (Exception e) { Debug.LogWarning("XQ_BENCH chrome tick: " + e.Message); }
        }

        void Tick()
        {
            RECT win, uw;
            if (XqFullscreenHide.ManualHidden || !TargetWindow(out win) || !RectOf(UnityHwnd(), out uw) || !EnsureBuilt()) { Hide(); return; }
            float k = DpiScale();
            bool right = RightSide(win, k);
            int bx, by, bw, bh;
            Place(win, uw, right, k, out bx, out by, out bw, out bh);
            if (_host == null) return;
            if (!_shown) { _shown = true; _host.style.display = DisplayStyle.Flex; }
            Pointer();
            var side = right ? "R" : "L";
            if (side == _side) return;
            _side = side;
            Debug.Log("XQ_BENCH chrome shown side=" + side + " rect=" + bx + "," + by + " " + bw + "x" + bh);
        }

        void Hide()
        {
            if (_host != null && _host.style.display != DisplayStyle.None) _host.style.display = DisplayStyle.None;
            if (_shown) Debug.Log("XQ_BENCH chrome hidden");
            _shown = _over = false;
            if (_hover) { _hover = false; if (_tip != null) _tip.EnableInClassList("on", false); }
            if (_blocks) { PetHost.I?.SetUiBlocksMovement(false); _blocks = false; }
        }

        bool TargetWindow(out RECT r)
        {
            r = default(RECT);
            return (XqBenchWindow.IsOpen && RectOf(XqBenchWindow.Hwnd, out r)) || (_always && FakeRect(out r));
        }

        /// <summary>-xq-bench-chrome-always：按 XqModeConfig 的比例造窗口矩形，只为截图。</summary>
        static bool FakeRect(out RECT r)
        {
            var mi = new MONINFO { cbSize = Marshal.SizeOf(typeof(MONINFO)) };
            var mon = MonitorRect(ref mi) ? mi.rcMonitor : new RECT { left = 0, top = 0, right = Screen.width, bottom = Screen.height };
            var c = XqModeConfig.I;
            int mw = Mathf.Max(1, mon.right - mon.left), mh = Mathf.Max(1, mon.bottom - mon.top);
            int w = Mathf.Max(c.bench_min_w, Mathf.RoundToInt(mw * c.bench_w_pct));
            int h = Mathf.Max(c.bench_min_h, Mathf.RoundToInt(mh * c.bench_h_pct));
            int x = mon.left + Mathf.RoundToInt(mw * c.bench_left_pct), y = mon.top + Mathf.RoundToInt(mh * c.bench_top_pct);
            r = new RECT { left = x, top = y, right = x + w, bottom = y + h };
            return true;
        }

        static IntPtr UnityHwnd()
        {
            var h = FindWindowW("UnityWndClass", "小祈");
            return h != IntPtr.Zero ? h : FindWindowW("UnityWndClass", null);
        }

        static bool RectOf(IntPtr h, out RECT r) { r = default(RECT); return h != IntPtr.Zero && GetWindowRect(h, out r); }

        /// <summary>工作台窗口优先，没开就看她的窗口（DPI 与显示器都按它算）。</summary>
        static IntPtr ProbeHwnd() { var h = XqBenchWindow.Hwnd; return h != IntPtr.Zero ? h : UnityHwnd(); }

        static bool MonitorRect(ref MONINFO mi)
        { var hm = MonitorFromWindow(ProbeHwnd(), Nearest); return hm != IntPtr.Zero && GetMonitorInfo(hm, ref mi); }

        // XQ(WB): 主屏 100% / 副屏 175% 时，Unity 进程若不是逐显示器 DPI 感知，GetDpiForWindow 在副屏上会返回 96（AGY G70 实测）。
        // 方钮要的是「所在那块显示器的真实缩放」，所以优先问显示器，取不到再退回窗口 DPI，两者取大。
        static float DpiScale()
        {
            var h = ProbeHwnd();
            uint dw = GetDpiForWindow(h), dm = 0, dy;
            try
            {
                var hm = MonitorFromWindow(h, Nearest);
                if (hm != IntPtr.Zero) GetDpiForMonitor(hm, 0, out dm, out dy); // 0 = MDT_EFFECTIVE_DPI
            }
            catch (Exception) { dm = 0; }
            uint d = dw > dm ? dw : dm;
            return d > 0 ? d / 96f : 1f;
        }

        /// <summary>窗口右边距够不够放方钮；取不到显示器就当够。</summary>
        static bool RightSide(RECT win, float k)
        {
            var mi = new MONINFO { cbSize = Marshal.SizeOf(typeof(MONINFO)) };
            return !MonitorRect(ref mi) || (mi.rcMonitor.right - win.right) >= Mathf.RoundToInt(RoomDip * k);
        }

        void Place(RECT win, RECT uw, bool right, float k, out int bx, out int by, out int bw, out int bh)
        {
            bx = by = bw = bh = 0;
            var panel = _host != null ? _host.panel : null;
            if (panel == null) return;
            bw = Mathf.Max(8, Mathf.RoundToInt(BtnDip * k));
            int gap = Mathf.RoundToInt(GapDip * k), inset = Mathf.RoundToInt(InsetDip * k), sp = Mathf.RoundToInt(SpacingDip * k);
            bx = right ? win.right + gap : win.left - gap - bw;
            by = win.top + inset;
            int y2 = by + bw + sp;
            Put(panel, uw, _btnExit, bx, by, bw, bw);
            Put(panel, uw, _btnChat, bx, y2, bw, bw);
            int tw = Mathf.RoundToInt(TipDip * k), th = Mathf.RoundToInt(TipHDip * k), tg = Mathf.RoundToInt(TipGapDip * k);
            Put(panel, uw, _tip, right ? bx - tg - tw : bx + bw + tg, y2 + (bw - th) / 2, tw, th);
            float s = XqPanelXf.UnitsPerPx(panel) * k, rad = RadiusDip * s, fs = WordDip * s; // s：1 DIP 等于多少面板单位
            Style(_btnExit, rad, fs); Style(_btnChat, rad, fs);
            if (_tip != null) _tip.style.fontSize = fs;
            bh = y2 + bw - by;
        }

        static void Style(VisualElement b, float r, float f)
        {
            if (b == null) return;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = r;
            var w = b.Q(className: "xq-bc-word");
            if (w != null) w.style.fontSize = f;
        }

        /// <summary>桌面坐标（原点左上、y 向下）→ 面板坐标，写 left/top/width/height；必须经 XqPanelXf.FromScreen。</summary>
        static void Put(IPanel panel, RECT uw, VisualElement el, int x, int y, int w, int h)
        {
            if (el == null) return;
            var tl = XqPanelXf.FromScreen(panel, new Vector2(x - uw.left, Screen.height - (y - uw.top)));
            var br = XqPanelXf.FromScreen(panel, new Vector2(x + w - uw.left, Screen.height - (y + h - uw.top)));
            el.style.left = tl.x;
            el.style.top = tl.y;
            el.style.width = Mathf.Abs(br.x - tl.x);
            el.style.height = Mathf.Abs(br.y - tl.y);
        }

        bool EnsureBuilt()
        {
            if (_built && _host != null)
            {
                if (_host.parent == null)   // 根被重建过（XqUi2Sample.Awake 里 Clear）就挂回去
                {
                    var d = PickDoc();
                    var rt = d != null ? d.rootVisualElement : null;
                    if (rt == null) return false;
                    rt.Add(_host);
                }
                return _host.panel != null;
            }
            if (Time.unscaledTime < _nextDoc) return false;
            _nextDoc = Time.unscaledTime + 2f;  // 找不到 UIDocument 时别每帧刷日志
            return Build();
        }

        bool Build()
        {
            var doc = PickDoc();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) { Debug.Log("XQ_BENCH chrome no UIDocument"); return false; }
            Debug.Log("XQ_BENCH chrome doc=" + doc.name);
            if (!_ussAdded)
            {
                var uss = Resources.Load<StyleSheet>("XqBenchChrome");
                if (uss == null) Debug.Log("XQ_BENCH chrome no uss"); else { root.styleSheets.Add(uss); _ussAdded = true; }
            }
            _host = new VisualElement { name = "xq-bench-chrome", pickingMode = PickingMode.Ignore };
            _host.style.position = Position.Absolute;
            _host.style.left = 0; _host.style.top = 0; _host.style.right = 0; _host.style.bottom = 0;
            _host.style.display = DisplayStyle.None;
            _host.AddToClassList("xq-root"); _host.AddToClassList(ThemeClass());
            _btnExit = Btn("xq-bench-chrome-exit", "近景", false);
            _btnChat = Btn("xq-bench-chrome-chat", "对话", true);
            _tip = new Label("还没开放") { pickingMode = PickingMode.Ignore };
            _tip.AddToClassList("xq-bc-tip");
            _host.Add(_btnExit); _host.Add(_btnChat); _host.Add(_tip);
            root.Add(_host);
            _built = true;
            Debug.Log("XQ_BENCH chrome boot always=" + _always);
            return true;
        }

        static VisualElement Btn(string name, string word, bool off)
        {
            var v = new VisualElement { name = name, pickingMode = PickingMode.Position };
            v.AddToClassList("xq-bc-btn");
            if (off) v.AddToClassList("off");
            var l = new Label(word) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("xq-bc-word");
            v.Add(l);
            return v;
        }

        void Pointer()
        {
            var panel = _host != null ? _host.panel : null;
            if (panel == null) return;
            MousePts(panel);
            bool overExit = OverEl(_btnExit), overChat = OverEl(_btnChat), over = overExit || overChat;
            if (over && !_over) Blocker();
            _over = over;
            if (overChat != _hover)
            {
                _hover = overChat;
                if (_tip != null) _tip.EnableInClassList("on", overChat);
                Debug.Log("XQ_BENCH chrome chat-hover=" + overChat);
            }
            if (over != _blocks) { PetHost.I?.SetUiBlocksMovement(over); _blocks = over; }
            if (!over || !Input.GetMouseButtonDown(0) || _clickFrame == Time.frameCount) return;
            _clickFrame = Time.frameCount;
            if (!overExit) { Debug.Log("XQ_BENCH chrome click chat(gray)"); return; }
            Debug.Log("XQ_BENCH chrome click exit");
            XqMode.Set(XqView.Close); // XQ(WB 复核): Bench→Close 的过渡里本来就会关网页窗口并还原她，不再提前 Close()
        }

        void MousePts(IPanel panel)
        {
            var m = Input.mousePosition;
            float sc = Mathf.Max(0.1f, Screen.height / Mathf.Max(1f, _host.worldBound.height));
            _pts[0] = XqPanelXf.FromScreen(panel, m);                     // 本仓库正解（已处理翻不翻 Y）
            // XQ(WB 复核): 去掉另外两路兜底。XqPanelXf 已是正解；ScreenToPanel 原样结果在不翻 Y 的机器上是镜像点，
            // 鼠标在屏幕镜像位置也会被当成点到方钮（误关工作台、误挡拖动）。
            _pts[1] = _pts[2] = _pts[0];
        }

        bool OverEl(VisualElement el)
        {
            if (el == null) return false;
            if (el.resolvedStyle.display == DisplayStyle.None || !el.visible) return false;
            var r = el.worldBound;
            if (float.IsNaN(r.x) || r.width < 1f || r.height < 1f) return false;
            return r.Contains(_pts[0]) || r.Contains(_pts[1]) || r.Contains(_pts[2]);
        }

        void Blocker()
        {
            if (_blockerSet || PetHost.I == null) return;
            _blockerSet = true;
            PetHost.I.RegisterMenuBlocker(ChromeBlocks);
            Debug.Log("XQ_BENCH chrome blocker on");
        }

        /// <summary>方钮上（或右键菜单开着）就别把这次点击当成拖她；菜单那半句照 XqGlassMenu 的写法。</summary>
        static bool ChromeBlocks()
        {
            return (_i != null && _i._shown && _i._over) || Xiaoqi.UI.XqMenu.IsOpen || XqGlassMenu.Holding;
        }

        /// <summary>优先 XqUi2Sample 下面的 UIDocument，其次名字含 Xq/Xiaoqi，最后第一个。</summary>
        static UIDocument PickDoc()
        {
            var docs = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (docs == null || docs.Length == 0) return null;
            var sample = UnityEngine.Object.FindFirstObjectByType<XqUi2Sample>(FindObjectsInactive.Include);
            foreach (var d in docs)
            {
                if (d == null) continue;
                if (sample != null && Under(d.transform, sample.transform)) return d;
                var n = d.name ?? "";
                if (n.IndexOf("Xq", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Xiaoqi", StringComparison.OrdinalIgnoreCase) >= 0) return d;
            }
            return docs[0];
        }

        static bool Under(Transform t, Transform root) { for (var p = t; p != null; p = p.parent) if (p == root) return true; return false; }

        static string ThemeClass()
        {
            var t = Xiaoqi.UI.XqStyle.Theme;
            return t == "blue" ? "xq-theme-blue" : (t == "violet" || t == "purple") ? "xq-theme-violet" : "xq-theme-pink";
        }
    }
}
