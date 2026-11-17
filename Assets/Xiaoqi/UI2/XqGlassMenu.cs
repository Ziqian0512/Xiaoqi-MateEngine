using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 玻璃版右键菜单，接替旧的半弧菜单 XqMenu（旧的仍在，命令行 -xq-oldmenu 可切回）。
    /// 规则：一级卡永远贴在她靠近屏幕中间的那一侧；二级卡从一级往外展开。
    /// 打开后位置按「稳定外框」摆（XqBodyAnchor），不跟着待机动作抖；开着时桌宠界面的胶囊让开。
    /// 功能与旧菜单一致：今天 / 说句话(灰) / 让她…(回到中间·睡一会·下窗·摸头·跳舞) / 安静一会 / 直播间(灰) / 设置，
    /// 另加顶部三段形态开关（桌宠·近景·工作台）。
    /// </summary>
    public class XqGlassMenu
    {
        const float Gap = 12f;      // 卡与她、卡与卡的间距（面板单位）
        const float L1W = 244f;
        const float L2W = 224f;

        public static bool IsOpen { get; private set; }
        public static bool TestOpenRequest; // XQ: 验收钩子 -xq-menu-test 置位，下一次 Tick 自动打开菜单（默认不用）

        // 关菜单（点了某一行）的那一帧，ME 的「任意左键即拖动」还会看到同一次按下：
        // 菜单一关挡板就撤了，她被当成被拖，睡着立刻 wake_drag。关菜单后继续挡到鼠标抬起再多 3 帧。
        static bool _hold;
        static int _holdFrame;
        public static bool Holding
        {
            get
            {
                if (_hold && !Input.GetMouseButton(0) && Time.frameCount > _holdFrame + 3) _hold = false;
                return _hold;
            }
        }
        public VisualElement Root { get; private set; }

        readonly Action _openToday;
        VisualElement _l1, _l2, _l1Box, _l2Box;
        VisualElement _subRow;
        // 点击不再靠 ClickEvent / panel.Pick（实机上 Pick 在卡上返回 null）：每行登记 (元素,动作)，鼠标按下时按几何命中
        static readonly List<KeyValuePair<VisualElement, Action>> _c1 = new List<KeyValuePair<VisualElement, Action>>();
        static readonly List<KeyValuePair<VisualElement, Action>> _c2 = new List<KeyValuePair<VisualElement, Action>>();
        static List<KeyValuePair<VisualElement, Action>> _cur = _c1;
        IPanel _panelRef;
        float _scaleRef = 1f;
        bool _open, _sideRight; // _sideRight = 菜单在她右边
        string _sub;
        bool _danceOpen;
        float _t0;
        float _subT0;
        bool _blockerSet;
        int _dumpAt = -1;
        string _lastPicked = "";

        public static bool Enabled
        {
            get
            {
                var a = Environment.GetCommandLineArgs();
                return Array.IndexOf(a, "-xq-oldmenu") < 0;
            }
        }

        public XqGlassMenu(Action openToday)
        {
            _openToday = openToday;
        }

        public void Build(VisualElement hudRoot)
        {
            Root = new VisualElement { name = "xq-menu2", pickingMode = PickingMode.Ignore };
            Root.AddToClassList("xq-menu2");
            Root.style.display = DisplayStyle.None;

            _l1 = Card("xq-mcard", out _l1Box);
            _l2 = Card("xq-mcard xq-mcard2", out _l2Box);
            _l2.style.display = DisplayStyle.None;
            Root.Add(_l1);
            Root.Add(_l2);
            hudRoot.Add(Root);
            Xiaoqi.UI.XqMenu.Replaced = true;
        }

        /// <summary>
        /// 平铺结构：host 里 阴影 → 背景(绝对铺满，圆角) → 内容(正常流)。
        /// 不再用 .xq-body：那一层是 overflow:hidden + 圆角 + 主题色底，内容放在里面，
        /// 实机上整块内容不出现（卡面只剩白底）。这里内容不在任何裁剪容器里。
        /// </summary>
        static VisualElement Card(string cls, out VisualElement box)
        {
            var host = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in cls.Split(' ')) if (c.Length > 0) host.AddToClassList(c);
            var shadow = new VisualElement { pickingMode = PickingMode.Ignore };
            shadow.AddToClassList("xq-shadow");
            var bg = new VisualElement { pickingMode = PickingMode.Ignore };
            bg.AddToClassList("xq-mbg");
            box = new VisualElement { pickingMode = PickingMode.Position };
            box.AddToClassList("xq-mbox");
            host.Add(shadow);
            host.Add(bg);
            host.Add(box);
            if (Debug2)
            {
                bg.style.backgroundColor = new Color(0.2f, 0.9f, 0.9f, 1f);
                box.style.backgroundColor = new Color(1f, 0.2f, 0.8f, 0.55f);
            }
            return host;
        }

        /// <summary>-xq-menu-debug：卡背景青、内容区洋红、每行绿、文字底黄，用来看是哪一层画不出来。</summary>
        static readonly bool Debug2 = Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-menu-debug") >= 0;

        // ---- 桌面坐标：ME 移动的是窗口，她在窗口里基本不动，窗口内坐标不能用来判断她在屏幕哪一边 ----
        [StructLayout(LayoutKind.Sequential)] struct WRECT { public int left, top, right, bottom; }
        [StructLayout(LayoutKind.Sequential)] struct WPOINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] struct WMONINFO { public int cbSize; public WRECT rcMonitor; public WRECT rcWork; public uint dwFlags; }
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")] static extern IntPtr FindWindowW(string cls, string title);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out WRECT r);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(WPOINT pt, uint flags);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr mon, ref WMONINFO mi);
        static IntPtr _hwnd;

        /// <summary>窗口左上角在桌面上的位置，以及她身体中线所在显示器的范围（桌面坐标）。失败返回 false。</summary>
        static bool Desk(Rect body, out int winLeft, out int monLeft, out int monRight)
        {
            winLeft = monLeft = monRight = 0;
            try
            {
                if (_hwnd == IntPtr.Zero) _hwnd = FindWindowW("UnityWndClass", "小祈");
                if (_hwnd == IntPtr.Zero) _hwnd = FindWindowW("UnityWndClass", null);
                if (_hwnd == IntPtr.Zero) return false;
                WRECT wr;
                if (!GetWindowRect(_hwnd, out wr)) return false;
                var pt = new WPOINT { x = wr.left + Mathf.RoundToInt(body.center.x), y = wr.top + Mathf.RoundToInt(Screen.height - body.center.y) };
                var mon = MonitorFromPoint(pt, 2);
                if (mon == IntPtr.Zero) return false;
                var mi = new WMONINFO { cbSize = Marshal.SizeOf(typeof(WMONINFO)) };
                if (!GetMonitorInfo(mon, ref mi)) return false;
                winLeft = wr.left;
                monLeft = mi.rcMonitor.left;
                monRight = mi.rcMonitor.right;
                return monRight > monLeft;
            }
            catch (Exception) { _hwnd = IntPtr.Zero; return false; }
        }

        // ------------------------------------------------------------ 打开 / 关闭

        Rect _anch;
        bool _hasAnch;
        bool _placeLogged;

        public void Open(IPanel panel, float scale)
        {
            _hasAnch = false; // XQ: 每次打开重新取外框
            _placeLogged = false;
            if (_open) return;
            var body = XqBodyAnchor.Stable();
            if (body.height < 8f) return;
            _open = true;
            IsOpen = true;
            _sub = null;
            _danceOpen = false;
            _subRow = null;
            _t0 = Time.unscaledTime;
            _dumpAt = Time.frameCount + 4;

            // 选哪一侧：默认放在她靠近屏幕中间的一侧；那一侧放不下两张卡时换另一侧
            float need = (L1W + L2W + Gap * 3f) * scale;
            float roomRight = Screen.width - body.xMax;
            float roomLeft = body.xMin;
            bool preferRight = body.center.x < Screen.width * 0.5f;
            string how = "window";
            int wl, ml, mr;
            if (Desk(body, out wl, out ml, out mr))
            {
                // 她在显示器的哪半边，就决定菜单朝哪边；空间也按显示器边界算
                float deskCx = wl + body.center.x;
                preferRight = deskCx < (ml + mr) * 0.5f;
                roomRight = mr - (wl + body.xMax);
                roomLeft = (wl + body.xMin) - ml;
                how = "desktop cx=" + deskCx.ToString("0") + " mon=" + ml + ".." + mr;
            }
            _sideRight = preferRight;
            if (preferRight && roomRight < need && roomLeft > roomRight) _sideRight = false;
            else if (!preferRight && roomLeft < need && roomRight > roomLeft) _sideRight = true;
            Debug.Log("XQ_MENU2 side-pick " + how + " roomL=" + roomLeft.ToString("0") + " roomR=" + roomRight.ToString("0") + " need=" + need.ToString("0") + " -> " + (_sideRight ? "R" : "L"));

            Rebuild();
            Root.style.display = DisplayStyle.Flex;
            Debug.Log("XQ_MENU2 open side=" + (_sideRight ? "R" : "L") + " body=" + body.xMin.ToString("0") + ".." + body.xMax.ToString("0") + " scale=" + scale.ToString("0.00"));
        }

        public void Close(string by)
        {
            if (!_open) return;
            _open = false;
            IsOpen = false;
            _sub = null;
            _danceOpen = false;
            Root.style.display = DisplayStyle.None;
            Xiaoqi.UI.XqMenu.LastCloseFrame = Time.frameCount;
            _hold = true;
            _holdFrame = Time.frameCount;
            Debug.Log("XQ_MENU2 close by=" + by);
        }

        public bool Hit(VisualElement picked)
        {
            if (!_open) return false;
            for (var e = picked; e != null; e = e.parent)
                if (e == _l1 || e == _l2) return true;
            return OverCard();
        }

        /// <summary>用几何判断鼠标是否在卡上（两种坐标换算都试）。</summary>
        bool OverCard()
        {
            if (!_open || _panelRef == null) return false;
            foreach (var p in MousePanelPoints(_panelRef, _scaleRef))
            {
                if (Contains(_l1, p)) return true;
                if (!string.IsNullOrEmpty(_sub) && Contains(_l2, p)) return true;
            }
            return false;
        }

        static string LabelOf(VisualElement el)
        {
            var l = el as Label ?? el.Q<Label>();
            return l != null ? l.text : "?";
        }

        static bool Contains(VisualElement el, Vector2 p)
        {
            if (el == null) return false;
            var r = el.worldBound;
            if (float.IsNaN(r.x) || r.width < 1f) return false;
            return r.Contains(p);
        }

        static Vector2[] MousePanelPoints(IPanel panel, float scale)
        {
            var m = Input.mousePosition;
            float sc = Mathf.Max(0.1f, scale);
            return new[]
            {
                RuntimePanelUtils.ScreenToPanel(panel, m),
                new Vector2(m.x / sc, (Screen.height - m.y) / sc),
            };
        }

        /// <summary>按下时找被点的那一行。inside=点在卡上（含灰行/空隙）。</summary>
        bool Locate(IPanel panel, float scale, out Action act, out string how)
        {
            act = null; how = "";
            var pts = MousePanelPoints(panel, scale);
            for (int i = 0; i < pts.Length; i++)
            {
                var p = pts[i];
                if (!string.IsNullOrEmpty(_sub))
                {
                    foreach (var kv in _c2)
                        if (Contains(kv.Key, p)) { act = kv.Value; how = "l2/p" + i + " row='" + LabelOf(kv.Key) + "'"; return true; }
                    if (Contains(_l2, p)) { how = "l2-card/p" + i; return true; }
                }
                foreach (var kv in _c1)
                    if (Contains(kv.Key, p)) { act = kv.Value; how = "l1/p" + i + " row='" + LabelOf(kv.Key) + "'"; return true; }
                if (Contains(_l1, p)) { how = "l1-card/p" + i; return true; }
            }
            how = "none p0=" + pts[0].x.ToString("0") + "," + pts[0].y.ToString("0") + " p1=" + pts[1].x.ToString("0") + "," + pts[1].y.ToString("0") + " H=" + Screen.height;
            return false;
        }

        // ------------------------------------------------------------ 输入（HUD 每帧调）

        /// <summary>返回 true 表示这次按键已被菜单用掉。</summary>
        public bool HandlePointer(IPanel panel, float scale, VisualElement picked, bool onHer, bool overChrome)
        {
            if (!_blockerSet && PetHost.I != null)
            {
                _blockerSet = true;
                PetHost.I.SuppressHostMenu();
                PetHost.I.RegisterMenuBlocker(() => Xiaoqi.UI.XqMenu.IsOpen || Holding);
            }
            _panelRef = panel;
            _scaleRef = scale;
            if (_open)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (!string.IsNullOrEmpty(_sub)) { _sub = null; _danceOpen = false; Rebuild(); }
                    else Close("esc");
                    return true;
                }
                if (Input.GetMouseButtonDown(1)) { Close("right"); return true; }
                if (Input.GetMouseButtonDown(0))
                {
                    Action act; string how;
                    if (Locate(panel, scale, out act, out how))
                    {
                        Debug.Log("XQ_MENU2 click " + how + " picked=" + Describe(picked) + (act == null ? " (no action)" : ""));
                        act?.Invoke();
                        return true;
                    }
                    if (Hit(picked)) return true;

                    Debug.Log("XQ_MENU2 blank-click " + how + " picked=" + Describe(picked) + " mouse=" + Input.mousePosition.x.ToString("0") + "," + Input.mousePosition.y.ToString("0")
                        + " l1=" + Rect2(_l1) + (_l2.resolvedStyle.display == DisplayStyle.None ? "" : " l2=" + Rect2(_l2)));
                    Close("blank");
                    return false;
                }
                if (PetHost.I != null && !PetHost.I.AvatarVisible) { Close("hidden"); return false; }
                return false;
            }
            if (Input.GetMouseButtonDown(1) && onHer && !overChrome && !XqMode.Busy)
            {
                Open(panel, scale);
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------ 摆位置（HUD 每帧调）

        public void Tick(IPanel panel, float scale)
        {
            if (TestOpenRequest && !_open && Root != null && panel != null) { TestOpenRequest = false; Open(panel, scale); Debug.Log("XQ_MENU2 test-open mode=" + Xiaoqi.Mode.XqMode.Current); }
            if (!_open || Root == null || panel == null) return;
            _panelRef = panel;
            _scaleRef = scale;
            if (_dumpAt >= 0 && Time.frameCount >= _dumpAt) { _dumpAt = -1; DumpTree(_l1, 0); }
            var live = XqBodyAnchor.Stable();
            if (live.height < 8f) return;
            // XQ: B1 #5 菜单开着期间锁住位置：她在窗口里待机/抬手/歪头都不动菜单（ME 走路是挪窗口，菜单跟着窗口走）；
            // 只有外框真的挪了（中心偏出高度 25%，或高度变了 20%）才换到新位置，并重新打一条日志。
            if (!_hasAnch)
            {
                _anch = live; _hasAnch = true;
            }
            else
            {
                float tol = Mathf.Clamp(_anch.height * 0.25f, 40f, Mathf.Max(60f, Screen.height * 0.10f)); // XQ: 近景外框很大，容差按屏高封顶
                if (Vector2.Distance(live.center, _anch.center) > tol || Mathf.Abs(live.height - _anch.height) > _anch.height * 0.2f)
                {
                    _anch = live;
                    if (Debug2) Debug.Log("XQ_MENU2 anchor-moved body=" + live.xMin.ToString("0") + ".." + live.xMax.ToString("0") + " y=" + live.yMin.ToString("0") + ".." + live.yMax.ToString("0"));
                }
            }
            // XQ: 近景态：位置完全由屏幕脸区决定（不看外框，镜头/待机怎么变都不动）；桌宠态保持原逻辑
            bool closeMode = Xiaoqi.Mode.XqMode.Current == Xiaoqi.Mode.XqView.Close;
            var body = closeMode ? live : _anch;

            var tl = XqPanelXf.FromScreen(panel, new Vector2(body.xMin, body.yMax)); // XQ: B1 #5 屏幕左下→面板左上（ScreenToPanel 不翻 Y）
            var br = XqPanelXf.FromScreen(panel, new Vector2(body.xMax, body.yMin));
            float limitW = Root.parent != null ? Root.parent.worldBound.width : 0f;
            float limitH = Root.parent != null ? Root.parent.worldBound.height : 0f;
            if (limitW < 16f) limitW = Screen.width / Mathf.Max(0.1f, scale);
            if (limitH < 16f) limitH = Screen.height / Mathf.Max(0.1f, scale);

            float minX = 8f;
            int wl2, ml2, mr2;
            if (Desk(body, out wl2, out ml2, out mr2))
            {
                // 卡不出她所在的显示器（窗口比显示器大时，卡会画到别的屏上或屏外）
                minX = Mathf.Max(8f, (ml2 - wl2) / Mathf.Max(0.1f, scale) + 8f);
                float monRightPanel = (mr2 - wl2) / Mathf.Max(0.1f, scale) - 8f;
                if (monRightPanel > minX + 100f) limitW = Mathf.Min(limitW, monRightPanel + 8f);
            }
            float w1 = Wid(_l1, L1W), h1 = Hei(_l1, 340f);
            float k = Ease(Mathf.Clamp01((Time.unscaledTime - _t0) / 0.18f));
            float dir = _sideRight ? 1f : -1f;

            float closeFaceL = 0f, closeFaceR = 0f; // XQ: 近景脸区左右边界（面板单位），二级卡放不下时用
            float x1 = _sideRight ? br.x + Gap : tl.x - Gap - w1;
            float y1 = tl.y + (br.y - tl.y) * 0.04f;
            if (closeMode)
            {
                // 脸区（屏宽 38–62%、屏高 18–65%）之外：优先脸区右侧，右边放不下一二级两张卡才换左侧；竖向从脸区上沿起
                float monL = minX - 8f;
                float monR = Mathf.Max(monL + 200f, limitW);
                float monW = monR - monL;
                float faceR = monL + (XqCloseLayout.FaceL + XqCloseLayout.FaceW) * monW;
                float faceL = monL + XqCloseLayout.FaceL * monW;
                closeFaceL = faceL; closeFaceR = faceR;
                float needP = w1 + Wid(_l2, L2W) + Gap * 3f;
                float roomR = monR - faceR, roomL = faceL - monL;
                _sideRight = roomR >= needP || roomR >= roomL;
                x1 = _sideRight ? faceR + Gap : faceL - Gap - w1;
                y1 = XqCloseLayout.FaceT * limitH;
            }
            x1 = Mathf.Clamp(x1, minX, Mathf.Max(minX, limitW - w1 - 8f));
            y1 = Mathf.Clamp(y1, 8f, Mathf.Max(8f, limitH - h1 - 56f));
            _l1.style.left = x1;
            _l1.style.top = y1;
            if (!_placeLogged) { _placeLogged = true; if (closeMode) Debug.Log("XQ_MENU2 place close-face side=" + (_sideRight ? "R" : "L") + " l1=" + x1.ToString("0") + "," + y1.ToString("0") + " limit=" + limitW.ToString("0") + "x" + limitH.ToString("0")); Debug.Log("XQ_MENU2 place body(screen)=" + body.xMin.ToString("0") + ".." + body.xMax.ToString("0") + " y=" + body.yMin.ToString("0") + ".." + body.yMax.ToString("0") + " H=" + Screen.height + " tl(panel)=" + tl.x.ToString("0") + "," + tl.y.ToString("0") + " l1=" + x1.ToString("0") + "," + y1.ToString("0") + " scale=" + scale.ToString("0.00")); }
            if (k < 0.999f) _l1.style.opacity = k; else _l1.style.opacity = StyleKeyword.Null;
            _l1.style.translate = new Translate((1f - k) * 10f * -dir, 0f, 0f);

            bool showSub = !string.IsNullOrEmpty(_sub);
            _l2.style.display = showSub ? DisplayStyle.Flex : DisplayStyle.None;
            if (showSub)
            {
                float w2 = Wid(_l2, L2W), h2 = Hei(_l2, 200f);
                float rowTop = y1 + 56f;
                if (_subRow != null)
                {
                    float wy = _subRow.worldBound.y;
                    if (!float.IsNaN(wy) && wy > 0f) rowTop = wy - 6f;
                }
                // 二级永远在一级的外侧
                float x2 = _sideRight ? x1 + w1 + Gap : x1 - Gap - w2;
                // XQ: 近景里一级卡这侧放不下二级卡（16:9 脸区两侧各只有约 365，两张卡要 504）→ 二级卡放到脸区另一侧，不再压在一级卡上
                if (closeMode && closeFaceR > 0f)
                {
                    if (_sideRight && x2 + w2 > limitW - 8f) x2 = closeFaceL - Gap - w2;
                    else if (!_sideRight && x2 < minX) x2 = closeFaceR + Gap;
                }
                float y2 = Mathf.Clamp(rowTop, 8f, Mathf.Max(8f, limitH - h2 - 56f));
                x2 = Mathf.Clamp(x2, minX, Mathf.Max(minX, limitW - w2 - 8f));
                float k2 = Ease(Mathf.Clamp01((Time.unscaledTime - _subT0) / 0.18f));
                _l2.style.left = x2;
                _l2.style.top = y2;
                if (k2 < 0.999f) _l2.style.opacity = k2; else _l2.style.opacity = StyleKeyword.Null;
                _l2.style.translate = new Translate((1f - k2) * 10f * -dir, 0f, 0f);
            }
        }

        // ------------------------------------------------------------ 内容

        void Rebuild()
        {
            _l1Box.Clear();
            _l2Box.Clear();
            _c1.Clear();
            _c2.Clear();
            _cur = _c1;
            _subRow = null;
            var st = XiaoqiLink.Current;

            // 状态行（点它 = 今天）
            var stat = new VisualElement();
            stat.AddToClassList("xq-mstat");
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("xq-mdot");
            var where = new Label(StatusLine(st)) { pickingMode = PickingMode.Ignore };
            where.AddToClassList("xq-mstat-text");
            where.style.color = new Color(0.23f, 0.18f, 0.33f, 0.62f);
            var energy = new Label(st != null && st.body != null ? "精力 " + Mathf.RoundToInt(Mathf.Clamp(st.body.stamina, 0f, 100f)) : "")
            { pickingMode = PickingMode.Ignore };
            energy.AddToClassList("xq-mstat-energy");
            energy.style.color = XqTheme.AccentInk;
            dot.style.backgroundColor = XqTheme.Accent;
            stat.Add(dot); stat.Add(where); stat.Add(energy);
            _c1.Add(new KeyValuePair<VisualElement, Action>(stat, () => { Close("action"); _openToday?.Invoke(); }));
            _l1Box.Add(stat);

            // 形态切换（桌宠/近景/工作台）只放右下方钮，菜单里不再放（主人 11-16 定）

            _l1Box.Add(Row("sun", "今天", () => { Close("action"); _openToday?.Invoke(); }));
            _l1Box.Add(Gray("chat", "说句话", "还没开通"));
            _l1Box.Add(SubRow("spark", "让她…", "doing"));
            string quietWord = XiaoqiRuntime.Dnd ? "安静一会 · 安静中" : "安静一会";
            _l1Box.Add(SubRow("moon", quietWord, "quiet"));
            _l1Box.Add(Gray("live", "直播间", "还在做"));
            var sep = new VisualElement { pickingMode = PickingMode.Ignore };
            sep.AddToClassList("xq-msep");
            _l1Box.Add(sep);
            var setRow = Row("gear", "设置", () => { Close("action"); XiaoqiSettings.Open(); });
            setRow.AddToClassList("xq-mrow-set");
            _l1Box.Add(setRow);

            _cur = _c2;
            if (_sub == "doing") BuildDoing();
            else if (_sub == "quiet") BuildQuiet();
        }

        void SetSub(string kind, VisualElement row)
        {
            _sub = _sub == kind ? null : kind;
            _danceOpen = false;
            _subT0 = Time.unscaledTime;
            Rebuild();
        }

        VisualElement SubRow(string glyph, string text, string kind)
        {
            VisualElement row = null;
            row = Row(glyph, text, () => SetSub(kind, row), chev: true);
            if (_sub == kind)
            {
                row.AddToClassList("xq-mrow-open");
                _subRow = row;
            }
            return row;
        }

        void BuildDoing()
        {
            var st = XiaoqiLink.Current;
            bool outside = st != null && st.where != "dorm";
            string why = (st != null && st.seg != null ? st.seg.name : "") + "，回来再说";
            bool asleep = Xiaoqi.Spike.PetPresence.IsAsleep;

            _l2Box.Add(Row("home", "回到中间", () =>
            {
                PetHost.I?.ExitWindowSit();
                PetHost.I?.MoveToCenter();
                Close("action");
            }));

            if (outside) _l2Box.Add(Gray("bed", asleep ? "叫醒她" : "睡一会", why));
            else
                _l2Box.Add(Row("bed", asleep ? "叫醒她" : "睡一会", () =>
                {
                    if (asleep) Xiaoqi.Spike.PetPresence.Wake();
                    else Xiaoqi.Spike.PetPresence.Nap(XiaoqiRuntime.Config != null ? XiaoqiRuntime.Config.nap_min : 20);
                    Close("action");
                }));

            if (PetHost.I != null)
            {
                foreach (var act in PetHost.I.Actions)
                {
                    if (act.Id.StartsWith("idle_") || act.Id.StartsWith("dance_")) continue;
                    var a = act;
                    string g = a.Id == "unsit" ? "win" : "heart";
                    _l2Box.Add(Row(g, a.Label, () =>
                    {
                        if (!PetHost.I.RunAction(a.Id)) Debug.Log("XQ_MENU2 action failed " + a.Id);
                        Close("action");
                    }));
                }
            }

            bool hasDance = PetHost.I != null && PetHost.I.Dances.Count > 0;
            if (!hasDance && PetHost.I != null)
                foreach (var act in PetHost.I.Actions)
                    if (act.Id.StartsWith("dance_")) { hasDance = true; break; }
            if (hasDance)
            {
                if (outside) _l2Box.Add(Gray("music", "跳舞", why));
                else
                {
                    var dance = Row("music", "跳舞", () => { _danceOpen = !_danceOpen; Rebuild(); }, chev: true);
                    if (_danceOpen) dance.AddToClassList("xq-mrow-open");
                    _l2Box.Add(dance);
                    if (_danceOpen) _l2Box.Add(DanceChips());
                }
            }
        }

        VisualElement DanceChips()
        {
            var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("xq-mchips");
            if (PetHost.I == null) return wrap;
            foreach (var name in PetHost.I.Dances)
            {
                string dn = name;
                wrap.Add(Chip(dn, false, () =>
                {
                    if (PetHost.I.PlayDance(dn)) Close("action");
                    else Debug.Log("XQ_MENU2 dance failed " + dn);
                }));
            }
            foreach (var act in PetHost.I.Actions)
            {
                if (!act.Id.StartsWith("dance_")) continue;
                var a = act;
                wrap.Add(Chip(a.Label, false, () => { PetHost.I.RunAction(a.Id); Close("action"); }));
            }
            if (PetHost.I.IsDancing)
                wrap.Add(Chip("停下", true, () => { PetHost.I.StopDance(); Close("action"); }));
            return wrap;
        }

        void BuildQuiet()
        {
            _l2Box.Add(Row("moon", "30 分钟", () => { XiaoqiRuntime.SetQuietMinutes(30); Close("action"); }));
            _l2Box.Add(Row("moon", "1 小时", () => { XiaoqiRuntime.SetQuietMinutes(60); Close("action"); }));
            _l2Box.Add(Row("moon", "到明早 8 点", () => { XiaoqiRuntime.SetQuietTomorrow8(); Close("action"); }));
            _l2Box.Add(Row("moon", "一直", () => { XiaoqiRuntime.SetQuietForever(); Close("action"); }));
            if (XiaoqiRuntime.Dnd)
                _l2Box.Add(Row("sun", "不安静了", () => { XiaoqiRuntime.ClearQuiet(); Close("action"); }));
        }

        // ------------------------------------------------------------ 小部件

        static VisualElement Row(string glyph, string text, Action click, bool chev = false)
        {
            var row = new VisualElement();
            row.AddToClassList("xq-mrow");
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            ic.AddToClassList("xq-mic");
            var g = new XqGlyph(glyph, Color.white);
            g.AddToClassList("xq-mglyph");
            ic.Add(g);
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("xq-mtext");
            // 不全靠 USS 变量：颜色同时直接写在元素上，主题变量没解析出来也看得见
            label.style.color = XqTheme.Ink;
            ic.style.backgroundColor = XqTheme.Accent;
            row.Add(ic);
            row.Add(label);
            if (chev)
            {
                var c = new XqGlyph("chev", XqTheme.Ink);
                c.AddToClassList("xq-mchev");
                row.Add(c);
            }
            if (click != null) _cur.Add(new KeyValuePair<VisualElement, Action>(row, click));
            return row;
        }

        static VisualElement Gray(string glyph, string text, string tag)
        {
            var row = Row(glyph, text, null);
            row.AddToClassList("xq-mrow-gray");
            row.Q<Label>().style.color = new Color(0.23f, 0.18f, 0.33f, 0.4f);
            row.Q(className: "xq-mic").style.backgroundColor = new Color(0.23f, 0.18f, 0.33f, 0.14f);
            var t = new Label(tag) { pickingMode = PickingMode.Ignore };
            t.AddToClassList("xq-mtag");
            t.style.color = new Color(0.23f, 0.18f, 0.33f, 0.45f);
            row.Add(t);
            return row;
        }

        static VisualElement Chip(string text, bool stop, Action click)
        {
            var c = new Label(text);
            c.AddToClassList("xq-mchip");
            c.style.color = stop ? XqTheme.AccentInk : XqTheme.Ink;
            if (stop) c.AddToClassList("xq-mchip-stop");
            _cur.Add(new KeyValuePair<VisualElement, Action>(c, click));
            return c;
        }

        static string StatusLine(StageData s)
        {
            if (Xiaoqi.Spike.PetPresence.IsAsleep || (s != null && s.pose == "sleep"))
                return "睡着了" + PlaceSuffix(s);
            if (s == null) return "还没有";
            if (s.where == "out") return "外出";
            string place = PlaceName(s.where);
            string act = s.seg != null ? (string.IsNullOrEmpty(s.seg.name) ? s.seg.act : s.seg.name) : "";
            if (string.IsNullOrEmpty(place) && string.IsNullOrEmpty(act)) return "还没有";
            return string.IsNullOrEmpty(act) ? place : place + " · " + act;
        }

        static string PlaceSuffix(StageData s)
        {
            string p = s != null ? PlaceName(s.where) : "";
            return string.IsNullOrEmpty(p) ? "" : " · " + p;
        }

        static string PlaceName(string where)
        {
            if (string.IsNullOrEmpty(where)) return "";
            if (where == "dorm") return "宿舍";
            if (where == "out") return "外出";
            return where;
        }

        static float Wid(VisualElement el, float fb)
        {
            float w = el.resolvedStyle.width;
            return float.IsNaN(w) || w < 2f ? fb : w;
        }

        static float Hei(VisualElement el, float fb)
        {
            float h = el.resolvedStyle.height;
            return float.IsNaN(h) || h < 2f ? fb : h;
        }

        // ---------------- 诊断：卡里到底有没有东西、画在哪、什么颜色（每次打开只打一次）
        static string Describe(VisualElement e)
        {
            if (e == null) return "null";
            string cls = "";
            foreach (var c in e.GetClasses()) cls += "." + c;
            return e.GetType().Name + cls;
        }

        static string Rect2(VisualElement e)
        {
            var b = e.worldBound;
            return b.xMin.ToString("0") + "," + b.yMin.ToString("0") + " " + b.width.ToString("0") + "x" + b.height.ToString("0");
        }

        static void DumpTree(VisualElement e, int depth)
        {
            if (e == null || depth > 4) return;
            var rs = e.resolvedStyle;
            string extra = "";
            var lb = e as Label;
            if (lb != null) extra = " text=\"" + lb.text + "\" color=" + ColorUtility.ToHtmlStringRGBA(rs.color) + " font=" + rs.fontSize.ToString("0");
            Debug.Log("XQ_MENU2 tree " + new string(' ', depth * 2) + Describe(e) + " rect=" + Rect2(e)
                + " disp=" + rs.display + " vis=" + rs.visibility + " op=" + rs.opacity.ToString("0.00")
                + " bg=" + ColorUtility.ToHtmlStringRGBA(rs.backgroundColor) + extra);
            int n = 0;
            foreach (var c in e.Children())
            {
                if (depth >= 3 && n++ >= 3) break;
                DumpTree(c, depth + 1);
            }
        }

        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
