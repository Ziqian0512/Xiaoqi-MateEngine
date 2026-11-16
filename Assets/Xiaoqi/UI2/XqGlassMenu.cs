using System;
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
        public VisualElement Root { get; private set; }

        readonly Action _openToday;
        VisualElement _l1, _l2, _l1Box, _l2Box;
        VisualElement _subRow;
        bool _open, _sideRight; // _sideRight = 菜单在她右边
        string _sub;
        bool _danceOpen;
        float _t0;
        float _subT0;
        bool _blockerSet;

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

            _l1 = XqPetHud.Glass("xq-mcard", true);
            _l1Box = Content(_l1);
            _l2 = XqPetHud.Glass("xq-mcard xq-mcard2", true);
            _l2Box = Content(_l2);
            _l2.style.display = DisplayStyle.None;
            Root.Add(_l1);
            Root.Add(_l2);
            hudRoot.Add(Root);
            Xiaoqi.UI.XqMenu.Replaced = true;
        }

        static VisualElement Content(VisualElement glassHost)
        {
            var body = glassHost.Q(className: "xq-body");
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("xq-mbox");
            body.Add(box);
            return box;
        }

        // ------------------------------------------------------------ 打开 / 关闭

        public void Open(IPanel panel, float scale)
        {
            if (_open) return;
            var body = XqBodyAnchor.Stable();
            if (body.height < 8f) return;
            _open = true;
            IsOpen = true;
            _sub = null;
            _danceOpen = false;
            _subRow = null;
            _t0 = Time.unscaledTime;

            // 选哪一侧：默认放在她靠近屏幕中间的一侧；那一侧放不下两张卡时换另一侧
            float need = (L1W + L2W + Gap * 3f) * scale;
            float roomRight = Screen.width - body.xMax;
            float roomLeft = body.xMin;
            bool preferRight = body.center.x < Screen.width * 0.5f;
            _sideRight = preferRight;
            if (preferRight && roomRight < need && roomLeft > roomRight) _sideRight = false;
            else if (!preferRight && roomLeft < need && roomRight > roomLeft) _sideRight = true;

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
            Debug.Log("XQ_MENU2 close by=" + by);
        }

        public bool Hit(VisualElement picked)
        {
            if (!_open) return false;
            for (var e = picked; e != null; e = e.parent)
                if (e == _l1 || e == _l2) return true;
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
                PetHost.I.RegisterMenuBlocker(() => Xiaoqi.UI.XqMenu.IsOpen);
            }
            if (_open)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (!string.IsNullOrEmpty(_sub)) { _sub = null; _danceOpen = false; Rebuild(); }
                    else Close("esc");
                    return true;
                }
                if (Input.GetMouseButtonDown(1)) { Close("right"); return true; }
                if (Input.GetMouseButtonDown(0) && !Hit(picked)) { Close("blank"); return false; }
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
            if (!_open || Root == null || panel == null) return;
            var body = XqBodyAnchor.Stable();
            if (body.height < 8f) return;

            var tl = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(body.xMin, body.yMax));
            var br = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(body.xMax, body.yMin));
            float limitW = Root.parent != null ? Root.parent.worldBound.width : 0f;
            float limitH = Root.parent != null ? Root.parent.worldBound.height : 0f;
            if (limitW < 16f) limitW = Screen.width / Mathf.Max(0.1f, scale);
            if (limitH < 16f) limitH = Screen.height / Mathf.Max(0.1f, scale);

            float w1 = Wid(_l1, L1W), h1 = Hei(_l1, 340f);
            float k = Ease(Mathf.Clamp01((Time.unscaledTime - _t0) / 0.18f));
            float dir = _sideRight ? 1f : -1f;

            float x1 = _sideRight ? br.x + Gap : tl.x - Gap - w1;
            float y1 = tl.y + (br.y - tl.y) * 0.04f;
            x1 = Mathf.Clamp(x1, 8f, Mathf.Max(8f, limitW - w1 - 8f));
            y1 = Mathf.Clamp(y1, 8f, Mathf.Max(8f, limitH - h1 - 56f));
            _l1.style.left = x1;
            _l1.style.top = y1;
            _l1.style.opacity = k;
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
                float y2 = Mathf.Clamp(rowTop, 8f, Mathf.Max(8f, limitH - h2 - 56f));
                x2 = Mathf.Clamp(x2, 8f, Mathf.Max(8f, limitW - w2 - 8f));
                float k2 = Ease(Mathf.Clamp01((Time.unscaledTime - _subT0) / 0.18f));
                _l2.style.left = x2;
                _l2.style.top = y2;
                _l2.style.opacity = k2;
                _l2.style.translate = new Translate((1f - k2) * 10f * -dir, 0f, 0f);
            }
        }

        // ------------------------------------------------------------ 内容

        void Rebuild()
        {
            _l1Box.Clear();
            _l2Box.Clear();
            _subRow = null;
            var st = XiaoqiLink.Current;

            // 状态行（点它 = 今天）
            var stat = new VisualElement();
            stat.AddToClassList("xq-mstat");
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("xq-mdot");
            var where = new Label(StatusLine(st)) { pickingMode = PickingMode.Ignore };
            where.AddToClassList("xq-mstat-text");
            var energy = new Label(st != null && st.body != null ? "精力 " + Mathf.RoundToInt(Mathf.Clamp(st.body.stamina, 0f, 100f)) : "")
            { pickingMode = PickingMode.Ignore };
            energy.AddToClassList("xq-mstat-energy");
            stat.Add(dot); stat.Add(where); stat.Add(energy);
            stat.RegisterCallback<ClickEvent>(_ => { Close("action"); _openToday?.Invoke(); });
            _l1Box.Add(stat);

            // 形态三段开关
            var seg = new VisualElement { pickingMode = PickingMode.Ignore };
            seg.AddToClassList("xq-mseg");
            seg.Add(Seg("桌宠", "user", XqView.Pet));
            seg.Add(Seg("近景", "expand", XqView.Close));
            seg.Add(Seg("工作台", "cal", XqView.Bench));
            _l1Box.Add(seg);

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
            row.Add(ic);
            row.Add(label);
            if (chev)
            {
                var c = new XqGlyph("chev", XqTheme.Ink);
                c.AddToClassList("xq-mchev");
                row.Add(c);
            }
            if (click != null) row.RegisterCallback<ClickEvent>(_ => click());
            return row;
        }

        static VisualElement Gray(string glyph, string text, string tag)
        {
            var row = Row(glyph, text, null);
            row.AddToClassList("xq-mrow-gray");
            var t = new Label(tag) { pickingMode = PickingMode.Ignore };
            t.AddToClassList("xq-mtag");
            row.Add(t);
            return row;
        }

        static VisualElement Chip(string text, bool stop, Action click)
        {
            var c = new Label(text);
            c.AddToClassList("xq-mchip");
            if (stop) c.AddToClassList("xq-mchip-stop");
            c.RegisterCallback<ClickEvent>(_ => click());
            return c;
        }

        VisualElement Seg(string text, string glyph, XqView view)
        {
            var el = new VisualElement();
            el.AddToClassList("xq-mseg-btn");
            bool on = XqMode.Current == view;
            bool avail = view != XqView.Bench || XqMode.BenchAvailable;
            if (on) el.AddToClassList("xq-mseg-on");
            if (!avail) el.AddToClassList("xq-mseg-off");
            var g = new XqGlyph(glyph, on ? Color.white : XqTheme.Ink);
            g.AddToClassList("xq-mseg-glyph");
            var t = new Label(avail ? text : "工作台 ·未开") { pickingMode = PickingMode.Ignore };
            t.AddToClassList("xq-mseg-text");
            if (on) t.AddToClassList("xq-mseg-text-on");
            el.Add(g);
            el.Add(t);
            el.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (!avail || on || XqMode.Busy) return;
                Close("action");
                XqMode.Set(view);
            });
            return el;
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

        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
