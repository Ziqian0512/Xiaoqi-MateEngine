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

            // 左上时钟区：大号左低右高倾斜蝴蝶结（不压时间），日期地点与温度天气上下拉开呼吸间距
            XqMoonStyle.Clear(_cardNext); XqMoonStyle.Clear(_bodyNext);
            XqMoonStyle.Sticker(_cardNext, "XqStickerRibbon", 92f, -42f, -30f);
            var bowEl = _cardNext[_cardNext.childCount - 1];
            bowEl.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(-16f, AngleUnit.Degree));
            bowEl.BringToFront();
            _locPill.RemoveFromHierarchy();
            var infoRow = new VisualElement { pickingMode = PickingMode.Ignore };
            infoRow.style.flexDirection = FlexDirection.Column;
            infoRow.style.alignItems = Align.FlexStart;
            infoRow.style.marginTop = 12f;
            _locPill.style.backgroundColor = new Color(0.24f, 0.18f, 0.42f, 0.92f);
            SetAllBorder(_locPill, 1f, new Color(1f, 1f, 1f, 0.45f));
            SetRadius(_locPill, 10f);
            _locPill.style.paddingLeft = _locPill.style.paddingRight = 10f;
            _locPill.style.paddingTop = _locPill.style.paddingBottom = 4f;
            _locPill.style.marginTop = _locPill.style.marginBottom = 0f;
            _weatherPill = new Label("☁ 同步中") { pickingMode = PickingMode.Ignore };
            _weatherPill.style.backgroundColor = new Color(0.97f, 0.94f, 1.00f, 0.94f);
            SetAllBorder(_weatherPill, 1f, Color.white);
            SetRadius(_weatherPill, 10f);
            _weatherPill.style.paddingLeft = _weatherPill.style.paddingRight = 9f;
            _weatherPill.style.paddingTop = _weatherPill.style.paddingBottom = 4f;
            _weatherPill.style.marginTop = 10f;
            _weatherPill.style.color = XqMoonStyle.Ink;
            _weatherPill.style.unityFontStyleAndWeight = FontStyle.Bold;
            _weatherPill.style.whiteSpace = WhiteSpace.NoWrap;
            infoRow.Add(_locPill); infoRow.Add(_weatherPill);
            _bodyNext.Add(infoRow);

            // V21 右上角：慢慢来 Q 版盖被子竖卡（XqChibiCardBg 整卡渐变+弧形被边 + 中文从右到左竖排）
            _remindPill.RemoveFromHierarchy();
            _host.Add(_remindPill);
            _remindPill.Clear();
            _remindPill.style.position = Position.Absolute;
            _remindPill.style.backgroundColor = Color.clear;
            SetAllBorder(_remindPill, 0f, Color.clear);
            SetRadius(_remindPill, 0f);
            var chibiBg = Resources.Load<Texture2D>("XqChibiCardBg");
            if (chibiBg != null)
            {
                _remindPill.style.backgroundImage = Background.FromTexture2D(chibiBg);
                _remindPill.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            }
            _remindPill.style.marginLeft = _remindPill.style.marginTop = 0f;
            _remindPill.style.paddingLeft = _remindPill.style.paddingRight = 6f;
            _remindPill.style.paddingBottom = 10f;
            _remindPill.style.paddingTop = 44f;
            _remindPill.style.justifyContent = Justify.Center;
            _remindPill.style.alignItems = Align.Center;
            _remindPill.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(0f, AngleUnit.Degree));
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

            // V24 右上角：「正在」横幅隐藏时间与末尾地点后缀，允许两行居中换行不穿模
            _nextPill.RemoveFromHierarchy();
            _host.Add(_nextPill);
            _nextPill.style.position = Position.Absolute;
            XqMoonStyle.Clear(_nextPill);
            var banTex = Resources.Load<Texture2D>("XqBannerNext");
            if (banTex != null)
            {
                _nextPill.style.backgroundImage = Background.FromTexture2D(banTex);
                _nextPill.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            }
            _nextPill.style.marginTop = 0f;
            _nextPill.style.paddingTop = 18f; _nextPill.style.paddingBottom = 14f;
            _nextPill.style.paddingLeft = _nextPill.style.paddingRight = 38f;
            _nextPill.style.justifyContent = Justify.Center;
            _nextPill.style.alignItems = Align.Center;
            _nextPill.style.overflow = Overflow.Hidden;
            if (_nextTime != null) _nextTime.style.display = DisplayStyle.None;
            if (_tipBench != null) _tipBench.style.display = DisplayStyle.None;
            if (_nextPill.childCount > 0)
            {
                var nextTop = _nextPill[0];
                nextTop.style.width = Length.Percent(100f);
                nextTop.style.overflow = Overflow.Hidden;
                nextTop.style.justifyContent = Justify.Center;
                nextTop.style.alignItems = Align.Center;
                foreach (var ch in nextTop.Children())
                    if (ch is Label l) { l.style.flexShrink = 0f; l.style.minWidth = StyleKeyword.Auto; }
            }
            if (_nextWhat != null)
            {
                _nextWhat.style.flexGrow = 0f;
                _nextWhat.style.flexShrink = 1f;
                _nextWhat.style.minWidth = 0f;
                _nextWhat.style.whiteSpace = WhiteSpace.Normal;
                _nextWhat.style.unityTextAlign = TextAnchor.MiddleCenter;
                _nextWhat.style.overflow = Overflow.Hidden;
            }
            _nextPill.Q("xq-old-schedule-progress")?.RemoveFromHierarchy();

            // V23 音乐卡：紧凑方正，取消右侧大按钮，改为歌名/歌手在进度条上方，播放键居中并入底部控制行 (|< ▶ >| ♡)
            _cardMusic.Q("xq-soft-shadow")?.RemoveFromHierarchy();
            XqMoonStyle.Clear(_cardMusic);
            XqMoonStyle.Clear(_bodyMusic);
            _bodyMusic.style.backgroundColor = new Color(0.24f, 0.18f, 0.38f, 0.94f);
            SetAllBorder(_bodyMusic, 0f, Color.clear);
            SetRadius(_bodyMusic, 18f);
            _bodyMusic.Clear();

            var defaultCover = Resources.Load<Texture2D>("XqMusicCover");
            var bgCover = new VisualElement { pickingMode = PickingMode.Ignore };
            bgCover.style.position = Position.Absolute; bgCover.style.left = bgCover.style.top = bgCover.style.right = bgCover.style.bottom = 0f;
            if (defaultCover != null) bgCover.style.backgroundImage = Background.FromTexture2D(defaultCover);
            bgCover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            SetRadius(bgCover, 18f);
            _bodyMusic.Add(bgCover);
            var liveCover = new VisualElement { pickingMode = PickingMode.Ignore };
            liveCover.style.position = Position.Absolute; liveCover.style.left = liveCover.style.top = liveCover.style.right = liveCover.style.bottom = 0f;
            liveCover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            SetRadius(liveCover, 18f);
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
            scrim.style.backgroundColor = new Color(0.14f, 0.10f, 0.24f, 0.48f);
            SetRadius(scrim, 18f);
            _bodyMusic.Add(scrim);

            var mContent = new VisualElement { pickingMode = PickingMode.Ignore };
            mContent.style.flexGrow = 1f; mContent.style.justifyContent = Justify.FlexEnd;
            mContent.style.paddingLeft = mContent.style.paddingRight = 15f;
            mContent.style.paddingTop = 12f; mContent.style.paddingBottom = 13f;

            var mDrop = new TextShadow { offset = new Vector2(0f, 1.5f), blurRadius = 4f, color = new Color(0.08f, 0.05f, 0.16f, 0.72f) };
            var mTxtCol = new VisualElement { pickingMode = PickingMode.Ignore };
            mTxtCol.style.marginBottom = 8f;
            _musicTitle.style.color = Color.white; _musicTitle.style.unityTextAlign = TextAnchor.MiddleLeft; _musicTitle.style.textShadow = mDrop;
            _musicTitle.style.whiteSpace = WhiteSpace.NoWrap; _musicTitle.style.overflow = Overflow.Hidden; _musicTitle.style.textOverflow = TextOverflow.Ellipsis;
            _musicArtist.style.color = new Color(0.94f, 0.91f, 0.99f, 0.92f); _musicArtist.style.unityTextAlign = TextAnchor.MiddleLeft; _musicArtist.style.textShadow = mDrop;
            _musicArtist.style.whiteSpace = WhiteSpace.NoWrap; _musicArtist.style.overflow = Overflow.Hidden; _musicArtist.style.textOverflow = TextOverflow.Ellipsis;
            _musicArtist.style.marginTop = 2f; _musicArtist.style.minHeight = 14f;
            mTxtCol.Add(_musicTitle); mTxtCol.Add(_musicArtist);
            mContent.Add(mTxtCol);

            var barTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            barTrack.style.height = 3.5f; barTrack.style.marginBottom = 10f;
            barTrack.style.backgroundColor = new Color(1f, 1f, 1f, 0.36f); SetRadius(barTrack, 2f);
            _musicFill.style.backgroundColor = Color.white; barTrack.Add(_musicFill);
            mContent.Add(barTrack);

            var mBotRow = new VisualElement { pickingMode = PickingMode.Ignore };
            mBotRow.style.flexDirection = FlexDirection.Row; mBotRow.style.justifyContent = Justify.SpaceAround; mBotRow.style.alignItems = Align.Center;
            void SetupTransportIcon(VisualElement btn, bool isNext)
            {
                btn.style.backgroundColor = Color.clear; btn.style.width = 24f; btn.style.height = 24f;
                var l = btn.Q<Label>(); if (l != null) l.style.display = DisplayStyle.None;
                var ico = new VisualElement { pickingMode = PickingMode.Ignore };
                ico.style.width = 15f; ico.style.height = 15f;
                ico.generateVisualContent += mgc =>
                {
                    var p = mgc.painter2D;
                    p.fillColor = Color.white;
                    if (!isNext)
                    {
                        p.BeginPath(); p.MoveTo(new Vector2(2f, 2f)); p.LineTo(new Vector2(4.2f, 2f)); p.LineTo(new Vector2(4.2f, 13f)); p.LineTo(new Vector2(2f, 13f)); p.ClosePath(); p.Fill();
                        p.BeginPath(); p.MoveTo(new Vector2(5f, 7.5f)); p.LineTo(new Vector2(13f, 2f)); p.LineTo(new Vector2(13f, 13f)); p.ClosePath(); p.Fill();
                    }
                    else
                    {
                        p.BeginPath(); p.MoveTo(new Vector2(2f, 2f)); p.LineTo(new Vector2(10f, 7.5f)); p.LineTo(new Vector2(2f, 13f)); p.ClosePath(); p.Fill();
                        p.BeginPath(); p.MoveTo(new Vector2(10.8f, 2f)); p.LineTo(new Vector2(13f, 2f)); p.LineTo(new Vector2(13f, 13f)); p.LineTo(new Vector2(10.8f, 13f)); p.ClosePath(); p.Fill();
                    }
                };
                btn.Add(ico);
            }
            SetupTransportIcon(_musicPrev, false);
            _musicPlay.style.width = 28f; _musicPlay.style.height = 28f; SetRadius(_musicPlay, 14f);
            _musicPlay.style.backgroundColor = new Color(1f, 1f, 1f, 0.97f);
            _musicPlay.style.borderBottomWidth = 2f; _musicPlay.style.borderBottomColor = new Color(0.76f, 0.70f, 0.92f, 0.90f);
            _musicPlayLbl.style.color = XqMoonStyle.Ink;
            SetupTransportIcon(_musicNext, true);
            _musicLike.style.display = DisplayStyle.Flex; _musicLike.style.backgroundColor = Color.clear; _musicLike.style.width = 24f; _musicLike.style.height = 24f;
            if (_musicLikeLbl != null) { _musicLikeLbl.style.color = new Color(1f, 0.80f, 0.92f, 1f); _musicLikeLbl.style.textShadow = mDrop; }
            mBotRow.Add(_musicPrev); mBotRow.Add(_musicPlay); mBotRow.Add(_musicNext); mBotRow.Add(_musicLike);
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

            // V22 右上角魔法少女星星玻璃试管电量计：动态液体在底层，高精玻璃外框 XqBatteryTubeFrame 叠在上层
            _tokenCapsule = new VisualElement { name = "xq-token-capsule", pickingMode = PickingMode.Ignore };
            _tokenCapsule.style.position = Position.Absolute;
            _tokenCapsule.style.backgroundColor = Color.clear;
            SetAllBorder(_tokenCapsule, 0f, Color.clear);

            var tokTube = new VisualElement { pickingMode = PickingMode.Ignore };
            tokTube.style.position = Position.Absolute;
            tokTube.style.left = Length.Percent(21.5f); tokTube.style.right = Length.Percent(21.5f);
            tokTube.style.top = Length.Percent(26.5f); tokTube.style.bottom = Length.Percent(2.5f);
            tokTube.generateVisualContent += mgc =>
            {
                var r = tokTube.contentRect;
                if (r.width < 8f || r.height < 16f) return;
                var p = mgc.painter2D;
                float fill = 0.80f;
                float phase = Time.realtimeSinceStartup * 2.6f;
                float surfY = r.height * (1f - fill);
                float rad = r.width * 0.5f;
                float arcCy = r.height - rad;

                // 上半矩形带（surfY..arcCy）分 12 段桃粉->柔紫顺滑渐变；下半圆弧（arcCy..bottom）单色柔紫封闭圆弧，既有渐变又零三角化锯齿
                var topCol = new Color(1.00f, 0.82f, 0.90f, 0.94f);
                var botCol = new Color(0.72f, 0.56f, 0.98f, 0.96f);

                // 背面波浪
                p.fillColor = new Color(0.78f, 0.66f, 0.98f, 0.50f);
                p.BeginPath();
                p.MoveTo(new Vector2(0f, arcCy));
                for (int s = 0; s <= 14; s++)
                {
                    float u = s / 14f;
                    p.LineTo(new Vector2(r.width * u, surfY - 2.5f + Mathf.Sin(u * 4.5f - phase) * 2.8f));
                }
                p.LineTo(new Vector2(r.width, arcCy));
                p.ClosePath(); p.Fill();

                const int bands = 12;
                for (int b = 0; b < bands; b++)
                {
                    float t0 = b / (float)bands;
                    float t1 = (b + 1) / (float)bands;
                    float y0 = Mathf.Lerp(surfY, arcCy, t0);
                    float y1 = Mathf.Lerp(surfY, arcCy, t1) + 0.6f;
                    p.fillColor = Color.Lerp(topCol, botCol, (t0 + t1) * 0.5f);
                    p.BeginPath();
                    if (b == 0)
                    {
                        for (int s = 0; s <= 14; s++)
                        {
                            float u = s / 14f;
                            float wy = surfY + Mathf.Sin(u * 4.8f + phase) * 2.8f;
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

                // 底部半圆弧单次封闭填充（与最末条带完全同色）
                p.fillColor = botCol;
                p.BeginPath();
                p.MoveTo(new Vector2(0f, arcCy - 0.5f));
                p.LineTo(new Vector2(r.width, arcCy - 0.5f));
                for (int s = 0; s <= 16; s++)
                {
                    float a = (s / 16f) * Mathf.PI;
                    p.LineTo(new Vector2(rad + Mathf.Cos(a) * rad, arcCy + Mathf.Sin(a) * rad));
                }
                p.ClosePath(); p.Fill();
            };
            tokTube.schedule.Execute(() => tokTube.MarkDirtyRepaint()).Every(42);
            _tokenCapsule.Add(tokTube);

            var tubeFrame = Resources.Load<Texture2D>("XqBatteryTubeFrame");
            if (tubeFrame != null)
            {
                var frameImg = new Image { image = tubeFrame, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                frameImg.style.position = Position.Absolute;
                frameImg.style.left = frameImg.style.top = frameImg.style.right = frameImg.style.bottom = 0f;
                _tokenCapsule.Add(frameImg);
            }

            var tokHead = new Label("电量") { pickingMode = PickingMode.Ignore };
            tokHead.style.position = Position.Absolute;
            tokHead.style.top = Length.Percent(18.5f);
            tokHead.style.left = 0f; tokHead.style.right = 0f;
            tokHead.style.unityTextAlign = TextAnchor.MiddleCenter;
            tokHead.style.color = new Color(0.28f, 0.20f, 0.50f, 1f);
            tokHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            _marks.Add(tokHead);
            _tokenCapsule.Add(tokHead);

            _tokPctLbl = new Label("80%") { pickingMode = PickingMode.Ignore };
            _tokPctLbl.style.position = Position.Absolute;
            _tokPctLbl.style.top = Length.Percent(36f);
            _tokPctLbl.style.left = 0f; _tokPctLbl.style.right = 0f;
            _tokPctLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            _tokPctLbl.style.color = XqMoonStyle.Ink;
            _tokPctLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tokTube.Add(_tokPctLbl);
            _host.Add(_tokenCapsule);

            // 让所有新增近景悬浮卡片/贴纸也随鼠标移动呈现 3D 视差错层
            AnimAdd(_nextPill, 1, new Vector2(10f, -6f));
            AnimAdd(_remindPill, 2, new Vector2(12f, 6f));
            AnimAdd(_tokenCapsule, 3, new Vector2(14f, 4f));
            AnimAdd(_approvedDock, 4, new Vector2(-10f, 8f));
            AnimAdd(_spectrumRow, 5, new Vector2(0f, 4f));
        }

        void PlaceApprovedClose(float width, float height)
        {
            float k = Mathf.Min(width / 1344f, height / 756f);
            float chatW = 540f * k;
            float chatL = (width - chatW) * 0.5f;
            float dockH = 36f * k;
            float dockT = height - 88f * k;

            SetApprovedRect(_cardNext, width * .042f, height * .055f, 340f * k, 150f * k);
            // V23 音乐卡紧凑方正 214x176
            SetApprovedRect(_cardMusic, width * .026f, dockT + dockH - 176f * k, 214f * k, 176f * k);
            if (_bodyMusic != null) { _bodyMusic.style.paddingTop = _bodyMusic.style.paddingBottom = _bodyMusic.style.paddingLeft = _bodyMusic.style.paddingRight = 0f; }
            // 声浪 bar 与输入框同宽居中，高度加高以完整展现动态轮廓波浪
            if (_spectrumRow != null)
            {
                _spectrumRow.style.left = chatL;
                _spectrumRow.style.right = StyleKeyword.Auto;
                _spectrumRow.style.width = chatW;
                _spectrumRow.style.bottom = height - dockT + 26f * k;
                _spectrumRow.style.height = 54f * k;
            }
            // 单行黑字回复紧贴输入框上方右对齐
            SetApprovedRect(_cardSay, chatL, dockT - 26f * k, chatW, 24f * k);
            if (_bodySay != null) { _bodySay.style.paddingTop = _bodySay.style.paddingBottom = _bodySay.style.paddingLeft = _bodySay.style.paddingRight = 0f; }
            SetApprovedRect(_chatDock, chatL, dockT, chatW, dockH);
            SetApprovedRect(_modeDock, width - 238f * k, dockT, 220f * k, dockH);
            // V26 右上角置顶：「正在」猫耳横幅缩小至 218x88，文字放大，紧致协调
            if (_nextPill != null) SetApprovedRect(_nextPill, width - 236f * k, height * .028f, 218f * k, 88f * k);
            // V21 签名位于声浪正上方
            if (_plate != null)
            {
                SetApprovedRect(_plate, (width - 350f * k) * 0.5f, height * .615f, 350f * k, 86f * k);
                _plate.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(0f, AngleUnit.Degree));
            }

            // V26 右侧中上：慢慢来盖被子竖卡 + 电量试管紧接在「正在」横幅下方
            if (_remindPill != null)
            {
                SetApprovedRect(_remindPill, width - 196f * k, height * .175f, 94f * k, 164f * k);
                _remindPill.style.paddingTop = 44f * k;
                foreach (var vl in _remindPill.Query<Label>().ToList()) vl.style.fontSize = 11.5f * k;
                if (_chibiPeekImg != null)
                {
                    _chibiPeekImg.style.width = 86f * k; _chibiPeekImg.style.height = 68f * k;
                    _chibiPeekImg.style.left = 4f * k; _chibiPeekImg.style.top = -22f * k;
                }
            }
            if (_tokenCapsule != null)
            {
                SetApprovedRect(_tokenCapsule, width - 92f * k, height * .148f, 64f * k, 244f * k);
                SetRadius(_tokenCapsule, 0f);
                if (_tokPctLbl != null) { _tokPctLbl.style.fontSize = 11f * k; _tokPctLbl.style.color = XqMoonStyle.Ink; }
            }

            // 左右各 2 个贴纸本体围绕小祈四散布局（饱满心形与立体磁带比例，文字绝对居中）
            SetApprovedRect(_approvedDock, 0f, height * .22f, width, height * .42f);
            float[] lx = { width * .195f, width * .175f, width * .695f, width * .725f };
            float[] ly = { 4f * k, 108f * k, 0f, 106f * k };
            float[] lw = { 96f * k, 108f * k, 106f * k, 88f * k };
            float[] lh = { 90f * k, 80f * k, 74f * k, 90f * k };
            int idx = 0;
            foreach (var el in _approvedDock.Children())
            {
                el.style.left = lx[idx % 4];
                el.style.top = ly[idx % 4];
                el.style.width = lw[idx % 4];
                el.style.height = lh[idx % 4];
                el.style.paddingTop = idx == 2 ? 16f * k : (idx == 3 ? 22f * k : 0f);
                el.style.paddingBottom = (idx == 0 || idx == 1) ? 8f * k : 0f;
                el.style.paddingLeft = idx == 3 ? 22f * k : 0f;
                var lbl = el.Q<Label>();
                if (lbl != null)
                {
                    lbl.style.width = Length.Percent(100f);
                    lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                    lbl.style.marginTop = lbl.style.marginBottom = lbl.style.marginLeft = lbl.style.marginRight = 0f;
                    lbl.style.paddingTop = lbl.style.paddingBottom = lbl.style.paddingLeft = lbl.style.paddingRight = 0f;
                    lbl.style.fontSize = idx == 3 ? 16f * k : 11.5f * k;
                }
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
            SetRadius(_nextPill, 0f);
            _bodyNext.style.paddingLeft = _bodyNext.style.paddingRight = 0f;
            _bodyNext.style.paddingTop = 0f; _bodyNext.style.paddingBottom = 0f;
            _nextTime.style.fontSize = 11f * k; _nextWhat.style.fontSize = 11.5f * k;
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
        static readonly int[] ApprovedPages = { 0, 1, 2, 5 };
        VisualElement _panoBg, _musicPageControls;
        Label _musicPageTitle, _musicPageArtist, _musicPageTime, _benchClockLbl, _benchMetaLine;
        Image _musicPageCover;

        void BuildApprovedRail()
        {
            _rail = Box(64f * _k, PanH * _k, 32f * _k, new Color(.96f, .94f, 1f, .74f));
            Abs(_rail, 6f * _k, 0, 64f * _k, PanH * _k); Border(_rail, 1.2f, new Color(1f, 1f, 1f, 0.90f));
            _rail.style.alignItems = Align.Center; _rail.style.paddingTop = 22f * _k; _rail.style.paddingBottom = 18f * _k;
            var logo = L("祈", 22f, XqMoonStyle.Soft, true); logo.style.marginBottom = 24f * _k; _rail.Add(logo);
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
                var icon = new XqGlyph(icons[n], XqMoonStyle.Soft); icon.style.width = icon.style.height = 17f * _k; button.Add(icon);
                if (!string.IsNullOrEmpty(labels[n]))
                {
                    var label = L(labels[n], 8.5f, XqMoonStyle.Soft, false); label.style.marginTop = 2f * _k; button.Add(label);
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
            _panel.style.backgroundColor = new Color(.97f, .95f, .99f, 1f);
            Round(_panel, 24f * _k);
            Border(_panel, 1.5f, new Color(1f, 1f, 1f, 0.90f));
            _stage.Add(_panel);

            // 全景小祈插画底版（铺满整个工作台，按当前页面自动切换室内/室外/夕阳全景）
            _panoBg = new VisualElement { name = "xq-bench-pano", pickingMode = PickingMode.Ignore };
            _panoBg.style.position = Position.Absolute;
            _panoBg.style.left = _panoBg.style.top = _panoBg.style.right = _panoBg.style.bottom = 0f;
            _panoBg.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            Round(_panoBg, 23f * _k);
            _panel.Add(_panoBg);

            var panoVeil = new VisualElement { pickingMode = PickingMode.Ignore };
            panoVeil.style.position = Position.Absolute;
            panoVeil.style.left = panoVeil.style.top = panoVeil.style.right = panoVeil.style.bottom = 0f;
            panoVeil.style.backgroundColor = new Color(0.98f, 0.96f, 1f, 0.14f);
            Round(panoVeil, 23f * _k);
            _panel.Add(panoVeil);

            _header = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(_header, 0, 0, PanW * _k, 76f * _k); _panel.Add(_header);
            _topClock = L("", 16f, Ink, false); _topClock.style.display = DisplayStyle.None;
            _charTopText = L("", 10f, Ink2, false); _charTopText.style.display = DisplayStyle.None;
            _metaLabel = L("", 9.5f, Ink2, false); Abs(_metaLabel, (PanW - 560f) * _k, 28f * _k, -1, -1); _header.Add(_metaLabel);

            var switcher = Box(220f * _k, 34f * _k, 17f * _k, new Color(1f, 1f, 1f, .76f));
            Abs(switcher, (PanW - 395f) * _k, 20f * _k, 220f * _k, 34f * _k);
            Border(switcher, 1f, Color.white);
            switcher.style.flexDirection = FlexDirection.Row; switcher.style.alignItems = Align.Center;
            _petModeBtn = ModePill("桌宠", false, Ink2, Color.clear); _closeModeBtn = ModePill("近景", false, Ink2, Color.clear);
            _benchModeTag = ModePill("工作台", true, Color.white, new Color(.48f, .38f, .86f));
            switcher.Add(_petModeBtn); switcher.Add(_closeModeBtn); switcher.Add(_benchModeTag); _header.Add(switcher);

            _modeBtn = Box(96f * _k, 34f * _k, 17f * _k, new Color(1f, 1f, 1f, .76f));
            Abs(_modeBtn, (PanW - 164f) * _k, 20f * _k, 96f * _k, 34f * _k);
            Border(_modeBtn, 1f, Color.white); _modeBtn.style.justifyContent = Justify.Center;
            _modeBtnLabel = L(IsOfficeMode() ? "💼 办公模式" : "🏠 居家模式", 10f, Ink, false); _modeBtn.Add(_modeBtnLabel); _header.Add(_modeBtn);

            _refreshBtn = Box(32f * _k, 32f * _k, 16f * _k, new Color(1f, 1f, 1f, .78f));
            Abs(_refreshBtn, (PanW - 56f) * _k, 21f * _k, 32f * _k, 32f * _k);
            Border(_refreshBtn, 1f, Color.white);
            _refreshBtn.style.alignItems = Align.Center; _refreshBtn.style.justifyContent = Justify.Center;
            _refreshBtn.Add(L("↻", 15f, Ink2, false)); _header.Add(_refreshBtn);

            _charBox = new VisualElement { pickingMode = PickingMode.Ignore }; _charBox.style.display = DisplayStyle.None;
            _nameplate = new VisualElement { pickingMode = PickingMode.Ignore }; _nameplate.style.display = DisplayStyle.None;
            _charBotText = L("", 10f, Ink2, false);
            _leftHost = new VisualElement { name = "xq-bench-left", pickingMode = PickingMode.Ignore };
            _bentoHost = new VisualElement { name = "xq-bento-host", pickingMode = PickingMode.Ignore };
            _panel.Add(_leftHost); _panel.Add(_bentoHost);

            _offBar = Box(300f * _k, 30f * _k, 12f * _k, new Color(.96f, .91f, .96f, .90f));
            Abs(_offBar, (PanW - 322f) * _k, (PanH - 39f) * _k, 300f * _k, 30f * _k); _offBar.style.flexDirection = FlexDirection.Row;
            _offBar.Add(L("接口暂不可用 · 显示缓存", 10f, Ink2, false));
            _offRetry = Box(42f * _k, 24f * _k, 10f * _k, Color.white); _offRetry.Add(L("重试", 10f, Ink, false)); _offBar.Add(_offRetry); _panel.Add(_offBar);
            _toast = L("", 11f, Color.white, false); _toast.style.display = DisplayStyle.None; _panel.Add(_toast);
        }

        void UpdateBenchBackdrop()
        {
            if (_panoBg == null) return;
            string bgName = _page == 1 ? "XqBgCampus" : (_page == 2 ? "XqBgSunset" : "XqBgDorm");
            if (_page == 0)
            {
                var life = _bd?.Get("life");
                string place = Str(life, "place.place") + Str(life, "place.doing");
                if (place.Contains("路") || place.Contains("本部") || place.Contains("校园")) bgName = "XqBgCampus";
            }
            var tex = Resources.Load<Texture2D>(bgName);
            if (tex != null) _panoBg.style.backgroundImage = Background.FromTexture2D(tex);
        }

        void ApprovedContentLayout()
        {
            UpdateBenchBackdrop();
            if (_charBox != null) _charBox.style.display = DisplayStyle.None;
            _leftHost.style.display = DisplayStyle.Flex;
            if (_page == 5)
            {
                Abs(_leftHost, 28f * _k, 84f * _k, 460f * _k, 486f * _k);
                Abs(_bentoHost, 514f * _k, 84f * _k, 476f * _k, 486f * _k);
            }
            else
            {
                // 左右浮动半透明卡片，中间完全留白透出底层全景小祈
                Abs(_leftHost, 28f * _k, 78f * _k, 324f * _k, (PanH - 96f) * _k);
                Abs(_bentoHost, (PanW - 352f) * _k, 78f * _k, 326f * _k, (PanH - 96f) * _k);
            }
        }

        VisualElement GlassCard(VisualElement host, float pad = 16f)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.backgroundColor = new Color(0.98f, 0.97f, 1.00f, 0.72f);
            Border(el, 1.2f, new Color(1f, 1f, 1f, 0.92f));
            Round(el, 18f * _k);
            el.style.paddingLeft = el.style.paddingRight = pad * _k;
            el.style.paddingTop = el.style.paddingBottom = (pad - 2f) * _k;
            el.style.marginBottom = 14f * _k;
            host.Add(el);
            return el;
        }

        void BuildApprovedHome(bool loading)
        {
            var state = _bd.Get("state"); var life = _bd.Get("life");
            // 左侧：极简大时钟 + 地点天气 + 一行状态胶囊（不放时间轴！）
            _benchClockLbl = L(DateTime.Now.ToString("HH:mm"), 58f, new Color(0.18f, 0.14f, 0.30f, 0.96f), true);
            _benchClockLbl.style.marginTop = 8f * _k;
            _benchClockLbl.style.textShadow = new TextShadow { blurRadius = 10f, color = new Color(1f, 1f, 1f, 0.92f) };
            _benchClockLbl.schedule.Execute(() => { if (_benchClockLbl != null) _benchClockLbl.text = DateTime.Now.ToString("HH:mm"); }).Every(1000);
            _leftHost.Add(_benchClockLbl);

            string place = Str(life, "place.place"); if (place.Length == 0) place = "薄扶林 · 宿舍";
            string wDesc = Str(life, "weather.desc"); string wTemp = Str(life, "weather.temp");
            string wStr = !string.IsNullOrEmpty(wTemp) ? wTemp + "°C " + wDesc : "阴";
            _benchMetaLine = L(DateTime.Now.ToString("ddd MM/dd").ToUpperInvariant() + " · " + place + " · " + wStr, 11.5f, new Color(0.20f, 0.16f, 0.34f, 0.92f), true);
            _benchMetaLine.style.textShadow = new TextShadow { blurRadius = 8f, color = Color.white };
            _benchMetaLine.style.marginBottom = 14f * _k;
            _leftHost.Add(_benchMetaLine);

            int bpm = Mathf.RoundToInt(Num(state, "me.bpm", 71f));
            int energy = Mathf.RoundToInt(Num(state, "needs.energy", 0.80f) * (Num(state, "needs.energy", 0.80f) <= 1f ? 100f : 1f));
            var vital = Box(-1, 32f * _k, 16f * _k, new Color(1f, 1f, 1f, 0.78f));
            vital.style.alignSelf = Align.FlexStart;
            vital.style.paddingLeft = vital.style.paddingRight = 14f * _k;
            vital.style.justifyContent = Justify.Center;
            Border(vital, 1.2f, Color.white);
            vital.Add(L($"♥ {bpm} BPM  ·  ⚡ 元气 {energy}%", 10.5f, Ink, true));
            _leftHost.Add(vital);

            // 右侧：最新一句话气泡 + 1大2小今日照片拼贴 + 底部迷你音乐条
            var quoteCard = GlassCard(_bentoHost, 16f);
            quoteCard.Add(L("❝ 小祈刚刚说：", 10f, Ink2, true));
            string lastSay = Str(state, "last_reply");
            if (string.IsNullOrEmpty(lastSay)) lastSay = Str(state, "mood");
            if (string.IsNullOrEmpty(lastSay) || lastSay.Length < 4) lastSay = "今天阳光软绵绵的，想陪你一起发呆~";
            var qTxt = WrapTxt(lastSay, 12.5f, Ink, true, 120);
            qTxt.style.marginTop = 6f * _k;
            quoteCard.Add(qTxt);

            var photoCard = GlassCard(_bentoHost, 14f);
            photoCard.Add(L("今日照片", 11f, Ink, true));
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.style.flexDirection = FlexDirection.Row; grid.style.marginTop = 10f * _k;
            var bigTile = MakePhotoTile("宿舍午后", "XqPhotoDorm", "", Color.white, 154f * _k, 174f * _k);
            bigTile.style.marginRight = 8f * _k;
            Click(bigTile, () => ShowPhotoPreview("宿舍午后", "XqPhotoDorm", "日常插画 · 静态展示素材", Color.white));
            var rightCol = new VisualElement { pickingMode = PickingMode.Ignore };
            var t1 = MakePhotoTile("认真记笔记", "XqPhotoStudy", "", Color.white, 130f * _k, 83f * _k);
            t1.style.marginBottom = 8f * _k;
            Click(t1, () => ShowPhotoPreview("认真记笔记", "XqPhotoStudy", "日常插画 · 静态展示素材", Color.white));
            var t2 = MakePhotoTile("开心比耶", "XqPhotoPeace", "", Color.white, 130f * _k, 83f * _k);
            Click(t2, () => ShowPhotoPreview("开心比耶", "XqPhotoPeace", "日常插画 · 静态展示素材", Color.white));
            rightCol.Add(t1); rightCol.Add(t2);
            grid.Add(bigTile); grid.Add(rightCol);
            photoCard.Add(grid);

            // 底部迷你音乐条
            var musicCard = GlassCard(_bentoHost, 12f);
            musicCard.style.flexDirection = FlexDirection.Row; musicCard.style.alignItems = Align.Center;
            musicCard.style.marginBottom = 0f;
            var cov = Box(42f * _k, 42f * _k, 10f * _k, new Color(.54f, .44f, .86f, .25f));
            cov.style.marginRight = 10f * _k; cov.style.flexShrink = 0f;
            var covImg = new Image { image = XqMusicBridge.Cover ?? Resources.Load<Texture2D>("XqMusicCover"), scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            covImg.style.width = covImg.style.height = Length.Percent(100); Round(covImg, 10f * _k); cov.Add(covImg);
            var mInfo = new VisualElement { pickingMode = PickingMode.Ignore };
            mInfo.style.flexGrow = 1f; mInfo.style.flexShrink = 1f; mInfo.style.overflow = Overflow.Hidden;
            var nowM = XqMusicBridge.Current;
            var mTitle = L(string.IsNullOrEmpty(nowM.title) ? "星夜的微光" : nowM.title, 11f, Ink, true);
            mTitle.style.whiteSpace = WhiteSpace.NoWrap; mTitle.style.overflow = Overflow.Hidden; mTitle.style.textOverflow = TextOverflow.Ellipsis;
            var mArt = L(string.IsNullOrEmpty(nowM.artist) ? "天宫祈 · 一起听" : nowM.artist, 9.5f, Ink2, false);
            mInfo.Add(mTitle); mInfo.Add(mArt);
            var ctrls = new VisualElement { pickingMode = PickingMode.Ignore };
            ctrls.style.flexDirection = FlexDirection.Row; ctrls.style.alignItems = Align.Center; ctrls.style.flexShrink = 0f;
            var bPrev = L("⏮", 12f, Ink, true); bPrev.style.marginRight = 8f * _k; bPrev.pickingMode = PickingMode.Position; Click(bPrev, () => XqMusicBridge.Prev());
            var bPlay = L(XqMusicBridge.IsPlaying ? "⏸" : "▶", 14f, new Color(.44f, .32f, .84f), true); bPlay.style.marginRight = 8f * _k; bPlay.pickingMode = PickingMode.Position; Click(bPlay, () => XqMusicBridge.TogglePlay());
            var bNext = L("⏭", 12f, Ink, true); bNext.pickingMode = PickingMode.Position; Click(bNext, () => XqMusicBridge.Next());
            ctrls.Add(bPrev); ctrls.Add(bPlay); ctrls.Add(bNext);
            musicCard.Add(cov); musicCard.Add(mInfo); musicCard.Add(ctrls);
            musicCard.schedule.Execute(() => {
                var c = XqMusicBridge.Current;
                mTitle.text = string.IsNullOrEmpty(c.title) ? "星夜的微光" : c.title;
                mArt.text = string.IsNullOrEmpty(c.artist) ? "天宫祈 · 一起听" : c.artist;
                bPlay.text = XqMusicBridge.IsPlaying ? "⏸" : "▶";
                if (XqMusicBridge.Cover != null) covImg.image = XqMusicBridge.Cover;
            }).Every(600);
        }

        void BuildApprovedLife(bool loading)
        {
            _pgMax = 0; // 禁用外层 PagePoll 滚轮，让右侧 ScrollView 独立单层滚动
            var life = _bd.Get("life");
            // 左侧上：深紫精致小月历卡片
            var cal = new VisualElement { pickingMode = PickingMode.Ignore };
            cal.style.backgroundColor = new Color(0.20f, 0.18f, 0.30f, 0.88f);
            Border(cal, 1.2f, new Color(1f, 1f, 1f, 0.45f));
            Round(cal, 18f * _k);
            cal.style.paddingLeft = cal.style.paddingRight = 16f * _k;
            cal.style.paddingTop = cal.style.paddingBottom = 14f * _k;
            cal.style.marginBottom = 14f * _k;
            var now = DateTime.Now;
            cal.Add(L(now.ToString("MMMM yyyy"), 14f, Color.white, true));
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
                var cellEl = Box(36f * _k, 24f * _k, 12f * _k, dayNum == now.Day ? new Color(0.56f, 0.42f, 0.96f, 0.95f) : Color.clear);
                cellEl.style.alignItems = Align.Center; cellEl.style.justifyContent = Justify.Center;
                if (dayNum >= 1) cellEl.Add(L(dayNum.ToString(), 10.5f, Color.white, dayNum == now.Day));
                row.Add(cellEl);
            }
            while (row != null && row.childCount < 7) row.Add(Box(36f * _k, 24f * _k, 0f, Color.clear));
            _leftHost.Add(cal);

            // 左侧下：今日随笔卡片
            var noteCard = GlassCard(_leftHost, 16f);
            noteCard.Add(L("Diary Note · 今日随笔", 11.5f, Ink, true));
            string diary = Str(life, "diary");
            if (string.IsNullOrEmpty(diary)) diary = Str(life, "place.doing");
            if (string.IsNullOrEmpty(diary)) diary = "今天也在认真生活，慢慢记录每个微小的瞬间~";
            var dTxt = WrapTxt(diary, 11.5f, Ink2, false, 160);
            dTxt.style.marginTop = 8f * _k;
            noteCard.Add(dTxt);

            // 右侧：唯一完整今日时间轴（独立单层 ScrollView）
            var tlCard = GlassCard(_bentoHost, 16f);
            tlCard.style.flexGrow = 1f; tlCard.style.marginBottom = 0f;
            tlCard.Add(L("今日时间轴", 14f, Ink, true));
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.style.flexGrow = 1f; sv.style.marginTop = 10f * _k;
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
                    var item = Box(-1, 34f * _k, 17f * _k, active ? new Color(0.54f, 0.42f, 0.94f, 0.88f) : new Color(1f, 1f, 1f, 0.55f));
                    item.style.flexDirection = FlexDirection.Row; item.style.alignItems = Align.Center;
                    item.style.paddingLeft = item.style.paddingRight = 14f * _k;
                    item.style.marginBottom = 8f * _k;
                    string txt = (active ? "● " : "") + Str(segments[i], "start") + "  " + Str(segments[i], "name");
                    var lbl = L(txt, 11f, active ? Color.white : Ink, active);
                    lbl.style.whiteSpace = WhiteSpace.NoWrap; lbl.style.overflow = Overflow.Hidden; lbl.style.textOverflow = TextOverflow.Ellipsis;
                    item.Add(lbl);
                    sv.Add(item);
                }
            }
            tlCard.Add(sv);
        }

        void BuildApprovedMemory(bool loading)
        {
            var memory = _bd.Get("memory"); var growth = _bd.Get("growth");
            var bond = GlassCard(_leftHost, 18f);
            bond.Add(L("羁绊与成长", 12f, Ink2, true));
            string days = Str(growth, "days");
            var bigDay = L("相伴第 " + (string.IsNullOrEmpty(days) ? "42" : days) + " 天", 24f, Ink, true);
            bigDay.style.marginTop = 8f * _k; bigDay.style.marginBottom = 16f * _k;
            bond.Add(bigDay);

            void AddBar(string title, int pct, Color fillCol)
            {
                var box = new VisualElement { pickingMode = PickingMode.Ignore };
                box.style.backgroundColor = new Color(1f, 1f, 1f, 0.65f); Round(box, 12f * _k);
                box.style.paddingLeft = box.style.paddingRight = 12f * _k;
                box.style.paddingTop = box.style.paddingBottom = 10f * _k;
                box.style.marginBottom = 10f * _k;
                box.Add(L($"{title} {pct}%", 11f, Ink, true));
                var trk = Box(-1, 8f * _k, 4f * _k, new Color(0.84f, 0.80f, 0.92f, 0.45f));
                trk.style.marginTop = 6f * _k;
                var fl = Box(-1, 8f * _k, 4f * _k, fillCol);
                fl.style.width = Length.Percent(Mathf.Clamp(pct, 5, 100));
                trk.Add(fl); box.Add(trk); bond.Add(box);
            }
            var st = _bd.Get("state");
            int closePct = Mathf.RoundToInt(Num(st, "closeness", 0.87f) * (Num(st, "closeness", 0.87f) <= 1f ? 100f : 1f));
            int secPct = Mathf.RoundToInt(Num(st, "security", 0.99f) * (Num(st, "security", 0.99f) <= 1f ? 100f : 1f));
            AddBar("亲密度", closePct, new Color(0.98f, 0.64f, 0.78f, 0.95f));
            AddBar("安全感", secPct, new Color(0.68f, 0.66f, 0.98f, 0.95f));

            var stageRow = new VisualElement { pickingMode = PickingMode.Ignore };
            stageRow.style.flexDirection = FlexDirection.Row; stageRow.style.alignItems = Align.Center;
            stageRow.style.justifyContent = Justify.SpaceBetween; stageRow.style.marginTop = 6f * _k;
            stageRow.Add(L("当前阶段", 11f, Ink2, false));
            var stagePill = Box(-1, 24f * _k, 12f * _k, new Color(0.86f, 0.82f, 0.98f, 0.78f));
            stagePill.style.paddingLeft = stagePill.style.paddingRight = 12f * _k; stagePill.style.justifyContent = Justify.Center;
            stagePill.Add(L("✦ 默契同频", 10.5f, Ink, true));
            stageRow.Add(stagePill); bond.Add(stageRow);

            var rHead = L("小祈记得的你", 14f, Ink, true);
            rHead.style.marginBottom = 10f * _k;
            rHead.style.textShadow = new TextShadow { blurRadius = 8f, color = Color.white };
            _bentoHost.Add(rHead);

            var files = Tk(memory, "files") as JArray;
            if (files == null || files.Count == 0)
            {
                var empty = GlassCard(_bentoHost, 16f);
                empty.Add(WrapTxt(loading ? "正在同步记忆…" : "还没有记忆记录", 11.5f, Ink2, false, 100));
            }
            else
            {
                _pgMax = Mathf.Max(0, files.Count - 3); int begin = Mathf.Clamp(_pgOff, 0, _pgMax);
                for (int i = begin; i < files.Count && i < begin + 3; i++)
                {
                    var note = GlassCard(_bentoHost, 14f);
                    var top = new VisualElement { pickingMode = PickingMode.Ignore };
                    top.style.flexDirection = FlexDirection.Row; top.style.justifyContent = Justify.SpaceBetween;
                    top.Add(L("📌 " + CleanMemTag(Str(files[i], "file")), 10.5f, new Color(.48f, .36f, .82f), true));
                    top.Add(L("★", 11f, new Color(.98f, .76f, .36f), true));
                    note.Add(top);
                    var body = WrapTxt(CleanMemBody(Str(files[i], "head"), Str(files[i], "body")), 11.5f, Ink, false, 120);
                    body.style.marginTop = 6f * _k;
                    note.Add(body);
                }
            }
        }

        void RefreshApprovedMusic() { }
        internal static void ReviewPage(int page) { if (_i != null && _i._shown) _i.SelectPage(page); }
    }
}