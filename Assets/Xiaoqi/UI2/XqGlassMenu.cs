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
        const float L1W = 228f;
        const float L2W = 254f;

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

        // G74 W5：设置页是这张菜单的第三级。设置卡要问「鼠标在不在菜单上」、要能把菜单一起关掉
        static XqGlassMenu _curInst;
        public static bool OverCardNow { get { return _curInst != null && _curInst.OverCard(); } }
        public static void CloseCurrent(string by) { if (_curInst != null) _curInst.Close(by); }
        int _setPage = -1;                                   // 二级里选中的设置分类（-1 = 没选）
        readonly List<VisualElement> _setRowEls = new List<VisualElement>();

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
            _curInst = this;
            Root = new VisualElement { name = "xq-menu2", pickingMode = PickingMode.Ignore };
            Root.AddToClassList("xq-menu2");
            Root.style.display = DisplayStyle.None;

            _l1 = Card("xq-mcard", out _l1Box);
            _l2 = Card("xq-mcard xq-mcard2", out _l2Box);
            _l2.style.display = DisplayStyle.None;
            _l1.style.width=L1W;_l2.style.width=L2W;
            foreach(var card in new[]{_l1,_l2}){var bg=card.Q(className:"xq-mbg");if(bg!=null)XqVoiceOverlay.Surface(bg);}
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
            shadow.style.position=Position.Absolute;shadow.style.left=0;shadow.style.right=0;shadow.style.top=2;shadow.style.bottom=-2;
            shadow.style.backgroundColor=new Color(.36f,.29f,.44f,.06f);shadow.style.borderTopLeftRadius=shadow.style.borderTopRightRadius=shadow.style.borderBottomLeftRadius=shadow.style.borderBottomRightRadius=16;
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
        VisualElement _hudNext;   // W5b：近景里菜单被装饰卡压在下面（r16 close_l3_behave.png）；打开时把 HUD 根抬到最上，关闭时放回原位
        bool _hudLifted;

        void LiftHud()
        {
            var hr = Root != null ? Root.parent : null;
            var pp = hr != null ? hr.parent : null;
            if (pp == null || hr == null) return;
            int i = pp.IndexOf(hr);
            if (i < 0 || i == pp.childCount - 1) { _hudLifted = false; return; }
            _hudNext = pp[i + 1];
            hr.BringToFront();
            _hudLifted = true;
            Debug.Log("XQ_MENU2 lift hud above=" + (_hudNext != null ? _hudNext.name : "-"));
        }

        void DropHud()
        {
            if (!_hudLifted) return;
            _hudLifted = false;
            var hr = Root != null ? Root.parent : null;
            if (hr != null && hr.parent != null && _hudNext != null && _hudNext.parent == hr.parent) hr.PlaceBehind(_hudNext);
            _hudNext = null;
        }

        public void Open(IPanel panel, float scale)
        {
            _hasAnch = false; // XQ: 每次打开重新取外框
            _placeLogged = false;
            if (_open) return;
            XqSettingsPanel.CloseIfOpen(); // XQ: B1 第二轮 #14 菜单与设置页互斥
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
            LiftHud();
            Debug.Log("XQ_MENU2 open side=" + (_sideRight ? "R" : "L") + " body=" + body.xMin.ToString("0") + ".." + body.xMax.ToString("0") + " scale=" + scale.ToString("0.00"));
        }

        public void Close(string by)
        {
            if (!_open) return;
            _open = false;
            IsOpen = false;
            _sub = null;
            _setPage = -1;
            XqSettingsPanel.CloseIfOpen();   // W5：三级跟着关
            _danceOpen = false;
            Root.style.display = DisplayStyle.None;
            DropHud();
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
            // XQ: B1 第二轮 #18 只用翻过 Y 的点。以前 pts[0] 是未翻 Y 的 ScreenToPanel（本机 flipsY=False，镜像点），
            // 菜单跨过屏中线时镜像点也落在卡内，整张菜单上下颠倒命中（点「让她…」开了今天）。第二个点保留同值，避免动下标。
            var pt = XqPanelXf.FromScreen(panel, m);
            return new[] { pt, pt };
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
                    if (_sub == "settings" || XqSettingsPanel.CascadeShown) Close("esc");   // W5：Esc 统一关（设置的二、三级 + 菜单）
                    else if (!string.IsNullOrEmpty(_sub)) { _sub = null; _danceOpen = false; Rebuild(); }
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
            // XQ: B1 第二轮 #16 近景态不开桌宠右键菜单（近景自己有左栏）；测试钩子 TestOpenRequest 不受影响
            if (Input.GetMouseButtonDown(1) && onHer && !overChrome && !XqMode.Busy && XqMode.Current != XqView.Close && XqMode.Current != XqView.Bench)  // XQ: G74 工作台里也不开桌宠右键菜单
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
            if (showSub) TickSettings(panel, minX, limitW, limitH);
        }

        // ------------------------------------------------------------ 内容

        void Rebuild()
        {
            _l1Box.Clear();_l2Box.Clear();_c1.Clear();_c2.Clear();_cur=_c1;_subRow=null;
            var st=XiaoqiLink.Current;
            var header=new VisualElement {pickingMode=PickingMode.Ignore};header.style.height=62;header.style.flexShrink=0;
            var line=new Label(StatusLine(st)){enableRichText=false,pickingMode=PickingMode.Ignore};line.style.fontSize=12;line.style.color=XqTheme.Ink;line.style.position=Position.Absolute;line.style.left=7;line.style.top=7;line.style.width=133;line.style.whiteSpace=WhiteSpace.NoWrap;line.style.overflow=Overflow.Hidden;line.style.textOverflow=TextOverflow.Ellipsis;header.Add(line);
            var energy=new Label(st?.body!=null?"精力 "+Mathf.RoundToInt(st.body.stamina)+" · 正在陪着你":"生活状态同步中"){pickingMode=PickingMode.Ignore};energy.style.position=Position.Absolute;energy.style.left=8;energy.style.top=33;energy.style.fontSize=10;energy.style.color=new Color(.66f,.59f,.71f);header.Add(energy);
            var today=new Label("今天 ↗");today.style.position=Position.Absolute;today.style.right=2;today.style.top=5;today.style.fontSize=11;today.style.paddingLeft=today.style.paddingRight=7;today.style.paddingTop=today.style.paddingBottom=5;today.style.color=XqTheme.AccentInk;header.Add(today);_c1.Add(new KeyValuePair<VisualElement,Action>(today,()=>{Close("today");_openToday?.Invoke();}));_l1Box.Add(header);
            var voiceHint=new Label("按住 Ctrl+N 说话 · 松开发送"){pickingMode=PickingMode.Ignore};voiceHint.style.fontSize=10;voiceHint.style.color=new Color(.60f,.53f,.67f);voiceHint.style.paddingLeft=8;voiceHint.style.paddingBottom=12;_l1Box.Add(voiceHint);
            _l1Box.Add(SubRow("spark","和她互动","doing"));_l1Box.Add(SubRow("home","桌面位置","position"));_l1Box.Add(SubRow("moon","声音与勿扰","quiet"));
            var sep=new VisualElement {pickingMode=PickingMode.Ignore};sep.AddToClassList("xq-msep");_l1Box.Add(sep);_l1Box.Add(Row("gear","偏好设置…",()=>{Close("settings");XqSettingsPanel.Open();}));
            _cur=_c2;_setRowEls.Clear();
            if(_sub=="talk")BuildTalk();else if(_sub=="doing"||_sub=="dance")BuildDoing();else if(_sub=="position")BuildPosition();else if(_sub=="quiet")BuildQuiet();
        }
        void Heading(string text){var l=new Label(text){pickingMode=PickingMode.Ignore};l.style.fontSize=12;l.style.paddingLeft=8;l.style.paddingTop=7;l.style.paddingBottom=12;l.style.color=XqTheme.Ink;_l2Box.Add(l);}
        internal static void ReviewMenu(string sub)
        {
            var i=_curInst;if(i==null||i._panelRef==null)return;
            if(!i._open)i.Open(i._panelRef,i._scaleRef);i._sub=sub;i.Rebuild();
        }
        internal static Newtonsoft.Json.Linq.JObject ReviewBounds()
        {
            var i=_curInst;var report=new Newtonsoft.Json.Linq.JObject();if(i==null)return report;
            foreach(var pair in new[]{new KeyValuePair<string,VisualElement>("primary",i._l1),new KeyValuePair<string,VisualElement>("secondary",i._l2)})
            {var r=pair.Value.worldBound;report[pair.Key]=new Newtonsoft.Json.Linq.JArray(r.x,r.y,r.width,r.height);}
            report["primaryActions"]=_c1.Count;report["secondaryActions"]=_c2.Count;return report;
        }
        static VisualElement Detail(string glyph,string text,string detail,Action action)
        {
            var row=Row(glyph,text,action);row.style.height=54;row.style.minHeight=54;
            var label=row.Q<Label>();label.style.position=Position.Absolute;label.style.left=43;label.style.top=7;label.style.fontSize=12;label.style.width=185;
            var sub=new Label(detail){pickingMode=PickingMode.Ignore,enableRichText=false};sub.style.position=Position.Absolute;sub.style.left=43;sub.style.top=29;sub.style.fontSize=10;sub.style.width=185;sub.style.color=new Color(.65f,.58f,.70f);sub.style.whiteSpace=WhiteSpace.NoWrap;sub.style.textOverflow=TextOverflow.Ellipsis;sub.style.overflow=Overflow.Hidden;row.Add(sub);
            if(action==null){row.style.opacity=.55f;row.tooltip=detail;}return row;
        }
        void BuildTalk()
        {
            Heading("说说话");_l2Box.Add(Detail("chat","打字聊天","输入一条消息",()=>{Close("type");if(XqMode.Current==XqView.Pet)XqVoiceOverlay.OpenTyping();else XqVoiceOverlay.Notice("请使用近景下方输入框。");}));
            _l2Box.Add(Detail("mic","按住说话 · Ctrl+N","按住此项或 Ctrl+N · 松开发送",()=>{Close("voice");XqVoiceOverlay.BeginMenuVoice();}));
            _l2Box.Add(Detail("win","分享当前窗口标题","只分享应用与标题，不发送截图",()=>{Close("screen");XqScreenCompanion.PushNow(XqCloseData.Ensure(),XqVoiceOverlay.Notice);}));
        }
        void RunHost(string id){var h=PetHost.I;bool ok=h!=null&&h.Ready&&h.RunAction(id);Close("action");if(!ok)XqVoiceOverlay.Notice("这个动作暂时不可用，请等模型加载完成。");}
        void BuildPosition()
        {
            Heading("桌面位置");var h=PetHost.I;
            _l2Box.Add(Detail("home","回当前屏幕中间","在小祈所在显示器内归位",h!=null&&h.Ready?(Action)(()=>{h.ExitWindowSit();h.MoveToCenter();Close("center");}):null));
            _l2Box.Add(Detail("win","离开窗边",h!=null&&h.IsWindowSitting?"回到桌面上":"现在没有坐在窗口上",h!=null&&h.IsWindowSitting?(Action)(()=>RunHost("unsit")):null));
            _l2Box.Add(Detail("gear","自动坐窗口","长期行为开关在偏好设置",()=>{Close("settings");XqSettingsPanel.OpenPage(1);}));
        }

        static readonly string[] SetGlyphs = { "heart", "spark", "win", "music", "live", "home" };

        void BuildSettingsCats()
        {
            var names = XqSettingsPanel.PageNames;
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var r = Row(SetGlyphs[i % SetGlyphs.Length], names[i], () => SelectSettingsPage(idx), chev: true);
                if (idx == _setPage) r.AddToClassList("xq-mrow-open");
                _setRowEls.Add(r);
                _l2Box.Add(r);
            }
        }

        /// <summary>悬停或点了二级里的某个分类：高亮这一行，开 / 切三级卡。</summary>
        void SelectSettingsPage(int idx)
        {
            if (idx < 0 || idx >= _setRowEls.Count || idx == _setPage) return;
            _setPage = idx;
            for (int i = 0; i < _setRowEls.Count; i++) _setRowEls[i].EnableInClassList("xq-mrow-open", i == idx);
            Debug.Log("XQ_MENU2 settings page=" + XqSettingsPanel.PageNames[idx]);
            XqSettingsPanel.OpenCascade(idx);
        }

        void TickSettings(IPanel panel, float minX, float limitW, float limitH)
        {
            if (_sub != "settings" || _setRowEls.Count == 0) return;
            var pt = XqPanelXf.FromScreen(panel, Input.mousePosition);
            for (int i = 0; i < _setRowEls.Count; i++)
                if (Contains(_setRowEls[i], pt)) { SelectSettingsPage(i); break; }
            if (_setPage < 0 || _setPage >= _setRowEls.Count) return;
            var l2 = _l2.worldBound; var l1 = _l1.worldBound;
            bool toRight = l2.center.x >= l1.center.x;
            XqSettingsPanel.CascadeTick(l2, _setRowEls[_setPage].worldBound, toRight, minX, limitW, limitH);
        }

        void SetSub(string kind, VisualElement row)
        {
            XqSettingsPanel.CloseIfOpen();   // W5：换二级 / 收二级，三级先收
            _setPage = -1;
            _sub = _sub == kind ? null : kind;
            _danceOpen = false;
            _subT0 = Time.unscaledTime;
            Rebuild();
        }

        VisualElement SubRow(string glyph, string text, string kind)
        {
            VisualElement row = null;
            row = Row(glyph, text, () => SetSub(kind, row), chev: true);
            if (_sub == kind || (kind=="doing" && _sub=="dance"))
            {
                row.style.backgroundColor=new Color(.937f,.91f,.965f);
                row.AddToClassList("xq-mrow-open");
                _subRow = row;
            }
            return row;
        }

        void BuildDoing()
        {
            var h=PetHost.I;bool ready=h!=null&&h.Ready;var st=XiaoqiLink.Current;bool outside=st!=null&&!string.IsNullOrEmpty(st.where)&&st.where!="dorm";bool asleep=Xiaoqi.Spike.PetPresence.IsAsleep;
            if(_sub=="dance")
            {
                Heading("选一支舞");_l2Box.Add(Row("chev","返回互动",()=>{_sub="doing";Rebuild();}));var seen=new HashSet<string>();if(h!=null)foreach(var action in h.Actions){var act=action;if(!act.Id.StartsWith("dance_")||!seen.Add(act.Id))continue;_l2Box.Add(Detail("music",act.Label,"内置动作",ready&&!outside?(Action)(()=>RunHost(act.Id)):null));}
                _l2Box.Add(Detail("music","停止跳舞","结束手动舞蹈",h!=null&&h.IsDancing?(Action)(()=>{h.StopDance();Close("dance-stop");}):null));return;
            }
            Heading("和她互动");_l2Box.Add(Detail("heart","摸摸头","轻轻摸摸她的头",ready&&!asleep?(Action)(()=>{Xiaoqi.Spike.XiaoqiFace.React(.65f,.32f);Close("pat");}):null));
            _l2Box.Add(Detail("spark","换个姿势",ready?"换个舒服一点的姿势":"模型尚未就绪",ready&&!asleep?(Action)(()=>RunHost("pose_next")):null));
            _l2Box.Add(Detail("bed",asleep?"叫醒她":"小睡一会",outside?"她正在外面，回来再休息":"20 分钟 · 随时叫醒",ready&&(!outside||asleep)?(Action)(()=>{if(asleep)Xiaoqi.Spike.PetPresence.Wake();else Xiaoqi.Spike.PetPresence.Nap(XiaoqiRuntime.Config?.nap_min??20);Close("nap");}):null));
            _l2Box.Add(Detail("music","选一支舞",outside?"她正在外面，回来再跳":"和她一起放松一下",ready&&!outside?(Action)(()=>{_sub="dance";_subT0=Time.unscaledTime;Rebuild();}):null));
        }
        void BuildQuiet()
        {
            Heading("声音与勿扰");if(XiaoqiRuntime.Dnd)_l2Box.Add(Detail("sun","结束勿扰",XiaoqiRuntime.Config.muted?"声音仍保持静音，可在下方打开":"恢复主动陪伴",()=>{XiaoqiRuntime.ClearQuiet();Rebuild();}));
            _l2Box.Add(Detail("moon","勿扰 30 分钟","暂缓主动打扰，保留手动互动",()=>{XiaoqiRuntime.SetQuietMinutes(30);Rebuild();}));_l2Box.Add(Row("moon","勿扰 1 小时",()=>{XiaoqiRuntime.SetQuietMinutes(60);Rebuild();}));_l2Box.Add(Row("moon","勿扰至明早 8 点",()=>{XiaoqiRuntime.SetQuietTomorrow8();Rebuild();}));
            bool muted=XiaoqiRuntime.Config.muted;_l2Box.Add(Detail("music",muted?"打开小祈的声音":"静音小祈",muted?"当前已静音，不改变播放器音量":"小祈音量 "+Mathf.RoundToInt(XiaoqiRuntime.Config.volume*100)+"%",()=>{XqSettingsApply.SetMuted(!muted);XiaoqiConfig.Save(XiaoqiRuntime.Config);Rebuild();}));_l2Box.Add(Row("gear","更多声音设置…",()=>{Close("settings");XqSettingsPanel.OpenPage(3);}));
        }

        // ------------------------------------------------------------ 小部件

        static VisualElement Row(string glyph, string text, Action click, bool chev = false)
        {
            var row = new VisualElement();
            row.AddToClassList("xq-mrow");
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            ic.AddToClassList("xq-mic");
            var g = new XqGlyph(glyph, new Color(.61f,.53f,.70f));
            g.AddToClassList("xq-mglyph");
            ic.Add(g);
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("xq-mtext");
            // 不全靠 USS 变量：颜色同时直接写在元素上，主题变量没解析出来也看得见
            label.style.color = XqTheme.Ink;
            ic.style.backgroundColor = Color.clear;
            ic.style.width=22;ic.style.height=22;ic.style.marginRight=12;row.style.height=42;row.style.minHeight=42;row.style.paddingLeft=10;row.style.paddingRight=10;label.style.fontSize=13;
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
