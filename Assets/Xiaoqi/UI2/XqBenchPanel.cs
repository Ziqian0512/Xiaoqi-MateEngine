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
    /// 她：缩回桌宠大小（屏高的 28%~42%，默认 34%），只贴在板的上沿 / 左沿 / 右沿——
    ///   上沿：探头 / 探出上半身；左右沿：探头 / 靠边站。板盖在她前面，被盖住的部分就是「躲在板后面」。
    ///   下面不会有她。拖她：只会吸附到这三条边（拖到板下方会被推回最近的边）；点她换姿势。
    ///   （坐 / 躺 / 抱需要新动画，不在这一版。）
    ///
    /// - Build 在 Warm()（启动后预建，隐藏）或第一次 Show 时做，按「显示器」尺寸而不是当前小窗口算，点工作台时不再现场构建。
    /// - 动画靠 UI 调度器的 Tick（只管画面）；指针一律由 XqUi2Sample.Update 每帧调 Poll()（不再挂调度器，高刷新率下会漏按下那一帧）。
    /// - 命中一律轮询 worldBound + XqPanelXf.FromScreen（#18 的教训：只用翻 Y 的那个点）。
    /// - 数据沿用 XqCloseData（/api/body/day|feed）。连不上就用上次缓存（xq_bench_cache.json）并亮「本地服务没开 · 重试」。
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

        /// <summary>XqUi2Sample.Update 每帧调。</summary>
        public static void Poll()
        {
            if (_i == null) return;
            try { if (_i._shown || _i._exitT >= 0f) _i.Tick(); }   // 动画也每帧在这里推（重建面板后调度器会丢，W2 板不显示的嫌疑）
            catch (Exception e) { Debug.LogWarning("XQ_BENCH tick: " + e.Message); }
            if (!_i._shown) return;
            try { _i.PollSelf(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH poll: " + e.Message); }
        }

        // ---------------- 色（跟随 pink / blue / violet 主题）----------------
        static Color Rgb(int r, int g, int b, float a = 1f) { return new Color(r / 255f, g / 255f, b / 255f, a); }
        static readonly Color Ink = Rgb(42, 33, 62);
        static readonly Color Ink2 = Rgb(59, 47, 85, 0.58f);
        static readonly Color Ink3 = Rgb(59, 47, 85, 0.30f);
        static readonly Color Line = Rgb(59, 47, 85, 0.07f);
        static readonly Color Dark = Rgb(62, 52, 86);

        static void ThemePalette(out Color acc, out Color accDeep, out Color glassBg, out Color railBg, out Color trackBg)
        {
            string th = XqUi2Sample.CurrentTheme;
            if (th == "blue")
            {
                acc = Rgb(90, 168, 255); accDeep = Rgb(42, 111, 196);
                glassBg = Rgb(245, 250, 255); railBg = Rgb(228, 240, 254, 0.64f); trackBg = Rgb(90, 168, 255, 0.16f);
            }
            else if (th == "violet")
            {
                acc = Rgb(177, 132, 255); accDeep = Rgb(123, 79, 208);
                glassBg = Rgb(249, 245, 255); railBg = Rgb(236, 226, 254, 0.64f); trackBg = Rgb(177, 132, 255, 0.16f);
            }
            else
            {
                acc = Rgb(255, 132, 178); accDeep = Rgb(214, 71, 127);
                glassBg = Rgb(255, 247, 251); railBg = Rgb(250, 230, 240, 0.64f); trackBg = Rgb(255, 132, 178, 0.16f);
            }
        }
        static Color Acc { get { Color a, d, g, r, t; ThemePalette(out a, out d, out g, out r, out t); return a; } }

        // 设计稿尺寸（和预览 v3 一致）：侧栏 66 + 间距 16 + 白板 800×600
        const float RailW = 66f, Gap = 16f, PanW = 800f, PanH = 600f, StageW = RailW + Gap + PanW;
        const int RowN = 6;

        // ---------------- 几何（以「屏高 = 1」为单位，和她的镜头落点同一套）----------------
        struct Geo { public float a, ph, k, stageL, stageT, stageW, boardL, boardR; }

        static void MonitorDims(out float w, out float h)
        {
            w = Screen.width; h = Screen.height;
            // W3：窗口已经铺满她所在的显示器（近景/工作台），这时 Screen 就是真尺寸；多屏时 mainWindowDisplayInfo 可能是另一块屏（实测 2560x1440 vs 1920x1080）
            bool full = XqCloseCamera.IsActive && Screen.width >= 1000 && Screen.height >= 600;
            if (!full)
            {
                try
                {
                    var di = Screen.mainWindowDisplayInfo;
                    if (di.width > 0 && di.height > 0) { w = di.width; h = di.height; }
                }
                catch (Exception) { }
            }
            if (w < 100f || h < 100f) { w = 1920f; h = 1080f; }
        }

        static float PanelPx(float W, float H)
        {
            float ph = 0.74f * H;                       // W2：板退到左边 ~62% 宽，右边留给她
            float stageW = ph * StageW / PanH;
            if (stageW > 0.62f * W) ph = 0.62f * W * PanH / StageW;
            return Mathf.Clamp(ph, 420f, 1100f);
        }

        static Geo GeoOf(float W, float H)
        {
            var g = new Geo();
            g.a = W / H;
            g.ph = PanelPx(W, H) / H;
            g.k = g.ph / PanH;
            g.stageW = StageW * g.k;
            // 位置：默认靠左偏下留出头顶给她；拖过就用拖到的（夹在显示器内）
            float l0 = _sx >= 0f ? _sx : 0.04f * g.a;
            float t0 = _sy >= 0f ? _sy : (1f - g.ph) * 0.5f;
            g.stageL = Mathf.Clamp(l0, 0.02f, Mathf.Max(0.02f, g.a - g.stageW - 0.02f));
            g.stageT = Mathf.Clamp(t0, 0.03f, Mathf.Max(0.03f, 1f - g.ph - 0.07f));   // 底下留给叠层卡片
            g.boardL = g.stageL + (RailW + Gap) * g.k;
            g.boardR = g.stageL + g.stageW;
            return g;
        }

        // ---------------- 她贴在哪（静态，XqMode 进入前就要用）----------------
        static float _sx = -1f, _sy = -1f;   // 工作台被拖过之后的位置（屏高=1 单位；<0 = 用默认）
        static int _edge = 0;        // 0 上沿  1 左沿  2 右沿（没有「下沿」）
        static float _pos = 0.72f;   // 沿这条边的位置 0~1
        static int _pose = 1;        // 0 探头  1 探出上半身 / 靠边站

        /// <summary>她的目标：身高占屏高的比例，眼睛落在镜头视口的 (vx,vy)（左下原点）。</summary>
        public static void PetTarget(out float frac, out float vx, out float vy)
        {
            // W3：她固定一个乖巧站姿，立在半透工作台的正后方（板中心），随板一起动。
            var c = XqModeConfig.I;
            float W, H; MonitorDims(out W, out H);
            var g = GeoOf(W, H);
            frac = Mathf.Clamp(c.bench_stage_frac, 0.5f, 0.95f);
            float ex = 0.5f * (g.boardL + g.boardR) + c.bench_stage_dx * g.ph;
            float headTop = g.stageT + c.bench_stage_top * g.ph;      // 头顶相对板上沿（负 = 探出板上沿）
            float ey = headTop + 0.09f * frac;                        // 眼睛在头顶下 9% 身高
            ey = Mathf.Max(ey, 0.012f + 0.09f * frac);                // 头顶别出屏
            ex = Mathf.Clamp(ex, 0.04f, g.a - 0.04f);
            vx = ex / g.a; vy = 1f - ey;
        }

        /// <summary>贴边时的歪头 / 转身（度）。左沿：头往左探、脸朝板；右沿反过来；上沿不歪。</summary>
        public static void PetTilt(out float roll, out float yaw)
        {
            // W2「陀螺仪」：她朝光标（或真陀螺仪）的方向轻轻转身 + 歪头；正 yaw = 脸朝屏幕左
            float deg = Mathf.Clamp(XqModeConfig.I.bench_parallax_deg, 0f, 18f);
            yaw = -_pxS * deg;
            roll = yaw * 0.25f;
        }
        static float _pxS, _pyS;           // 平滑后的视差输入 -1~1（右/上为正）
        static float _lastTy = 999f;
        static bool _gyroTried, _gyroOk;

        /// <summary>XqMode 进入工作台后调：把贴边的歪头/转身角度交给镜头那边。</summary>
        public static void ApplyTilt()
        {
            float r, y; PetTilt(out r, out y);
            XqCloseCamera.SetTilt(r, y);
        }

        static void ApplyPet()
        {
            float f, vx, vy; PetTarget(out f, out vx, out vy);
            ApplyTilt();
            XqCloseCamera.NudgeEyes(vx, vy);
        }

        /// <summary>鼠标（屏高=1 单位，y 向下）→ 三条边里最近的一条与位置。下沿不在候选里。</summary>
        static void SnapTo(float mx, float my)
        {
            float W, H; MonitorDims(out W, out H);
            var g = GeoOf(W, H);
            float m = 0.12f * g.ph;
            float y0 = g.stageT + 0.14f * g.ph, y1 = g.stageT + 0.69f * g.ph;
            float bestD = 1e9f; int be = _edge; float bp = _pos;
            // 上沿
            { float x = Mathf.Clamp(mx, g.boardL + m, g.boardR - m); float d = Mathf.Sqrt((mx - x) * (mx - x) + (my - g.stageT) * (my - g.stageT));
              if (d < bestD) { bestD = d; be = 0; bp = Mathf.InverseLerp(g.boardL + m, g.boardR - m, x); } }
            // 左沿
            { float y = Mathf.Clamp(my, y0, y1); float d = Mathf.Sqrt((mx - g.stageL) * (mx - g.stageL) + (my - y) * (my - y));
              if (d < bestD) { bestD = d; be = 1; bp = Mathf.InverseLerp(y0, y1, y); } }
            // 右沿
            { float y = Mathf.Clamp(my, y0, y1); float d = Mathf.Sqrt((mx - g.boardR) * (mx - g.boardR) + (my - y) * (my - y));
              if (d < bestD) { bestD = d; be = 2; bp = Mathf.InverseLerp(y0, y1, y); } }
            _edge = be; _pos = Mathf.Clamp01(bp);
        }

        // ---------------- 状态 ----------------
        bool _built, _shown, _glass, _diag, _everBuilt;
        VisualElement _shadow;
        float _k = 1f;            // 设计 px → 面板单位
        float _builtPx, _builtUpp;
        float _t;                 // 进入后的秒数
        float _exitT = -1f;       // >=0 退出动画中
        int _lastClickFrame = -1;
        bool _offline;
        XqCloseDay _day;
        string _say;
        float _glassAnim;
        int _drag;                // 1 = 正在按住她
        int _bdrag;               // 1 = 正在拖整个工作台
        Vector2 _bdragMouse, _bdragStage, _bdragPrev;
        float _dragTilt, _dragScale = 1f;
        bool _bdragMoved;
        VisualElement _header;
        Vector2 _dragFrom;
        bool _dragMoved;
        float _toastT = -1f;

        VisualElement _host, _stage, _rail, _panel, _glassLayer, _sweep, _offBar, _offRetry, _close, _tg, _knob;
        readonly VisualElement[] _row = new VisualElement[RowN];
        readonly float[] _rowHover = new float[RowN];
        readonly List<VisualElement> _nav = new List<VisualElement>();
        readonly List<float> _navHover = new List<float>();
        readonly List<VisualElement> _fills = new List<VisualElement>();
        readonly List<VisualElement> _knobs = new List<VisualElement>();
        readonly List<float> _fillPct = new List<float>();
        float _closeHover, _tgHover, _retryHover;
        VisualElement _timelineHost;
        Label _sayLabel, _metaLabel, _toast;
        readonly Label[] _sliderVal = new Label[3];
        XqCloseData _data;

        // ---------------- 显示 / 隐藏 ----------------
        void ShowSelf()
        {
            EnsureBuilt();
            if (_host == null) return;
            _glass = PlayerPrefs.GetInt("xq_bench_glass2", 1) == 1;      // W3：默认就是半透毛玻璃
            _glassAnim = _glass ? 1f : 0f;
            _shown = true; _exitT = -1f; _t = 0f; _drag = 0; _bdrag = 0; _toastT = -1f;
            ApplyStagePos();
            _host.style.display = DisplayStyle.Flex;
            _host.visible = true;
            _host.BringToFront();
            _diag = false; _lastTy = 999f;
            _data = XqCloseData.Ensure();
            _data.Changed -= OnData; _data.Changed += OnData;
            _data.Begin();
            LoadCacheIfEmpty();
            Refill();
            TickVisuals();
            PetHost.I?.SetUiBlocksMovement(true);
            Debug.Log("XQ_BENCH open k=" + _k.ToString("0.000") + " glass=" + _glass + " edge=" + _edge + " pose=" + _pose);
        }

        void HideSelf()
        {
            if (!_shown) return;
            _shown = false; _drag = 0; _bdrag = 0;
            if (_data != null) { _data.Changed -= OnData; _data.End(); }
            _exitT = 0f;
            PetHost.I?.SetUiBlocksMovement(false);
            Debug.Log("XQ_BENCH close");
        }

        // ---------------- 构建 ----------------
        void EnsureBuilt()
        {
            float W, H; MonitorDims(out W, out H);
            float px = PanelPx(W, H);
            var doc = XqClosePanel.PickDoc();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) { Debug.Log("XQ_BENCH no UIDocument"); return; }
            float upp = root.panel != null ? XqPanelXf.UnitsPerPx(root.panel) : 1f;
            if (_built && _host != null && _host.parent != null && Mathf.Abs(px - _builtPx) / _builtPx < 0.03f && Mathf.Abs(upp - _builtUpp) / _builtUpp < 0.03f) return;

            var uss = Resources.Load<StyleSheet>("XqClose");
            if (uss != null && !root.styleSheets.Contains(uss)) root.styleSheets.Add(uss);

            if (_host != null && _host.parent != null) _host.parent.Remove(_host);
            _built = false;
            _builtPx = px; _builtUpp = upp;
            _nav.Clear(); _navHover.Clear(); _fills.Clear(); _knobs.Clear(); _fillPct.Clear();
            _k = (px / PanH) * upp;

            _host = new VisualElement { name = "xq-bench-host", pickingMode = PickingMode.Ignore };
            _host.AddToClassList("xq-root");
            _host.style.position = Position.Absolute;
            _host.style.left = 0; _host.style.top = 0; _host.style.right = 0; _host.style.bottom = 0;
            _host.style.display = DisplayStyle.None;

            _stage = new VisualElement { name = "xq-bench-stage", pickingMode = PickingMode.Ignore };
            Abs(_stage, 0, 0, StageW * _k, PanH * _k);
            _stage.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            _host.Add(_stage);
            BuildRail();
            BuildPanel();
            root.Add(_host);
            ApplyStagePos();
            _built = true;
            bool rb = _everBuilt; _everBuilt = true;
            Debug.Log("XQ_BENCH built(rebuild=" + (rb ? "y" : "n") + ") k=" + _k.ToString("0.000") + " panelPx=" + px.ToString("0") + " monitor=" + W.ToString("0") + "x" + H.ToString("0"));
        }

        void ApplyStagePos()
        {
            if (_stage == null) return;
            float W, H; MonitorDims(out W, out H);
            var g = GeoOf(W, H);
            _stage.style.left = g.stageL * H * _builtUpp;
            _stage.style.top = g.stageT * H * _builtUpp;
        }

        void BuildRail()
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            _rail = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_rail, 0, 0, RailW * _k, PanH * _k);
            Round(_rail, 33f * _k);
            _rail.style.backgroundColor = railBg;
            Border(_rail, 1.5f, Rgb(255, 255, 255, 0.72f));
            _rail.style.alignItems = Align.Center;
            _rail.style.paddingTop = 16f * _k; _rail.style.paddingBottom = 16f * _k;
            var dot = Box(8f * _k, 8f * _k, 4f * _k, acc);
            dot.style.marginBottom = 12f * _k;
            _rail.Add(dot);
            string[] icons = { "home", "heart", "chat", "spark", "gear" };
            for (int n = 0; n < icons.Length; n++)
            {
                var b = Box(46f * _k, 46f * _k, 16f * _k, n == 0 ? Color.white : new Color(1, 1, 1, 0));
                if (n == 0) Border(b, 1f, new Color(acc.r, acc.g, acc.b, 0.35f));
                b.style.marginBottom = 6f * _k;
                b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
                var g = new XqGlyph(icons[n], n == 0 ? accDeep : new Color(Ink.r, Ink.g, Ink.b, n >= 3 ? 0.45f : 0.78f));
                g.style.width = 22f * _k; g.style.height = 22f * _k;
                b.Add(g);
                if (n == 4) b.style.marginTop = new StyleLength(StyleKeyword.Auto); // 设置键沉到底
                _nav.Add(b); _navHover.Add(0f); _rail.Add(b);
            }
            _stage.Add(_rail);
        }

        void BuildPanel()
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            _shadow = Box((PanW + 26f) * _k, (PanH + 26f) * _k, 44f * _k, new Color(accDeep.r * 0.25f, accDeep.g * 0.20f, accDeep.b * 0.40f, 0.14f));
            Abs(_shadow, (RailW + Gap - 13f) * _k, 6f * _k, (PanW + 26f) * _k, (PanH + 26f) * _k);
            _stage.Add(_shadow);

            _panel = new VisualElement { name = "xq-bench-panel", pickingMode = PickingMode.Ignore };
            Abs(_panel, (RailW + Gap) * _k, 0, PanW * _k, PanH * _k);
            Round(_panel, 38f * _k);
            _panel.style.backgroundColor = glassBg;
            _panel.style.overflow = Overflow.Hidden;
            _glassLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_glassLayer, 0, 0, PanW * _k, PanH * _k);
            Round(_glassLayer, 38f * _k);
            Border(_glassLayer, 1.8f, Rgb(255, 255, 255, 0.88f));
            _glassLayer.style.opacity = 0f;
            _panel.Add(_glassLayer);

            // 顶部高光柔带 + 二次元拖手指示条
            var topGlow = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(topGlow, 0, 0, PanW * _k, 98f * _k);
            topGlow.style.backgroundColor = new Color(1f, 1f, 1f, 0.24f);
            _panel.Add(topGlow);

            _header = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_header, 0, 0, PanW * _k, 98f * _k);
            _panel.Add(_header);
            var grab = Box(44f * _k, 4f * _k, 2f * _k, new Color(Ink.r, Ink.g, Ink.b, 0.18f));
            Abs(grab, (PanW - 44f) * 0.5f * _k, 11f * _k, 44f * _k, 4f * _k);
            _panel.Add(grab);
            var badge = new XqGlyph("spark", acc);
            Abs(badge, 40f * _k, 31f * _k, 18f * _k, 18f * _k);
            _panel.Add(badge);
            var title = L("XIAOQI WORKBENCH", 24f, Ink, true);
            Abs(title, 66f * _k, 27f * _k, -1, -1);
            _metaLabel = L("主人 · --:-- · 今日", 11.5f, Ink2, false);
            Abs(_metaLabel, 40f * _k, 62f * _k, -1, -1);
            _panel.Add(title); _panel.Add(_metaLabel);
            _close = Box(34f * _k, 34f * _k, 17f * _k, new Color(1f, 1f, 1f, 0.45f));
            Border(_close, 1f, Rgb(255, 255, 255, 0.7f));
            Abs(_close, (PanW - 40f - 34f) * _k, 26f * _k, 34f * _k, 34f * _k);
            _close.style.alignItems = Align.Center; _close.style.justifyContent = Justify.Center;
            _close.Add(L("×", 22f, Ink2, false));
            _panel.Add(_close);
            var gm = new XqGlyph("moon", accDeep); Abs(gm, (PanW - 40f - 34f - 40f) * _k, 32f * _k, 20f * _k, 20f * _k); _panel.Add(gm);

            float y0 = 98f, rh = 80f;
            for (int r = 0; r < RowN; r++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                Abs(row, 28f * _k, (y0 + r * rh) * _k, (PanW - 56f) * _k, (rh - 6f) * _k);
                Round(row, 18f * _k);
                row.style.paddingLeft = 14f * _k; row.style.paddingRight = 14f * _k;
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                if (r < RowN - 1) { row.style.borderBottomWidth = 1f; row.style.borderBottomColor = Line; }
                _row[r] = row; _panel.Add(row);
            }
            VisualElement c;
            Row(0, "今日日程", "按今天的安排走", out c);
            _timelineHost = c; _timelineHost.style.flexDirection = FlexDirection.Row;
            Row(1, "精力", "越高越有劲", out c); AddSlider(c, 0);
            Row(2, "困倦", "越高越想睡", out c); AddSlider(c, 1);
            Row(3, "社交", "她今天跟人说了多少话", out c); AddSlider(c, 2);
            Row(4, "最近说的", "她最近一句", out c);
            c.style.justifyContent = Justify.FlexEnd;
            _sayLabel = L("还没有", 12.5f, Ink, false);
            var bub = new VisualElement { pickingMode = PickingMode.Ignore };
            bub.style.backgroundColor = Rgb(243, 241, 247);
            bub.style.paddingLeft = bub.style.paddingRight = 14f * _k; bub.style.paddingTop = bub.style.paddingBottom = 9f * _k;
            Round(bub, 14f * _k); bub.style.borderBottomLeftRadius = 4f * _k; bub.style.maxWidth = 380f * _k;
            _sayLabel.style.whiteSpace = WhiteSpace.Normal;
            bub.Add(_sayLabel); c.Add(bub);
            Row(5, "薄雾玻璃", "关掉会实一点；默认半透，背后的她和桌面都透出来", out c);
            c.style.justifyContent = Justify.FlexEnd;
            _tg = Box(56f * _k, 30f * _k, 15f * _k, Dark);
            _knob = Box(24f * _k, 24f * _k, 12f * _k, Color.white);
            Abs(_knob, 29f * _k, 3f * _k, 24f * _k, 24f * _k);
            _tg.Add(_knob); c.Add(_tg);

            _sweep = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_sweep, -200f * _k, -120f * _k, 150f * _k, 900f * _k);
            _sweep.style.backgroundColor = new Color(1, 1, 1, 0.30f);
            _sweep.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(18f, AngleUnit.Degree));
            _panel.Add(_sweep);

            // 离线条
            _offBar = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_offBar, (PanW - 30f - 290f) * _k, (PanH - 18f - 36f) * _k, 290f * _k, 36f * _k);
            Round(_offBar, 18f * _k); _offBar.style.backgroundColor = Ink;
            _offBar.style.flexDirection = FlexDirection.Row; _offBar.style.alignItems = Align.Center;
            _offBar.style.paddingLeft = 14f * _k; _offBar.style.opacity = 0f;
            var ot = L("本地服务没开 · 显示的是上次的内容", 11.5f, Color.white, true);
            ot.style.flexGrow = 1f;
            _offRetry = Box(52f * _k, 26f * _k, 13f * _k, Color.white);
            _offRetry.style.alignItems = Align.Center; _offRetry.style.justifyContent = Justify.Center;
            _offRetry.style.marginRight = 5f * _k;
            _offRetry.Add(L("重试", 11.5f, Ink, true));
            _offBar.Add(ot); _offBar.Add(_offRetry);
            _panel.Add(_offBar);

            // 轻提示（点还没开的页签时）
            _toast = L("", 12f, Color.white, true);
            Abs(_toast, 40f * _k, (PanH - 54f) * _k, -1, -1);
            _toast.style.backgroundColor = Ink;
            _toast.style.paddingLeft = _toast.style.paddingRight = 14f * _k;
            _toast.style.paddingTop = _toast.style.paddingBottom = 8f * _k;
            Round(_toast, 16f * _k);
            _toast.style.opacity = 0f;
            _panel.Add(_toast);

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
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            ctl.style.justifyContent = Justify.FlexEnd;
            var tr = new VisualElement { pickingMode = PickingMode.Ignore };
            tr.style.width = 300f * _k; tr.style.height = 6f * _k; tr.style.backgroundColor = trackBg;
            Round(tr, 3f * _k);
            tr.style.marginRight = 16f * _k;
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(fill, 0, 0, 0, 6f * _k); fill.style.backgroundColor = acc; Round(fill, 3f * _k);
            var kn = Box(14f * _k, 14f * _k, 7f * _k, Color.white);
            Border(kn, 2.5f * _k, accDeep);
            Abs(kn, -7f * _k, -4f * _k, 14f * _k, 14f * _k);
            tr.Add(fill); tr.Add(kn);
            _fills.Add(fill); _knobs.Add(kn); _fillPct.Add(0f);
            _sliderVal[idx] = L("--", 12f, accDeep, true);
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
                    Border(d, 2f * _k, isNow ? Color.white : (past ? Ink : Ink3));
                    if (isNow) d.name = "now-dot";
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

        // ---------------- 动画（只管画面）----------------
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
            if (_shown && !_diag && _t > 0.7f)
            {
                _diag = true;
                try
                {
                    var wb = _stage != null ? _stage.worldBound : new Rect();
                    Debug.Log("XQ_BENCH diag host.disp=" + _host.resolvedStyle.display + " vis=" + _host.visible
                        + " parent=" + (_host.parent != null ? _host.parent.name : "null")
                        + " stage=" + wb.x.ToString("0") + "," + wb.y.ToString("0") + "," + wb.width.ToString("0") + "x" + wb.height.ToString("0")
                        + " op=" + (_stage != null ? _stage.resolvedStyle.opacity.ToString("0.00") : "-")
                        + " panelSize=" + (_host.panel != null ? _host.panel.visualTree.layout.width.ToString("0") + "x" + _host.panel.visualTree.layout.height.ToString("0") : "nopanel")
                        + " screen=" + Screen.width + "x" + Screen.height + " pxS=" + _pxS.ToString("0.00")
                        + " pet=" + (PetHost.I != null ? PetHost.I.ScreenRect().ToString() : "-")
                        + " builtPx=" + _builtPx.ToString("0") + " k=" + _k.ToString("0.000"));
                }
                catch (Exception e) { Debug.Log("XQ_BENCH diag err " + e.Message); }
            }
        }

        void TickVisuals()
        {
            float k = _k;
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
            // W3 AGY 改版：左栏 + 右板刚体倾转 + 拖板惯性倾斜 + 主题色毛玻璃胶囊行
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var cfg = XqModeConfig.I;
            float dt = Time.unscaledDeltaTime;
            _dragTilt = Mathf.Lerp(_dragTilt, 0f, 1f - Mathf.Exp(-10f * dt));
            _dragScale = Mathf.Lerp(_dragScale, (_bdrag == 1 && _bdragMoved) ? 1.015f : 1f, 1f - Mathf.Exp(-12f * dt));
            float dx = _pxS, dy = -_pyS;     // UI 的 y 向下
            float shift = Mathf.Clamp(cfg.bench_parallax_px, 0f, 40f);
            float rotD = Mathf.Clamp(cfg.bench_parallax_rot, 0f, 6f);
            float depthSc = 1f + 0.016f * Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            _stage.style.opacity = op;
            _stage.style.scale = new Scale(new Vector3(sc * depthSc * _dragScale, sc * depthSc * _dragScale, 1f));
            _stage.style.translate = new Translate(dx * shift * k, ty + dy * shift * 0.6f * k, 0f);
            _stage.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(dx * rotD + _dragTilt, AngleUnit.Degree));
            if (_shadow != null) _shadow.style.translate = new Translate(-dx * 4f * k, -dy * 2.5f * k, 0f);

            for (int n = 0; n < _nav.Count; n++)
            {
                float p = EaseOut((_t - 0.10f - n * 0.05f) / 0.45f);
                _nav[n].style.opacity = p;
                _nav[n].style.translate = new Translate(-10f * k * (1f - p) + _navHover[n] * 3f * k, 0f, 0f);
                if (n > 0) _nav[n].style.backgroundColor = new Color(1f, 1f, 1f, 0.34f * _navHover[n]);
            }
            for (int r = 0; r < RowN; r++)
            {
                float p = EaseOut((_t - 0.20f - r * 0.08f) / 0.60f);
                _row[r].style.opacity = p;
                _row[r].style.translate = new Translate((_rowHover[r] * 4f + _pxS * 1.6f) * k, ((1f - p) * 14f + (-_pyS) * 1.0f) * k, 0f);
                _row[r].style.backgroundColor = new Color(1f, 1f, 1f, 0.32f * _rowHover[r]);
            }
            for (int s = 0; s < _fills.Count; s++)
            {
                float p = EaseOut((_t - 0.45f - (s + 1) * 0.08f) / 1.0f);
                float w = 300f * k * _fillPct[s] * p;
                _fills[s].style.width = w;
                _knobs[s].style.left = w - 7f * k;
            }
            var nd = _timelineHost != null ? _timelineHost.Q("now-dot") : null;
            if (nd != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
                nd.style.scale = new Scale(new Vector3(1f + 0.25f * pulse, 1f + 0.25f * pulse, 1f));
            }
            float ph = (_t % 6f) / 6f;
            float sx = ph < 0.4f ? Mathf.Lerp(-200f, PanW + 80f, ph / 0.4f) : -400f;
            _sweep.style.left = sx * k;
            _sweep.style.opacity = _t > 1.2f ? 1f : 0f;
            float gt = _glass ? 1f : 0f;
            _glassAnim = Mathf.MoveTowards(_glassAnim, gt, dt * 4f);
            _panel.style.backgroundColor = new Color(glassBg.r, glassBg.g, glassBg.b, Mathf.Lerp(0.88f, 0.58f, _glassAnim));
            _glassLayer.style.opacity = _glassAnim;
            _tg.style.backgroundColor = Color.Lerp(Rgb(217, 214, 224), acc, _glassAnim);
            _knob.style.left = Mathf.Lerp(3f, 29f, _glassAnim) * k;
            _tg.style.scale = new Scale(Vector3.one * (1f + 0.06f * _tgHover));
            _close.style.backgroundColor = new Color(0f, 0f, 0f, 0.07f * _closeHover);
            float ot = Mathf.MoveTowards(_offBar.style.opacity.value, _offline ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            _offBar.style.opacity = ot;
            _offRetry.style.scale = new Scale(Vector3.one * (1f + 0.08f * _retryHover));
            if (_toastT >= 0f)
            {
                _toastT += Time.unscaledDeltaTime;
                float a = _toastT < 0.2f ? _toastT / 0.2f : (_toastT > 1.6f ? Mathf.Clamp01(1f - (_toastT - 1.6f) / 0.4f) : 1f);
                _toast.style.opacity = a;
                if (_toastT > 2.0f) { _toastT = -1f; _toast.style.opacity = 0f; }
            }
        }

        // ---------------- 指针（每帧）----------------
        bool OverEl(VisualElement el)
        {
            if (el == null || _host == null || _host.panel == null) return false;
            if (el.resolvedStyle.display == DisplayStyle.None) return false;
            var r = el.worldBound;
            if (float.IsNaN(r.x) || r.width < 1f || r.height < 1f) return false;
            return r.Contains(XqPanelXf.FromScreen(_host.panel, Input.mousePosition));
        }

        bool OverAnySelf() { return OverEl(_rail) || OverEl(_panel); }

        static bool OnHer()
        {
            if (PetHost.I == null) return false;
            var r = PetHost.I.ScreenRect();
            return r.width > 2f && r.height > 2f && r.Contains((Vector2)Input.mousePosition);
        }

        /// <summary>陀螺仪视差输入：有真陀螺仪（平板）就用它，否则用光标相对屏幕中心的位置。她随之轻转。</summary>
        void UpdateParallax(float dt)
        {
            var c = XqModeConfig.I;
            if (!c.bench_parallax) { _pxS = Mathf.MoveTowards(_pxS, 0f, dt * 3f); _pyS = Mathf.MoveTowards(_pyS, 0f, dt * 3f); }
            else
            {
                float nx, ny;
                if (!_gyroTried)
                {
                    _gyroTried = true;
                    try { if (SystemInfo.supportsGyroscope) { Input.gyro.enabled = true; _gyroOk = true; } } catch (Exception) { _gyroOk = false; }
                    Debug.Log("XQ_BENCH parallax src=" + (_gyroOk ? "gyro" : "mouse"));
                }
                if (_gyroOk)
                {
                    var gv = Input.gyro.gravity;                 // 竖直拿着时 x 左右、y 上下
                    nx = Mathf.Clamp(gv.x * 3f, -1f, 1f); ny = Mathf.Clamp((gv.y + 0.6f) * 2.5f, -1f, 1f);
                }
                else
                {
                    nx = Mathf.Clamp((Input.mousePosition.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
                    ny = Mathf.Clamp((Input.mousePosition.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f, -1f, 1f);
                }
                float a = 1f - Mathf.Exp(-5f * dt);
                _pxS = Mathf.Lerp(_pxS, nx, a); _pyS = Mathf.Lerp(_pyS, ny, a);
            }
            float roll, yaw; PetTilt(out roll, out yaw);
            if (Mathf.Abs(yaw - _lastTy) > 0.05f) { _lastTy = yaw; XqCloseCamera.SetTilt(roll, yaw); }
        }

        void Toast(string s) { _toast.text = s; _toastT = 0f; }

        void PollSelf()
        {
            if (_host == null || _host.panel == null || _exitT >= 0f) return;
            float dt = Time.unscaledDeltaTime;
            // 悬停反馈
            for (int r = 0; r < RowN; r++) _rowHover[r] = Mathf.MoveTowards(_rowHover[r], OverEl(_row[r]) ? 1f : 0f, dt * 6f);
            for (int n = 0; n < _nav.Count; n++) _navHover[n] = Mathf.MoveTowards(_navHover[n], OverEl(_nav[n]) ? 1f : 0f, dt * 8f);
            _closeHover = Mathf.MoveTowards(_closeHover, OverEl(_close) ? 1f : 0f, dt * 8f);
            _tgHover = Mathf.MoveTowards(_tgHover, (OverEl(_tg) || OverEl(_row[5])) ? 1f : 0f, dt * 8f);
            _retryHover = Mathf.MoveTowards(_retryHover, (_offline && OverEl(_offRetry)) ? 1f : 0f, dt * 8f);

            UpdateParallax(dt);
            if (XqMode.Busy) { _drag = 0; _bdrag = 0; return; }

            bool down = Input.GetMouseButtonDown(0), held = Input.GetMouseButton(0);
            if (down && _lastClickFrame != Time.frameCount)
            {
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
                    _glass = !_glass; PlayerPrefs.SetInt("xq_bench_glass2", _glass ? 1 : 0); PlayerPrefs.Save();
                    Debug.Log("XQ_BENCH glass=" + _glass);
                    return;
                }
                for (int n = 0; n < _nav.Count; n++)
                    if (OverEl(_nav[n]))
                    {
                        _lastClickFrame = Time.frameCount;
                        if (n > 0) { Debug.Log("XQ_BENCH nav " + n + " (W2)"); Toast("这一页下一期开放"); }
                        return;
                    }
                if (OverEl(_rail) || OverEl(_header))
                {
                    // 侧栏空白处 / 板顶部：按住拖整个工作台
                    _lastClickFrame = Time.frameCount; _bdrag = 1; _bdragMoved = false;
                    float Hn = Mathf.Max(1f, Screen.height);
                    _bdragMouse = new Vector2(Input.mousePosition.x / Hn, 1f - Input.mousePosition.y / Hn);
                    _bdragPrev = _bdragMouse;
                    float W0, H0; MonitorDims(out W0, out H0);
                    var g0 = GeoOf(W0, H0);
                    _bdragStage = new Vector2(g0.stageL, g0.stageT);
                    return;
                }
            }
            if (_bdrag == 1)
            {
                if (held)
                {
                    float Hn = Mathf.Max(1f, Screen.height);
                    var mn = new Vector2(Input.mousePosition.x / Hn, 1f - Input.mousePosition.y / Hn);
                    if (!_bdragMoved && (mn - _bdragMouse).magnitude * Hn > 4f) { _bdragMoved = true; Debug.Log("XQ_BENCH board drag start"); }
                    if (_bdragMoved)
                    {
                        float vx = (mn.x - _bdragPrev.x) / Mathf.Max(0.004f, dt);
                        _dragTilt = Mathf.Clamp(_dragTilt + Mathf.Clamp(vx * 0.9f, -3.5f, 3.5f) * 0.25f, -3.5f, 3.5f);
                        _bdragPrev = mn;
                        _sx = _bdragStage.x + (mn.x - _bdragMouse.x);
                        _sy = _bdragStage.y + (mn.y - _bdragMouse.y);
                        float W1, H1; MonitorDims(out W1, out H1);
                        var g1 = GeoOf(W1, H1);          // 取夹过的值写回，避免拖出屏后累积
                        _sx = g1.stageL; _sy = g1.stageT;
                        ApplyStagePos();
                        ApplyPet();                      // W3：她在板正后方，跟板一起动
                    }
                }
                else
                {
                    if (_bdragMoved) Debug.Log("XQ_BENCH board drag end x=" + _sx.ToString("0.000") + " y=" + _sy.ToString("0.000"));
                    _bdrag = 0;
                }
                return;
            }
            if (_drag == 1)
            {
                var m = (Vector2)Input.mousePosition;
                if (held)
                {
                    if (!_dragMoved && (m - _dragFrom).magnitude > 8f) { _dragMoved = true; Debug.Log("XQ_BENCH drag start"); }
                    if (_dragMoved)
                    {
                        float H = Mathf.Max(1f, Screen.height);
                        SnapTo(m.x / H, 1f - m.y / H);
                        ApplyPet();
                    }
                }
                else
                {
                    _drag = 0;
                    if (_dragMoved) Debug.Log("XQ_BENCH drag end edge=" + _edge + " pos=" + _pos.ToString("0.00"));
                    else
                    {
                        _pose = 1 - _pose;
                        Debug.Log("XQ_BENCH pose=" + _pose);
                        ApplyPet();
                    }
                }
            }
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
