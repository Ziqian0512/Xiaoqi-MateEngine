using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V13 近景态完整视觉与交互集群：
    /// - 全屏背景：INORI 大字水印 + 横穿全屏 40 根毛玻璃动态音频频谱 + 右上角系统音频识别胶囊
    /// - 左上：通透蓝紫粉秒数时钟 + 天气穿衣叮嘱 + 主线胶囊
    /// - 左下：大专辑封面音乐卡（|< || >| ♥）
    /// - 人物四周：4 颗散落快捷表情按钮（脸红 / 比心 / 打盹 / 换姿势）+ 3 处无框颜文字点缀
    /// - 右侧错落：肩侧气泡（带 TTS 播报） / 圆环元气心跳综合卡 / 番茄专注计时器 / 摸头+看屏幕
    /// </summary>
    public partial class XqClosePanel
    {
        VisualElement _cardNext, _cardMusic, _cardEnergy, _cardSay, _cardTiny, _ringFocus;
        VisualElement _bodyNext, _bodyMusic, _bodyEnergy, _bodySay, _bodyTiny, _bodyRing;
        VisualElement _nextSchedCard, _remindPill, _nextPill, _actPat, _actScreen, _actDance;
        VisualElement _staminaTrack, _staminaFill, _tinyTicks;
        VisualElement _musicPrev, _musicPlay, _musicNext, _musicLike, _musicFill;
        VisualElement _sysAudioPill, _spectrumRow, _focusBtn, _sayTtsPill;
        VisualElement _exprBlush, _exprHeart, _exprSleep, _exprPose;
        readonly List<VisualElement> _spectrumBars = new List<VisualElement>();

        Label _clockBig, _locPill, _nextKicker, _nextTime, _nextWhat;
        Label _musicTitle, _musicArtist, _musicPlayLbl, _musicLikeLbl, _musicElapsed, _musicDuration;
        Image _musicCover;
        Label _actPatLbl, _actScreenLbl, _actDanceLbl;
        Label _sayText, _sayTtsLbl, _sysAudioLbl;
        Label _tinyChip, _tinyArrow, _tinyBig, _tinySummary;
        Label _ringHeart, _ringWord, _energyWord;
        Label _focusTimeLbl, _focusBtnLbl;

        XqMiniLine _tinySpark;
        XqArc _energyArc;
        bool _musicHooked;

        // 专注计时器状态
        bool _focusRunning;
        float _focusRemainSec = 25f * 60f;

        readonly List<Label> _kickers = new List<Label>();
        readonly List<Label> _marks = new List<Label>();
        readonly List<Label> _texts = new List<Label>();
        readonly List<Label> _bigs = new List<Label>();
        readonly List<VisualElement> _actionPills = new List<VisualElement>();

        static Color Rgb(int r, int g, int b, float a = 1f) { return new Color(r / 255f, g / 255f, b / 255f, a); }
        static readonly Color ClGlassWhite = Rgb(252, 250, 255, 0.93f);
        static readonly Color ClGlassPink = Rgb(255, 246, 252, 0.93f);
        static readonly Color ClGlassLav = Rgb(246, 241, 255, 0.94f);
        static readonly Color ClHeroGlass = Rgb(252, 249, 255, 0.94f);
        static readonly Color ClBorderSoft = Rgb(224, 208, 252, 0.96f);
        static readonly Color ClBorderLav = Rgb(216, 198, 250, 0.96f);
        static readonly Color ClViolet = Rgb(148, 108, 232, 0.98f);
        static readonly Color ClPink = Rgb(228, 122, 182, 0.98f);
        static readonly Color ClInk = Rgb(70, 48, 114, 1f);
        static readonly Color ClInkSoft = Rgb(134, 114, 172, 0.92f);

        void BuildCards()
        {
            _kickers.Clear(); _marks.Clear(); _texts.Clear(); _bigs.Clear();
            _actionPills.Clear(); _spectrumBars.Clear();
            XqLiquidGlass.ClearSlots();

            // 0b. 横穿全屏 160 根高密度粉色透明毛玻璃音频频谱柱（稍矮，留出更多呼吸空间）
            _spectrumRow = new VisualElement { name = "xq-close-spectrum", pickingMode = PickingMode.Ignore };
            _spectrumRow.style.position = Position.Absolute;
            _spectrumRow.style.left = Length.Percent(3f);
            _spectrumRow.style.right = Length.Percent(3f);
            _spectrumRow.style.bottom = Length.Percent(20f);
            _spectrumRow.style.height = Length.Percent(10.5f);
            _spectrumRow.style.flexDirection = FlexDirection.Row;
            _spectrumRow.style.alignItems = Align.FlexEnd;
            _spectrumRow.style.justifyContent = Justify.SpaceBetween;
            for (int i = 0; i < XqSysAudioBridge.BarCount; i++)
            {
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.style.width = 2.5f;
                bar.style.height = Length.Percent(10f);
                bar.style.backgroundColor = new Color(0.95f, 0.54f, 0.80f, 0.36f);
                SetRadius(bar, 1.5f);
                _spectrumBars.Add(bar);
                _spectrumRow.Add(bar);
            }
            _host.Add(_spectrumRow);

            // 0c. 无边框系统音频标注（放在声音 bar 的右下角）
            _sysAudioPill = new VisualElement { pickingMode = PickingMode.Ignore };
            _sysAudioPill.style.position = Position.Absolute;
            _sysAudioPill.style.right = Length.Percent(3f);
            _sysAudioPill.style.bottom = Length.Percent(16.8f);
            _sysAudioPill.style.backgroundColor = Color.clear;
            _sysAudioLbl = new Label("♫ 系统音频：待机中") { pickingMode = PickingMode.Ignore };
            _sysAudioLbl.style.color = new Color(0.38f, 0.24f, 0.62f, 0.88f);
            _sysAudioLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _kickers.Add(_sysAudioLbl);
            _sysAudioPill.Add(_sysAudioLbl);
            _host.Add(_sysAudioPill);

            // ② 左上：无外框印刷级深紫时钟 + 破框蝴蝶结 + 奶霜提醒条 + 正在做进度卡
            _cardNext = CardShell("xq-cl-card-next", Color.clear, Color.clear, 0f, 0f, "xq-close-card-next", out _bodyNext);
            var clockHalo = new TextShadow { offset = new Vector2(0f, 0f), blurRadius = 10f, color = new Color(1f, 1f, 1f, 0.95f) };
            _clockBig = new Label(DateTime.Now.ToString("HH:mm:ss")) { pickingMode = PickingMode.Ignore };
            _clockBig.style.color = Rgb(90, 62, 166, 1f);
            _clockBig.style.textShadow = clockHalo;
            _clockBig.style.unityFontStyleAndWeight = FontStyle.Bold;
            _clockBig.style.whiteSpace = WhiteSpace.NoWrap;
            _bodyNext.Add(_clockBig);
            XqLiquidGlass.AddBreakoutSticker(_cardNext, "XqStickerRibbon", 48f, 42f, 22f, -10f, 10f);

            _locPill = new Label("正在同步日期、地点与天气…") { pickingMode = PickingMode.Ignore };
            _locPill.style.color = Rgb(106, 82, 162, 0.96f);
            _locPill.style.textShadow = clockHalo;
            _locPill.style.unityFontStyleAndWeight = FontStyle.Bold;
            _locPill.style.whiteSpace = WhiteSpace.NoWrap;
            _locPill.style.marginTop = 2f;
            _locPill.style.marginBottom = 8f;
            _kickers.Add(_locPill);
            _bodyNext.Add(_locPill);

            _remindPill = new VisualElement { pickingMode = PickingMode.Ignore };
            _remindPill.style.backgroundColor = ClGlassPink;
            SetAllBorder(_remindPill, 1f, ClBorderSoft);
            SetRadius(_remindPill, 12f);
            _remindPill.style.paddingLeft = 10f; _remindPill.style.paddingRight = 10f;
            _remindPill.style.paddingTop = 5f; _remindPill.style.paddingBottom = 5f;
            _remindPill.style.marginBottom = 7f;
            var tipLbl = new Label("✦ 慢慢来，我在这里哦 (｡･ω･｡)") { pickingMode = PickingMode.Ignore };
            tipLbl.style.color = ClInk;
            tipLbl.style.whiteSpace = WhiteSpace.NoWrap;
            tipLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _kickers.Add(tipLbl);
            _remindPill.Add(tipLbl);
            _bodyNext.Add(_remindPill);
            XqLiquidGlass.ApplyAcrylicB(_remindPill);

            _nextPill = new VisualElement { pickingMode = PickingMode.Ignore };
            _nextPill.style.backgroundColor = ClGlassWhite;
            SetAllBorder(_nextPill, 1f, ClBorderSoft);
            _nextPill.style.flexDirection = FlexDirection.Column;
            var nextTop = new VisualElement { pickingMode = PickingMode.Ignore };
            nextTop.style.flexDirection = FlexDirection.Row;
            nextTop.style.alignItems = Align.Center;
            var nextDot = new Label("● 正在：") { pickingMode = PickingMode.Ignore };
            nextDot.style.color = ClViolet;
            nextDot.style.unityFontStyleAndWeight = FontStyle.Bold;
            nextDot.style.whiteSpace = WhiteSpace.NoWrap;
            _kickers.Add(nextDot);
            _nextTime = new Label("") { pickingMode = PickingMode.Ignore };
            _nextTime.style.color = ClViolet;
            _nextTime.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nextTime.style.whiteSpace = WhiteSpace.NoWrap;
            _nextTime.style.marginRight = 5f;
            _nextWhat = new Label("还没有主线安排") { pickingMode = PickingMode.Ignore };
            _nextWhat.style.color = ClInk;
            _nextWhat.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nextWhat.style.flexShrink = 1f;
            nextTop.Add(nextDot);
            nextTop.Add(_nextTime);
            nextTop.Add(_nextWhat);
            _nextPill.Add(nextTop);

            var nextTrack = new VisualElement { name = "xq-old-schedule-progress", pickingMode = PickingMode.Ignore };
            nextTrack.style.display = DisplayStyle.None;
            nextTrack.style.height = 4f;
            nextTrack.style.marginTop = 5f;
            nextTrack.style.backgroundColor = new Color(0.55f, 0.42f, 0.92f, 0.16f);
            SetRadius(nextTrack, 2f);
            var nextFill = new VisualElement { pickingMode = PickingMode.Ignore };
            nextFill.style.width = Length.Percent(64f);
            nextFill.style.height = Length.Percent(100f);
            nextFill.style.backgroundColor = new Color(0.62f, 0.42f, 0.96f, 0.82f);
            SetRadius(nextFill, 2f);
            nextTrack.Add(nextFill);
            _nextPill.Add(nextTrack);

            _bodyNext.Add(_nextPill);
            XqLiquidGlass.ApplyAcrylicB(_nextPill);
            Add("card-next", XqCloseLayout.NextL, XqCloseLayout.NextT, XqCloseLayout.NextW, XqCloseLayout.NextH);

            // ③ 左下：横向精致紫夜星空音乐卡（参考 starry nights 版式 + 破框小云朵贴纸）
            _cardMusic = CardShell("xq-cl-card-music", ClHeroGlass, ClBorderSoft, 1f, 0f, "xq-close-card-music", out _bodyMusic);
            XqLiquidGlass.AddBreakoutSticker(_cardMusic, "XqStickerCloud", 54f, 38f, -10f, -14f, 6f);
            XqLiquidGlass.ApplyAcrylicB(_bodyMusic);

            var mTop = new VisualElement { pickingMode = PickingMode.Ignore };
            mTop.style.flexDirection = FlexDirection.Row;
            mTop.style.alignItems = Align.Center;
            mTop.style.marginBottom = 8f;

            var mArt = new VisualElement { pickingMode = PickingMode.Ignore };
            mArt.style.width = 64f; mArt.style.height = 64f;
            mArt.style.flexShrink = 0f;
            mArt.style.marginRight = 10f;
            mArt.style.alignItems = Align.Center; mArt.style.justifyContent = Justify.Center;
            _musicCover = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            _musicCover.style.width = Length.Percent(100f); _musicCover.style.height = Length.Percent(100f);
            mArt.Add(_musicCover);
            mTop.Add(mArt);

            var mInfo = new VisualElement { pickingMode = PickingMode.Ignore };
            mInfo.style.flexGrow = 1f; mInfo.style.flexShrink = 1f;
            mInfo.style.overflow = Overflow.Hidden;
            mInfo.style.justifyContent = Justify.Center;
            _musicTitle = new Label(XqMusicBridge.Current.title) { pickingMode = PickingMode.Ignore };
            _musicTitle.style.color = ClInk;
            _musicTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _musicTitle.style.whiteSpace = WhiteSpace.NoWrap;
            _musicTitle.style.overflow = Overflow.Hidden;
            _musicTitle.style.textOverflow = TextOverflow.Ellipsis;
            _texts.Add(_musicTitle);
            mInfo.Add(_musicTitle);

            _musicArtist = new Label(XqMusicBridge.Current.artist) { pickingMode = PickingMode.Ignore };
            _musicArtist.style.color = ClInkSoft;
            _musicArtist.style.whiteSpace = WhiteSpace.NoWrap;
            _musicArtist.style.overflow = Overflow.Hidden;
            _musicArtist.style.textOverflow = TextOverflow.Ellipsis;
            _musicArtist.style.marginTop = 2f;
            _kickers.Add(_musicArtist);
            mInfo.Add(_musicArtist);

            var mSub = new Label("LISTENING TOGETHER / 一起听") { pickingMode = PickingMode.Ignore };
            mSub.style.color = ClPink;
            mSub.style.marginTop = 3f;
            _marks.Add(mSub);
            mInfo.Add(mSub);
            mTop.Add(mInfo);
            _bodyMusic.Add(mTop);

            var mTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            mTrack.style.height = 4f;
            mTrack.style.backgroundColor = Rgb(232, 222, 252, 0.95f);
            SetRadius(mTrack, 2f);
            mTrack.style.marginBottom = 4f;
            _musicFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _musicFill.style.height = Length.Percent(100f);
            _musicFill.style.width = Length.Percent(XqMusicBridge.Progress * 100f);
            _musicFill.style.backgroundColor = ClViolet;
            SetRadius(_musicFill, 2f);
            mTrack.Add(_musicFill);
            _bodyMusic.Add(mTrack);

            var mTimes = new VisualElement { pickingMode = PickingMode.Ignore };
            mTimes.style.flexDirection = FlexDirection.Row;
            mTimes.style.justifyContent = Justify.SpaceBetween;
            mTimes.style.marginBottom = 6f;
            var tL = new Label("--:--") { pickingMode = PickingMode.Ignore }; tL.style.color = ClInkSoft; _marks.Add(tL);
            var tR = new Label("未接入播放") { pickingMode = PickingMode.Ignore }; tR.style.color = ClInkSoft; _marks.Add(tR);
            _musicElapsed = tL; _musicDuration = tR;
            mTimes.Add(tL); mTimes.Add(tR);
            _bodyMusic.Add(mTimes);

            var mCtrls = new VisualElement { pickingMode = PickingMode.Ignore };
            mCtrls.style.flexDirection = FlexDirection.Row;
            mCtrls.style.alignItems = Align.Center;
            mCtrls.style.justifyContent = Justify.Center;
            _musicPrev = MakeMusicBtn("|<", false, out _);
            _musicPrev.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); XqMusicBridge.Prev(); RefreshMusicCard(); });
            _musicPlay = MakeMusicBtn(XqMusicBridge.IsPlaying ? "||" : "▶", true, out _musicPlayLbl);
            _musicPlay.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); XqMusicBridge.TogglePlay(); RefreshMusicCard(); });
            _musicNext = MakeMusicBtn(">|", false, out _);
            _musicNext.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); XqMusicBridge.Next(); RefreshMusicCard(); });
            _musicLike = MakeMusicBtn(XqMusicBridge.Current.liked ? "♥" : "♡", false, out _musicLikeLbl);
            _musicLike.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); XqMusicBridge.ToggleLike(); RefreshMusicCard(); });
            _musicLike.SetEnabled(false); _musicLike.tooltip = "收藏请在播放器中操作";
            mCtrls.Add(_musicPrev); mCtrls.Add(_musicPlay); mCtrls.Add(_musicNext); mCtrls.Add(_musicLike);
            _bodyMusic.Add(mCtrls);
            Add("card-music", XqCloseLayout.MusicL, XqCloseLayout.MusicT, XqCloseLayout.MusicW, XqCloseLayout.MusicH);
            if (!_musicHooked) { _musicHooked = true; XqMusicBridge.OnChanged += RefreshMusicCard; }

            // ⑤ 右上（靠内）：@Amamiya_Inori 肩侧气泡
            _cardSay = CardShell("xq-cl-card-say", ClGlassPink, ClBorderSoft, 1.5f, 0f, "xq-close-card-say", out _bodySay);
            XqLiquidGlass.ApplyAcrylicB(_bodySay);
            var rpHead = new VisualElement { pickingMode = PickingMode.Ignore };
            rpHead.style.flexDirection = FlexDirection.Row;
            rpHead.style.alignItems = Align.Center;
            rpHead.style.justifyContent = Justify.SpaceBetween;
            rpHead.style.marginBottom = 4f;
            var rpId = new Label("✦ @Amamiya_Inori · 自己人") { pickingMode = PickingMode.Ignore };
            rpId.style.color = ClViolet;
            rpId.style.unityFontStyleAndWeight = FontStyle.Bold;
            _kickers.Add(rpId);
            _sayTtsPill = new VisualElement { pickingMode = PickingMode.Position };
            _sayTtsPill.style.backgroundColor = new Color(0.62f, 0.42f, 0.96f, 0.14f);
            _sayTtsPill.style.paddingLeft = 8f; _sayTtsPill.style.paddingRight = 8f;
            _sayTtsPill.style.paddingTop = 2f; _sayTtsPill.style.paddingBottom = 2f;
            SetRadius(_sayTtsPill, 8f);
            _sayTtsPill.style.flexShrink = 0f;
            _sayTtsLbl = new Label("♪ 播报") { pickingMode = PickingMode.Ignore };
            _sayTtsLbl.style.color = ClPink;
            _sayTtsLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _sayTtsLbl.style.whiteSpace = WhiteSpace.NoWrap;
            _kickers.Add(_sayTtsLbl);
            _sayTtsPill.Add(_sayTtsLbl);
            _sayTtsPill.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                string txt = !string.IsNullOrEmpty(XqVoiceChatClient.LatestReplyFull) ? XqVoiceChatClient.LatestReplyFull : _sayText.text;
                XqVoiceChatClient.Ensure().PlayTts(txt);
            });
            rpHead.Add(rpId); rpHead.Add(_sayTtsPill);
            _bodySay.Add(rpHead);

            _sayText = new Label("等待新的消息…") { pickingMode = PickingMode.Ignore };
            _sayText.style.color = ClInk;
            _sayText.style.unityFontStyleAndWeight = FontStyle.Bold;
            _sayText.style.whiteSpace = WhiteSpace.Normal;
            _texts.Add(_sayText);
            _bodySay.Add(_sayText);
            Add("card-say", XqCloseLayout.SayL, XqCloseLayout.SayT, XqCloseLayout.SayW, XqCloseLayout.SayH);

            // ⑥ 右中（向外拉出）：圆环元气电量 + 实时心跳 + 黏人象限
            _cardTiny = CardShell("xq-cl-card-tiny", ClGlassWhite, ClBorderSoft, 1.5f, 0f, "xq-close-card-tiny", out _bodyTiny);
            XqLiquidGlass.ApplyAcrylicB(_bodyTiny);
            var vitalRow = new VisualElement { pickingMode = PickingMode.Ignore };
            vitalRow.style.flexDirection = FlexDirection.Row;
            vitalRow.style.alignItems = Align.Center;
            vitalRow.style.flexGrow = 1f;

            var ringBox = new VisualElement { pickingMode = PickingMode.Ignore };
            ringBox.style.width = 58f; ringBox.style.height = 58f;
            ringBox.style.alignItems = Align.Center; ringBox.style.justifyContent = Justify.Center;
            ringBox.style.marginRight = 12f;
            _energyArc = new XqArc { Amount = 0.87f };
            _energyArc.style.position = Position.Absolute;
            _energyArc.style.left = 0; _energyArc.style.top = 0; _energyArc.style.right = 0; _energyArc.style.bottom = 0;
            _tinyBig = new Label("87%") { pickingMode = PickingMode.Ignore };
            _tinyBig.style.color = ClViolet;
            _tinyBig.style.unityFontStyleAndWeight = FontStyle.Bold;
            _texts.Add(_tinyBig);
            ringBox.Add(_energyArc); ringBox.Add(_tinyBig);
            vitalRow.Add(ringBox);

            var statCol = new VisualElement { pickingMode = PickingMode.Ignore };
            statCol.style.flexGrow = 1f;
            _tinyChip = new Label("⚡ 今日元气电量 · 87%") { pickingMode = PickingMode.Ignore };
            _tinyChip.style.color = ClInk; _tinyChip.style.unityFontStyleAndWeight = FontStyle.Bold;
            _texts.Add(_tinyChip);
            _ringWord = new Label("♥ 实时心跳 75 BPM") { pickingMode = PickingMode.Ignore };
            _ringWord.style.color = ClViolet; _ringWord.style.unityFontStyleAndWeight = FontStyle.Bold;
            _ringWord.style.marginTop = 4f;
            _kickers.Add(_ringWord);
            _tinySummary = new Label("💖 黏人象限 · 亲密 87%") { pickingMode = PickingMode.Ignore };
            _tinySummary.style.color = ClPink; _tinySummary.style.unityFontStyleAndWeight = FontStyle.Bold;
            _tinySummary.style.marginTop = 4f;
            _kickers.Add(_tinySummary);
            statCol.Add(_tinyChip); statCol.Add(_ringWord); statCol.Add(_tinySummary);
            vitalRow.Add(statCol);
            _bodyTiny.Add(vitalRow);
            Add("card-tiny", XqCloseLayout.TinyL, XqCloseLayout.TinyT, XqCloseLayout.TinyW, XqCloseLayout.TinyH);

            // ⑦ 右下拆分为两个错落独立模块：摸摸她的头 + 看我屏幕（统一方案 B 预渲染亚克力）
            _ringFocus = new VisualElement { name = "xq-close-card-ring", pickingMode = PickingMode.Ignore };
            _ringFocus.AddToClassList("xq-cl-card");
            XqLiquidGlass.AddSoftShadow(_ringFocus, 14f);
            _actPat = MakeActionPill("♡ 摸摸她的头", ClGlassPink, ClInk, ClBorderSoft, 0f, out _actPatLbl);
            _actPat.style.flexGrow = 1f;
            _ringFocus.Add(_actPat);
            XqLiquidGlass.ApplyAcrylicB(_actPat);
            Add("card-ring", XqCloseLayout.RingL, XqCloseLayout.RingT, XqCloseLayout.RingW, XqCloseLayout.RingH);

            _cardEnergy = new VisualElement { name = "xq-close-card-energy", pickingMode = PickingMode.Ignore };
            _cardEnergy.AddToClassList("xq-cl-card");
            XqLiquidGlass.AddSoftShadow(_cardEnergy, 14f);
            _actScreen = MakeActionPill("✦ 看我屏幕", ClGlassWhite, ClInk, ClBorderSoft, 0f, out _actScreenLbl);
            _actScreen.style.flexGrow = 1f;
            _cardEnergy.Add(_actScreen);
            XqLiquidGlass.ApplyAcrylicB(_actScreen);
            Add("card-energy", XqCloseLayout.EnergyL, XqCloseLayout.EnergyT, XqCloseLayout.EnergyW, XqCloseLayout.EnergyH);

            _host.Add(_cardNext);
            _host.Add(_cardMusic);
            _host.Add(_cardSay);
            _host.Add(_cardTiny);
            _host.Add(_ringFocus);
            _host.Add(_cardEnergy);

            // 四散在人物周围的 4 颗快捷表情按钮 + 无框颜文字（绝不与右侧卡片重叠）
            _exprBlush = MakeScatterExpr("🌸 脸红", 29f, 24f, -3f, () => TriggerExpr("害羞", 0.75f, 0.55f));
            _exprHeart = MakeScatterExpr("☺ 开心", 26f, 48f, 2f, () => TriggerExpr("开心", 0.90f, 0.75f));
            _exprSleep = MakeScatterExpr("😪 打盹", 61f, 32f, 3f, () => TriggerExpr("困", 0.05f, 0.10f));
            _exprPose = MakeScatterExpr("✨ 换姿势", 61f, 48f, -2f, () => { Xiaoqi.Host.PetHost.I?.RunAction("pose_next"); });

            AddFreeKaomoji("^v^ ✦", 35f, 17f, -7f, ClViolet);
            AddFreeKaomoji("(>w<)♡", 62f, 22f, 6f, ClPink);
            AddFreeKaomoji("✿ (´∀｀)", 60f, 58f, -4f, ClPink);
        }

        VisualElement MakeScatterExpr(string text, float leftPct, float topPct, float deg, Action onClick)
        {
            var pill = new VisualElement { pickingMode = PickingMode.Position };
            pill.style.position = Position.Absolute;
            pill.style.left = Length.Percent(leftPct);
            pill.style.top = Length.Percent(topPct);
            pill.style.backgroundColor = ClGlassWhite;
            SetAllBorder(pill, 1f, ClBorderSoft);
            pill.style.paddingLeft = 12f; pill.style.paddingRight = 12f;
            pill.style.paddingTop = 6f; pill.style.paddingBottom = 6f;
            SetRadius(pill, 13f);
            pill.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(deg, AngleUnit.Degree));
            var lbl = new Label(text) { pickingMode = PickingMode.Ignore };
            lbl.style.color = ClInk;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _texts.Add(lbl);
            pill.Add(lbl);
            pill.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); onClick(); });
            _host.Add(pill);
            return pill;
        }

        void AddFreeKaomoji(string text, float leftPct, float topPct, float deg, Color col)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.position = Position.Absolute;
            l.style.left = Length.Percent(leftPct);
            l.style.top = Length.Percent(topPct);
            l.style.color = col;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(deg, AngleUnit.Degree));
            _texts.Add(l);
            _host.Add(l);
        }

        static void TriggerExpr(string spike, float joy, float fun)
        {
            Xiaoqi.Spike.XiaoqiFace.React(joy, fun);
        }

        void ToggleFocusTimer()
        {
            _focusRunning = !_focusRunning;
            if (!_focusRunning && _focusRemainSec <= 1f) _focusRemainSec = 25f * 60f;
            if (_focusBtnLbl != null) _focusBtnLbl.text = _focusRunning ? "⏸ 暂停" : "▶ 开始专注";
        }

        VisualElement MakeMusicBtn(string icon, bool primary, out Label lbl)
        {
            var b = new VisualElement { pickingMode = PickingMode.Position };
            b.style.width = primary ? 32f : 26f;
            b.style.height = primary ? 32f : 26f;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            b.style.marginLeft = 6f; b.style.marginRight = 6f;
            SetRadius(b, primary ? 16f : 10f);
            b.style.backgroundColor = primary ? ClViolet : Rgb(240, 232, 255, 0.85f);
            lbl = new Label(icon) { pickingMode = PickingMode.Ignore };
            lbl.style.color = primary ? Color.white : ClViolet;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _kickers.Add(lbl);
            b.Add(lbl);
            return b;
        }

        static string tDuration() { return XqMusicBridge.Current.durationSec > 0f ? XqMusicBridge.Clock(XqMusicBridge.Current.durationSec) : "--:--"; }
        void RefreshMusicCard()
        {
            if (_musicTitle == null) return;
            _musicPrev.SetEnabled(XqMusicBridge.CanPrev); _musicPlay.SetEnabled(XqMusicBridge.CanToggle); _musicNext.SetEnabled(XqMusicBridge.CanNext);
            if (_musicCover != null) _musicCover.image = XqMusicBridge.Cover;
            if (_musicElapsed != null) _musicElapsed.text = XqMusicBridge.Available && XqMusicBridge.Current.durationSec > 0f ? XqMusicBridge.Clock(XqMusicBridge.PositionSec) : "--:--";
            if (_musicDuration != null) _musicDuration.text = tDuration();
            var t = XqMusicBridge.Current;
            _musicTitle.text = t.title;
            if (_musicArtist != null) _musicArtist.text = t.artist;
            if (_musicPlayLbl != null) _musicPlayLbl.text = XqMusicBridge.IsPlaying ? "||" : "▶";
            if (_musicLikeLbl != null) _musicLikeLbl.text = t.liked ? "♥" : "♡";
            if (_musicFill != null) _musicFill.style.width = Length.Percent(Mathf.Clamp01(XqMusicBridge.Progress) * 100f);
        }

        VisualElement MakeActionPill(string label, Color bg, Color fg, Color border, float deg, out Label lbl)
        {
            var pill = new VisualElement { pickingMode = PickingMode.Position };
            pill.style.backgroundColor = bg;
            SetAllBorder(pill, 1.5f, border);
            pill.style.justifyContent = Justify.Center;
            pill.style.alignItems = Align.Center;
            if (Mathf.Abs(deg) > 0.01f) pill.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(deg, AngleUnit.Degree));
            lbl = new Label(label) { pickingMode = PickingMode.Ignore };
            lbl.style.color = fg;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _texts.Add(lbl);
            pill.Add(lbl);
            _actionPills.Add(pill);
            return pill;
        }

        static void SetAllBorder(VisualElement el, float w, Color c)
        {
            el.style.borderLeftWidth = w; el.style.borderRightWidth = w;
            el.style.borderTopWidth = w; el.style.borderBottomWidth = w;
            el.style.borderLeftColor = c; el.style.borderRightColor = c;
            el.style.borderTopColor = c; el.style.borderBottomColor = c;
        }

        VisualElement CardShell(string cls, Color bg, Color border, float borderW, float deg, string name, out VisualElement body)
        {
            var host = new VisualElement { name = name, pickingMode = PickingMode.Position };
            host.AddToClassList("xq-cl-card");
            if (!string.IsNullOrEmpty(cls)) host.AddToClassList(cls);

            body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("xq-cl-card-body");
            body.style.flexGrow = 1f;
            body.style.backgroundColor = bg;
            if (bg.a > 0.05f) XqLiquidGlass.AddSoftShadow(host, 18f);
            SetAllBorder(body, borderW > 0f ? 1f : 0f, border);
            if (Mathf.Abs(deg) > 0.01f) body.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(deg, AngleUnit.Degree));
            host.Add(body);
            return host;
        }

        void PlaceCards(float u)
        {
            float padX = 12f * u, padY = 10f * u;
            PlaceBody(_bodyNext, 18f * u, padX, padY);
            PlaceBody(_bodyMusic, 18f * u, padX, padY);
            PlaceBody(_bodySay, 16f * u, padX, padY);
            PlaceBody(_bodyTiny, 16f * u, padX, padY);
            PlaceBody(_bodyEnergy, 16f * u, padX, padY);

            if (_clockBig != null) _clockBig.style.fontSize = 27f * u;
            if (_nextPill != null)
            {
                SetRadius(_nextPill, 12f * u);
                _nextPill.style.paddingLeft = 10f * u; _nextPill.style.paddingRight = 10f * u;
                _nextPill.style.paddingTop = 6f * u; _nextPill.style.paddingBottom = 6f * u;
            }
            if (_nextTime != null) _nextTime.style.fontSize = 9.8f * u;
            if (_nextWhat != null) _nextWhat.style.fontSize = 9.8f * u;

            for (int i = 0; i < _actionPills.Count; i++)
            {
                var p = _actionPills[i];
                SetRadius(p, 14f * u);
                p.style.paddingLeft = 10f * u; p.style.paddingRight = 10f * u;
                p.style.paddingTop = 7f * u; p.style.paddingBottom = 7f * u;
            }

            foreach (var l in _kickers) l.style.fontSize = 9.6f * u;
            foreach (var l in _marks) l.style.fontSize = 8.8f * u;
            foreach (var l in _texts) l.style.fontSize = 10.6f * u;
            foreach (var l in _bigs) l.style.fontSize = 18f * u;
            RefreshMusicCard();
        }

        static void PlaceBody(VisualElement body, float r, float padX, float padY)
        {
            if (body == null) return;
            SetRadius(body, r);
            body.style.paddingLeft = padX; body.style.paddingRight = padX;
            body.style.paddingTop = padY; body.style.paddingBottom = padY;
        }
    }
}
