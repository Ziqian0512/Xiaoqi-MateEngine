using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    internal static class XqMoonStyle
    {
        internal static readonly Color Ink = new Color(.25f,.20f,.33f,1f);
        internal static readonly Color Soft = new Color(.49f,.42f,.57f,1f);
        internal static void Surface(VisualElement el, float radius)
        {
            el.Q("xq-material-specular")?.RemoveFromHierarchy();
            el.style.backgroundColor = Color.clear;
            var texture = Resources.Load<Texture2D>("XqMoonSurface");
            if (texture != null) el.style.backgroundImage = Background.FromTexture2D(texture);
            else el.style.backgroundColor = new Color(.97f,.95f,.99f,.9f);
            el.style.borderTopLeftRadius = el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = radius;
            el.style.borderTopWidth = el.style.borderLeftWidth = el.style.borderRightWidth = el.style.borderBottomWidth = 1f;
            el.style.borderTopColor = el.style.borderLeftColor = new Color(1f,1f,1f,.95f);
            el.style.borderRightColor = el.style.borderBottomColor = new Color(.85f,.80f,.91f,.7f);
        }
        internal static void Clear(VisualElement el)
        {
            if (el == null) return;
            el.Q("xq-material-specular")?.RemoveFromHierarchy();
            el.style.backgroundColor = Color.clear; el.style.backgroundImage = StyleKeyword.None;
            el.style.borderTopWidth = el.style.borderLeftWidth = el.style.borderRightWidth = el.style.borderBottomWidth = 0f;
        }
        internal static void Sticker(VisualElement parent, string resource, float w, float x, float y)
        {
            var texture = Resources.Load<Texture2D>(resource); if (texture == null) return;
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute; image.style.width = w; image.style.height = w * .82f;
            image.style.left = x; image.style.top = y; parent.Add(image);
        }
    }

    internal sealed class XqCatEars : VisualElement
    {
        internal XqCatEars()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += ctx => {
                var p = ctx.painter2D; p.lineWidth = 1.4f; p.strokeColor = new Color(.62f,.51f,.71f,.85f);
                p.fillColor = new Color(.98f,.95f,1f,.92f); p.BeginPath();
                p.MoveTo(new Vector2(2,21)); p.LineTo(new Vector2(6,2)); p.LineTo(new Vector2(18,16));
                p.LineTo(new Vector2(30,16)); p.LineTo(new Vector2(42,2)); p.LineTo(new Vector2(46,21)); p.ClosePath(); p.Fill(); p.Stroke();
            };
        }
    }

    internal sealed class XqVoiceTrace : VisualElement
    {
        public XqVoiceTrace() { pickingMode = PickingMode.Ignore; generateVisualContent += Paint; }
        void Paint(MeshGenerationContext context)
        {
            var w = contentRect.width; var h = contentRect.height; if (w < 2 || h < 2) return;
            var painter = context.painter2D; painter.lineWidth = 1.15f;
            painter.strokeColor = new Color(.62f,.48f,.71f,.54f); painter.BeginPath();
            for (int i = 0; i < 96; i++)
            {
                float amplitude = Mathf.Clamp01(XqSysAudioBridge.Bars[Mathf.Min(i, XqSysAudioBridge.BarCount - 1)]);
                float y = h * .5f + (i % 2 == 0 ? -1f : 1f) * amplitude * h * .42f;
                var point = new Vector2(w * i / 95f, y);
                if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
            }
            painter.Stroke();
        }
    }

    public partial class XqClosePanel
    {
        XqVoiceTrace _voiceTrace;
        VisualElement _approvedDock, _approvedListen, _approvedQuiet, _tokenCapsule;
        Label _approvedQuietLabel, _tokPctLbl, _weatherPill;
        Image _chibiPeekImg, _tokStarImg;
        readonly System.Collections.Generic.List<Image> _badgeImgs = new System.Collections.Generic.List<Image>();
        Vector3 _approvedDown;
        VisualElement _approvedPressed;

        void ApplyApprovedClose() { BuildCompanionClose(); }

        void BuildCompanionWave()
        {
            if (_spectrumRow != null)
            {
                _spectrumRow.style.display = DisplayStyle.Flex;
                _spectrumRow.SendToBack();
                foreach (var b in _spectrumRow.Children()) b.style.opacity = 0f;
                var waveCanvas = new VisualElement { name = "xq-wave-contour", pickingMode = PickingMode.Ignore };
                waveCanvas.style.position = Position.Absolute;
                waveCanvas.style.left = waveCanvas.style.top = waveCanvas.style.right = waveCanvas.style.bottom = 0f;
                waveCanvas.generateVisualContent += mgc =>
                {
                    var r = waveCanvas.contentRect;
                    if (r.width < 20f || r.height < 12f) return;
                    var p = mgc.painter2D;
                    const int n = 48;
                    float tNow = Time.realtimeSinceStartup;
                    float baseY = r.height - 6f;
                    float maxH = r.height - 14f;
                    float[] hs = new float[n];
                    int childCount = _spectrumRow.childCount - 1;
                    for (int i = 0; i < n; i++)
                    {
                        float u = i / (float)(n - 1);
                        float env = Mathf.Sin(u * Mathf.PI);
                        float raw = 0f;
                        if (childCount > 0)
                        {
                            int ci = Mathf.Clamp(Mathf.RoundToInt(u * (childCount - 1)), 0, childCount - 1);
                            raw = Mathf.Clamp01(_spectrumRow[ci].resolvedStyle.height / 24f);
                        }
                        float wave = 0.28f + 0.34f * Mathf.Abs(Mathf.Sin(u * 9.5f + tNow * 2.8f))
                                   + 0.22f * Mathf.Abs(Mathf.Cos(u * 15f - tNow * 3.9f));
                        float amp = Mathf.Max(raw, wave * (0.55f + 0.45f * env));
                        hs[i] = Mathf.Clamp(amp * maxH * (0.35f + 0.65f * env), 4f, maxH);
                    }

                    // 1. 随波形动态起伏的浅白粉磨砂外轮廓包络
                    p.fillColor = new Color(1.00f, 0.96f, 0.99f, 0.86f);
                    p.strokeColor = new Color(0.92f, 0.84f, 0.98f, 0.70f);
                    p.lineWidth = 1.2f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(2f, baseY + 5f));
                    p.LineTo(new Vector2(2f, baseY - hs[0] - 5f));
                    for (int i = 0; i < n; i++)
                    {
                        float x = Mathf.Lerp(8f, r.width - 8f, i / (float)(n - 1));
                        p.LineTo(new Vector2(x, baseY - hs[i] - 6f));
                    }
                    p.LineTo(new Vector2(r.width - 2f, baseY - hs[n - 1] - 5f));
                    p.LineTo(new Vector2(r.width - 2f, baseY + 5f));
                    p.ClosePath();
                    p.Fill();
                    p.Stroke();

                    // 2. 粉色→柔紫竖向波浪条
                    float step = (r.width - 20f) / (n - 1);
                    float barW = Mathf.Max(2.2f, step * 0.46f);
                    for (int i = 0; i < n; i++)
                    {
                        float u = i / (float)(n - 1);
                        float x = 10f + i * step - barW * 0.5f;
                        float h = hs[i];
                        p.fillColor = Color.Lerp(new Color(0.96f, 0.56f, 0.84f, 0.95f), new Color(0.72f, 0.56f, 0.96f, 0.95f), u);
                        p.BeginPath();
                        p.MoveTo(new Vector2(x, baseY));
                        p.LineTo(new Vector2(x, baseY - h));
                        p.LineTo(new Vector2(x + barW, baseY - h));
                        p.LineTo(new Vector2(x + barW, baseY));
                        p.ClosePath();
                        p.Fill();
                    }
                };
                waveCanvas.schedule.Execute(() => waveCanvas.MarkDirtyRepaint()).Every(42);
                _spectrumRow.Add(waveCanvas);
            }

        }

        void PlaceApprovedClose(float width, float height) { PlaceCompanionClose(width, height); }
        static void SetApprovedRect(VisualElement element, float x, float y, float width, float height)
        {
            if (element == null) return;
            element.style.position = Position.Absolute; element.style.left = x; element.style.top = y;
            element.style.width = width; element.style.height = height;
        }
    }

    public partial class XqBenchPanel
    {
        static readonly int[] ApprovedPages = { 0, 1, 2, 5 };
        VisualElement _panoBg, _musicPageControls;
        Label _musicPageTitle, _musicPageArtist, _musicPageTime, _benchClockLbl, _benchMetaLine;
        Image _musicPageCover, _centerPortrait;

        void BuildApprovedRail()
        {
            // 纯白利落 2D 侧栏 + 紫墨描边（底部齿轮清晰可读）
            _rail = Box(64f * _k, PanH * _k, 32f * _k, new Color(1f, 1f, 1f, 0.96f));
            Abs(_rail, 6f * _k, 0, 64f * _k, PanH * _k); Border(_rail, 1.4f, new Color(0.76f, 0.70f, 0.92f, 0.95f));
            _rail.style.alignItems = Align.Center; _rail.style.paddingTop = 20f * _k; _rail.style.paddingBottom = 18f * _k;
            var logoRing = Box(38f * _k, 38f * _k, 19f * _k, new Color(0.95f, 0.92f, 1f, 1f));
            Border(logoRing, 1.2f, new Color(0.68f, 0.60f, 0.88f, 0.9f));
            logoRing.style.alignItems = Align.Center; logoRing.style.justifyContent = Justify.Center; logoRing.style.marginBottom = 22f * _k;
            logoRing.Add(L("祈", 18f, Ink, true));
            _rail.Add(logoRing);

            string[] labels = { "此刻", "手账", "记忆", "" };
            string[] icons = { "spark", "cal", "heart", "gear" };
            for (int n = 0; n < labels.Length; n++)
            {
                if (n == labels.Length - 1)
                {
                    var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
                    spacer.style.flexGrow = 1f;
                    _rail.Add(spacer);
                }
                var button = Box(46f * _k, 46f * _k, 14f * _k, Color.clear);
                button.style.marginBottom = 8f * _k;
                button.style.alignItems = Align.Center; button.style.justifyContent = Justify.Center;
                var icon = new XqGlyph(icons[n], Ink); icon.style.width = icon.style.height = 17f * _k; button.Add(icon);
                if (!string.IsNullOrEmpty(labels[n]))
                {
                    var label = L(labels[n], 8.5f, Ink2, false); label.style.marginTop = 2f * _k; button.Add(label);
                }
                _nav.Add(button); _navHover.Add(0f); _navGlyph.Add(icon); _rail.Add(button);
            }
            _stage.Add(_rail);
        }

        void BuildApprovedPanel()
        {
            float x = 78f * _k;
            _panel = new VisualElement { name = "xq-bench-panel", pickingMode = PickingMode.Ignore };
            Abs(_panel, x, 0, PanW * _k, PanH * _k);
            _panel.style.backgroundColor = new Color(0.99f, 0.98f, 1.00f, 1f);
            Round(_panel, 24f * _k);
            Border(_panel, 1.6f, new Color(0.76f, 0.70f, 0.92f, 0.95f));
            _stage.Add(_panel);

            // 2D 极浅奶油紫网格 + 几何线条 + 中央双圆环画布
            var gridBg = new VisualElement { name = "xq-bench-2d-canvas", pickingMode = PickingMode.Ignore };
            gridBg.style.position = Position.Absolute;
            gridBg.style.left = gridBg.style.top = gridBg.style.right = gridBg.style.bottom = 0f;
            gridBg.generateVisualContent += mgc =>
            {
                var r = gridBg.contentRect;
                if (r.width < 40f || r.height < 40f) return;
                var p = mgc.painter2D;
                // 极浅方格底纹
                p.strokeColor = new Color(0.84f, 0.79f, 0.95f, 0.32f);
                p.lineWidth = 1f;
                float step = 28f * _k;
                for (float gx = step; gx < r.width - 12f; gx += step) { p.BeginPath(); p.MoveTo(new Vector2(gx, 16f)); p.LineTo(new Vector2(gx, r.height - 16f)); p.Stroke(); }
                for (float gy = step; gy < r.height - 12f; gy += step) { p.BeginPath(); p.MoveTo(new Vector2(16f, gy)); p.LineTo(new Vector2(r.width - 16f, gy)); p.Stroke(); }

                // 中央柔紫双圆环框（人物破框背景）
                Vector2 c = new Vector2(r.width * 0.49f, r.height * 0.52f);
                float rad1 = 176f * _k;
                float rad2 = 188f * _k;
                p.fillColor = new Color(0.94f, 0.91f, 0.99f, 0.52f);
                p.BeginPath();
                for (int i = 0; i <= 64; i++)
                {
                    float a = (i / 64f) * Mathf.PI * 2f;
                    Vector2 pt = c + new Vector2(Mathf.Cos(a) * rad1, Mathf.Sin(a) * rad1);
                    if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.ClosePath(); p.Fill();

                p.strokeColor = new Color(0.76f, 0.70f, 0.92f, 0.85f); p.lineWidth = 2.2f * _k;
                p.BeginPath();
                for (int i = 0; i <= 64; i++)
                {
                    float a = (i / 64f) * Mathf.PI * 2f;
                    Vector2 pt = c + new Vector2(Mathf.Cos(a) * rad1, Mathf.Sin(a) * rad1);
                    if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.Stroke();

                p.strokeColor = new Color(0.84f, 0.80f, 0.96f, 0.65f); p.lineWidth = 1.2f * _k;
                p.BeginPath();
                for (int i = 0; i <= 64; i++)
                {
                    float a = (i / 64f) * Mathf.PI * 2f;
                    Vector2 pt = c + new Vector2(Mathf.Cos(a) * rad2, Mathf.Sin(a) * rad2);
                    if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.Stroke();

                // 几条杂志风斜向装饰细线
                p.strokeColor = new Color(0.76f, 0.70f, 0.92f, 0.45f); p.lineWidth = 1f;
                p.BeginPath(); p.MoveTo(new Vector2(c.x - 240f * _k, 0f)); p.LineTo(new Vector2(c.x - 110f * _k, c.y - 110f * _k)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(c.x + 250f * _k, 0f)); p.LineTo(new Vector2(c.x + 120f * _k, c.y - 110f * _k)); p.Stroke();
            };
            _panel.Add(gridBg);

            // 中央破框 2D 大立绘
            _centerPortrait = new Image { image = Resources.Load<Texture2D>("XqBenchHero"), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            Abs(_centerPortrait, (PanW * 0.49f - 172f) * _k, 68f * _k, 344f * _k, 492f * _k);
            Round(_centerPortrait, 160f * _k);
            _panel.Add(_centerPortrait);

            _header = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(_header, 0, 0, PanW * _k, 74f * _k); _panel.Add(_header);
            _topClock = L("", 16f, Ink, false); _topClock.style.display = DisplayStyle.None;
            _charTopText = L("", 10f, Ink2, false); _charTopText.style.display = DisplayStyle.None;
            _metaLabel = L("", 9.5f, Ink2, false); _metaLabel.style.display = DisplayStyle.None;

            var switcher = Box(220f * _k, 34f * _k, 17f * _k, Color.white);
            Abs(switcher, (PanW - 395f) * _k, 20f * _k, 220f * _k, 34f * _k);
            Border(switcher, 1.3f, new Color(0.74f, 0.68f, 0.90f, 0.95f));
            switcher.style.flexDirection = FlexDirection.Row; switcher.style.alignItems = Align.Center;
            _petModeBtn = ModePill("桌宠", false, Ink2, Color.clear); _closeModeBtn = ModePill("近景", false, Ink2, Color.clear);
            _benchModeTag = ModePill("工作台", true, Color.white, new Color(.48f, .38f, .86f));
            switcher.Add(_petModeBtn); switcher.Add(_closeModeBtn); switcher.Add(_benchModeTag); _header.Add(switcher);

            _modeBtn = Box(96f * _k, 34f * _k, 17f * _k, Color.white);
            Abs(_modeBtn, (PanW - 164f) * _k, 20f * _k, 96f * _k, 34f * _k);
            Border(_modeBtn, 1.3f, new Color(0.74f, 0.68f, 0.90f, 0.95f)); _modeBtn.style.justifyContent = Justify.Center;
            _modeBtnLabel = L(IsOfficeMode() ? "💼 办公" : "🏠 居家", 10.5f, Ink, true); _modeBtn.Add(_modeBtnLabel); _header.Add(_modeBtn);

            _refreshBtn = Box(32f * _k, 32f * _k, 16f * _k, Color.white);
            Abs(_refreshBtn, (PanW - 56f) * _k, 21f * _k, 32f * _k, 32f * _k);
            Border(_refreshBtn, 1.3f, new Color(0.74f, 0.68f, 0.90f, 0.95f));
            _refreshBtn.style.alignItems = Align.Center; _refreshBtn.style.justifyContent = Justify.Center;
            _refreshBtn.Add(L("↻", 15f, Ink, true)); _header.Add(_refreshBtn);

            _charBox = new VisualElement { pickingMode = PickingMode.Ignore }; _charBox.style.display = DisplayStyle.None;
            _nameplate = new VisualElement { pickingMode = PickingMode.Ignore }; _nameplate.style.display = DisplayStyle.None;
            _charBotText = L("", 10f, Ink2, false);
            _leftHost = new VisualElement { name = "xq-bench-left", pickingMode = PickingMode.Ignore };
            _bentoHost = new VisualElement { name = "xq-bento-host", pickingMode = PickingMode.Ignore };
            _panel.Add(_leftHost); _panel.Add(_bentoHost);

            _offBar = Box(300f * _k, 30f * _k, 12f * _k, new Color(.96f, .91f, .96f, .95f));
            Abs(_offBar, (PanW - 322f) * _k, (PanH - 39f) * _k, 300f * _k, 30f * _k); _offBar.style.flexDirection = FlexDirection.Row;
            _offBar.Add(L("接口暂不可用 · 显示缓存", 10f, Ink2, false));
            _offRetry = Box(42f * _k, 24f * _k, 10f * _k, Color.white); _offRetry.Add(L("重试", 10f, Ink, false)); _offBar.Add(_offRetry); _panel.Add(_offBar);
            _toast = L("", 11f, Color.white, false); _toast.style.display = DisplayStyle.None; _panel.Add(_toast);
        }

        void ApprovedContentLayout()
        {
            if (_charBox != null) _charBox.style.display = DisplayStyle.None;
            bool settings = _page == 5;
            if (_centerPortrait != null) _centerPortrait.style.display = settings ? DisplayStyle.None : DisplayStyle.Flex;
            _leftHost.style.display = DisplayStyle.Flex;
            if (settings)
            {
                Abs(_leftHost, 28f * _k, 84f * _k, 460f * _k, 486f * _k);
                Abs(_bentoHost, 514f * _k, 84f * _k, 476f * _k, 486f * _k);
            }
            else
            {
                Abs(_leftHost, 28f * _k, 76f * _k, 316f * _k, (PanH - 96f) * _k);
                Abs(_bentoHost, (PanW - 344f) * _k, 76f * _k, 316f * _k, (PanH - 96f) * _k);
            }
        }

        VisualElement Card2D(VisualElement host, float pad = 14f)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.backgroundColor = Color.white;
            Border(el, 1.4f, new Color(0.46f, 0.38f, 0.68f, 0.72f));
            el.style.borderBottomWidth = 3f * _k;
            el.style.borderBottomColor = new Color(0.76f, 0.70f, 0.92f, 0.95f);
            Round(el, 14f * _k);
            el.style.paddingLeft = el.style.paddingRight = pad * _k;
            el.style.paddingTop = el.style.paddingBottom = (pad - 2f) * _k;
            el.style.marginBottom = 14f * _k;
            host.Add(el);
            return el;
        }

        VisualElement RetroWin2D(VisualElement host, string title, Color barColor)
        {
            var win = new VisualElement { pickingMode = PickingMode.Ignore };
            win.style.backgroundColor = Color.white;
            Border(win, 1.4f, new Color(0.32f, 0.26f, 0.48f, 0.85f));
            win.style.borderBottomWidth = 3f * _k;
            win.style.borderBottomColor = new Color(0.76f, 0.70f, 0.92f, 0.95f);
            Round(win, 10f * _k);
            win.style.marginBottom = 14f * _k;

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.height = 24f * _k;
            bar.style.backgroundColor = barColor;
            bar.style.borderBottomWidth = 1.2f; bar.style.borderBottomColor = new Color(0.32f, 0.26f, 0.48f, 0.75f);
            bar.style.borderTopLeftRadius = 9f * _k; bar.style.borderTopRightRadius = 9f * _k;
            bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.paddingLeft = bar.style.paddingRight = 10f * _k;
            bar.Add(L(title, 9.5f, Ink, true));
            bar.Add(L("—  □  ×", 9.5f, Ink, true));
            win.Add(bar);

            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.style.paddingLeft = body.style.paddingRight = 14f * _k;
            body.style.paddingTop = body.style.paddingBottom = 12f * _k;
            win.Add(body);
            host.Add(win);
            return body;
        }

        VisualElement RingGauge2D(float size, int pct, Color ringCol, string topIcon, string bottomTxt)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.width = size; box.style.height = size;
            box.style.alignItems = Align.Center; box.style.justifyContent = Justify.Center;
            box.generateVisualContent += mgc =>
            {
                var r = box.contentRect;
                if (r.width < 12f) return;
                var p = mgc.painter2D;
                Vector2 c = new Vector2(r.width * 0.5f, r.height * 0.5f);
                float rad = Mathf.Min(r.width, r.height) * 0.43f;
                p.lineWidth = 7f * _k;
                p.strokeColor = new Color(0.88f, 0.85f, 0.96f, 0.85f);
                p.BeginPath();
                for (int i = 0; i <= 48; i++)
                {
                    float a = (i / 48f) * Mathf.PI * 2f;
                    Vector2 pt = c + new Vector2(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad);
                    if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.Stroke();

                int segs = Mathf.Max(2, Mathf.RoundToInt(48f * Mathf.Clamp01(pct / 100f)));
                p.strokeColor = ringCol;
                p.BeginPath();
                for (int i = 0; i <= segs; i++)
                {
                    float a = -Mathf.PI * 0.5f + (i / 48f) * Mathf.PI * 2f;
                    Vector2 pt = c + new Vector2(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad);
                    if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.Stroke();
            };
            if (!string.IsNullOrEmpty(topIcon)) box.Add(L(topIcon, 11f, Ink, true));
            box.Add(L(pct + "%", 16f, Ink, true));
            if (!string.IsNullOrEmpty(bottomTxt)) box.Add(L(bottomTxt, 9.5f, Ink2, false));
            return box;
        }

        void BuildApprovedHome(bool loading)
        {
            var state = _bd.Get("state"); var life = _bd.Get("life");
            // 左侧上：利落 2D 大时钟 + 图标天气地点
            _benchClockLbl = L(DateTime.Now.ToString("HH:mm"), 58f, new Color(0.18f, 0.16f, 0.28f, 1f), true);
            _benchClockLbl.style.marginTop = 10f * _k;
            _benchClockLbl.schedule.Execute(() => { if (_benchClockLbl != null) _benchClockLbl.text = DateTime.Now.ToString("HH:mm"); }).Every(1000);
            _leftHost.Add(_benchClockLbl);

            string place = Str(life, "place.place"); if (place.Length == 0) place = "薄扶林 · 宿舍";
            string wTemp = Str(life, "weather.temp");
            _benchMetaLine = L("☁  " + (string.IsNullOrEmpty(wTemp) ? "16°C" : wTemp + "°C") + " · " + place, 12f, Ink2, true);
            _benchMetaLine.style.marginTop = 4f * _k;
            _benchMetaLine.style.marginBottom = 38f * _k;
            _leftHost.Add(_benchMetaLine);

            // 左侧下：2 个图形化 2D 卡片（圆环元气进度 + 粉色心电波形卡）
            int bpm = Mathf.RoundToInt(Num(state, "me.bpm", 66f));
            int energy = Mathf.RoundToInt(Num(state, "needs.energy", 0.80f) * (Num(state, "needs.energy", 0.80f) <= 1f ? 100f : 1f));
            var statRow = new VisualElement { pickingMode = PickingMode.Ignore };
            statRow.style.flexDirection = FlexDirection.Row;

            var eCard = Card2D(statRow, 10f);
            eCard.style.width = 138f * _k; eCard.style.height = 156f * _k;
            eCard.style.marginRight = 12f * _k; eCard.style.alignItems = Align.Center; eCard.style.justifyContent = Justify.Center;
            eCard.Add(RingGauge2D(112f * _k, energy, new Color(0.56f, 0.48f, 0.94f, 1f), "⚡", "元气"));

            var hCard = Card2D(statRow, 10f);
            hCard.style.width = 138f * _k; hCard.style.height = 156f * _k;
            hCard.style.backgroundColor = new Color(1.00f, 0.92f, 0.95f, 1f);
            hCard.style.alignItems = Align.Center; hCard.style.justifyContent = Justify.SpaceBetween;
            var hTop = new VisualElement { pickingMode = PickingMode.Ignore };
            hTop.style.width = Length.Percent(100); hTop.style.alignItems = Align.FlexEnd;
            hTop.Add(L("♥", 13f, new Color(0.95f, 0.48f, 0.66f), true));
            hCard.Add(hTop);
            var ecg = new VisualElement { pickingMode = PickingMode.Ignore };
            ecg.style.width = 112f * _k; ecg.style.height = 56f * _k;
            ecg.generateVisualContent += mgc =>
            {
                var r = ecg.contentRect;
                var p = mgc.painter2D;
                p.strokeColor = new Color(0.94f, 0.46f, 0.66f, 1f);
                p.lineWidth = 2.4f * _k;
                float cy = r.height * 0.56f;
                Vector2[] pts = {
                    new Vector2(0f, cy), new Vector2(r.width * .24f, cy),
                    new Vector2(r.width * .32f, cy - 10f * _k), new Vector2(r.width * .40f, cy + 12f * _k),
                    new Vector2(r.width * .48f, cy - 26f * _k), new Vector2(r.width * .56f, cy + 20f * _k),
                    new Vector2(r.width * .64f, cy - 8f * _k), new Vector2(r.width * .72f, cy),
                    new Vector2(r.width, cy)
                };
                p.BeginPath();
                for (int i = 0; i < pts.Length; i++) if (i == 0) p.MoveTo(pts[i]); else p.LineTo(pts[i]);
                p.Stroke();
            };
            hCard.Add(ecg);
            hCard.Add(L(bpm.ToString(), 22f, Ink, true));
            _leftHost.Add(statRow);

            // 右侧上：粉紫复古小窗「小祈刚刚说」
            var quoteBody = RetroWin2D(_bentoHost, "小祈刚刚说", new Color(0.96f, 0.78f, 0.92f, 1f));
            string lastSay = Str(state, "last_reply");
            if (string.IsNullOrEmpty(lastSay)) lastSay = Str(state, "mood");
            if (string.IsNullOrEmpty(lastSay) || lastSay.Length < 4) lastSay = "今天阳光软绵绵的，想陪你一起发呆~";
            quoteBody.Add(WrapTxt(lastSay, 12f, Ink, true, 90));

            // 右侧中：1大2小 2D 拍立得照片拼贴（带胶带贴纸，纯视觉无文字挡图）
            var pGrid = new VisualElement { pickingMode = PickingMode.Ignore };
            pGrid.style.flexDirection = FlexDirection.Row; pGrid.style.marginBottom = 14f * _k;
            VisualElement Polaroid(string title, string res, float w, float h)
            {
                var frame = new VisualElement { pickingMode = PickingMode.Position };
                frame.style.width = w; frame.style.height = h;
                frame.style.backgroundColor = Color.white;
                Border(frame, 1.3f, new Color(0.46f, 0.38f, 0.68f, 0.75f));
                frame.style.paddingLeft = frame.style.paddingRight = frame.style.paddingTop = 6f * _k;
                frame.style.paddingBottom = 14f * _k;
                var img = new Image { image = Resources.Load<Texture2D>(res), scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
                img.style.width = img.style.height = Length.Percent(100);
                frame.Add(img);
                var tape = Box(28f * _k, 9f * _k, 2f * _k, new Color(0.94f, 0.88f, 0.72f, 0.88f));
                Abs(tape, (w - 28f * _k) * 0.5f, -5f * _k, 28f * _k, 9f * _k);
                frame.Add(tape);
                Click(frame, () => ShowPhotoPreview(title, res, "日常插画 · 点击查看", Color.white));
                return frame;
            }
            var bigPol = Polaroid("宿舍午后", "XqPhotoDorm", 156f * _k, 196f * _k);
            bigPol.style.marginRight = 12f * _k;
            bigPol.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(-1.5f, AngleUnit.Degree));
            var rCol = new VisualElement { pickingMode = PickingMode.Ignore };
            var pol1 = Polaroid("认真记笔记", "XqPhotoStudy", 122f * _k, 92f * _k);
            pol1.style.marginBottom = 12f * _k;
            pol1.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(1.5f, AngleUnit.Degree));
            var pol2 = Polaroid("开心比耶", "XqPhotoPeace", 122f * _k, 92f * _k);
            rCol.Add(pol1); rCol.Add(pol2);
            pGrid.Add(bigPol); pGrid.Add(rCol);
            _bentoHost.Add(pGrid);

            // 右侧下：2D 迷你音乐条（封面 + 波形线 + 深色矢量控制图标）
            var mCard = Card2D(_bentoHost, 10f);
            mCard.style.flexDirection = FlexDirection.Row; mCard.style.alignItems = Align.Center; mCard.style.marginBottom = 0f;
            var covImg = new Image { image = XqMusicBridge.Cover ?? Resources.Load<Texture2D>("XqMusicCover"), scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            covImg.style.width = covImg.style.height = 46f * _k; Round(covImg, 8f * _k); covImg.style.marginRight = 10f * _k; covImg.style.flexShrink = 0f;
            mCard.Add(covImg);
            var mRight = new VisualElement { pickingMode = PickingMode.Ignore };
            mRight.style.flexGrow = 1f; mRight.style.flexShrink = 1f;
            var nowM = XqMusicBridge.Current;
            var mTitle = L(string.IsNullOrEmpty(nowM.title) ? "星夜的微光" : nowM.title, 10.5f, Ink, true);
            mTitle.style.whiteSpace = WhiteSpace.NoWrap; mTitle.style.overflow = Overflow.Hidden; mTitle.style.textOverflow = TextOverflow.Ellipsis;
            mRight.Add(mTitle);
            var wave = new VisualElement { pickingMode = PickingMode.Ignore };
            wave.style.height = 14f * _k; wave.style.marginTop = 3f * _k; wave.style.marginBottom = 3f * _k;
            wave.generateVisualContent += mgc =>
            {
                var r = wave.contentRect;
                var p = mgc.painter2D;
                int bars = 26;
                float step = r.width / bars;
                for (int i = 0; i < bars; i++)
                {
                    float h = (0.25f + 0.75f * Mathf.Abs(Mathf.Sin(i * 0.7f))) * r.height;
                    p.fillColor = i < 14 ? Ink : new Color(0.75f, 0.70f, 0.86f, 0.75f);
                    float x = i * step;
                    float y = (r.height - h) * 0.5f;
                    p.BeginPath(); p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + 2f, y));
                    p.LineTo(new Vector2(x + 2f, y + h)); p.LineTo(new Vector2(x, y + h)); p.ClosePath(); p.Fill();
                }
            };
            mRight.Add(wave);
            var ctrls = new VisualElement { pickingMode = PickingMode.Ignore };
            ctrls.style.flexDirection = FlexDirection.Row; ctrls.style.alignItems = Align.Center; ctrls.style.justifyContent = Justify.SpaceBetween;
            var bPrev = Box(20f * _k, 18f * _k, 4f * _k, Color.clear);
            bPrev.generateVisualContent += mgc => { var p = mgc.painter2D; p.fillColor = Ink; p.BeginPath(); p.MoveTo(new Vector2(3, 3)); p.LineTo(new Vector2(5, 3)); p.LineTo(new Vector2(5, 13)); p.LineTo(new Vector2(3, 13)); p.ClosePath(); p.Fill(); p.BeginPath(); p.MoveTo(new Vector2(6, 8)); p.LineTo(new Vector2(14, 3)); p.LineTo(new Vector2(14, 13)); p.ClosePath(); p.Fill(); };
            Click(bPrev, () => XqMusicBridge.Prev());
            var bPlay = L(XqMusicBridge.IsPlaying ? "❚❚" : "▶", 11f, Ink, true); bPlay.pickingMode = PickingMode.Position; Click(bPlay, () => XqMusicBridge.TogglePlay());
            var bNext = Box(20f * _k, 18f * _k, 4f * _k, Color.clear);
            bNext.generateVisualContent += mgc => { var p = mgc.painter2D; p.fillColor = Ink; p.BeginPath(); p.MoveTo(new Vector2(3, 3)); p.LineTo(new Vector2(11, 8)); p.LineTo(new Vector2(3, 13)); p.ClosePath(); p.Fill(); p.BeginPath(); p.MoveTo(new Vector2(12, 3)); p.LineTo(new Vector2(14, 3)); p.LineTo(new Vector2(14, 13)); p.LineTo(new Vector2(12, 13)); p.ClosePath(); p.Fill(); };
            Click(bNext, () => XqMusicBridge.Next());
            var bLike = L("♡", 12f, new Color(0.92f, 0.46f, 0.66f), true);
            ctrls.Add(bPrev); ctrls.Add(bPlay); ctrls.Add(bNext); ctrls.Add(bLike);
            mRight.Add(ctrls);
            mCard.Add(mRight);
            mCard.schedule.Execute(() => {
                var c = XqMusicBridge.Current;
                mTitle.text = string.IsNullOrEmpty(c.title) ? "星夜的微光" : c.title;
                bPlay.text = XqMusicBridge.IsPlaying ? "❚❚" : "▶";
                if (XqMusicBridge.Cover != null) covImg.image = XqMusicBridge.Cover;
            }).Every(600);
        }

        void BuildApprovedLife(bool loading)
        {
            _pgMax = 0;
            var life = _bd.Get("life");
            // 左侧上：深紫 2D 小月历
            var cal = new VisualElement { pickingMode = PickingMode.Ignore };
            cal.style.backgroundColor = new Color(0.20f, 0.18f, 0.30f, 1f);
            Border(cal, 1.4f, new Color(0.46f, 0.38f, 0.68f, 0.85f));
            Round(cal, 16f * _k);
            cal.style.paddingLeft = cal.style.paddingRight = 16f * _k;
            cal.style.paddingTop = cal.style.paddingBottom = 14f * _k;
            cal.style.marginBottom = 16f * _k;
            var now = DateTime.Now;
            cal.Add(L(now.ToString("yyyy.MM"), 15f, Color.white, true));
            var wk = new VisualElement { pickingMode = PickingMode.Ignore };
            wk.style.flexDirection = FlexDirection.Row; wk.style.justifyContent = Justify.SpaceBetween;
            wk.style.marginTop = 10f * _k; wk.style.marginBottom = 6f * _k;
            foreach (var d in new[] { "日", "一", "二", "三", "四", "五", "六" })
            {
                var dl = L(d, 9.5f, new Color(0.84f, 0.80f, 0.96f, 0.85f), true);
                dl.style.width = 36f * _k; dl.style.unityTextAlign = TextAnchor.MiddleCenter; wk.Add(dl);
            }
            cal.Add(wk);
            int days = DateTime.DaysInMonth(now.Year, now.Month);
            int firstDow = (int)new DateTime(now.Year, now.Month, 1).DayOfWeek;
            VisualElement row = null;
            for (int cell = 0; cell < firstDow + days; cell++)
            {
                if (cell % 7 == 0)
                {
                    row = new VisualElement { pickingMode = PickingMode.Ignore };
                    row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.SpaceBetween;
                    row.style.marginBottom = 3f * _k; cal.Add(row);
                }
                int dayNum = cell - firstDow + 1;
                var cellEl = Box(36f * _k, 24f * _k, 12f * _k, dayNum == now.Day ? new Color(0.58f, 0.44f, 0.98f, 1f) : Color.clear);
                cellEl.style.alignItems = Align.Center; cellEl.style.justifyContent = Justify.Center;
                if (dayNum >= 1) cellEl.Add(L(dayNum.ToString(), 10.5f, Color.white, dayNum == now.Day));
                row.Add(cellEl);
            }
            while (row != null && row.childCount < 7) row.Add(Box(36f * _k, 24f * _k, 0f, Color.clear));
            _leftHost.Add(cal);

            // 左侧下：复古小窗「Diary」
            var dBody = RetroWin2D(_leftHost, "Diary · 今日随笔", new Color(0.84f, 0.90f, 1.00f, 1f));
            string diary = Str(life, "diary");
            if (string.IsNullOrEmpty(diary) || diary.Length < 4) diary = "晒着太阳慢慢收拾，记录今天平淡又可爱的小日常~";
            dBody.Add(L("🐱 今日心情：晴朗舒展", 11f, new Color(0.46f, 0.36f, 0.82f), true));
            var dTxt = WrapTxt(diary, 11.5f, Ink, false, 160);
            dTxt.style.marginTop = 8f * _k;
            dBody.Add(dTxt);

            // 右侧：严格锁死高度的 2D 竖向圆点时间轴（绝不穿出底边！）
            var tlCard = Card2D(_bentoHost, 16f);
            tlCard.style.height = (PanH - 104f) * _k;
            tlCard.style.marginBottom = 0f;
            tlCard.Add(L("今日时间轴", 14f, Ink, true));
            var svWrap = new VisualElement { pickingMode = PickingMode.Ignore };
            svWrap.style.flexGrow = 1f; svWrap.style.marginTop = 10f * _k;
            svWrap.style.overflow = Overflow.Hidden;
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.style.flexGrow = 1f; sv.style.maxHeight = (PanH - 172f) * _k;
            var segments = Tk(life, "segments") as JArray;
            if (segments == null || segments.Count == 0)
            {
                sv.Add(WrapTxt(loading ? "正在同步今日日程…" : "今天还没有日程记录", 11.5f, Ink2, false, 80));
            }
            else
            {
                int nowMin = NowMin();
                for (int i = 0; i < segments.Count; i++)
                {
                    int a = Mathf.RoundToInt(Num(segments[i], "a", 0));
                    int b = Mathf.RoundToInt(Num(segments[i], "b", a + 60));
                    bool active = nowMin >= a && nowMin < b;
                    string sName = Str(segments[i], "name");
                    string icon = sName.Contains("睡") ? "🌙" : (sName.Contains("洗") || sName.Contains("醒") ? "☀️" : (sName.Contains("饭") || sName.Contains("吃") ? "🍚" : (sName.Contains("课") ? "🏫" : "📚")));
                    var rItem = new VisualElement { pickingMode = PickingMode.Ignore };
                    rItem.style.flexDirection = FlexDirection.Row; rItem.style.alignItems = Align.Center;
                    rItem.style.marginBottom = 8f * _k;
                    var dot = Box(10f * _k, 10f * _k, 5f * _k, active ? new Color(0.56f, 0.42f, 0.96f, 1f) : new Color(0.84f, 0.80f, 0.94f, 1f));
                    dot.style.marginRight = 10f * _k; dot.style.flexShrink = 0f;
                    rItem.Add(dot);
                    var pill = Box(-1, 32f * _k, 16f * _k, active ? new Color(0.54f, 0.42f, 0.94f, 1f) : new Color(0.97f, 0.96f, 1f, 1f));
                    pill.style.flexGrow = 1f; pill.style.flexShrink = 1f;
                    pill.style.flexDirection = FlexDirection.Row; pill.style.alignItems = Align.Center;
                    pill.style.paddingLeft = pill.style.paddingRight = 12f * _k;
                    var lbl = L($"{icon}  {Str(segments[i], "start")}  {sName}", 10.5f, active ? Color.white : Ink, active);
                    lbl.style.whiteSpace = WhiteSpace.NoWrap; lbl.style.overflow = Overflow.Hidden; lbl.style.textOverflow = TextOverflow.Ellipsis;
                    pill.Add(lbl);
                    rItem.Add(pill);
                    sv.Add(rItem);
                }
            }
            svWrap.Add(sv); tlCard.Add(svWrap);
        }

        void BuildApprovedMemory(bool loading)
        {
            var memory = _bd.Get("memory"); var growth = _bd.Get("growth");
            var bond = Card2D(_leftHost, 18f);
            bond.style.height = (PanH - 104f) * _k;
            bond.style.justifyContent = Justify.SpaceBetween;
            // 顶部撕页日历天数徽章
            string days = Str(growth, "days");
            var dayTag = Box(96f * _k, 52f * _k, 10f * _k, new Color(0.95f, 0.92f, 1f, 1f));
            Border(dayTag, 1.3f, new Color(0.46f, 0.38f, 0.68f, 0.8f));
            dayTag.style.alignItems = Align.Center; dayTag.style.justifyContent = Justify.Center;
            dayTag.Add(L((string.IsNullOrEmpty(days) ? "42" : days) + " DAYS", 15f, Ink, true));
            bond.Add(dayTag);

            // 双圆环同心仪表（亲密 + 安全感）
            var st = _bd.Get("state");
            int closePct = Mathf.RoundToInt(Num(st, "closeness", 0.87f) * (Num(st, "closeness", 0.87f) <= 1f ? 100f : 1f));
            int secPct = Mathf.RoundToInt(Num(st, "security", 0.99f) * (Num(st, "security", 0.99f) <= 1f ? 100f : 1f));
            var rings = new VisualElement { pickingMode = PickingMode.Ignore };
            rings.style.flexDirection = FlexDirection.Row; rings.style.justifyContent = Justify.SpaceAround;
            rings.Add(RingGauge2D(118f * _k, closePct, new Color(0.96f, 0.56f, 0.74f, 1f), "♥", "亲密"));
            rings.Add(RingGauge2D(118f * _k, secPct, new Color(0.62f, 0.54f, 0.96f, 1f), "🛡", "安全感"));
            bond.Add(rings);

            var stagePill = Box(-1, 34f * _k, 17f * _k, new Color(0.94f, 0.91f, 1f, 1f));
            Border(stagePill, 1.2f, new Color(0.68f, 0.60f, 0.88f, 0.9f));
            stagePill.style.alignItems = Align.Center; stagePill.style.justifyContent = Justify.Center;
            stagePill.Add(L("✦  默契同频  ✦", 11.5f, Ink, true));
            bond.Add(stagePill);

            // 右侧：3 张图形化 2D 贴纸记忆卡（短句摘要，不堆长规则）
            var files = Tk(memory, "files") as JArray;
            if (files == null || files.Count == 0)
            {
                var empty = Card2D(_bentoHost, 16f);
                empty.Add(WrapTxt(loading ? "正在同步记忆…" : "还没有记忆记录", 11.5f, Ink2, false, 100));
            }
            else
            {
                Color[] pastel = {
                    new Color(0.90f, 0.97f, 0.96f, 1f),
                    new Color(0.93f, 0.92f, 1.00f, 1f),
                    new Color(1.00f, 0.92f, 0.94f, 1f),
                };
                string[] memIcons = { "🍵", "🌙", "🎵" };
                _pgMax = Mathf.Max(0, files.Count - 3); int begin = Mathf.Clamp(_pgOff, 0, _pgMax);
                for (int i = begin; i < files.Count && i < begin + 3; i++)
                {
                    int ci = i - begin;
                    var note = Card2D(_bentoHost, 12f);
                    note.style.flexDirection = FlexDirection.Row; note.style.alignItems = Align.Center;
                    note.style.marginBottom = 16f * _k;
                    note.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(ci == 1 ? 1.5f : -1.5f, AngleUnit.Degree));
                    var sq = Box(64f * _k, 64f * _k, 12f * _k, pastel[ci % 3]);
                    Border(sq, 1.2f, new Color(0.46f, 0.38f, 0.68f, 0.65f));
                    sq.style.alignItems = Align.Center; sq.style.justifyContent = Justify.Center;
                    sq.style.marginRight = 14f * _k; sq.style.flexShrink = 0f;
                    sq.Add(L(memIcons[ci % 3], 24f, Ink, true));
                    note.Add(sq);
                    var col = new VisualElement { pickingMode = PickingMode.Ignore };
                    col.style.flexGrow = 1f; col.style.flexShrink = 1f;
                    col.Add(L("📌 " + CleanMemTag(Str(files[i], "file")), 11f, new Color(.48f, .36f, .82f), true));
                    var summary = CleanMemBody(Str(files[i], "head"), Str(files[i], "body"));
                    if (summary.Length > 34) summary = summary.Substring(0, 34) + "…";
                    var body = WrapTxt(summary, 11.5f, Ink, false, 60);
                    body.style.marginTop = 4f * _k;
                    col.Add(body);
                    note.Add(col);
                }
            }
        }

        void RefreshApprovedMusic() { }
        internal static void ReviewPage(int page) { if (_i != null && _i._shown) _i.SelectPage(page); }
    }
}