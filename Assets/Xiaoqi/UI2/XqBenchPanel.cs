using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;
using Xiaoqi.Spike;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 梦幻粉紫工作台（V5 一体式画框舞台版）：
    /// - 最左 76×620 曜石黑紫导航坞：与右侧主画框严格 100% 等高，6 颗大图标均分撑满
    /// - 右侧 1020×620 一体式暮光薰衣草画框舞台：
    ///   - 顶栏常驻超大时钟 + 地点 + 显式 [🐾 桌宠 | ✨ 近景 | ● 工作台] 三态切换胶囊 + 居家/办公开关
    ///   - 中央 280×448 半身舞台框，底层 96% 实心曜石黑紫名牌横幅封口（彻底解决穿脚）
    ///   - 左翼 272px 与右翼 420px 呈现松弛有度的三层撞色手账卡片，彻底消除透底与拥挤感
    /// </summary>
    public partial class XqBenchPanel
    {
        static XqBenchPanel _i;
        public static bool IsShown { get { return _i != null && _i._shown; } }
        public static bool OverNow { get { return _i != null && _i._shown && _i.OverAnySelf(); } }

        public static void Warm()
        {
            if (_i == null) _i = new XqBenchPanel();
            try
            {
                _i.EnsureBuilt();
                XqBenchData.Ensure().PrefetchAll();
            }
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

        public static void Poll()
        {
            if (_i == null) return;
            try { if (_i._shown || _i._exitT >= 0f) _i.Tick(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH tick: " + e.Message); }
            if (!_i._shown) return;
            try { _i.PollSelf(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH poll: " + e.Message); }
        }

        // ---------------- V6 统一蓝紫粉少女毛玻璃色板 ----------------
        static Color Rgb(int r, int g, int b, float a = 1f) { return new Color(r / 255f, g / 255f, b / 255f, a); }
        static readonly Color Ink = Rgb(44, 28, 72);
        static readonly Color Ink2 = Rgb(92, 70, 134, 0.86f);
        static readonly Color Ink3 = Rgb(126, 102, 168, 0.65f);
        static readonly Color Line = Rgb(164, 128, 238, 0.22f);
        static readonly Color DarkCard = Rgb(255, 255, 255, 0.90f);
        static readonly Color DarkDock = Rgb(255, 255, 255, 0.84f);
        static readonly Color LilacCard = Rgb(255, 242, 252, 0.94f);

        static void ThemePalette(out Color acc, out Color accDeep, out Color glassBg, out Color railBg, out Color trackBg)
        {
            string th = XqUi2Sample.CurrentTheme;
            if (th == "blue")
            {
                acc = Rgb(114, 168, 255); accDeep = Rgb(92, 122, 242);
                glassBg = Rgb(228, 236, 255); railBg = DarkDock; trackBg = Rgb(114, 168, 255, 0.24f);
            }
            else if (th == "violet")
            {
                acc = Rgb(154, 133, 175); accDeep = Rgb(121, 101, 143);
                glassBg = Rgb(242, 239, 247); railBg = DarkDock; trackBg = Rgb(154, 133, 175, .24f);
            }
            else
            {
                acc = Rgb(242, 114, 198); accDeep = Rgb(146, 98, 246);
                glassBg = Rgb(240, 232, 255); railBg = DarkDock; trackBg = Rgb(242, 114, 198, 0.24f);
            }
        }

        // 设计稿尺寸：76(左等高坞) + 16 + 1020(一体舞台: 左翼296 + 中央立绘窗280 + 右翼444) = 1112 x 620
        const float Gap = 16f, LeftW = 296f, CharW = 280f, RightW = 444f;
        static float RailW => CampusPreview ? 66f : 128f;
        const float PanW = LeftW + CharW + RightW; // 1020
        const float PanH = 620f, TopH = 76f, BotH = 96f, MidH = PanH - TopH - BotH; // 448
        static float StageW => RailW + Gap + PanW; // 1112

        struct Geo { public float a, ph, k, stageL, stageT, stageW, charCenterX; }

        static void MonitorDims(out float w, out float h)
        {
            w = Screen.width; h = Screen.height;
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
            if (CampusPreview) return Mathf.Min(810f, H * .90f, W * .94f * PanH / StageW);
            float ph = 0.76f * H;
            float stageW = ph * StageW / PanH;
            if (stageW > 0.89f * W) ph = 0.89f * W * PanH / StageW;
            return Mathf.Clamp(ph, 460f, 1100f);
        }

        static Geo GeoOf(float W, float H)
        {
            var g = new Geo();
            g.a = W / H;
            g.ph = PanelPx(W, H) / H;
            g.k = g.ph / PanH;
            g.stageW = StageW * g.k;
            float l0 = _sx >= 0f ? _sx : (g.a - g.stageW) * 0.5f;
            float t0 = _sy >= 0f ? _sy : (1f - g.ph) * 0.5f;
            g.stageL = Mathf.Clamp(l0, 0.02f, Mathf.Max(0.02f, g.a - g.stageW - 0.02f));
            g.stageT = Mathf.Clamp(t0, 0.03f, Mathf.Max(0.03f, 1f - g.ph - 0.04f));
            g.charCenterX = g.stageL + (RailW + Gap + LeftW + CharW * 0.5f) * g.k;
            return g;
        }

        static float _sx = -1f, _sy = -1f;

        /// <summary>
        /// V6.1 画框探头聚焦：放大至上半身（胸部以上到猫耳）从粉紫相框中探出，彻底不露腿脚。
        /// </summary>
        public static void PetTarget(out float frac, out float vx, out float vy)
        {
            float W, H; MonitorDims(out W, out H);
            var g = GeoOf(W, H);
            frac = 2.15f * g.ph;
            float headTop = g.stageT + 0.06f * g.ph;
            float ex = g.charCenterX + _pxS * 0.008f * g.a;
            float ey = headTop + 0.08f * frac - _pyS * 0.006f;
            ex = Mathf.Clamp(ex, g.stageL + (RailW + Gap + LeftW + 36f) * g.k, g.stageL + (RailW + Gap + LeftW + CharW - 36f) * g.k);
            vx = ex / g.a;
            vy = 1f - ey;
        }

        public static void PetTilt(out float roll, out float yaw)
        {
            float deg = Mathf.Min(7f, Mathf.Clamp(XqModeConfig.I.bench_parallax_deg, 0f, 18f));
            yaw = -_pxS * deg;
            roll = 0f;
        }

        static float _pxS, _pyS, _lastTy = 999f, _lastFarX = 999f, _lastFarY = 999f;

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

        // ---------------- 状态 ----------------
        bool _built, _shown, _diag, _everBuilt;
        float _k = 1f, _builtPx, _builtUpp;
        float _t, _exitT = -1f;
        int _lastClickFrame = -1;
        bool _bdrag, _bdragMoved;
        Vector2 _bdragMouse, _bdragStage;
        float _toastT = -1f;

        VisualElement _host, _stage, _rail, _panel, _charBox, _nameplate, _header;
        VisualElement _leftHost, _bentoHost;
        VisualElement _petModeBtn, _closeModeBtn, _benchModeTag, _modeBtn, _refreshBtn, _offBar, _offRetry;
        Label _topClock, _charTopText, _charBotText, _metaLabel, _modeBtnLabel, _toast;

        readonly List<VisualElement> _nav = new List<VisualElement>();
        readonly List<float> _navHover = new List<float>();
        readonly List<XqGlyph> _navGlyph = new List<XqGlyph>();
        readonly List<KeyValuePair<VisualElement, Action>> _pageClicks = new List<KeyValuePair<VisualElement, Action>>();
        VisualElement _pressedEl;
        float _pressedAt;

        // ---------------- 显示 / 隐藏 ----------------
        void ShowSelf()
        {
            EnsureBuilt();
            if (_host == null) return;
            _shown = true; _exitT = -1f; _t = 0f; _bdrag = false; _toastT = -1f; _pressedEl = null;
            ApplyStagePos();
            _host.style.display = DisplayStyle.Flex;
            _host.visible = true;
            _host.BringToFront();
            _diag = false; _lastTy = 999f;
            XqBenchData.Ensure().PrefetchAll();
            PageShow();
            TickVisuals();
            PetHost.I?.SetUiBlocksMovement(true);
            Debug.Log("XQ_BENCH open v5 k=" + _k.ToString("0.000") + " page=" + _page);
        }

        void HideSelf()
        {
            if (!_shown) return;
            _shown = false; _bdrag = false; _pressedEl = null;
            XqCloseCamera.SetBenchViewportBottom(0f);
            PageHide();
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
            _built = false; _builtPx = px; _builtUpp = upp;
            _nav.Clear(); _navHover.Clear(); _navGlyph.Clear(); _pageClicks.Clear();
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

            if (CampusPreview) { BuildCampusRail(); BuildCampusPanel(); }
            else { BuildApprovedRail(); BuildApprovedPanel(); }

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
            if (_shown)
            {
                float frameBotScreenY = g.stageT + (PanH - BotH) * g.ph / PanH;
                XqCloseCamera.SetBenchViewportBottom(1f - frameBotScreenY);
            }
        }

        /// <summary>最左曜石黑紫导航坞：top=0, height=620*_k 与主画框 100% 严格等高。</summary>
        void BuildRail()
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            _rail = new VisualElement { name = "xq-bench-rail", pickingMode = PickingMode.Ignore };
            Abs(_rail, 0, 0, RailW * _k, PanH * _k);
            Round(_rail, 38f * _k);
            _rail.style.backgroundColor = railBg;
            Border(_rail, 2f * _k, Rgb(255, 255, 255, 0.96f));
            _rail.style.alignItems = Align.Center;
            _rail.style.justifyContent = Justify.SpaceBetween;
            _rail.style.paddingTop = 22f * _k;
            _rail.style.paddingBottom = 22f * _k;

            string[] icons = { "spark", "cal", "chat", "heart", "sun", "gear" };
            string[] tips = { "此刻", "手账", "相册", "成长", "歌单", "设置" };
            for (int n = 0; n < icons.Length; n++)
            {
                bool on = n == _page;
                var b = Box(54f * _k, 64f * _k, 18f * _k, on ? accDeep : Rgb(244, 236, 255, 0.68f));
                Border(b, 1.2f, on ? Color.white : Rgb(255, 255, 255, 0.75f));
                b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
                var g = new XqGlyph(icons[n], on ? Color.white : Ink2);
                g.style.width = 21f * _k; g.style.height = 21f * _k;
                b.Add(g);
                var lb = L(tips[n], 9.5f, on ? Color.white : Ink2, on);
                lb.style.marginTop = 3f * _k;
                b.Add(lb);
                _nav.Add(b); _navHover.Add(0f); _navGlyph.Add(g); _rail.Add(b);
            }
            _stage.Add(_rail);
        }

        void BuildPanel()
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            float panL = (RailW + Gap) * _k;
            var shadow = Box((PanW + 18f) * _k, (PanH + 18f) * _k, 42f * _k, Rgb(20, 8, 44, 0.28f));
            Abs(shadow, panL - 9f * _k, 10f * _k, (PanW + 18f) * _k, (PanH + 18f) * _k);
            _stage.Add(shadow);

            _panel = new VisualElement { name = "xq-bench-panel", pickingMode = PickingMode.Ignore };
            Abs(_panel, panL, 0, PanW * _k, PanH * _k);
            Round(_panel, 36f * _k);
            Border(_panel, 2.5f * _k, Rgb(255, 255, 255, 0.90f));

            // 整块面板统一铺实底柔光薰衣草底色（不再挖空，彻底消除中央圆角黑角）
            var bandBg = new Color(glassBg.r, glassBg.g, glassBg.b, 1f);
            _panel.style.backgroundColor = bandBg;

            var topBand = Box(PanW * _k, TopH * _k, 0f, bandBg);
            Abs(topBand, 0, 0, PanW * _k, TopH * _k);
            topBand.style.borderTopLeftRadius = 33f * _k; topBand.style.borderTopRightRadius = 33f * _k;
            _panel.Add(topBand);

            var botBand = Box(PanW * _k, BotH * _k, 0f, bandBg);
            Abs(botBand, 0, (PanH - BotH) * _k, PanW * _k, BotH * _k);
            botBand.style.borderBottomLeftRadius = 33f * _k; botBand.style.borderBottomRightRadius = 33f * _k;
            _panel.Add(botBand);

            var leftBg = Box(LeftW * _k, MidH * _k, 0f, bandBg);
            Abs(leftBg, 0, TopH * _k, LeftW * _k, MidH * _k);
            _panel.Add(leftBg);

            var rightBg = Box(RightW * _k, MidH * _k, 0f, bandBg);
            Abs(rightBg, (LeftW + CharW) * _k, TopH * _k, RightW * _k, MidH * _k);
            _panel.Add(rightBg);

            // 中央「LIVE 探头画框」：2D 专属半身立绘（不透桌面不露腿）+ 双层相框 + 左上 LIVE + 右上颜文字
            _charBox = new VisualElement { name = "xq-char-box", pickingMode = PickingMode.Ignore };
            Abs(_charBox, (LeftW + 8f) * _k, (TopH + 4f) * _k, (CharW - 16f) * _k, (MidH - 6f) * _k);
            Round(_charBox, 24f * _k);
            Border(_charBox, 3.5f * _k, Color.white);
            _charBox.style.backgroundColor = Rgb(238, 228, 255, 1f);

            var heroTex = Resources.Load<Texture2D>("XqBenchHero");
            if (heroTex != null)
            {
                var heroImg = new Image { image = heroTex, scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
                heroImg.style.position = Position.Absolute;
                heroImg.style.left = 0; heroImg.style.top = 0; heroImg.style.right = 0; heroImg.style.bottom = 0;
                Round(heroImg, 21f * _k);
                _charBox.Add(heroImg);
            }

            var liveBadge = Box(-1, 22f * _k, 11f * _k, Rgb(255, 255, 255, 0.92f));
            Border(liveBadge, 1.2f * _k, Rgb(244, 112, 188, 0.85f));
            Abs(liveBadge, 12f * _k, 12f * _k, -1, 22f * _k);
            liveBadge.style.paddingLeft = liveBadge.style.paddingRight = 9f * _k;
            liveBadge.style.justifyContent = Justify.Center;
            liveBadge.Add(L("● LIVE", 9.5f, Rgb(236, 78, 162), true));
            _charBox.Add(liveBadge);

            var kaoBadge = Box(-1, 22f * _k, 10f * _k, Rgb(255, 255, 255, 0.94f));
            Abs(kaoBadge, (CharW - 96f) * _k, 12f * _k, -1, 22f * _k);
            kaoBadge.style.paddingLeft = kaoBadge.style.paddingRight = 9f * _k;
            kaoBadge.style.justifyContent = Justify.Center;
            kaoBadge.Add(L("^v^ >w<", 9.5f, accDeep, true));
            _charBox.Add(kaoBadge);
            _panel.Add(_charBox);

            // 顶栏（含可拖动区域 + 超大时钟 + 显式 [桌宠 | 近景 | 工作台] 切换胶囊）
            _header = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_header, 0, 0, PanW * _k, TopH * _k);
            _panel.Add(_header);

            _topClock = L(DateTime.Now.ToString("HH:mm"), 38f, accDeep, true);
            Abs(_topClock, 28f * _k, 16f * _k, -1, -1);
            _header.Add(_topClock);

            var locPill = Box(-1, 26f * _k, 13f * _k, Rgb(255, 255, 255, 0.88f));
            Border(locPill, 1f, Color.white);
            Abs(locPill, 136f * _k, 26f * _k, -1, 26f * _k);
            locPill.style.paddingLeft = locPill.style.paddingRight = 12f * _k;
            locPill.style.justifyContent = Justify.Center;
            _charTopText = L(DateTime.Now.ToString("ddd MM/dd").ToUpperInvariant() + " · 薄扶林 · 加载中", 10.5f, Ink, true);
            locPill.Add(_charTopText);
            _header.Add(locPill);

            _metaLabel = L("此刻", 9.5f, Ink2, true);
            Abs(_metaLabel, 138f * _k, 54f * _k, -1, -1);
            _header.Add(_metaLabel);

            // 右上显式三态切换胶囊（统一柔和白紫毛玻璃）
            var switcher = Box(220f * _k, 36f * _k, 18f * _k, Rgb(255, 255, 255, 0.86f));
            Border(switcher, 1.3f, Color.white);
            Abs(switcher, (PanW - 396f) * _k, 20f * _k, 220f * _k, 36f * _k);
            switcher.style.flexDirection = FlexDirection.Row;
            switcher.style.alignItems = Align.Center;
            switcher.style.justifyContent = Justify.SpaceBetween;
            switcher.style.paddingLeft = switcher.style.paddingRight = 4f * _k;

            _petModeBtn = ModePill("🐾 桌宠", false, Ink2, Color.clear);
            _closeModeBtn = ModePill("✨ 近景", false, Ink2, Color.clear);
            _benchModeTag = ModePill("● 工作台", true, Color.white, accDeep);
            switcher.Add(_petModeBtn); switcher.Add(_closeModeBtn); switcher.Add(_benchModeTag);
            _header.Add(switcher);

            _modeBtn = Box(96f * _k, 34f * _k, 17f * _k, Rgb(255, 255, 255, 0.92f));
            Border(_modeBtn, 1.2f, Color.white);
            Abs(_modeBtn, (PanW - 164f) * _k, 21f * _k, 96f * _k, 34f * _k);
            _modeBtn.style.alignItems = Align.Center; _modeBtn.style.justifyContent = Justify.Center;
            _modeBtnLabel = L(IsOfficeMode() ? "💼 办公模式" : "🏠 居家模式", 10.8f, Ink, true);
            _modeBtn.Add(_modeBtnLabel);
            _header.Add(_modeBtn);

            _refreshBtn = Box(34f * _k, 34f * _k, 17f * _k, Rgb(255, 255, 255, 0.92f));
            Border(_refreshBtn, 1.2f, Color.white);
            Abs(_refreshBtn, (PanW - 58f) * _k, 21f * _k, 34f * _k, 34f * _k);
            _refreshBtn.style.alignItems = Align.Center; _refreshBtn.style.justifyContent = Justify.Center;
            _refreshBtn.Add(L("↻", 14f, Ink, true));
            _header.Add(_refreshBtn);

            // 左右双翼容器（包裹中央舞台，留出呼吸感）
            _leftHost = new VisualElement { name = "xq-bench-left", pickingMode = PickingMode.Ignore };
            Abs(_leftHost, 20f * _k, (TopH + 6f) * _k, (LeftW - 26f) * _k, (MidH + 40f) * _k);
            _panel.Add(_leftHost);

            _bentoHost = new VisualElement { name = "xq-bento-host", pickingMode = PickingMode.Ignore };
            Abs(_bentoHost, (LeftW + CharW + 8f) * _k, (TopH + 6f) * _k, (RightW - 28f) * _k, (MidH + 40f) * _k);
            _panel.Add(_bentoHost);

            // 底部动漫风艺术字贴图封口（XqArtLogo.png：AMAMIYA Inori ✦）
            _nameplate = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_nameplate, (LeftW + (CharW - 280f) * 0.5f) * _k, (PanH - 86f) * _k, 280f * _k, 68f * _k);
            _nameplate.style.alignItems = Align.Center;
            _nameplate.style.justifyContent = Justify.Center;
            var logoTex = Resources.Load<Texture2D>("XqArtLogo");
            if (logoTex != null)
            {
                var logoImg = new Image { image = logoTex, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                logoImg.style.width = Length.Percent(100f); logoImg.style.height = Length.Percent(100f);
                _nameplate.Add(logoImg);
            }
            else
            {
                _nameplate.Add(L("AMAMIYA Inori ✦", 18f, accDeep, true));
            }
            _charBotText = L("", 9f, Color.clear, false);
            _panel.Add(_nameplate);

            // 离线条 & 轻提示 Toast
            _offBar = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_offBar, (PanW - 304f) * _k, (PanH - 54f) * _k, 280f * _k, 32f * _k);
            Round(_offBar, 16f * _k); _offBar.style.backgroundColor = DarkCard;
            _offBar.style.flexDirection = FlexDirection.Row; _offBar.style.alignItems = Align.Center;
            _offBar.style.paddingLeft = 12f * _k; _offBar.style.opacity = 0f;
            var ot = L("部分接口连不上 · 正展示缓存", 10.5f, Color.white, true);
            ot.style.flexGrow = 1f;
            _offRetry = Box(46f * _k, 22f * _k, 11f * _k, Color.white);
            _offRetry.style.alignItems = Align.Center; _offRetry.style.justifyContent = Justify.Center;
            _offRetry.style.marginRight = 5f * _k;
            _offRetry.Add(L("重试", 10.5f, Ink, true));
            _offBar.Add(ot); _offBar.Add(_offRetry);
            _panel.Add(_offBar);

            _toast = L("", 11f, Color.white, true);
            Abs(_toast, 22f * _k, (PanH - 52f) * _k, -1, -1);
            _toast.style.backgroundColor = Rgb(20, 13, 34, 0.96f);
            _toast.style.paddingLeft = _toast.style.paddingRight = 14f * _k;
            _toast.style.paddingTop = _toast.style.paddingBottom = 6f * _k;
            Round(_toast, 14f * _k);
            _toast.style.opacity = 0f;
            _panel.Add(_toast);

            _stage.Add(_panel);
        }

        VisualElement ModePill(string text, bool active, Color fg, Color bg)
        {
            var b = Box(68f * _k, 26f * _k, 13f * _k, bg);
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.Add(L(text, 10.5f, fg, active));
            return b;
        }

        // ---------------- 动画与交互轮询 ----------------
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
            if (_shown) _t += dt;
            else if (_exitT >= 0f)
            {
                _exitT += dt;
                if (_exitT >= 0.28f) { _exitT = -1f; _host.style.display = DisplayStyle.None; return; }
            }
            else return;
            TickVisuals();

            if (_shown && !_diag && _t > 0.7f)
            {
                _diag = true;
                try
                {
                    var wb = _stage != null ? _stage.worldBound : new Rect();
                    Debug.Log("XQ_BENCH diag host.disp=" + _host.resolvedStyle.display + " stage=" + wb.x.ToString("0") + "," + wb.y.ToString("0") + "," + wb.width.ToString("0") + "x" + wb.height.ToString("0") + " screen=" + Screen.width + "x" + Screen.height);
                }
                catch (Exception) { }
            }
        }

        void TickVisuals()
        {
            PageTick(Time.unscaledDeltaTime);
            float k = _k;
            float op, sc, ty;
            if (_exitT >= 0f)
            {
                float e = EaseOut(_exitT / 0.28f);
                op = 1f - e; sc = 1f - 0.05f * e; ty = 16f * k * e;
            }
            else
            {
                float p = EaseBack(_t / 0.55f);
                op = Mathf.Clamp01(_t / 0.25f); sc = 0.93f + 0.07f * p; ty = (1f - p) * 20f * k;
            }
            _stage.style.opacity = op;
            _stage.style.scale = new Scale(new Vector3(sc, sc, 1f));
            _stage.style.translate = new Translate(-_pxS * 6f * k, ty - (-_pyS) * 4f * k, 0f);

            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            for (int n = 0; n < _nav.Count; n++)
            {
                float p = EaseOut((_t - 0.04f - n * 0.03f) / 0.35f);
                _nav[n].style.opacity = p;
                bool on = ApprovedPages[n] == _page;
                _nav[n].style.backgroundColor = on ? Rgb(255,255,255,.72f) : Rgb(255,255,255,.05f + .18f * _navHover[n]);
            }
            if (_bentoHost != null) _bentoHost.style.opacity = _pgFade;
            if (_leftHost != null) _leftHost.style.opacity = _pgFade;

            bool showOff = _bd != null && _bd.Offline(_page) && !_bd.Loading(_page);
            _offBar.style.opacity = Mathf.MoveTowards(_offBar.style.opacity.value, showOff ? 1f : 0f, Time.unscaledDeltaTime * 4f);

            if (_pressedEl != null && (!Input.GetMouseButton(0) || Time.unscaledTime - _pressedAt > 0.25f))
            {
                _pressedEl.style.scale = new Scale(Vector3.one);
                _pressedEl = null;
            }
            if (_toastT >= 0f)
            {
                _toastT += Time.unscaledDeltaTime;
                float a = _toastT < 0.15f ? _toastT / 0.15f : (_toastT > 2.0f ? Mathf.Clamp01(1f - (_toastT - 2.0f) / 0.4f) : 1f);
                _toast.style.opacity = a;
                if (_toastT > 2.4f) { _toastT = -1f; _toast.style.opacity = 0f; }
            }
        }

        bool OverEl(VisualElement el)
        {
            if (el == null || _host == null || _host.panel == null) return false;
            if (el.resolvedStyle.display == DisplayStyle.None) return false;
            var r = el.worldBound;
            if (float.IsNaN(r.x) || r.width < 1f || r.height < 1f) return false;
            return r.Contains(XqPanelXf.FromScreen(_host.panel, Input.mousePosition));
        }

        bool OverAnySelf() { return OverEl(_rail) || OverEl(_panel); }

        void UpdateParallax(float dt)
        {
            if (CampusPreview) { _pxS = _pyS = 0f; return; }
            var c = XqModeConfig.I;
            if (!c.bench_parallax) { _pxS = Mathf.MoveTowards(_pxS, 0f, dt * 3f); _pyS = Mathf.MoveTowards(_pyS, 0f, dt * 3f); }
            else
            {
                float nx = Mathf.Clamp((Input.mousePosition.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
                float ny = Mathf.Clamp((Input.mousePosition.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f, -1f, 1f);
                float a = 1f - Mathf.Exp(-5f * dt);
                _pxS = Mathf.Lerp(_pxS, nx, a); _pyS = Mathf.Lerp(_pyS, ny, a);
            }
            float roll, yaw; PetTilt(out roll, out yaw);
            if (Mathf.Abs(yaw - _lastTy) > 0.05f || Mathf.Abs(_pxS - _lastFarX) > 0.004f || Mathf.Abs(_pyS - _lastFarY) > 0.004f)
            {
                _lastTy = yaw; _lastFarX = _pxS; _lastFarY = _pyS;
                ApplyPet();
            }
        }

        void Toast(string s) { if (_toast == null) return; _toast.text = s; _toastT = 0f; }

        void PressVisual(VisualElement el)
        {
            if (el == null) return;
            _pressedEl = el;
            _pressedAt = Time.unscaledTime;
            el.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
        }

        Action _approvedArm;
        VisualElement _approvedArmEl;
        Vector2 _approvedArmAt;
        internal static bool DismissPhotoOverlay()
        {
            var modal = _i != null && _i._panel != null ? _i._panel.Q("xq-photo-modal") : null;
            if (modal == null) return false; modal.RemoveFromHierarchy(); return true;
        }
        Action ApprovedAction(out VisualElement target)
        {
            target = null;
            if (OverEl(_petModeBtn)) { target = _petModeBtn; return () => XqMode.Set(XqView.Pet); }
            if (OverEl(_closeModeBtn)) { target = _closeModeBtn; return () => XqMode.Set(XqView.Close); }
            if (OverEl(_refreshBtn)) { target = _refreshBtn; return () => { _bd?.Refresh(); Toast("正在同步…"); }; }
            if (OverEl(_modeBtn)) { target = _modeBtn; return ToggleSceneMode; }
            if (_bd != null && _bd.Offline(_page) && OverEl(_offRetry)) { target = _offRetry; return () => _bd.Refresh(); }
            for (int i = 0; i < _nav.Count; i++) if (OverEl(_nav[i])) { target = _nav[i]; if (CampusPreview && i == _nav.Count - 1) return CampusConsole; int page = ApprovedPages[i]; return () => SelectPage(page); }
            for (int i = _pageClicks.Count - 1; i >= 0; i--) if (OverEl(_pageClicks[i].Key)) { target = _pageClicks[i].Key; return _pageClicks[i].Value; }
            return null;
        }
        void PollSelf()
        {
            if (_host == null || _host.panel == null || _exitT >= 0f) return;
            var modal = _panel.Q("xq-photo-modal");
            if (modal != null)
            {
                _approvedArm = null; _bdrag = false;
                if (Input.GetMouseButtonDown(0) && OverEl(modal)) { _approvedArmEl = modal; _approvedArmAt = Input.mousePosition; }
                if (Input.GetMouseButtonUp(0) && _approvedArmEl == modal && OverEl(modal) && Vector2.Distance(_approvedArmAt, Input.mousePosition) <= 8f) DismissPhotoOverlay();
                return;
            }
            float dt = Time.unscaledDeltaTime;
            for (int n = 0; n < _nav.Count; n++) _navHover[n] = Mathf.MoveTowards(_navHover[n], OverEl(_nav[n]) ? 1f : 0f, dt * 8f);
            UpdateParallax(dt);
            if (XqMode.Busy) { _bdrag = false; _approvedArm = null; return; }
            PagePoll(false);
            bool down = Input.GetMouseButtonDown(0), held = Input.GetMouseButton(0), up = Input.GetMouseButtonUp(0);
            if (down)
            {
                _approvedArmAt = Input.mousePosition;
                _approvedArm = ApprovedAction(out _approvedArmEl);
                if (_approvedArm != null) PressVisual(_approvedArmEl);
                else if (OverEl(_rail) || OverEl(_header))
                {
                    _bdrag = true; _bdragMoved = false;
                    float h = Mathf.Max(1f, Screen.height);
                    _bdragMouse = new Vector2(Input.mousePosition.x / h, 1f - Input.mousePosition.y / h);
                    float w0, h0; MonitorDims(out w0, out h0); var geometry = GeoOf(w0, h0);
                    _bdragStage = new Vector2(geometry.stageL, geometry.stageT);
                }
            }
            if (_bdrag && held)
            {
                float h = Mathf.Max(1f, Screen.height);
                var pointer = new Vector2(Input.mousePosition.x / h, 1f - Input.mousePosition.y / h);
                if ((pointer - _bdragMouse).magnitude * h > 4f) _bdragMoved = true;
                if (_bdragMoved)
                {
                    _sx = _bdragStage.x + pointer.x - _bdragMouse.x; _sy = _bdragStage.y + pointer.y - _bdragMouse.y;
                    float w0, h0; MonitorDims(out w0, out h0); var geometry = GeoOf(w0, h0);
                    _sx = geometry.stageL; _sy = geometry.stageT; ApplyStagePos();
                }
            }
            if (up)
            {
                var action = _approvedArm; _approvedArm = null;
                bool sameTarget = _approvedArmEl != null && _approvedArmEl.panel != null && OverEl(_approvedArmEl);
                bool click = Vector2.Distance(_approvedArmAt, Input.mousePosition) <= 8f;
                _bdrag = false;
                if (action != null && sameTarget && click)
                { try { action(); } catch (Exception e) { Debug.LogWarning("XQ_BENCH click: " + e.Message); } }
            }
        }

        // ---------------- 辅助 ----------------
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
            if (w >= 0f) e.style.width = w;
            if (h >= 0f) e.style.height = h;
            e.style.flexShrink = 0f;
            Round(e, r); e.style.backgroundColor = bg;
            return e;
        }

        Label L(string text, float size, Color col, bool bold)
        {
            var l = new Label(text ?? "") { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size * _k; l.style.color = col;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0f;
            l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0f;
            return l;
        }
    }
}
