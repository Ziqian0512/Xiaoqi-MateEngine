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

        void ApplyApprovedClose()
        {
            XqMoonStyle.Clear(_host);
            // 隐藏旧版冗余卡，保留 _plate（AMAMIYA Inori 签名）与 _spectrumRow（放在签名与回复之间的粉色波浪 bar）
            foreach (var el in new[] { _cardTiny, _cardEnergy, _ringFocus, _exprBlush, _exprHeart, _exprSleep, _exprPose, _sysAudioPill, _actDance })
                if (el != null) el.style.display = DisplayStyle.None;
            foreach (var child in _host.Children()) if (child is Label) child.style.display = DisplayStyle.None;
            if (_plate != null) _plate.style.display = DisplayStyle.Flex;
            if (_spectrumRow != null)
            {
                _spectrumRow.style.display = DisplayStyle.Flex;
                _spectrumRow.style.bottom = Length.Percent(17.5f);
                _spectrumRow.style.height = Length.Percent(7.2f);
                _spectrumRow.SendToBack();
            }

            // 左上时钟区：放大蝴蝶结贴纸，日期地点与温度天气拆分成两个单行小胶囊并排
            XqMoonStyle.Clear(_cardNext); XqMoonStyle.Clear(_bodyNext);
            XqMoonStyle.Sticker(_cardNext, "XqStickerRibbon", 56f, -18f, -16f);
            _locPill.RemoveFromHierarchy();
            var infoRow = new VisualElement { pickingMode = PickingMode.Ignore };
            infoRow.style.flexDirection = FlexDirection.Row;
            infoRow.style.alignItems = Align.Center;
            infoRow.style.marginTop = 8f;
            _locPill.style.backgroundColor = new Color(0.24f, 0.18f, 0.42f, 0.92f);
            SetAllBorder(_locPill, 1f, new Color(1f, 1f, 1f, 0.45f));
            SetRadius(_locPill, 10f);
            _locPill.style.paddingLeft = _locPill.style.paddingRight = 10f;
            _locPill.style.paddingTop = _locPill.style.paddingBottom = 4f;
            _locPill.style.marginTop = _locPill.style.marginBottom = 0f;
            _locPill.style.marginRight = 6f;
            _weatherPill = new Label("☁ 同步中") { pickingMode = PickingMode.Ignore };
            _weatherPill.style.backgroundColor = new Color(0.97f, 0.94f, 1.00f, 0.92f);
            SetAllBorder(_weatherPill, 1f, Color.white);
            SetRadius(_weatherPill, 10f);
            _weatherPill.style.paddingLeft = _weatherPill.style.paddingRight = 9f;
            _weatherPill.style.paddingTop = _weatherPill.style.paddingBottom = 4f;
            _weatherPill.style.color = XqMoonStyle.Ink;
            _weatherPill.style.unityFontStyleAndWeight = FontStyle.Bold;
            _weatherPill.style.whiteSpace = WhiteSpace.NoWrap;
            infoRow.Add(_locPill); infoRow.Add(_weatherPill);
            _bodyNext.Add(infoRow);

            // V20 右上角：慢慢来 Q 版探头竖卡（瘦长粉紫渐变 + 中文从右到左竖排）
            _remindPill.RemoveFromHierarchy();
            _host.Add(_remindPill);
            _remindPill.Clear();
            _remindPill.style.position = Position.Absolute;
            _remindPill.style.backgroundImage = StyleKeyword.None;
            _remindPill.style.backgroundColor = Color.clear;
            SetAllBorder(_remindPill, 1.5f, Color.white);
            SetRadius(_remindPill, 14f);
            _remindPill.style.marginLeft = _remindPill.style.marginTop = 0f;
            _remindPill.style.paddingLeft = _remindPill.style.paddingRight = 6f;
            _remindPill.style.paddingBottom = 8f;
            _remindPill.style.paddingTop = 36f;
            _remindPill.style.justifyContent = Justify.Center;
            _remindPill.style.alignItems = Align.Center;
            _remindPill.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(0f, AngleUnit.Degree));
            _remindPill.generateVisualContent += mgc =>
            {
                var r = _remindPill.contentRect;
                if (r.width < 4f || r.height < 4f) return;
                var p = mgc.painter2D;
                const int bands = 24;
                for (int i = 0; i < bands; i++)
                {
                    float t = i / (float)(bands - 1);
                    p.fillColor = Color.Lerp(new Color(1.00f, 0.90f, 0.96f, 0.95f), new Color(0.90f, 0.87f, 1.00f, 0.95f), t);
                    float y0 = r.height * (i / (float)bands);
                    float y1 = r.height * ((i + 1) / (float)bands) + 0.5f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(0f, y0)); p.LineTo(new Vector2(r.width, y0));
                    p.LineTo(new Vector2(r.width, y1)); p.LineTo(new Vector2(0f, y1));
                    p.ClosePath(); p.Fill();
                }
            };
            var vRow = new VisualElement { pickingMode = PickingMode.Ignore };
            vRow.style.flexDirection = FlexDirection.Row;
            vRow.style.justifyContent = Justify.Center;
            vRow.style.alignItems = Align.FlexStart;
            var vLeft = new Label("我\n在\n这\n里\n哦") { pickingMode = PickingMode.Ignore };
            var vRight = new Label("慢\n慢\n来\n，") { pickingMode = PickingMode.Ignore };
            foreach (var vl in new[] { vLeft, vRight })
            {
                vl.style.color = XqMoonStyle.Ink;
                vl.style.unityFontStyleAndWeight = FontStyle.Bold;
                vl.style.unityTextAlign = TextAnchor.UpperCenter;
            }
            vRight.style.marginLeft = 5f;
            vRow.Add(vLeft); vRow.Add(vRight);
            _remindPill.Add(vRow);
            var chibiTex = Resources.Load<Texture2D>("XqChibiPeek");
            if (chibiTex != null)
            {
                _chibiPeekImg = new Image { image = chibiTex, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                _chibiPeekImg.style.position = Position.Absolute;
                _remindPill.Add(_chibiPeekImg);
            }

            // V20 右下角：正在做的事胶囊移到模式切换条正上方
            _nextPill.RemoveFromHierarchy();
            _host.Add(_nextPill);
            _nextPill.style.position = Position.Absolute;
            XqMoonStyle.Clear(_nextPill);
            _nextPill.style.backgroundColor = new Color(1f, 1f, 1f, 0.88f);
            SetAllBorder(_nextPill, 1f, new Color(1f, 1f, 1f, 0.92f));
            SetRadius(_nextPill, 12f);
            _nextPill.style.borderLeftWidth = 3f; _nextPill.style.borderLeftColor = new Color(.54f, .38f, .86f, 1f);
            _nextPill.style.marginTop = 0f;
            _nextPill.style.paddingTop = _nextPill.style.paddingBottom = 4f;
            _nextPill.style.paddingLeft = _nextPill.style.paddingRight = 10f;
            _nextPill.style.justifyContent = Justify.Center;
            _nextPill.Q("xq-old-schedule-progress")?.RemoveFromHierarchy();

            // 音乐卡重塑为 Spotify 紧凑布局（彻底清除外层 _cardMusic 底框，亮白控制键）
            _cardMusic.Q("xq-soft-shadow")?.RemoveFromHierarchy();
            XqMoonStyle.Clear(_cardMusic);
            XqMoonStyle.Clear(_bodyMusic);
            _bodyMusic.style.backgroundColor = new Color(0.24f, 0.18f, 0.38f, 0.92f);
            SetAllBorder(_bodyMusic, 1f, new Color(1f, 1f, 1f, 0.55f));
            SetRadius(_bodyMusic, 18f);
            _bodyMusic.Clear();

            var defaultCover = Resources.Load<Texture2D>("XqMusicCover");
            var bgCover = new VisualElement { pickingMode = PickingMode.Ignore };
            bgCover.style.position = Position.Absolute; bgCover.style.left = bgCover.style.top = bgCover.style.right = bgCover.style.bottom = 0f;
            if (defaultCover != null) bgCover.style.backgroundImage = Background.FromTexture2D(defaultCover);
            bgCover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            SetRadius(bgCover, 17f);
            _bodyMusic.Add(bgCover);
            var liveCover = new VisualElement { pickingMode = PickingMode.Ignore };
            liveCover.style.position = Position.Absolute; liveCover.style.left = liveCover.style.top = liveCover.style.right = liveCover.style.bottom = 0f;
            liveCover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            SetRadius(liveCover, 17f);
            _bodyMusic.Add(liveCover);
            liveCover.schedule.Execute(() => {
                var cov = XqMusicBridge.Cover;
                bool hasLive = cov != null;
                liveCover.style.display = hasLive ? DisplayStyle.Flex : DisplayStyle.None;
                bgCover.style.display = hasLive ? DisplayStyle.None : DisplayStyle.Flex;
                if (hasLive) liveCover.style.backgroundImage = Background.FromTexture2D(cov);
            }).Every(500);

            var scrim = new VisualElement { pickingMode = PickingMode.Ignore };
            scrim.style.position = Position.Absolute; scrim.style.left = scrim.style.top = scrim.style.right = scrim.style.bottom = 0f;
            scrim.style.backgroundColor = new Color(0.14f, 0.10f, 0.24f, 0.52f);
            SetRadius(scrim, 17f);
            _bodyMusic.Add(scrim);

            var mContent = new VisualElement { pickingMode = PickingMode.Ignore };
            mContent.style.flexGrow = 1f; mContent.style.justifyContent = Justify.SpaceBetween;
            mContent.style.paddingLeft = mContent.style.paddingRight = 14f;
            mContent.style.paddingTop = mContent.style.paddingBottom = 11f;

            var mTopRow = new VisualElement { pickingMode = PickingMode.Ignore };
            mTopRow.style.flexDirection = FlexDirection.Row; mTopRow.style.justifyContent = Justify.SpaceBetween; mTopRow.style.alignItems = Align.Center;
            var mIcon = new Label("♫") { pickingMode = PickingMode.Ignore };
            mIcon.style.color = new Color(1f, 1f, 1f, 0.95f); mIcon.style.fontSize = 14f;
            var mBadge = new VisualElement { pickingMode = PickingMode.Ignore };
            mBadge.style.backgroundColor = new Color(1f, 1f, 1f, 0.90f); SetRadius(mBadge, 10f);
            mBadge.style.paddingLeft = mBadge.style.paddingRight = 8f; mBadge.style.paddingTop = mBadge.style.paddingBottom = 2f;
            var mBadgeLbl = new Label("🎧 一起听") { pickingMode = PickingMode.Ignore };
            mBadgeLbl.style.color = XqMoonStyle.Ink; mBadgeLbl.style.fontSize = 9.5f; mBadgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            mBadge.Add(mBadgeLbl);
            mTopRow.Add(mIcon); mTopRow.Add(mBadge);
            mContent.Add(mTopRow);

            var mMidRow = new VisualElement { pickingMode = PickingMode.Ignore };
            mMidRow.style.flexDirection = FlexDirection.Row; mMidRow.style.justifyContent = Justify.SpaceBetween; mMidRow.style.alignItems = Align.Center;
            var mTxtCol = new VisualElement { pickingMode = PickingMode.Ignore };
            mTxtCol.style.flexGrow = 1f; mTxtCol.style.flexShrink = 1f; mTxtCol.style.overflow = Overflow.Hidden; mTxtCol.style.marginRight = 10f;
            _musicTitle.style.color = Color.white; _musicTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
            _musicTitle.style.whiteSpace = WhiteSpace.NoWrap; _musicTitle.style.overflow = Overflow.Hidden; _musicTitle.style.textOverflow = TextOverflow.Ellipsis;
            _musicArtist.style.color = new Color(0.92f, 0.89f, 0.98f, 0.90f); _musicArtist.style.unityTextAlign = TextAnchor.MiddleLeft;
            _musicArtist.style.whiteSpace = WhiteSpace.NoWrap; _musicArtist.style.overflow = Overflow.Hidden; _musicArtist.style.textOverflow = TextOverflow.Ellipsis;
            mTxtCol.Add(_musicTitle); mTxtCol.Add(_musicArtist);
            _musicPlay.style.width = 36f; _musicPlay.style.height = 36f; SetRadius(_musicPlay, 11f);
            _musicPlay.style.backgroundColor = new Color(1f, 1f, 1f, 0.94f); _musicPlay.style.flexShrink = 0f;
            _musicPlayLbl.style.color = XqMoonStyle.Ink;
            mMidRow.Add(mTxtCol); mMidRow.Add(_musicPlay);
            mContent.Add(mMidRow);

            var mBotRow = new VisualElement { pickingMode = PickingMode.Ignore };
            mBotRow.style.flexDirection = FlexDirection.Row; mBotRow.style.alignItems = Align.Center;
            _musicPrev.style.backgroundColor = Color.clear; _musicPrev.style.width = 22f; _musicPrev.style.height = 22f;
            var prevLbl = _musicPrev.Q<Label>(); if (prevLbl != null) { prevLbl.style.color = Color.white; prevLbl.style.unityFontStyleAndWeight = FontStyle.Bold; }
            var barTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            barTrack.style.flexGrow = 1f; barTrack.style.height = 3.5f; barTrack.style.marginLeft = 6f; barTrack.style.marginRight = 6f;
            barTrack.style.backgroundColor = new Color(1f, 1f, 1f, 0.34f); SetRadius(barTrack, 2f);
            _musicFill.style.backgroundColor = Color.white; barTrack.Add(_musicFill);
            _musicNext.style.backgroundColor = Color.clear; _musicNext.style.width = 22f; _musicNext.style.height = 22f;
            var nextLbl = _musicNext.Q<Label>(); if (nextLbl != null) { nextLbl.style.color = Color.white; nextLbl.style.unityFontStyleAndWeight = FontStyle.Bold; }
            _musicLike.style.display = DisplayStyle.Flex; _musicLike.style.backgroundColor = Color.clear; _musicLike.style.width = 22f; _musicLike.style.height = 22f;
            if (_musicLikeLbl != null) _musicLikeLbl.style.color = new Color(1f, 0.78f, 0.90f, 1f);
            mBotRow.Add(_musicPrev); mBotRow.Add(barTrack); mBotRow.Add(_musicNext); mBotRow.Add(_musicLike);
            mContent.Add(mBotRow);
            _bodyMusic.Add(mContent);

            // 回复：清零 _bodySay padding，单行深黑字右对齐在输入框正上方
            _cardSay.Q("xq-soft-shadow")?.RemoveFromHierarchy();
            XqMoonStyle.Clear(_cardSay); XqMoonStyle.Clear(_bodySay);
            if (_bodySay.childCount > 0) _bodySay[0].style.display = DisplayStyle.None;
            _sayText.style.unityTextAlign = TextAnchor.MiddleRight;
            _sayText.style.whiteSpace = WhiteSpace.NoWrap;
            _sayText.style.overflow = Overflow.Hidden;
            _sayText.style.textOverflow = TextOverflow.Ellipsis;
            _sayText.style.color = new Color(0.10f, 0.08f, 0.14f, 0.96f);
            _sayText.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 6f, color = new Color(1f, 1f, 1f, 0.95f) };

            // 输入框：细长通透柔紫微雾胶囊
            _chatInput.textEdition.placeholder = "和小祈说句话……";
            _chatInput.style.color = XqMoonStyle.Ink;
            _chatDock.style.backgroundImage = StyleKeyword.None;
            _chatDock.style.backgroundColor = new Color(0.97f, 0.95f, 1f, 0.76f);
            SetAllBorder(_chatDock, 1f, new Color(0.78f, 0.70f, 0.96f, 0.65f));
            SetRadius(_chatDock, 18f);
            if (_chatMicBtn != null) _chatMicBtn.style.backgroundColor = new Color(0.91f, 0.87f, 0.99f, 0.90f);
            if (_chatMicLbl != null) _chatMicLbl.style.color = XqMoonStyle.Ink;
            XqMoonStyle.Surface(_modeDock, 18f);

            // V20 4 个互动：直接以 4 张透明贴纸作为按钮本体（去矩形框），文字居中压在贴纸内，左右各 2 个围绕小祈
            _approvedDock = new VisualElement { name = "xq-close-interactions", pickingMode = PickingMode.Ignore };
            _approvedDock.style.position = Position.Absolute;
            _host.Add(_approvedDock);
            _actPat.RemoveFromHierarchy(); _actScreen.RemoveFromHierarchy();
            _approvedDock.Add(_actPat);
            _approvedListen = new VisualElement { pickingMode = PickingMode.Position };
            _approvedDock.Add(_approvedListen); _approvedDock.Add(_actScreen);
            _approvedQuiet = new VisualElement { pickingMode = PickingMode.Position };
            _approvedDock.Add(_approvedQuiet);

            string[] badgeTexNames = { "XqBadgeHeart", "XqBadgeCassette", "XqBadgeWindow", "XqBadgeMoon" };
            string[] badgeTexts = { "摸摸头", "听我说", "看我屏幕", "嘘" };
            float[] badgeDeg = { -4.0f, 3.0f, 2.0f, -3.0f };
            _badgeImgs.Clear();
            int bi = 0;
            foreach (var control in _approvedDock.Children())
            {
                XqMoonStyle.Clear(control);
                control.Clear();
                control.style.position = Position.Absolute;
                control.style.justifyContent = Justify.Center;
                control.style.alignItems = Align.Center;
                control.style.backgroundColor = Color.clear;
                SetAllBorder(control, 0f, Color.clear);
                SetRadius(control, 0f);
                control.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(badgeDeg[bi % 4], AngleUnit.Degree));
                control.style.paddingLeft = control.style.paddingRight = control.style.paddingTop = control.style.paddingBottom = 0f;

                var bTex = Resources.Load<Texture2D>(badgeTexNames[bi % 4]);
                if (bTex != null)
                {
                    control.style.backgroundImage = Background.FromTexture2D(bTex);
                    control.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                }

                Label lbl;
                if (bi == 0) { _actPatLbl.text = badgeTexts[0]; lbl = _actPatLbl; }
                else if (bi == 2) { _actScreenLbl.text = badgeTexts[2]; lbl = _actScreenLbl; }
                else if (bi == 3) { _approvedQuietLabel = new Label(badgeTexts[3]) { pickingMode = PickingMode.Ignore }; lbl = _approvedQuietLabel; }
                else { lbl = new Label(badgeTexts[bi]) { pickingMode = PickingMode.Ignore }; }
                lbl.pickingMode = PickingMode.Ignore;
                lbl.style.color = XqMoonStyle.Ink;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                lbl.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 4f, color = new Color(1f, 1f, 1f, 0.92f) };
                control.Add(lbl);
                bi++;
            }

            // V20 右上角竖向电量试管（顶部翅膀星星 + 粉紫渐变波浪液体 + 左侧刻度）
            _tokenCapsule = new VisualElement { name = "xq-token-capsule", pickingMode = PickingMode.Ignore };
            _tokenCapsule.style.position = Position.Absolute;
            _tokenCapsule.style.backgroundColor = new Color(1f, 1f, 1f, 0.94f);
            SetAllBorder(_tokenCapsule, 1.5f, new Color(0.84f, 0.78f, 0.96f, 0.95f));
            _tokenCapsule.style.paddingLeft = _tokenCapsule.style.paddingRight = 6f;
            _tokenCapsule.style.paddingTop = 10f; _tokenCapsule.style.paddingBottom = 6f;
            _tokenCapsule.style.alignItems = Align.Center;
            var starTex = Resources.Load<Texture2D>("XqStarTop");
            if (starTex != null)
            {
                _tokStarImg = new Image { image = starTex, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                _tokStarImg.style.position = Position.Absolute;
                _tokenCapsule.Add(_tokStarImg);
            }
            var tokHead = new Label("电量") { pickingMode = PickingMode.Ignore };
            tokHead.style.color = new Color(0.32f, 0.24f, 0.56f, 1f); tokHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            tokHead.style.marginBottom = 4f; _marks.Add(tokHead);
            var tokTube = new VisualElement { pickingMode = PickingMode.Ignore };
            tokTube.style.width = Length.Percent(100f); tokTube.style.flexGrow = 1f;
            tokTube.style.backgroundColor = new Color(0.97f, 0.95f, 1f, 0.85f);
            SetAllBorder(tokTube, 1f, new Color(0.76f, 0.68f, 0.94f, 0.85f));
            tokTube.generateVisualContent += mgc =>
            {
                var r = tokTube.contentRect;
                if (r.width < 8f || r.height < 16f) return;
                var p = mgc.painter2D;
                float fill = 0.80f;
                float phase = Time.realtimeSinceStartup * 2.6f;
                float surfY = r.height * (1f - fill);
                // 背面浅紫水波峰
                p.fillColor = new Color(0.76f, 0.62f, 0.98f, 0.55f);
                p.BeginPath();
                p.MoveTo(new Vector2(0f, r.height));
                for (int s = 0; s <= 12; s++)
                {
                    float u = s / 12f;
                    float wy = surfY - 2f + Mathf.Sin(u * 4.5f - phase) * 3.2f;
                    p.LineTo(new Vector2(r.width * u, wy));
                }
                p.LineTo(new Vector2(r.width, r.height));
                p.ClosePath(); p.Fill();

                // 主液体：粉桃 -> 柔紫垂直渐变带 + 前景正弦波浪液面
                const int bands = 20;
                for (int b = 0; b < bands; b++)
                {
                    float t0 = b / (float)bands;
                    float t1 = (b + 1) / (float)bands;
                    float y0 = Mathf.Lerp(surfY, r.height, t0);
                    float y1 = Mathf.Lerp(surfY, r.height, t1) + 0.6f;
                    p.fillColor = Color.Lerp(new Color(1.00f, 0.84f, 0.92f, 0.96f), new Color(0.70f, 0.54f, 0.98f, 0.98f), (t0 + t1) * 0.5f);
                    p.BeginPath();
                    if (b == 0)
                    {
                        for (int s = 0; s <= 12; s++)
                        {
                            float u = s / 12f;
                            float wy = surfY + Mathf.Sin(u * 4.8f + phase) * 3.2f;
                            if (s == 0) p.MoveTo(new Vector2(0f, wy)); else p.LineTo(new Vector2(r.width * u, wy));
                        }
                        p.LineTo(new Vector2(r.width, y1)); p.LineTo(new Vector2(0f, y1));
                    }
                    else
                    {
                        p.MoveTo(new Vector2(0f, y0)); p.LineTo(new Vector2(r.width, y0));
                        p.LineTo(new Vector2(r.width, y1)); p.LineTo(new Vector2(0f, y1));
                    }
                    p.ClosePath(); p.Fill();
                }

                // 左侧试管刻度线
                p.strokeColor = new Color(0.46f, 0.36f, 0.72f, 0.72f);
                p.lineWidth = 1.2f;
                for (int t = 1; t <= 12; t++)
                {
                    float ty = r.height * (t / 13f);
                    float len = (t % 3 == 0) ? 7f : 4f;
                    p.BeginPath(); p.MoveTo(new Vector2(2f, ty)); p.LineTo(new Vector2(2f + len, ty)); p.Stroke();
                }
            };
            tokTube.schedule.Execute(() => tokTube.MarkDirtyRepaint()).Every(42);
            _tokPctLbl = new Label("80%") { pickingMode = PickingMode.Ignore };
            _tokPctLbl.style.position = Position.Absolute;
            _tokPctLbl.style.top = Length.Percent(24f);
            _tokPctLbl.style.left = 0f; _tokPctLbl.style.right = 0f;
            _tokPctLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            _tokPctLbl.style.color = XqMoonStyle.Ink;
            _tokPctLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tokTube.Add(_tokPctLbl);
            _tokenCapsule.Add(tokHead); _tokenCapsule.Add(tokTube);
            _host.Add(_tokenCapsule);
        }

        void PlaceApprovedClose(float width, float height)
        {
            float k = Mathf.Min(width / 1344f, height / 756f);
            float chatW = 540f * k;
            float chatL = (width - chatW) * 0.5f;
            float dockH = 36f * k;
            float dockT = height - 88f * k;

            SetApprovedRect(_cardNext, width * .026f, height * .042f, 360f * k, 110f * k);
            // 音乐卡窄一点高一点，底边与输入框对齐
            SetApprovedRect(_cardMusic, width * .026f, dockT + dockH - 180f * k, 214f * k, 180f * k);
            if (_bodyMusic != null) { _bodyMusic.style.paddingTop = _bodyMusic.style.paddingBottom = _bodyMusic.style.paddingLeft = _bodyMusic.style.paddingRight = 0f; }
            // 声浪 bar 与输入框同宽居中，不伸向左右卡片
            if (_spectrumRow != null)
            {
                _spectrumRow.style.left = chatL;
                _spectrumRow.style.right = StyleKeyword.Auto;
                _spectrumRow.style.width = chatW;
                _spectrumRow.style.bottom = height - dockT + 26f * k;
                _spectrumRow.style.height = 46f * k;
            }
            // 单行黑字回复紧贴输入框上方右对齐
            SetApprovedRect(_cardSay, chatL, dockT - 26f * k, chatW, 24f * k);
            if (_bodySay != null) { _bodySay.style.paddingTop = _bodySay.style.paddingBottom = _bodySay.style.paddingLeft = _bodySay.style.paddingRight = 0f; }
            SetApprovedRect(_chatDock, chatL, dockT, chatW, dockH);
            SetApprovedRect(_modeDock, width - 224f * k, dockT, 206f * k, dockH);
            // V20 右下角：正在做的事胶囊位于模式条正上方，留出呼吸间距
            if (_nextPill != null) SetApprovedRect(_nextPill, width - 224f * k, dockT - 42f * k, 206f * k, 30f * k);
            // V20 签名更低更大气，水平居中，与回复之间留出声浪条空间
            if (_plate != null)
            {
                SetApprovedRect(_plate, (width - 350f * k) * 0.5f, height * .635f, 350f * k, 86f * k);
                _plate.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(0f, AngleUnit.Degree));
            }

            // V20 右上角：瘦长 Q 版探头竖卡（上）+ 电量试管（下）一上一下排列
            if (_remindPill != null)
            {
                SetApprovedRect(_remindPill, width - 108f * k, height * .085f, 78f * k, 146f * k);
                SetRadius(_remindPill, 14f * k);
                _remindPill.style.paddingTop = 36f * k;
                foreach (var vl in _remindPill.Query<Label>().ToList()) vl.style.fontSize = 11.5f * k;
                if (_chibiPeekImg != null)
                {
                    _chibiPeekImg.style.width = 76f * k; _chibiPeekImg.style.height = 60f * k;
                    _chibiPeekImg.style.left = 1f * k; _chibiPeekImg.style.top = -36f * k;
                }
            }
            if (_tokenCapsule != null)
            {
                SetApprovedRect(_tokenCapsule, width - 96f * k, height * .085f + 174f * k, 56f * k, 148f * k);
                SetRadius(_tokenCapsule, 22f * k);
                if (_tokStarImg != null)
                {
                    _tokStarImg.style.width = 54f * k; _tokStarImg.style.height = 34f * k;
                    _tokStarImg.style.left = 0f; _tokStarImg.style.top = -22f * k;
                }
                if (_tokPctLbl != null) { _tokPctLbl.style.fontSize = 11f * k; _tokPctLbl.style.color = XqMoonStyle.Ink; }
                foreach (var child in _tokenCapsule.Children())
                {
                    if (child is VisualElement && !(child is Label) && !(child is Image))
                        SetRadius(child, 16f * k);
                }
            }

            // 左右各 2 个贴纸本体围绕小祈四散布局（保持原始比例不压扁）
            SetApprovedRect(_approvedDock, 0f, height * .22f, width, height * .42f);
            float[] lx = { width * .195f, width * .175f, width * .695f, width * .725f };
            float[] ly = { 4f * k, 108f * k, 0f, 106f * k };
            float[] lw = { 94f * k, 100f * k, 106f * k, 88f * k };
            float[] lh = { 80f * k, 68f * k, 74f * k, 90f * k };
            int idx = 0;
            foreach (var el in _approvedDock.Children())
            {
                el.style.left = lx[idx % 4];
                el.style.top = ly[idx % 4];
                el.style.width = lw[idx % 4];
                el.style.height = lh[idx % 4];
                el.style.paddingTop = idx == 2 ? 12f * k : (idx == 3 ? 10f * k : 0f);
                el.style.paddingLeft = idx == 3 ? 8f * k : 0f;
                var lbl = el.Q<Label>();
                if (lbl != null) lbl.style.fontSize = idx == 3 ? 16f * k : 11.5f * k;
                idx++;
            }

            if (_clockBig != null) { _clockBig.style.fontSize = 42f * k; _clockBig.style.color = new Color(0.24f, 0.16f, 0.42f, 0.96f); _clockBig.style.unityFontStyleAndWeight = FontStyle.Bold; _clockBig.style.textShadow = new TextShadow { blurRadius = 8f, color = new Color(1f, 1f, 1f, .92f) }; }
            foreach (var label in _kickers) { label.style.fontSize = 10f * k; label.style.color = XqMoonStyle.Soft; label.style.unityFontStyleAndWeight = FontStyle.Normal; }
            foreach (var label in _marks) label.style.fontSize = 9.5f * k;
            foreach (var label in _texts) { label.style.fontSize = 11f * k; label.style.color = XqMoonStyle.Ink; label.style.unityFontStyleAndWeight = FontStyle.Bold; }
            if (_musicTitle != null) { _musicTitle.style.color = Color.white; _musicTitle.style.fontSize = 12.5f * k; }
            if (_musicArtist != null) { _musicArtist.style.color = new Color(0.92f, 0.89f, 0.98f, 0.90f); _musicArtist.style.fontSize = 10f * k; }
            var pLbl = _musicPrev?.Q<Label>(); if (pLbl != null) { pLbl.style.color = Color.white; pLbl.style.fontSize = 11f * k; }
            var nLbl = _musicNext?.Q<Label>(); if (nLbl != null) { nLbl.style.color = Color.white; nLbl.style.fontSize = 11f * k; }
            if (_musicLikeLbl != null) { _musicLikeLbl.style.color = new Color(1f, 0.78f, 0.90f, 1f); _musicLikeLbl.style.fontSize = 12f * k; }
            if (_sayText != null) { _sayText.style.fontSize = 12.5f * k; _sayText.style.color = new Color(0.10f, 0.08f, 0.14f, 0.96f); _sayText.style.unityFontStyleAndWeight = FontStyle.Bold; }
            if (_locPill != null) { _locPill.style.color = Color.white; _locPill.style.fontSize = 10f * k; _locPill.style.whiteSpace = WhiteSpace.NoWrap; }
            if (_weatherPill != null) { _weatherPill.style.color = XqMoonStyle.Ink; _weatherPill.style.fontSize = 10f * k; _weatherPill.style.whiteSpace = WhiteSpace.NoWrap; }
            SetRadius(_nextPill, 12f * k);
            _bodyNext.style.paddingLeft = _bodyNext.style.paddingRight = 0f;
            _bodyNext.style.paddingTop = 0f; _bodyNext.style.paddingBottom = 0f;
            _nextTime.style.fontSize = _nextWhat.style.fontSize = 10f * k;
            _chatInput.style.fontSize = 11.5f * k;
            var inp = _chatInput.Q(className: "unity-text-field__input");
            if (inp != null) inp.style.color = XqMoonStyle.Ink;
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
            _rail = Box(64f * _k, PanH * _k, 32f * _k, new Color(.95f, .93f, .99f, .86f));
            Abs(_rail, 6f * _k, 0, 64f * _k, PanH * _k); Border(_rail, 1f, new Color(1f, 1f, 1f, 0.88f));
            _rail.style.alignItems = Align.Center; _rail.style.paddingTop = 22f * _k; _rail.style.paddingBottom = 18f * _k;
            var logo = L("祈.", 22f, XqMoonStyle.Soft, true); logo.style.marginBottom = 22f * _k; _rail.Add(logo);
            string[] labels = { "此刻", "手账", "歌单", "记忆", "设置" };
            string[] icons = { "spark", "cal", "sun", "heart", "gear" };
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
                var icon = new XqGlyph(icons[n], XqMoonStyle.Soft); icon.style.width = icon.style.height = 17f * _k; button.Add(icon);
                var label = L(labels[n], 8.5f, XqMoonStyle.Soft, false); label.style.marginTop = 2f * _k; button.Add(label);
                _nav.Add(button); _navHover.Add(0f); _navGlyph.Add(icon); _rail.Add(button);
            }
            _stage.Add(_rail);
        }

        void BuildApprovedPanel()
        {
            float x = (RailW + Gap) * _k;
            _panel = new VisualElement { name = "xq-bench-panel", pickingMode = PickingMode.Ignore };
            Abs(_panel, x, 0, PanW * _k, PanH * _k); XqMoonStyle.Surface(_panel, 24f * _k);
            _panel.style.backgroundColor = new Color(.97f, .95f, .99f, 1f);
            _stage.Add(_panel);
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