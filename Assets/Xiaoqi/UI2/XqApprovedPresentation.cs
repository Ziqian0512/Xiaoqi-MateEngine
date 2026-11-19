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
        VisualElement _approvedDock, _approvedListen, _approvedQuiet;
        Label _approvedQuietLabel;
        Vector3 _approvedDown;
        VisualElement _approvedPressed;

        void ApplyApprovedClose()
        {
            // The full-screen host has no background and never clips the desktop or the model.
            XqMoonStyle.Clear(_host);
            foreach (var el in new[] { _plate, _cardTiny, _cardEnergy, _ringFocus, _exprBlush, _exprHeart, _exprSleep, _exprPose, _sysAudioPill, _actDance })
                if (el != null) el.style.display = DisplayStyle.None;
            foreach (var child in _host.Children()) if (child is Label) child.style.display = DisplayStyle.None;
            _spectrumRow.style.display = DisplayStyle.None;
            _voiceTrace = new XqVoiceTrace { name = "xq-voice-trace" };
            _voiceTrace.style.position = Position.Absolute; _host.Add(_voiceTrace);
            XqMoonStyle.Clear(_cardNext); XqMoonStyle.Clear(_bodyNext);
            _remindPill.style.backgroundImage = StyleKeyword.None;
            _remindPill.style.backgroundColor = new Color(.99f,.97f,.92f,.9f);
            SetAllBorder(_remindPill, 0f, Color.clear); SetRadius(_remindPill, 6f);
            _remindPill.style.marginLeft = 8f; _remindPill.style.marginTop = 12f;
            _remindPill.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(-1f, AngleUnit.Degree));
            XqMoonStyle.Clear(_nextPill);
            _nextPill.style.borderLeftWidth = 2f; _nextPill.style.borderLeftColor = new Color(.67f,.54f,.76f,1f);
            _nextPill.style.marginTop = 14f;
            _nextPill.Q("xq-old-schedule-progress")?.RemoveFromHierarchy();
            _cardMusic.Q("xq-soft-shadow")?.RemoveFromHierarchy(); _cardSay.Q("xq-soft-shadow")?.RemoveFromHierarchy();
            XqMoonStyle.Surface(_bodyMusic, 18f); XqMoonStyle.Surface(_bodySay, 13f);
            var fallback = new Label("♪") { pickingMode = PickingMode.Ignore };
            fallback.style.position = Position.Absolute; fallback.style.width = fallback.style.height = Length.Percent(100);
            fallback.style.unityTextAlign = TextAnchor.MiddleCenter; fallback.style.fontSize = 26f; fallback.style.color = XqMoonStyle.Soft;
            _musicCover.parent.style.backgroundColor = new Color(.69f,.63f,.77f,.12f); SetRadius(_musicCover.parent, 10f);
            _musicCover.parent.Insert(0, fallback);
            _musicCover.schedule.Execute(() => fallback.style.display = XqMusicBridge.Cover == null ? DisplayStyle.Flex : DisplayStyle.None).Every(500);
            _chatInput.textEdition.placeholder = "和小祈说句话……";
            _bodySay.style.unityTextAlign = TextAnchor.MiddleCenter;
            _sayText.style.unityFontStyleAndWeight = FontStyle.Normal;
            _musicLike.style.display = DisplayStyle.None;
            _approvedDock = new VisualElement { name = "xq-close-interactions", pickingMode = PickingMode.Position };
            _approvedDock.style.position = Position.Absolute; XqMoonStyle.Surface(_approvedDock, 24f);
            _approvedDock.style.paddingTop = _approvedDock.style.paddingBottom = 12f;
            _approvedDock.style.alignItems = Align.Stretch;
            _host.Add(_approvedDock);
            _actPat.RemoveFromHierarchy(); _actScreen.RemoveFromHierarchy();
            _approvedDock.Add(_actPat);
            _approvedListen = new VisualElement { pickingMode = PickingMode.Position };
            _approvedListen.Add(new Label("听我说") { pickingMode = PickingMode.Ignore });
            _approvedDock.Add(_approvedListen); _approvedDock.Add(_actScreen);
            _approvedQuiet = new VisualElement { pickingMode = PickingMode.Position };
            _approvedQuietLabel = new Label("安静陪伴") { pickingMode = PickingMode.Ignore }; _approvedQuiet.Add(_approvedQuietLabel);
            _approvedDock.Add(_approvedQuiet);
            foreach (var control in _approvedDock.Children())
            {
                XqMoonStyle.Clear(control); control.style.flexGrow = 0f;
                control.style.marginLeft = control.style.marginRight = 4f;
                control.style.marginTop = control.style.marginBottom = 5f;
                control.style.justifyContent = Justify.Center; control.style.alignItems = Align.Center;
                control.style.height = 40f; control.style.color = XqMoonStyle.Soft;
            }
            _actPatLbl.text = "♡ 摸摸头"; _actScreenLbl.text = "看我屏幕";
            var ears = new XqCatEars(); ears.style.position = Position.Absolute; ears.style.width = 48f; ears.style.height = 23f; ears.style.left = 20f; ears.style.top = -15f; _bodyMusic.Add(ears);
            XqMoonStyle.Sticker(_chatDock, "XqStickerRibbon", 30f, 12f, -17f);
            XqMoonStyle.Sticker(_approvedDock, "XqStickerCloud", 32f, 17f, -17f);
            XqMoonStyle.Surface(_chatDock, 25f); XqMoonStyle.Surface(_modeDock, 23f);
        }

        void PlaceApprovedClose(float width, float height)
        {
            float k = Mathf.Min(width / 1344f, height / 756f);
            SetApprovedRect(_cardNext, width * .047f, height * .14f, 320f * k, 235f * k);
            SetApprovedRect(_cardMusic, width * .048f, height * .575f, 290f * k, 145f * k);
            SetApprovedRect(_cardSay, width * .36f, height * .744f, width * .43f, 72f * k);
            SetApprovedRect(_chatDock, width * .535f - 304f * k, height - 93f * k, 608f * k, 51f * k);
            SetApprovedRect(_modeDock, width - 246f * k, 30f * k, 206f * k, 36f * k);
            SetApprovedRect(_approvedDock, width - 128f * k, height * .38f, 85f * k, 235f * k);
            SetApprovedRect(_voiceTrace, 45f * k, height - 35f * k, width - 90f * k, 18f * k);
            if (_clockBig != null) { _clockBig.style.fontSize = 44f * k; _clockBig.style.color = XqMoonStyle.Soft; _clockBig.style.unityFontStyleAndWeight = FontStyle.Normal; _clockBig.style.textShadow = new TextShadow { blurRadius = 4f, color = new Color(1f,1f,1f,.6f) }; }
            foreach (var label in _kickers) { label.style.fontSize = 11f * k; label.style.color = XqMoonStyle.Soft; label.style.unityFontStyleAndWeight = FontStyle.Normal; }
            foreach (var label in _marks) label.style.fontSize = 10f * k;
            foreach (var label in _texts) { label.style.fontSize = 12f * k; label.style.color = XqMoonStyle.Ink; label.style.unityFontStyleAndWeight = FontStyle.Normal; }
            _locPill.style.whiteSpace = WhiteSpace.Normal;
            SetRadius(_remindPill, 6f * k); SetRadius(_nextPill, 0f);
            _bodyNext.style.paddingLeft = _bodyNext.style.paddingRight = 0f;
            _bodyNext.style.paddingTop = 0f; _bodyNext.style.paddingBottom = 0f;
            _nextTime.style.fontSize = _nextWhat.style.fontSize = 11f * k;
            _nextPill.style.paddingLeft = 12f * k;
            foreach (var el in _approvedDock.Children()) { el.style.height = 38f * k; el.style.fontSize = 11f * k; }
            _chatInput.style.fontSize = 12f * k;
            _voiceTrace.MarkDirtyRepaint();
        }
        static void SetApprovedRect(VisualElement element, float x, float y, float width, float height)
        {
            if (element == null) return;
            element.style.position = Position.Absolute; element.style.left = x; element.style.top = y;
            element.style.width = width; element.style.height = height;
        }
    }

    public partial class XqBenchPanel
    {
        static readonly int[] ApprovedPages = { 0, 1, 4, 2, 5 };
        VisualElement _musicPageControls;
        Label _musicPageTitle, _musicPageArtist, _musicPageTime;
        Image _musicPageCover;

        void BuildApprovedRail()
        {
            _rail = Box(RailW * _k, PanH * _k, 25f * _k, new Color(.91f,.89f,.94f,.9f));
            Abs(_rail, 0, 0, RailW * _k, PanH * _k); Border(_rail, 1f, Color.white);
            _rail.style.alignItems = Align.Stretch; _rail.style.paddingTop = 23f * _k;
            _rail.style.paddingLeft = _rail.style.paddingRight = 10f * _k;
            var logo = L("祈.", 30f, XqMoonStyle.Soft, false); logo.style.marginLeft = 14f * _k; logo.style.marginBottom = 26f * _k; _rail.Add(logo);
            string[] labels = { "她的此刻", "生活手账", "歌单与播放器", "记忆与成长", "偏好设置" };
            string[] icons = { "spark", "cal", "sun", "heart", "gear" };
            for (int n = 0; n < labels.Length; n++)
            {
                var button = Box(-1, 43f * _k, 10f * _k, Color.clear);
                button.style.marginBottom = 10f * _k; button.style.flexDirection = FlexDirection.Row;
                button.style.alignItems = Align.Center; button.style.paddingLeft = 10f * _k;
                var icon = new XqGlyph(icons[n], XqMoonStyle.Soft); icon.style.width = icon.style.height = 15f * _k; button.Add(icon);
                var label = L(labels[n], 10f, XqMoonStyle.Soft, false); label.style.marginLeft = 8f * _k; button.Add(label);
                _nav.Add(button); _navHover.Add(0f); _navGlyph.Add(icon); _rail.Add(button);
            }
            _stage.Add(_rail);
            XqMoonStyle.Sticker(_rail, "XqStickerRibbon", 35f * _k, 95f * _k, -12f * _k);
        }

        void BuildApprovedPanel()
        {
            float x = (RailW + Gap) * _k;
            _panel = new VisualElement { name = "xq-bench-panel", pickingMode = PickingMode.Ignore };
            Abs(_panel, x, 0, PanW * _k, PanH * _k); XqMoonStyle.Surface(_panel, 24f * _k); _stage.Add(_panel);
            _header = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(_header, 0, 0, PanW * _k, 80f * _k); _panel.Add(_header);
            _topClock = L("小祈的生活", 16f, Ink, false); Abs(_topClock, 26f * _k, 22f * _k, -1, -1); _header.Add(_topClock);
            _charTopText = L("", 10f, Ink2, false); Abs(_charTopText, 26f * _k, 49f * _k, -1, -1); _header.Add(_charTopText);
            _metaLabel = L("", 9f, Ink2, false); Abs(_metaLabel, 350f * _k, 51f * _k, -1, -1); _header.Add(_metaLabel);
            var switcher = Box(220f * _k, 34f * _k, 17f * _k, new Color(1f,1f,1f,.65f));
            Abs(switcher, (PanW - 395f) * _k, 20f * _k, 220f * _k, 34f * _k);
            switcher.style.flexDirection = FlexDirection.Row; switcher.style.alignItems = Align.Center;
            _petModeBtn = ModePill("桌宠", false, Ink2, Color.clear); _closeModeBtn = ModePill("近景", false, Ink2, Color.clear);
            _benchModeTag = ModePill("工作台", true, Color.white, new Color(.54f,.46f,.64f));
            switcher.Add(_petModeBtn); switcher.Add(_closeModeBtn); switcher.Add(_benchModeTag); _header.Add(switcher);
            _modeBtn = Box(96f * _k, 34f * _k, 17f * _k, new Color(1f,1f,1f,.65f));
            Abs(_modeBtn, (PanW - 164f) * _k, 20f * _k, 96f * _k, 34f * _k); _modeBtn.style.justifyContent = Justify.Center;
            _modeBtnLabel = L(IsOfficeMode() ? "办公模式" : "居家模式", 10f, Ink, false); _modeBtn.Add(_modeBtnLabel); _header.Add(_modeBtn);
            _refreshBtn = Box(32f * _k, 32f * _k, 16f * _k, Color.white); Abs(_refreshBtn, (PanW - 56f) * _k, 21f * _k, 32f * _k, 32f * _k);
            _refreshBtn.style.alignItems = Align.Center; _refreshBtn.style.justifyContent = Justify.Center; _refreshBtn.Add(L("↻", 15f, Ink2, false)); _header.Add(_refreshBtn);
            _charBox = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(_charBox, 26f * _k, 87f * _k, 286f * _k, 482f * _k);
            var portrait = new Image { image = Resources.Load<Texture2D>("XqBenchHero"), scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            portrait.style.width = portrait.style.height = Length.Percent(100); Round(portrait, 12f * _k); _charBox.Add(portrait); _panel.Add(_charBox);
            _nameplate = new VisualElement { pickingMode = PickingMode.Ignore }; _nameplate.style.display = DisplayStyle.None;
            _charBotText = L("", 10f, Ink2, false);
            _leftHost = new VisualElement { name = "xq-bench-left", pickingMode = PickingMode.Ignore };
            _bentoHost = new VisualElement { name = "xq-bento-host", pickingMode = PickingMode.Ignore }; _panel.Add(_leftHost); _panel.Add(_bentoHost);
            _offBar = Box(300f * _k, 30f * _k, 12f * _k, new Color(.96f,.91f,.96f,.95f));
            Abs(_offBar, (PanW - 322f) * _k, (PanH - 39f) * _k, 300f * _k, 30f * _k); _offBar.style.flexDirection = FlexDirection.Row;
            _offBar.Add(L("接口暂不可用 · 显示缓存", 10f, Ink2, false));
            _offRetry = Box(42f * _k, 24f * _k, 10f * _k, Color.white); _offRetry.Add(L("重试", 10f, Ink, false)); _offBar.Add(_offRetry); _panel.Add(_offBar);
            _toast = L("", 11f, Color.white, false); Abs(_toast, 24f * _k, (PanH - 36f) * _k, -1, -1);
            _toast.style.backgroundColor = new Color(.3f,.24f,.38f,.95f); _toast.style.paddingLeft = _toast.style.paddingRight = 12f * _k;
            Round(_toast, 10f * _k); _toast.style.opacity = 0f; _panel.Add(_toast);
        }

        void ApprovedContentLayout()
        {
            bool home = _page == 0; _charBox.style.display = home ? DisplayStyle.Flex : DisplayStyle.None;
            _leftHost.style.display = home ? DisplayStyle.None : DisplayStyle.Flex;
            if (home) Abs(_bentoHost, 340f * _k, 90f * _k, (PanW - 368f) * _k, 475f * _k);
            else
            {
                Abs(_leftHost, 26f * _k, 90f * _k, 460f * _k, 480f * _k);
                Abs(_bentoHost, 516f * _k, 90f * _k, 475f * _k, 480f * _k);
            }
        }

        VisualElement Reading(VisualElement host, float height)
        {
            var el = Card(host, height, false); el.style.backgroundColor = Color.clear; Border(el, 0, Color.clear);
            el.style.paddingLeft = el.style.paddingRight = 0; return el;
        }

        void BuildApprovedHome(bool loading)
        {
            var state = _bd.Get("state"); var life = _bd.Get("life");
            var heading = L("她的今天", 24f, Ink, false); heading.style.marginBottom = 16f * _k; _bentoHost.Add(heading);
            string mood = Str(state, "mood"); if (mood.Length == 0) mood = Str(state, "affect.quadrant");
            string doing = Str(life, "place.doing"); if (doing.Length == 0) doing = Str(state, "activity");
            _bentoHost.Add(WrapTxt((mood.Length > 0 ? mood : "心情暂未同步") + "  ·  " + (doing.Length > 0 ? doing : "活动暂未同步"), 12f, Ink2, false, 100));
            var daily = Reading(_bentoHost, 207f); daily.Add(L("今天留下的片刻", 12f, Ink, false));
            var segments = Tk(life, "segments") as JArray;
            if (segments == null || segments.Count == 0) daily.Add(WrapTxt(loading ? "正在同步今天的日程…" : "今天还没有可展示的日程", 11f, Ink2, false, 100));
            else
            {
                int now = NowMin(), start = 0; for (int i = 0; i < segments.Count; i++) if (Num(segments[i], "a", 0) <= now) start = i;
                start = Mathf.Max(0, start - 2);
                for (int i = start; i < segments.Count && i < start + 3; i++)
                {
                    var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.marginTop = 15f * _k;
                    row.Add(L(Str(segments[i], "start") + "   " + Str(segments[i], "name"), 12f, Ink, false));
                    row.Add(L(Str(segments[i], "place"), 10f, Ink2, false)); daily.Add(row);
                }
            }
            _bentoHost.Add(L("她镜头里的生活", 12f, Ink, false));
            var photos = new VisualElement { pickingMode = PickingMode.Ignore }; photos.style.flexDirection = FlexDirection.Row; photos.style.marginTop = 15f * _k;
            string[] names = { "XqPhotoDorm", "XqPhotoStudy", "XqPhotoPeace" }; string[] labels = { "宿舍午后", "认真记笔记", "开心比耶" };
            for (int i = 0; i < names.Length; i++)
            {
                var resource = names[i]; var title = labels[i];
                var tile = MakePhotoTile(title, resource, "", Color.white, 198f * _k, 108f * _k);
                tile.style.marginRight = 9f * _k; Click(tile, () => ShowPhotoPreview(title, resource, "日常插画 · 静态展示素材", Color.white)); photos.Add(tile);
            }
            _bentoHost.Add(photos);
            var foot = L("日常照片为展示素材 · 点击原位放大", 9.5f, Ink2, false); foot.style.marginTop = 18f * _k; _bentoHost.Add(foot);
        }

        void BuildApprovedMemory(bool loading)
        {
            var memory = _bd.Get("memory"); var growth = _bd.Get("growth");
            var left = Reading(_leftHost, 465f); left.Add(L("她记住的事", 21f, Ink, false));
            var files = Tk(memory, "files") as JArray;
            if (files == null || files.Count == 0) left.Add(WrapTxt(loading ? "正在同步记忆…" : "还没有记忆记录", 12f, Ink2, false, 100));
            else
            {
                _pgMax = Mathf.Max(0, files.Count - 4); int begin = Mathf.Clamp(_pgOff, 0, _pgMax);
                for (int i = begin; i < files.Count && i < begin + 4; i++)
                {
                    var item = new VisualElement { pickingMode = PickingMode.Ignore }; item.style.marginTop = 20f * _k;
                    item.Add(L(CleanMemTag(Str(files[i], "file")), 12f, Ink, false));
                    item.Add(WrapTxt(CleanMemBody(Str(files[i], "head"), Str(files[i], "body")), 11f, Ink2, false, 140)); left.Add(item);
                }
            }
            var right = Reading(_bentoHost, 465f); right.Add(L("她最近的变化", 21f, Ink, false));
            right.Add(WrapTxt("一起回看，那些慢慢发生的变化。", 10f, Ink2, false, 100));
            var weekly = Str(growth, "weekly"); var current = Str(growth, "self_now");
            right.Add(L("这一周", 12f, Ink, false)); right.Add(WrapTxt(weekly.Length > 0 ? weekly : "暂无周报", 12f, Ink2, false, 260));
            var subtitle = L("此刻关注", 12f, Ink, false); subtitle.style.marginTop = 22f * _k; right.Add(subtitle);
            right.Add(WrapTxt(current.Length > 0 ? current : "暂无记录", 12f, Ink2, false, 180));
            var curiosity = Tk(growth, "curiosity") as JArray;
            if (curiosity != null) for (int i = 0; i < curiosity.Count && i < 3; i++) right.Add(WrapTxt("· " + Str(curiosity[i], "topic"), 11f, Ink2, false, 100));
        }

        void BuildApprovedMusic()
        {
            var now = XqMusicBridge.Current;
            var left = Card(_leftHost, 454f, false); XqMoonStyle.Surface(left, 18f * _k);
            _musicPageCover = new Image { image = XqMusicBridge.Cover, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            var sleeve = Box(-1, 215f * _k, 16f * _k, new Color(.72f,.65f,.80f,.12f));
            var note = L("♪", 62f, Ink2, false); note.style.alignSelf = Align.Center; note.style.marginTop = 60f * _k; sleeve.Add(note);
            _musicPageCover.style.position = Position.Absolute; _musicPageCover.style.width = _musicPageCover.style.height = Length.Percent(100); sleeve.Add(_musicPageCover); left.Add(sleeve);
            sleeve.schedule.Execute(() => note.style.display = XqMusicBridge.Cover == null ? DisplayStyle.Flex : DisplayStyle.None).Every(500);
            _musicPageTitle = L(now.title, 19f, Ink, false); left.Add(_musicPageTitle);
            _musicPageArtist = L(now.artist, 12f, Ink2, false); left.Add(_musicPageArtist);
            _musicPageTime = L("", 11f, Ink2, false); _musicPageTime.style.marginTop = 16f * _k; left.Add(_musicPageTime);
            _musicPageControls = new VisualElement { pickingMode = PickingMode.Ignore }; _musicPageControls.style.flexDirection = FlexDirection.Row;
            _musicPageControls.style.marginTop = 18f * _k;
            MusicAction(_musicPageControls, "上一首", () => XqMusicBridge.Prev(), () => XqMusicBridge.CanPrev);
            MusicAction(_musicPageControls, "播放 / 暂停", () => XqMusicBridge.TogglePlay(), () => XqMusicBridge.CanToggle);
            MusicAction(_musicPageControls, "下一首", () => XqMusicBridge.Next(), () => XqMusicBridge.CanNext); left.Add(_musicPageControls);
            var right = Reading(_bentoHost, 455f); right.Add(L("和小祈一起听", 23f, Ink, false));
            right.Add(WrapTxt("播放来自你的系统播放器，切歌和暂停会同步到播放器。", 12f, Ink2, false, 100));
            right.Add(L("歌单", 15f, Ink, false));
            right.Add(WrapTxt("在网易云选择喜欢的歌单，回到这里继续一起听。歌单内容由网易云管理。", 12f, Ink2, false, 160));
            var button = Box(-1, 38f * _k, 10f * _k, Color.white); button.style.marginTop = 18f * _k;
            button.style.justifyContent = Justify.Center; button.style.paddingLeft = 14f * _k;
            button.Add(L("选择歌单 / 打开网易云", 12f, Ink, false)); Click(button, () => Application.OpenURL("orpheus://")); right.Add(button);
            RefreshApprovedMusic();
        }
        void MusicAction(VisualElement parent, string label, Action action, Func<bool> enabled)
        {
            var button = Box(-1, 34f * _k, 12f * _k, Color.white); button.style.marginRight = 12f * _k;
            button.style.justifyContent = Justify.Center; button.style.alignItems = Align.Center;
            button.style.paddingLeft = button.style.paddingRight = 10f * _k; button.Add(L(label, 11f, Ink, false));
            Click(button, () => { if (enabled()) action(); }); parent.Add(button);
        }
        void RefreshApprovedMusic()
        {
            if (_page != 4 || _musicPageTitle == null) return;
            var current = XqMusicBridge.Current; _musicPageTitle.text = current.title; _musicPageArtist.text = current.artist;
            _musicPageCover.image = XqMusicBridge.Cover;
            _musicPageTime.text = current.durationSec > 0 ? XqMusicBridge.Clock(XqMusicBridge.PositionSec) + " / " + XqMusicBridge.Clock(current.durationSec) : "--:-- / --:-- · 播放器未提供时长";
            for (int i = 0; i < _musicPageControls.childCount; i++)
            {
                bool available = i == 0 ? XqMusicBridge.CanPrev : i == 1 ? XqMusicBridge.CanToggle : XqMusicBridge.CanNext;
                _musicPageControls[i].style.opacity = available ? 1f : .4f;
            }
        }
        internal static void ReviewPage(int page) { if (_i != null && _i._shown) _i.SelectPage(page); }
    }
}