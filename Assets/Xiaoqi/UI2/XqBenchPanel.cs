using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// G74 W1 工作台（Unity 原生版）。和近景同一个窗口、同一套 UI Toolkit，不开 Edge、不起服务器页。
    /// 版式取 Fantasy World：纯白超大圆角 4:3 板 + 分离的悬浮药丸侧栏，内容是「左粗标题灰说明 / 右控件」的一行一行。
    ///
    /// - 只在 Show/Hide 里切显示；Build 在 Warm()（启动后预建，隐藏）或第一次 Show 时做一次，
    ///   屏幕大小变了（&gt;3%）才重建，所以点「工作台」时没有构建卡顿。
    /// - 动画一律手写 Tick（和近景同一做法），不依赖 USS transition。
    /// - 点击一律轮询 worldBound + XqPanelXf.FromScreen（#18 的教训：只用翻 Y 的那个点）。
    /// - 数据沿用 XqCloseData（/api/body/day|feed）。连不上就用上次缓存（xq_bench_cache.json）并亮「本地服务没开 · 重试」。
    /// - 今日页的行只放有数据来源的：日程（day.segs）、精力/困倦/社交（stage.body）、最近说的（feed）。
    ///   预览里的「今日小事」没有接口，不放。
    /// </summary>
    public class XqBenchPanel
    {
        static XqBenchPanel _i;
        public static bool IsShown { get { return _i != null && _i._shown; } }
        public static bool OverNow { get { return _i != null && _i._shown && _i.OverAnySelf(); } }

        public static void Warm()
        {
            if (_i == null) _i = new XqBenchPanel();
            try { _i.EnsureBuilt(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH warm: " + e.Message); }
        }

        public static void Show()
        {
            if (_i == null) _i = new XqBenchPanel();
            try { _i.ShowSelf(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH show: " + e.Message); }
        }

        public static void Hide()
        {
            if (_i == null) return;
            try { _i.HideSelf(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH hide: " + e.Message); }
        }

        // ---------------- 色 ----------------
        static Color Rgb(int r, int g, int b, float a = 1f) { return new Color(r / 255f, g / 255f, b / 255f, a); }
        static readonly Color Ink = Rgb(23, 21, 31);
        static readonly Color Ink2 = Rgb(23, 21, 31, 0.52f);
        static readonly Color Ink3 = Rgb(23, 21, 31, 0.30f);
        static readonly Color Line = Rgb(23, 21, 31, 0.08f);
        static readonly Color Acc = Rgb(255, 127, 174);
        static readonly Color Dark = Rgb(43, 40, 51);

        // 设计稿尺寸（和预览 v3 一致）：侧栏 66 + 间距 16 + 白板 800×600
        const float RailW = 66f, Gap = 16f, PanW = 800f, PanH = 600f, StageW = RailW + Gap + PanW;
        const int RowN = 6;

        // ---------------- 状态 ----------------
        bool _built, _shown, _glass;
        float _k = 1f;            // 设计 px → 面板单位
        float _builtPanelH;
        float _t;                 // 进入后的秒数
        float _exitT = -1f;       // >=0 退出动画中
        int _lastClickFrame = -1;
        bool _offline;
        XqCloseDay _day;
        string _say;
        float _glassAnim;

        VisualElement _host, _stage, _rail, _panel, _glassLayer, _sweep, _offBar, _offRetry, _close, _tg, _knob;
        readonly VisualElement[] _row = new VisualElement[RowN];
        readonly float[] _rowHover = new float[RowN];
        readonly List<VisualElement> _nav = new List<VisualElement>();
        readonly List<VisualElement> _fills = new List<VisualElement>();
        readonly List<VisualElement> _knobs = new List<VisualElement>();
        readonly List<float> _fillPct = new List<float>();
        VisualElement _timelineHost;
        Label _sayLabel, _metaLabel;
        Label[] _sliderVal = new Label[3];
        XqCloseData _data;
        bool _scheduled;

        // ---------------- 显示 / 隐藏 ----------------
        void ShowSelf()
        {
            EnsureBuilt();
            if (_host == null) return;
            _glass = PlayerPrefs.GetInt("xq_bench_glass", 0) == 1;
            _glassAnim = _glass ? 1f : 0f;
            _shown = true; _exitT = -1f; _t = 0f;
            _host.style.display = DisplayStyle.Flex;
            _data = XqCloseData.Ensure();
            _data.Changed -= OnData; _data.Changed += OnData;
            _data.Begin();
            LoadCacheIfEmpty();
            Refill();
            TickVisuals();
            PetHost.I?.SetUiBlocksMovement(true);
            Debug.Log("XQ_BENCH open k=" + _k.ToString("0.000") + " glass=" + _glass);
        }

        void HideSelf()
        {
            if (!_shown) return;
            _shown = false;
            if (_data != null) { _data.Changed -= OnData; _data.End(); }
            _exitT = 0f;
            PetHost.I?.SetUiBlocksMovement(false);
            Debug.Log("XQ_BENCH close");
        }

        // ---------------- 构建 ----------------
        void EnsureBuilt()
        {
            float panelH = Mathf.Clamp(Screen.height * 0.70f, 420f, 780f);
            if (panelH * PanW / PanH + (RailW + Gap) * panelH / PanH > Screen.width * 0.72f)
                panelH = Screen.width * 0.72f / ((PanW + RailW + Gap) / PanH);
            if (_built && Mathf.Abs(panelH - _builtPanelH) / _builtPanelH < 0.03f) return;

            var doc = XqClosePanel.PickDoc();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) { Debug.Log("XQ_BENCH no UIDocument"); return; }
            var uss = Resources.Load<StyleSheet>("XqClose");
            if (uss != null && !root.styleSheets.Contains(uss)) root.styleSheets.Add(uss);

            if (_host != null && _host.parent != null) _host.parent.Remove(_host);
            _built = false;
            _builtPanelH = panelH;
            _nav.Clear(); _fills.Clear(); _knobs.Clear(); _fillPct.Clear();
            float upp = root.panel != null ? XqPanelXf.UnitsPerPx(root.panel) : 1f;
            _k = (panelH / PanH) * upp;

            _host = new VisualElement { name = "xq-bench-host", pickingMode = PickingMode.Ignore };
            _host.AddToClassList("xq-root");
            Abs(_host, 0, 0, -1, -1);
            _host.style.right = 0; _host.style.bottom = 0;
            _host.style.display = DisplayStyle.None;

            _stage = new VisualElement { name = "xq-bench-stage", pickingMode = PickingMode.Ignore };
            Abs(_stage, 0, 0, StageW * _k, PanH * _k);
            _stage.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            _host.Add(_stage);
            BuildRail();
            BuildPanel();
            root.Add(_host);
            PlaceStage();
            if (!_scheduled) { _host.schedule.Execute(Tick).Every(16); _scheduled = true; }
            _built = true;
            Debug.Log("XQ_BENCH built k=" + _k.ToString("0.000") + " panelH=" + panelH.ToString("0"));
        }

        void PlaceStage()
        {
            float upp = _host.panel != null ? XqPanelXf.UnitsPerPx(_host.panel) : 1f;
            float stagePx = StageW * _k / upp;
            float hPx = PanH * _k / upp;
            float left = Mathf.Clamp(Screen.width * 0.27f, 0f, Mathf.Max(0f, Screen.width - stagePx - 30f));
            float top = (Screen.height - hPx) * 0.5f + Screen.height * 0.02f;
            _stage.style.left = left * upp;
            _stage.style.top = top * upp;
        }

        void BuildRail()
        {
            _rail = new VisualElement { pickingMode = PickingMode.Position };
            Abs(_rail, 0, 0, RailW * _k, PanH * _k);
            Round(_rail, 33f * _k);
            _rail.style.backgroundColor = Rgb(120, 110, 150, 0.50f);
            Border(_rail, 1f, Rgb(255, 255, 255, 0.34f));
            _rail.style.alignItems = Align.Center;
            _rail.style.paddingTop = 16f * _k; _rail.style.paddingBottom = 16f * _k;
            var dot = Box(6f * _k, 6f * _k, 3f * _k, Rgb(255, 255, 255, 0.75f));
            dot.style.marginBottom = 12f * _k;
            _rail.Add(dot);
            string[] icons = { "home", "heart", "chat", "spark", "gear" };
            for (int n = 0; n < icons.Length; n++)
            {
                var b = Box(46f * _k, 46f * _k, 16f * _k, n == 0 ? Color.white : new Color(1, 1, 1, 0));
                b.pickingMode = PickingMode.Position;
                b.style.marginBottom = 6f * _k;
                b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
                var g = new XqGlyph(icons[n], n == 0 ? Ink : new Color(1, 1, 1, n >= 3 ? 0.42f : 0.88f));
                g.style.width = 22f * _k; g.style.height = 22f * _k;
                b.Add(g);
                if (n == 4) b.style.marginTop = new StyleLength(StyleKeyword.Auto); // 设置键沉到底
                _nav.Add(b); _rail.Add(b);
            }
            _stage.Add(_rail);
        }

        void BuildPanel()
        {
            // 阴影三层（UI Toolkit 没有 box-shadow，用放大、偏移、低透明度的圆角矩形叠）
            for (int s = 3; s >= 1; s--)
            {
                var sh = Box((PanW + s * 14f) * _k, (PanH + s * 14f) * _k, (38f + s * 7f) * _k, new Color(0.06f, 0.03f, 0.16f, 0.07f));
                sh.pickingMode = PickingMode.Ignore;
                Abs(sh, (RailW + Gap - s * 7f) * _k, (s * 10f - 7f) * _k, (PanW + s * 14f) * _k, (PanH + s * 14f) * _k);
                _stage.Add(sh);
            }

            _panel = new VisualElement { name = "xq-bench-panel", pickingMode = PickingMode.Position };
            Abs(_panel, (RailW + Gap) * _k, 0, PanW * _k, PanH * _k);
            Round(_panel, 38f * _k);
            _panel.style.backgroundColor = Color.white;
            _panel.style.overflow = Overflow.Hidden;
            _glassLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_glassLayer, 0, 0, PanW * _k, PanH * _k);
            Round(_glassLayer, 38f * _k);
            Border(_glassLayer, 1.5f, Rgb(255, 255, 255, 0.9f));
            _glassLayer.style.opacity = 0f;
            _panel.Add(_glassLayer);

            // 标题区
            var title = L("XIAOQI WORKBENCH", 25f, Ink, true);
            Abs(title, 40f * _k, 28f * _k, -1, -1);
            _metaLabel = L("主人 · --:-- · 今日", 11.5f, Ink2, false);
            Abs(_metaLabel, 40f * _k, 62f * _k, -1, -1);
            _panel.Add(title); _panel.Add(_metaLabel);
            _close = Box(34f * _k, 34f * _k, 17f * _k, new Color(0, 0, 0, 0));
            _close.pickingMode = PickingMode.Position;
            Abs(_close, (PanW - 40f - 34f) * _k, 26f * _k, 34f * _k, 34f * _k);
            _close.style.alignItems = Align.Center; _close.style.justifyContent = Justify.Center;
            var x = L("×", 22f, Ink2, false); _close.Add(x);
            _panel.Add(_close);
            var gm = new XqGlyph("moon", Ink2); Abs(gm, (PanW - 40f - 34f - 40f) * _k, 32f * _k, 20f * _k, 20f * _k); _panel.Add(gm);

            // 行
            float y0 = 98f, rh = 80f;
            for (int r = 0; r < RowN; r++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                Abs(row, 40f * _k, (y0 + r * rh) * _k, (PanW - 80f) * _k, rh * _k);
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                if (r < RowN - 1) { row.style.borderBottomWidth = 1f; row.style.borderBottomColor = Line; }
                _row[r] = row; _panel.Add(row);
            }
            Row(0, "今日日程", "按今天的安排走", out var c0);
            _timelineHost = c0; _timelineHost.style.flexDirection = FlexDirection.Row;
            Row(1, "精力", "越高越有劲", out var c1); AddSlider(c1, 0);
            Row(2, "困倦", "越高越想睡", out var c2); AddSlider(c2, 1);
            Row(3, "社交", "她今天跟人说了多少话", out var c3); AddSlider(c3, 2);
            Row(4, "最近说的", "她最近一句", out var c4);
            c4.style.justifyContent = Justify.FlexEnd;
            _sayLabel = L("还没有", 12.5f, Ink, false);
            var bub = new VisualElement { pickingMode = PickingMode.Ignore };
            bub.style.backgroundColor = Rgb(243, 241, 247);
            bub.style.paddingLeft = bub.style.paddingRight = 14f * _k; bub.style.paddingTop = bub.style.paddingBottom = 9f * _k;
            Round(bub, 14f * _k); bub.style.borderBottomLeftRadius = 4f * _k; bub.style.maxWidth = 380f * _k;
            _sayLabel.style.whiteSpace = WhiteSpace.Normal;
            bub.Add(_sayLabel); c4.Add(bub);
            Row(5, "薄雾玻璃", "面板半透明，背后的壁纸透出来", out var c5);
            c5.style.justifyContent = Justify.FlexEnd;
            _tg = Box(56f * _k, 30f * _k, 15f * _k, Dark); _tg.pickingMode = PickingMode.Position;
            _knob = Box(24f * _k, 24f * _k, 12f * _k, Color.white);
            Abs(_knob, 29f * _k, 3f * _k, 24f * _k, 24f * _k);
            _tg.Add(_knob); c5.Add(_tg);

            // 高光扫过
            _sweep = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_sweep, -200f * _k, -120f * _k, 150f * _k, 900f * _k);
            _sweep.style.backgroundColor = new Color(1, 1, 1, 0.30f);
            _sweep.style.rotate = new Rotate(new Angle(18f, AngleUnit.Degree));
            _panel.Add(_sweep);

            // 离线条
            _offBar = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_offBar, (PanW - 30f - 290f) * _k, (PanH - 18f - 36f) * _k, 290f * _k, 36f * _k);
            Round(_offBar, 18f * _k); _offBar.style.backgroundColor = Ink;
            _offBar.style.flexDirection = FlexDirection.Row; _offBar.style.alignItems = Align.Center;
            _offBar.style.paddingLeft = 14f * _k; _offBar.style.opacity = 0f;
            var ot = L("本地服务没开 · 显示的是上次的内容", 11.5f, Color.white, true);
            ot.style.flexGrow = 1f;
            _offRetry = Box(52f * _k, 26f * _k, 13f * _k, Color.white); _offRetry.pickingMode = PickingMode.Position;
            _offRetry.style.alignItems = Align.Center; _offRetry.style.justifyContent = Justify.Center;
            _offRetry.style.marginRight = 5f * _k;
            _offRetry.Add(L("重试", 11.5f, Ink, true));
            _offBar.Add(ot); _offBar.Add(_offRetry);
            _panel.Add(_offBar);

            _stage.Add(_panel);
        }

        void Row(int r, string name, string hint, out VisualElement ctl)
        {
            var lab = new VisualElement { pickingMode = PickingMode.Ignore };
            lab.style.width = 220f * _k; lab.style.flexShrink = 0f;
            lab.Add(L(name, 15.5f, Ink, true));
            var h = L(hint, 11f, Ink2, false); h.style.marginTop = 5f * _k; lab.Add(h);
            ctl = new VisualElement { pickingMode = PickingMode.Ignore };
            ctl.style.flexGrow = 1f; ctl.style.flexDirection = FlexDirection.Row; ctl.style.alignItems = Align.Center;
            _row[r].Add(lab); _row[r].Add(ctl);
        }

        void AddSlider(VisualElement ctl, int idx)
        {
            ctl.style.justifyContent = Justify.FlexEnd;
            var tr = new VisualElement { pickingMode = PickingMode.Ignore };
            tr.style.width = 300f * _k; tr.style.height = 2f * _k; tr.style.backgroundColor = Rgb(23, 21, 31, 0.12f);
            tr.style.marginRight = 16f * _k;
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(fill, 0, 0, 0, 2f * _k); fill.style.backgroundColor = Ink;
            var kn = Box(10f * _k, 10f * _k, 5f * _k, Ink);
            Abs(kn, -5f * _k, -4f * _k, 10f * _k, 10f * _k);
            tr.Add(fill); tr.Add(kn);
            _fills.Add(fill); _knobs.Add(kn); _fillPct.Add(0f);
            _sliderVal[idx] = L("--", 12f, Ink2, true);
            _sliderVal[idx].style.width = 70f * _k; _sliderVal[idx].style.unityTextAlign = TextAnchor.MiddleRight;
            ctl.Add(tr); ctl.Add(_sliderVal[idx]);
        }

        // ---------------- 数据 ----------------
        void OnData()
        {
            if (!_shown || _data == null) return;
            try
            {
                if (_data.Ready) { _offline = false; _day = _data.Day ?? _day; if (!string.IsNullOrEmpty(_data.Say)) _say = _data.Say; SaveCache(); }
                else { _offline = true; LoadCacheIfEmpty(); }
                Refill();
            }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH data: " + e.Message); }
        }

        [Serializable] class Cache { public string day; public string say; }
        static string CachePath { get { return Path.Combine(Application.dataPath, "..", "xq_bench_cache.json"); } }

        void SaveCache()
        {
            try
            {
                var c = new Cache { day = _day != null ? JsonUtility.ToJson(_day) : null, say = _say };
                File.WriteAllText(CachePath, JsonUtility.ToJson(c));
            }
            catch (Exception e) { Debug.Log("XQ_BENCH cache save " + e.Message); }
        }

        void LoadCacheIfEmpty()
        {
            if (_day != null || !string.IsNullOrEmpty(_say)) return;
            try
            {
                if (!File.Exists(CachePath)) return;
                var c = JsonUtility.FromJson<Cache>(File.ReadAllText(CachePath));
                if (c == null) return;
                if (!string.IsNullOrEmpty(c.day)) _day = JsonUtility.FromJson<XqCloseDay>(c.day);
                _say = c.say;
            }
            catch (Exception e) { Debug.Log("XQ_BENCH cache load " + e.Message); }
        }

        void Refill()
        {
            if (_host == null) return;
            var now = DateTime.Now;
            int nowMin = (!_offline && _day != null && _day.now_min > 0) ? _day.now_min : now.Hour * 60 + now.Minute;
            string stamp = (!_offline && _day != null && !string.IsNullOrEmpty(_day.now)) ? _day.now : now.ToString("HH:mm");
            _metaLabel.text = "主人 · " + stamp + " · 今日";

            // 日程：以当前段为中心取 5 段
            _timelineHost.Clear();
            var segs = _day != null && _day.segs != null ? _day.segs : new XqCloseDay.Seg[0];
            if (segs.Length == 0)
            {
                _timelineHost.Add(L(_offline ? "没有缓存的日程" : "今天还没有日程", 12.5f, Ink2, false));
            }
            else
            {
                int cur = -1;
                for (int s = 0; s < segs.Length; s++) if (segs[s] != null && segs[s].a <= nowMin && nowMin < Mathf.Max(segs[s].b, segs[s].a + 1)) cur = s;
                int center = cur >= 0 ? cur : Mathf.Max(0, UpcomingIndex(segs, nowMin) - 1);
                int from = Mathf.Clamp(center - 1, 0, Mathf.Max(0, segs.Length - 5));
                int to = Mathf.Min(segs.Length, from + 5);
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                Abs(line, 40f * _k, 6f * _k, 420f * _k, 2f * _k);
                line.style.backgroundColor = Rgb(23, 21, 31, 0.12f);
                var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
                wrap.style.flexGrow = 1f; wrap.style.flexDirection = FlexDirection.Row; wrap.style.justifyContent = Justify.SpaceBetween;
                wrap.style.height = 56f * _k;
                wrap.Add(line);
                for (int s = from; s < to; s++)
                {
                    var sg = segs[s]; if (sg == null) continue;
                    bool past = sg.b > 0 && sg.b <= nowMin && s != cur;
                    bool isNow = s == cur;
                    var col = new VisualElement { pickingMode = PickingMode.Ignore };
                    col.style.width = 80f * _k; col.style.alignItems = Align.Center;
                    var d = Box(12f * _k, 12f * _k, 6f * _k, isNow ? Acc : (past ? Ink : Color.white));
                    Border(d, 2f, isNow ? Color.white : (past ? Ink : Ink3));
                    if (isNow) { d.name = "now-dot"; }
                    col.Add(d);
                    var tm = L(string.IsNullOrEmpty(sg.start) ? "" : sg.start, 11.5f, Ink, true); tm.style.marginTop = 7f * _k; col.Add(tm);
                    var nm = L(XqCloseDay.What(sg), 10.5f, isNow ? Ink : Ink2, isNow); nm.style.marginTop = 3f * _k;
                    nm.style.unityTextAlign = TextAnchor.UpperCenter; nm.style.whiteSpace = WhiteSpace.Normal; nm.style.maxWidth = 78f * _k;
                    col.Add(nm);
                    if (past) col.style.opacity = 0.5f;
                    wrap.Add(col);
                }
                _timelineHost.Add(wrap);
            }

            // 精力 / 困倦 / 社交（0–100，和近景同量纲）
            var b = _day != null ? _day.body : null;
            var vals = new float[] { b != null ? b.stamina : -1f, b != null ? b.sleepy : -1f, b != null ? b.social : -1f };
            for (int s = 0; s < 3; s++)
            {
                float v = vals[s];
                _fillPct[s] = v < 0f ? 0f : Mathf.Clamp01(v / 100f);
                _sliderVal[s].text = v < 0f ? "--" : Mathf.RoundToInt(Mathf.Clamp(v, 0f, 100f)).ToString();
            }
            _sayLabel.text = string.IsNullOrEmpty(_say) ? "还没有" : _say;
        }

        static int UpcomingIndex(XqCloseDay.Seg[] segs, int nowMin)
        {
            for (int s = 0; s < segs.Length; s++) if (segs[s] != null && segs[s].a > nowMin) return s;
            return segs.Length - 1;
        }

        // ---------------- 每帧 ----------------
        static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
        static float EaseBack(float x)
        {
            x = Mathf.Clamp01(x); const float c1 = 1.6f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        void Tick()
        {
            if (_host == null || _host.panel == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_shown) { _t += dt; }
            else if (_exitT >= 0f)
            {
                _exitT += dt;
                if (_exitT >= 0.30f)
                {
                    _exitT = -1f;
                    _host.style.display = DisplayStyle.None;
                    return;
                }
            }
            else return;
            TickVisuals();
            if (_shown) TickPointer(dt);
        }

        void TickVisuals()
        {
            float k = _k;
            // 整块：进入 0.8s 弹性放大 + 上浮；退出 0.3s 反向
            float op, sc, ty;
            if (_exitT >= 0f)
            {
                float e = EaseOut(_exitT / 0.30f);
                op = 1f - e; sc = 1f - 0.05f * e; ty = 18f * k * e;
            }
            else
            {
                float p = EaseBack(_t / 0.8f);
                op = Mathf.Clamp01(_t / 0.35f); sc = 0.93f + 0.07f * p; ty = (1f - p) * 26f * k;
            }
            _stage.style.opacity = op;
            _stage.style.scale = new Scale(new Vector3(sc, sc, 1f));
            _stage.style.translate = new Translate(0f, ty, 0f);

            // 侧栏图标错开浮入
            for (int n = 0; n < _nav.Count; n++)
            {
                float p = EaseOut((_t - 0.10f - n * 0.05f) / 0.45f);
                _nav[n].style.opacity = p;
                _nav[n].style.translate = new Translate(-10f * k * (1f - p), 0f, 0f);
            }
            // 行依次浮入 + 悬停右移
            for (int r = 0; r < RowN; r++)
            {
                float p = EaseOut((_t - 0.20f - r * 0.08f) / 0.60f);
                _row[r].style.opacity = p;
                _row[r].style.translate = new Translate(_rowHover[r] * 4f * k, (1f - p) * 14f * k, 0f);
            }
            // 细滑条从 0 长到值
            for (int s = 0; s < _fills.Count; s++)
            {
                float p = EaseOut((_t - 0.45f - (s + 1) * 0.08f) / 1.0f);
                float w = 300f * k * _fillPct[s] * p;
                _fills[s].style.width = w;
                _knobs[s].style.left = w - 5f * k;
            }
            // 当前段的点：脉冲
            var nd = _timelineHost != null ? _timelineHost.Q("now-dot") : null;
            if (nd != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
                nd.style.scale = new Scale(new Vector3(1f + 0.25f * pulse, 1f + 0.25f * pulse, 1f));
            }
            // 高光扫过：每 6 秒一次，前 40% 时间内从左划到右
            float ph = (_t % 6f) / 6f;
            float sx = ph < 0.4f ? Mathf.Lerp(-200f, PanW + 80f, ph / 0.4f) : -400f;
            _sweep.style.left = sx * k;
            _sweep.style.opacity = _t > 1.2f ? 1f : 0f;
            // 玻璃档：白板底色从不透明白到半透明
            float gt = _glass ? 1f : 0f;
            _glassAnim = Mathf.MoveTowards(_glassAnim, gt, Time.unscaledDeltaTime * 4f);
            _panel.style.backgroundColor = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.62f, _glassAnim));
            _glassLayer.style.opacity = _glassAnim;
            _tg.style.backgroundColor = Color.Lerp(Rgb(217, 214, 224), Dark, _glassAnim);
            _knob.style.left = Mathf.Lerp(3f, 29f, _glassAnim) * k;
            // 离线条
            float ot = Mathf.MoveTowards(_offBar.style.opacity.value, _offline ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            _offBar.style.opacity = ot;
            _offBar.pickingMode = ot > 0.5f ? PickingMode.Position : PickingMode.Ignore;
        }

        // ---------------- 点击 ----------------
        bool OverEl(VisualElement el)
        {
            if (el == null || _host == null || _host.panel == null) return false;
            if (el.resolvedStyle.display == DisplayStyle.None) return false;
            var r = el.worldBound;
            if (float.IsNaN(r.x) || r.width < 1f || r.height < 1f) return false;
            return r.Contains(XqPanelXf.FromScreen(_host.panel, Input.mousePosition));
        }

        bool OverAnySelf() { return OverEl(_rail) || OverEl(_panel); }

        void TickPointer(float dt)
        {
            for (int r = 0; r < RowN; r++)
                _rowHover[r] = Mathf.MoveTowards(_rowHover[r], OverEl(_row[r]) ? 1f : 0f, dt * 6f);
            if (!Input.GetMouseButtonDown(0) || _lastClickFrame == Time.frameCount || XqMode.Busy) return;
            if (OverEl(_close)) { _lastClickFrame = Time.frameCount; Debug.Log("XQ_BENCH btn close"); XqMode.Set(XqView.Close); return; }
            if (_offline && OverEl(_offRetry))
            {
                _lastClickFrame = Time.frameCount; Debug.Log("XQ_BENCH retry");
                if (_data != null) { _data.End(); _data.Begin(); }
                return;
            }
            if (OverEl(_tg) || OverEl(_row[5]))
            {
                _lastClickFrame = Time.frameCount;
                _glass = !_glass; PlayerPrefs.SetInt("xq_bench_glass", _glass ? 1 : 0); PlayerPrefs.Save();
                Debug.Log("XQ_BENCH glass=" + _glass);
                return;
            }
            for (int n = 1; n < _nav.Count; n++)
                if (OverEl(_nav[n])) { _lastClickFrame = Time.frameCount; Debug.Log("XQ_BENCH nav " + n + " (W2)"); return; }
        }

        // ---------------- 小工具 ----------------
        static void Abs(VisualElement el, float l, float t, float w, float h)
        {
            el.style.position = Position.Absolute;
            el.style.left = l; el.style.top = t;
            if (w >= 0f) el.style.width = w;
            if (h >= 0f) el.style.height = h;
        }

        static void Round(VisualElement el, float r)
        {
            el.style.borderTopLeftRadius = r; el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r; el.style.borderBottomRightRadius = r;
        }

        static void Border(VisualElement el, float w, Color c)
        {
            el.style.borderTopWidth = w; el.style.borderBottomWidth = w; el.style.borderLeftWidth = w; el.style.borderRightWidth = w;
            el.style.borderTopColor = c; el.style.borderBottomColor = c; el.style.borderLeftColor = c; el.style.borderRightColor = c;
        }

        VisualElement Box(float w, float h, float r, Color bg)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.width = w; e.style.height = h; e.style.flexShrink = 0f;
            Round(e, r); e.style.backgroundColor = bg;
            return e;
        }

        Label L(string text, float size, Color col, bool bold)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size * _k; l.style.color = col;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0f;
            l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0f;
            return l;
        }
    }
}
