using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Spike;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 梦幻粉紫工作台 V5 五页内容（0 此刻首页 / 1 手账 / 2 记忆 / 3 成长 / 4 Brain 轻控）：
    /// - 左右双翼松弛布局（左翼 _leftHost 270px，右翼 _bentoHost 416px），中央全让位给半身舞台
    /// - 全部数据直连 XqBenchData 并发+SWR缓存，记忆页彻底清洗英文 .md 文件名与 YAML 头
    /// </summary>
    public partial class XqBenchPanel
    {
        static readonly string[] PageName = { "此刻首页", "生活手账", "日常相册", "成长轨迹", "陪伴控制", "偏好设置" };
        const string HomeToken = "hk-rpH2Hj8G0IHQBlrx";
        const string OfficeToken = "of-w0_ioGzw2HeX4OSe";

        Label _benchMusicTitle, _benchMusicArtist, _benchMusicPlayLabel;
        VisualElement _benchMusicButton, _benchMusicFill;
        Image _benchMusicCover;
        int _page;
        int _pgOff;
        int _pgMax;
        float _pgFade = 1f;
        XqBenchData _bd;

        string _selNeed = "";
        int _selMem = -1;
        string _brainStatus = "";

        // ---------------- 生命周期与导航 ----------------

        void PageShow()
        {
            _bd = XqBenchData.Ensure();
            _bd.Changed -= OnBenchData; _bd.Changed += OnBenchData;
            _bd.PrefetchAll();
            _bd.Open(_page);
            if (_page == 1 && _pgOff == 0) _pgOff = -1;
            _pgFade = 0f;
            SyncCharFace(_page);
            RenderPage();
            ApplyNavSel();
        }

        void PageHide()
        {
            if (_bd != null) { _bd.Changed -= OnBenchData; _bd.Stop(); }
        }

        void RefreshBenchMusic()
        {
            if (_page != 0 || _benchMusicTitle == null) return;
            var track = XqMusicBridge.Current;
            _benchMusicTitle.text = track.title;
            _benchMusicArtist.text = track.artist;
            _benchMusicButton.SetEnabled(XqMusicBridge.CanToggle);
            _benchMusicButton.style.opacity = XqMusicBridge.CanToggle ? 1f : 0.4f;
            _benchMusicPlayLabel.text = XqMusicBridge.IsPlaying ? "||" : "▶";
            _benchMusicFill.style.width = Length.Percent(Mathf.Clamp01(XqMusicBridge.Progress) * 100f);
            _benchMusicCover.image = XqMusicBridge.Cover;
        }

        void OnBenchData()
        {
            if (!_shown || _bd == null) return;
            RenderPage();
        }

        void PageTick(float dt)
        {
            _pgFade = Mathf.MoveTowards(_pgFade, 1f, dt * 6f);
            RefreshBenchMusic();
        }

        void SelectPage(int p)
        {
            if (p < 0 || p > 5) return;
            if (p == _page) { if (_bd != null) _bd.Refresh(); return; }
            _page = p; _pgOff = (p == 1) ? -1 : 0; _pgFade = 0f; _selMem = -1;
            Debug.Log("XQ_BENCH page=" + p + " " + PageName[p]);
            if (_bd == null) _bd = XqBenchData.Ensure();
            _bd.Open(p);
            SyncCharFace(p);
            ApplyNavSel();
            RenderPage();
        }

        void ApplyNavSel()
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            for (int i = 0; i < _nav.Count; i++)
            {
                bool on = i == _page;
                _nav[i].style.backgroundColor = on ? accDeep : Rgb(244, 236, 255, 0.78f);
                Border(_nav[i], 1.2f, on ? Color.white : Rgb(255, 255, 255, 0.85f));
                if (i < _navGlyph.Count) { _navGlyph[i].Stroke = on ? Color.white : Ink2; _navGlyph[i].MarkDirtyRepaint(); }
                var lb = _nav[i].Q<Label>();
                if (lb != null) { lb.style.color = on ? Color.white : Ink; lb.style.unityFontStyleAndWeight = on ? FontStyle.Bold : FontStyle.Normal; }
            }
        }

        bool PagePoll(bool clickDown)
        {
            if (!_shown || _host == null || _host.panel == null) return false;
            float sc = Input.mouseScrollDelta.y;
            if (_pgMax > 0 && Mathf.Abs(sc) > 0.01f && (OverEl(_bentoHost) || OverEl(_leftHost)))
            {
                int next = Mathf.Clamp(_pgOff + (sc < 0f ? 1 : -1), 0, _pgMax);
                if (next != _pgOff) { _pgOff = next; RenderPage(); }
            }
            return false;
        }

        // ---------------- 人物表情联动与顶部信息同步 ----------------

        void SyncCharFace(int page)
        {
            if (PetHost.I == null || !PetHost.I.Ready) return;
            if (page == 1)
            {
                XiaoqiFace.DebugSpike("平静");
                PetHost.I.SetFace(new FaceFrame { lookDown = 0.45f, joy = 0.15f });
            }
            else if (page == 2)
            {
                XiaoqiFace.DebugSpike("想念");
                PetHost.I.SetFace(new FaceFrame { joy = 0.35f, fun = 0.2f });
            }
            else if (page == 3)
            {
                XiaoqiFace.DebugSpike("开心");
                PetHost.I.SetFace(new FaceFrame { joy = 0.7f, fun = 0.35f });
            }
            else
            {
                XiaoqiFace.DebugSpike("开心");
                PetHost.I.SetFace(new FaceFrame { joy = 0.3f });
            }
        }

        void PatInsideCharBox()
        {
            XiaoqiFace.DebugSpike("害羞");
            if (PetHost.I != null && PetHost.I.Ready)
                PetHost.I.SetFace(new FaceFrame { joy = 0.75f, fun = 0.55f, blink = 0.25f });
            Toast("唔…干嘛突然摸头啦");
            Debug.Log("XQ_BENCH pat-charbox");
        }

        void RefreshCharBox()
        {
            if (_topClock != null) _topClock.text = DateTime.Now.ToString("HH:mm");
            if (_charTopText == null || _charBotText == null) return;
            var st = _bd != null ? _bd.Get("state") : null;
            var lf = _bd != null ? _bd.Get("life") : null;
            var cur = XiaoqiLink.Current;

            string place = Str(st, "me.place");
            if (place.Length == 0) place = Str(lf, "place.where");
            if (place.Length == 0 && cur != null) place = cur.where ?? "";
            string wx = "";
            var w = Tk(lf, "weather");
            if (w != null && w.Type == JTokenType.Object)
                wx = Str(w, "desc") + " " + Num(w, "temp", 0f).ToString("0.#") + "°C";
            _charTopText.text = Join(" · ", DateTime.Now.ToString("ddd MM/dd").ToUpperInvariant(), place.Length > 0 ? place : "薄扶林宿舍", wx);

            int bpm = (int)Num(st, "me.bpm", -1f);
            string quad = Str(st, "affect.quadrant");
            string mood = Str(st, "mood");
            _charBotText.text = "♥ " + (bpm >= 0 ? bpm + " BPM" : "-- BPM")
                + (quad.Length > 0 ? " · 【" + quad + "】" : (mood.Length > 0 ? " · " + mood : ""));
        }

        static bool IsOfficeMode()
        {
            var cfg = XiaoqiRuntime.Config;
            return cfg != null && cfg.token == OfficeToken;
        }

        void ToggleSceneMode()
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            if (cfg == null) return;
            bool toOffice = cfg.token != OfficeToken;
            cfg.token = toOffice ? OfficeToken : HomeToken;
            cfg.bubble = !toOffice;
            XiaoqiConfig.Save(cfg);
            if (_modeBtnLabel != null) _modeBtnLabel.text = toOffice ? "💼 办公模式" : "🏠 居家模式";
            Toast(toOffice ? "已切办公防社死模式（隐藏私聊气泡）" : "已切居家模式（开启私聊气泡）");
            RenderPage();
        }

        // ---------------- 页面总渲染 ----------------

        void RenderPage()
        {
            if (_bentoHost == null || _leftHost == null) return;
            _leftHost.Clear();
            _bentoHost.Clear();
            _pageClicks.Clear();
            _pgMax = 0;
            if (_bd == null) _bd = XqBenchData.Ensure();

            RefreshCharBox();
            bool loading = _bd.Loading(_page);
            string stampKey = XqBenchData.KeysOf(_page).Length > 0 ? XqBenchData.KeysOf(_page)[0] : "state";
            var st = _bd.StampOf(stampKey);
            string when = st > DateTime.MinValue ? st.ToString("HH:mm:ss") : "--";
            if (_metaLabel != null)
                _metaLabel.text = PageName[_page] + " · " + (loading ? "同步中…" : (_bd.Offline(_page) ? "缓存 " + when : "已同步 " + when));

            try
            {
                switch (_page)
                {
                    case 0: BuildNowPage(loading); break;
                    case 1: BuildLifePage(loading); break;
                    case 2: BuildMemPage(loading); break;
                    case 3: BuildGrowthPage(loading); break;
                    case 4: BuildBrainPage(); break;
                    case 5: BuildSettingsBenchPage(); break;
                }
            }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH render page " + _page + ": " + e.Message); }
            _pgOff = Mathf.Clamp(_pgOff, 0, _pgMax);
        }

        // ---------------- 0. 此刻首页（V5 松弛有度首页：左主线轻控 + 右绝区零三卡 + 干净手账精选）----------------

        void BuildNowPage(bool loading)
        {
            var st = _bd.Get("state");
            var lf = _bd.Get("life");
            var mm = _bd.Get("memory");
            var dy = _bd.Get("diary");
            var gr = _bd.Get("growth");
            if (st == null && lf == null) { LoadingOrEmpty("此刻", loading); return; }

            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            // ==== 左翼（不重复时间：紧凑 5 条日程 + 上图下播大专辑音乐卡）====
            var cSched = Card(_leftHost, 244f, false);
            var sh = RowFlex(cSched);
            sh.Add(L("📅 今日 5 件小事", 11.5f, Ink, true));
            sh.Add(L("全部 ↗", 10f, accDeep, true));
            cSched.Add(Box(1, 4f, 0, Color.clear));
            var segs = Tk(lf, "segments") as JArray;
            var picks = PickMainSegs(segs, 5);
            if (picks.Count > 0)
            {
                int nowMin = NowMin();
                foreach (var s in picks)
                {
                    int a = (int)Num(s, "a", 0f), b = (int)Num(s, "b", 0f);
                    bool isNow = a <= nowMin && nowMin < Mathf.Max(b, a + 1);
                    var row = Box(-1, 32f * _k, 10f * _k, isNow ? accDeep : Rgb(245, 239, 255, 0.88f));
                    row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                    row.style.paddingLeft = row.style.paddingRight = 10f * _k;
                    row.style.marginBottom = 5f * _k;
                    var tm = L((isNow ? "● " : "") + Str(s, "start"), 10f, isNow ? Color.white : Ink2, true);
                    tm.style.marginRight = 8f * _k;
                    var nm = L(Cut(Str(s, "name"), 14), 10.5f, isNow ? Color.white : Ink, isNow);
                    row.Add(tm); row.Add(nm);
                    cSched.Add(row);
                }
            }
            else
            {
                cSched.Add(WrapTxt(Str(st, "activity").Length > 0 ? Str(st, "activity") : "此刻正安静陪在你身边", 11.5f, Ink, true, 24));
            }
            Click(cSched, () => SelectPage(1));

            // 上图下播大专辑音乐卡
            var cMusic = Card(_leftHost, 224f, false);
            cMusic.style.justifyContent = Justify.SpaceBetween;
            var mCover = Box(-1, 118f * _k, 14f * _k, Rgb(236, 224, 255, 0.95f));
            Border(mCover, 1.5f * _k, Color.white);
            mCover.style.alignItems = Align.Center; mCover.style.justifyContent = Justify.Center;
            _benchMusicCover = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            _benchMusicCover.style.width = Length.Percent(88f); _benchMusicCover.style.height = Length.Percent(82f);
            mCover.Add(_benchMusicCover);
            cMusic.Add(mCover);

            var mCtrlRow = new VisualElement { pickingMode = PickingMode.Ignore };
            mCtrlRow.style.flexDirection = FlexDirection.Row;
            mCtrlRow.style.alignItems = Align.Center;
            mCtrlRow.style.justifyContent = Justify.SpaceBetween;
            var trk = XqMusicBridge.Current;
            var mInfo = new VisualElement { pickingMode = PickingMode.Ignore };
            mInfo.style.flexShrink = 1f;
            _benchMusicTitle = L(trk.title, 11.5f, Ink, true); mInfo.Add(_benchMusicTitle);
            _benchMusicArtist = L(trk.artist, 9.5f, Ink2, false); mInfo.Add(_benchMusicArtist);
            var mBtn = Box(32f * _k, 32f * _k, 16f * _k, acc);
            mBtn.style.alignItems = Align.Center; mBtn.style.justifyContent = Justify.Center;
            _benchMusicPlayLabel = L(XqMusicBridge.IsPlaying ? "||" : "▶", 12f, Color.white, true); mBtn.Add(_benchMusicPlayLabel);
            _benchMusicButton = mBtn;
            Click(mBtn, () => XqMusicBridge.TogglePlay());
            mCtrlRow.Add(mInfo); mCtrlRow.Add(mBtn);
            cMusic.Add(mCtrlRow);

            var mBar = Box(-1, 5f * _k, 2.5f * _k, Rgb(232, 224, 255, 0.9f));
            var mFill = Box(-1, 5f * _k, 2.5f * _k, acc);
            mFill.style.width = Length.Percent(Mathf.Clamp01(XqMusicBridge.Progress) * 100f);
            _benchMusicFill = mFill;
            mBar.Add(mFill);
            cMusic.Add(mBar);

            // ==== 右翼（V11：刚刚说话 + 移至右侧的元气电量心跳 + 日常照片墙 + 互动双钮）====
            var cSay = Card(_bentoHost, 72f, false);
            string say = Str(st, "me.say");
            if (say.Length == 0) say = Str(st, "line");
            if (say.Length == 0) say = XiaoqiFeed.LatestSay ?? "";
            cSay.Add(L("💬 小祈 · 刚刚说 (^v^)", 9.8f, accDeep, true));
            cSay.Add(WrapTxt(say.Length > 0 ? "“" + say + "”" : "正在悄悄看着画框外的你呀~", 11.5f, Ink, true, 54));

            // 移至右侧的圆环元气电量 + 心跳
            var cBat = Card(_bentoHost, 82f, false);
            cBat.style.flexDirection = FlexDirection.Row;
            cBat.style.alignItems = Align.Center;
            float tok = Num(st, "tokens", 640000f), cap = Mathf.Max(1f, Num(st, "cap", 2000000f));
            int pct = Mathf.Clamp(Mathf.RoundToInt((1f - tok / cap) * 100f), 12, 100);
            int bpm = Mathf.RoundToInt(Num(st, "me.bpm", 75f));
            int cl = Mathf.RoundToInt(Num(st, "closeness", 87f));
            int sec = Mathf.RoundToInt(Num(st, "security", 99f));

            var ringWrap = Box(52f * _k, 52f * _k, 26f * _k, Rgb(246, 240, 255, 1f));
            Border(ringWrap, 4.5f * _k, acc);
            ringWrap.style.alignItems = Align.Center; ringWrap.style.justifyContent = Justify.Center;
            ringWrap.style.marginRight = 14f * _k;
            ringWrap.Add(L(pct + "%", 11f, acc, true));
            cBat.Add(ringWrap);

            var batInfo = new VisualElement { pickingMode = PickingMode.Ignore };
            batInfo.style.flexGrow = 1f;
            batInfo.Add(L("⚡ 今日元气电量 " + pct + "%", 11.5f, Ink, true));
            batInfo.Add(L("♥ 实时心跳 " + bpm + " BPM · 黏人亲密 " + cl + "% · 安全感 " + sec + "%", 10f, Ink2, true));
            cBat.Add(batInfo);

            // 小祈的日常照片墙
            var cGal = Card(_bentoHost, 240f, false);
            var gh = RowFlex(cGal);
            gh.Add(L("📸 小祈的日常照片墙", 11.5f, Ink, true));
            gh.Add(L("今日 3 张 ↗", 10f, accDeep, true));
            var gGrid = new VisualElement { pickingMode = PickingMode.Ignore };
            gGrid.style.flexDirection = FlexDirection.Row;
            gGrid.style.justifyContent = Justify.SpaceBetween;
            gGrid.style.marginTop = 8f * _k;
            gGrid.style.flexGrow = 1f;

            var gLeft = MakePhotoTile("宿舍午后", "XqPhotoDorm", "☀️", Rgb(236, 224, 255, 0.95f), 190f * _k, 174f * _k);
            Click(gLeft, () => ShowPhotoPreview("宿舍午后", "XqPhotoDorm", "☀️ · 午后阳光刚好照进宿舍书桌", Rgb(236, 224, 255, 1f)));
            var gRight = new VisualElement { pickingMode = PickingMode.Ignore };
            gRight.style.width = 186f * _k;
            gRight.style.justifyContent = Justify.SpaceBetween;
            var gTop = MakePhotoTile("认真记笔记", "XqPhotoStudy", "✏️", Rgb(255, 228, 245, 0.95f), 186f * _k, 83f * _k);
            var gBot = MakePhotoTile("开心比耶", "XqPhotoPeace", "✌️", Rgb(228, 240, 255, 0.95f), 186f * _k, 83f * _k);
            Click(gTop, () => ShowPhotoPreview("认真记笔记", "XqPhotoStudy", "✏️ · 在图书馆整理今天的专业课笔记", Rgb(255, 228, 245, 1f)));
            Click(gBot, () => ShowPhotoPreview("开心比耶", "XqPhotoPeace", "✌️ · 拍一张今天心情超好的自拍", Rgb(228, 240, 255, 1f)));
            gRight.Add(gTop); gRight.Add(gBot);
            gGrid.Add(gLeft); gGrid.Add(gRight);
            cGal.Add(gGrid);

            // 今日手账/记忆摘录（去掉无意义的摸摸头与看屏幕钮）
            var cMemo = Card(_bentoHost, 58f, false);
            cMemo.style.justifyContent = Justify.Center;
            var dItems = Tk(dy, "diary.items") as JArray;
            string memoTxt = "点进「手账」或「相册」查看今天更多小祈的日常记录";
            if (dItems != null)
            {
                for (int i = dItems.Count - 1; i >= 0; i--)
                {
                    if (Str(dItems[i], "text").Length > 0) { memoTxt = "📝 " + Cut(Str(dItems[i], "text"), 36); break; }
                }
            }
            cMemo.Add(L(memoTxt, 10.8f, Ink2, true));
            Click(cMemo, () => SelectPage(1));
        }

        VisualElement MakePhotoTile(string tag, string resName, string emoji, Color bg, float w, float h)
        {
            var tile = Box(w, h, 16f * _k, bg);
            Border(tile, 1.5f * _k, Color.white);
            tile.style.alignItems = Align.Center;
            tile.style.justifyContent = Justify.Center;
            var tex = Resources.Load<Texture2D>(resName);
            if (tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
                img.style.position = Position.Absolute;
                img.style.left = 0; img.style.top = 0; img.style.right = 0; img.style.bottom = 0;
                Round(img, 14f * _k);
                tile.Add(img);
            }
            else tile.Add(L(emoji, 24f, Ink, true));
            var cap = Box(-1, 20f * _k, 10f * _k, Rgb(255, 255, 255, 0.92f));
            cap.style.position = Position.Absolute;
            cap.style.left = 8f * _k; cap.style.bottom = 8f * _k;
            cap.style.paddingLeft = cap.style.paddingRight = 8f * _k;
            cap.style.justifyContent = Justify.Center;
            cap.Add(L(tag, 9.5f, Ink, true));
            tile.Add(cap);
            return tile;
        }

        void ShowPhotoPreview(string title, string resName, string desc, Color bg)
        {
            if (_panel == null) return;
            var old = _panel.Q("xq-photo-modal");
            if (old != null) old.RemoveFromHierarchy();

            var modal = new VisualElement { name = "xq-photo-modal", pickingMode = PickingMode.Position };
            Abs(modal, 0, 0, PanW * _k, PanH * _k);
            Round(modal, 36f * _k);
            modal.style.backgroundColor = Rgb(18, 12, 34, 0.56f);
            modal.style.alignItems = Align.Center;
            modal.style.justifyContent = Justify.Center;

            var card = Box(380f * _k, 380f * _k, 24f * _k, Rgb(252, 248, 255, 0.98f));
            Border(card, 1.5f * _k, Color.white);
            card.style.paddingLeft = card.style.paddingRight = 18f * _k;
            card.style.paddingTop = card.style.paddingBottom = 16f * _k;
            card.style.justifyContent = Justify.SpaceBetween;

            var preview = Box(-1, 270f * _k, 16f * _k, bg);
            Border(preview, 1.2f * _k, Color.white);
            preview.style.alignItems = Align.Center;
            preview.style.justifyContent = Justify.Center;
            var tex = Resources.Load<Texture2D>(resName);
            if (tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                img.style.width = Length.Percent(100f); img.style.height = Length.Percent(100f);
                preview.Add(img);
            }
            else preview.Add(L(title, 20f, Ink, true));
            card.Add(preview);

            card.Add(L(desc, 11.5f, Ink2, true));

            var closeBtn = Box(96f * _k, 30f * _k, 15f * _k, Rgb(146, 98, 246, 0.95f));
            closeBtn.style.alignSelf = Align.Center;
            closeBtn.style.alignItems = Align.Center;
            closeBtn.style.justifyContent = Justify.Center;
            closeBtn.Add(L("关闭预览", 10.5f, Color.white, true));
            closeBtn.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); modal.RemoveFromHierarchy(); });
            card.Add(closeBtn);

            modal.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); modal.RemoveFromHierarchy(); });
            _panel.Add(modal);
        }

        // ---------------- 5. 工作台内置设置页（不弹桌宠白框） ----------------

        void BuildSettingsBenchPage()
        {
            var cfg = XiaoqiRuntime.Config;
            var cLeft = Card(_leftHost, 386f, false);
            cLeft.Add(L("🎨 外观与交互", 12f, Ink, true));
            cLeft.Add(Box(1, 8f, 0, Color.clear));

            string curTheme = cfg != null ? cfg.theme : "pink";
            AddBenchSettingBtn(cLeft, "主题色：" + (curTheme == "blue" ? "晴空蓝" : curTheme == "violet" ? "星夜紫" : "梦幻粉"), () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                string next = XiaoqiRuntime.Config.theme == "pink" ? "violet" : XiaoqiRuntime.Config.theme == "violet" ? "blue" : "pink";
                XiaoqiRuntime.Config.theme = next;
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                XqUi2Sample.SetTheme(next);
                RenderPage();
            });
            AddBenchSettingBtn(cLeft, "看向鼠标：" + ((cfg == null || cfg.look_at_mouse) ? "开启" : "关闭"), () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                XiaoqiRuntime.Config.look_at_mouse = !XiaoqiRuntime.Config.look_at_mouse;
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                RenderPage();
            });
            AddBenchSettingBtn(cLeft, "双击摸头：" + ((cfg == null || cfg.pat_double) ? "开启" : "关闭"), () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                XiaoqiRuntime.Config.pat_double = !XiaoqiRuntime.Config.pat_double;
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                RenderPage();
            });

            var cRight = Card(_bentoHost, 386f, false);
            cRight.Add(L("🎙️ 语音对话与热键", 12f, Ink, true));
            cRight.Add(Box(1, 8f, 0, Color.clear));
            string hk = cfg != null && !string.IsNullOrEmpty(cfg.voice_hotkey) ? cfg.voice_hotkey : "N";
            AddBenchSettingBtn(cRight, "桌宠按住说话热键：" + hk + "（点击切换）", () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                var keys = new[] { "N", "V", "B", "Space" };
                int idx = Array.IndexOf(keys, XiaoqiRuntime.Config.voice_hotkey);
                XiaoqiRuntime.Config.voice_hotkey = keys[(idx + 1) % keys.Length];
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                RenderPage();
            });
            AddBenchSettingBtn(cRight, "自动语音播报回复：" + ((cfg == null || cfg.voice_tts_auto) ? "开启" : "关闭"), () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                XiaoqiRuntime.Config.voice_tts_auto = !XiaoqiRuntime.Config.voice_tts_auto;
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                RenderPage();
            });
            AddBenchSettingBtn(cRight, "桌宠回复播报概率：" + Mathf.RoundToInt((cfg != null ? cfg.pet_voice_probability : 0.35f) * 100f) + "%", () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                float value = XiaoqiRuntime.Config.pet_voice_probability;
                XiaoqiRuntime.Config.pet_voice_probability = value < 0.1f ? 0.35f : value < 0.4f ? 0.65f : value < 0.9f ? 1f : 0f;
                XiaoqiConfig.Save(XiaoqiRuntime.Config); RenderPage();
            });
            AddBenchSettingBtn(cRight, "近景字幕：" + ((cfg == null || cfg.close_subtitles) ? "显示" : "隐藏"), () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                XiaoqiRuntime.Config.close_subtitles = !XiaoqiRuntime.Config.close_subtitles;
                XiaoqiConfig.Save(XiaoqiRuntime.Config); RenderPage();
            });
            AddBenchSettingBtn(cRight, "静音：" + ((cfg != null && cfg.muted) ? "已静音" : "正常发声"), () =>
            {
                if (XiaoqiRuntime.Config == null) return;
                XqSettingsApply.SetMuted(!XiaoqiRuntime.Config.muted);
                XiaoqiRuntime.Config.muted = !XiaoqiRuntime.Config.muted;
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                RenderPage();
            });
        }

        void AddBenchSettingBtn(VisualElement parent, string label, Action onClick)
        {
            var row = Box(-1, 42f * _k, 14f * _k, Rgb(245, 239, 255, 0.90f));
            Border(row, 1f * _k, Color.white);
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 14f * _k;
            row.style.marginBottom = 10f * _k;
            row.Add(L(label, 11f, Ink, true));
            Click(row, onClick);
            parent.Add(row);
        }

        // ---------------- 1. 生活手账（时间轴 + 倒序日记 + 今日便签）----------------

        void BuildLifePage(bool loading)
        {
            var lf = _bd.Get("life");
            var dy = _bd.Get("diary");
            var td = _bd.Get("today");
            if (lf == null && dy == null) { LoadingOrEmpty("生活手账", loading); return; }

            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            // 左翼：黑紫风格今日时间轴（自动聚焦当前时段）
            var cSegs = Card(_leftHost, 444f, true);
            var sHd = RowFlex(cSegs);
            sHd.Add(L("今日时间轴（滚轮翻看）", 12f, Ink, true));
            sHd.Add(L(Str(lf, "date"), 10f, Ink2, false));
            var segs = Tk(lf, "segments") as JArray;
            int lifeBase = 0;
            if (segs == null || segs.Count == 0)
            {
                if (_pgOff < 0) _pgOff = 0;
                cSegs.Add(WrapTxt("今天还没有日程段", 11f, Ink2, false, 30));
            }
            else
            {
                int nowMin = NowMin();
                int showN = 6;
                int maxSegOff = Mathf.Max(0, segs.Count - showN);
                int nowIdx = -1, lastPassed = 0;
                for (int i = 0; i < segs.Count; i++)
                {
                    int sa = (int)Num(segs[i], "a", 0f), sb = (int)Num(segs[i], "b", 0f);
                    if (sa <= nowMin && nowMin < Mathf.Max(sb, sa + 1)) { nowIdx = i; break; }
                    if (sa <= nowMin) lastPassed = i;
                }
                if (nowIdx < 0) nowIdx = lastPassed;
                lifeBase = Mathf.Clamp(nowIdx - 2, 0, maxSegOff);
                if (_pgOff < 0) _pgOff = lifeBase;

                _pgMax = Mathf.Max(_pgMax, maxSegOff);
                int from = Mathf.Clamp(_pgOff, 0, maxSegOff);
                for (int i = from; i < from + showN && i < segs.Count; i++)
                {
                    var s = segs[i];
                    int a = (int)Num(s, "a", 0f), b = (int)Num(s, "b", 0f);
                    bool isNow = a <= nowMin && nowMin < Mathf.Max(b, a + 1);
                    var item = Box(-1, -1, 13f * _k, isNow ? acc : Rgb(245, 239, 255, 0.85f));
                    item.style.paddingLeft = item.style.paddingRight = 10f * _k;
                    item.style.paddingTop = item.style.paddingBottom = 7f * _k;
                    item.style.marginTop = 7f * _k;
                    var row = RowFlex(item);
                    row.Add(L((isNow ? "● " : "") + Str(s, "start") + "-" + Str(s, "end"), 10f, isNow ? Color.white : accDeep, true));
                    row.Add(L(Str(s, "io"), 9.5f, isNow ? Color.white : Ink2, false));
                    item.Add(WrapTxt(Str(s, "name") + " · " + Str(s, "place"), 10.8f, isNow ? Color.white : Ink, isNow, 24));
                    cSegs.Add(item);
                }
            }

            // 右翼上：最新手账日记片段（倒序优先最新）
            var cDiary = Card(_bentoHost, 286f, false);
            Border(cDiary, 1.8f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.42f));
            var dHd = RowFlex(cDiary);
            dHd.Add(L("小祈日记手账（最新优先）", 12.5f, Ink, true));
            Tag(dHd, Str(dy, "diary.status") == "fallback" ? "兜底版" : "手账正文", true);
            var items = new List<JToken>();
            var ia = Tk(dy, "diary.items") as JArray;
            if (ia != null) foreach (var it in ia) if (Str(it, "text").Length > 0) items.Add(it);
            if (items.Count == 0) cDiary.Add(WrapTxt("今天还没写出日记片段", 11f, Ink2, false, 30));
            else
            {
                items.Reverse();
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, items.Count - 3));
                int f = Mathf.Clamp(_pgOff - lifeBase, 0, Mathf.Max(0, items.Count - 3));
                for (int i = f; i < f + 3 && i < items.Count; i++)
                {
                    var box = Box(-1, -1, 12f * _k, Color.white);
                    Border(box, 1f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.18f));
                    box.style.paddingLeft = box.style.paddingRight = 10f * _k;
                    box.style.paddingTop = box.style.paddingBottom = 7f * _k;
                    box.style.marginTop = 7f * _k;
                    box.Add(L(Str(items[i], "start") + " · " + Str(items[i], "place"), 10f, accDeep, true));
                    box.Add(WrapTxt(Str(items[i], "text"), 10.8f, Ink, false, 48));
                    cDiary.Add(box);
                }
            }

            // 右翼下：曜石黑紫天气与小计划胶囊
            var cNote = Card(_bentoHost, 146f, true);
            cNote.Add(L("今日小计划 & 环境天气", 12f, Ink, true));
            var notes = Tk(td, "notes") as JArray;
            if (notes != null && notes.Count > 0)
            {
                for (int i = 0; i < notes.Count && i < 2; i++)
                    cNote.Add(WrapTxt("• " + Str(notes[i], "ts") + " " + Str(notes[i], "text"), 10.8f, Rgb(246, 232, 255, 0.92f), false, 46));
            }
            else
            {
                var w = Tk(lf, "weather");
                string wx = w != null ? Str(w, "desc") + " " + Num(w, "temp", 0f).ToString("0.#") + "°C · 湿度 " + Num(w, "humidity", 0f).ToString("0") + "%" : "保持好心情";
                cNote.Add(WrapTxt("此刻在：" + Str(lf, "place.doing") + "（" + wx + "）", 11f, Rgb(246, 232, 255, 0.92f), false, 52));
            }
        }

        // ---------------- 2. 共有记忆（纯中文清洗卡片 + 主动找你 + 倒序对话）----------------

        void BuildMemPage(bool loading)
        {
            var mem = _bd.Get("memory");
            var td = _bd.Get("today");
            if (mem == null && td == null) { LoadingOrEmpty("共有记忆", loading); return; }

            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            // 左翼：纯中文清洗记忆墙（绝不出现 ask-before-speaking-in-group / name: / metadata:）
            var cMem = Card(_leftHost, 444f, false);
            Border(cMem, 1.8f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.42f));
            var files = Tk(mem, "files") as JArray;
            var mHd = RowFlex(cMem);
            mHd.Add(L("她记住的事", 12.5f, Ink, true));
            Tag(mHd, files != null ? files.Count + " 份记忆" : "索引", true);
            if (files != null && files.Count > 0)
            {
                if (_selMem < 0 || _selMem >= files.Count) _selMem = 0;
                int showN = 3;
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, files.Count - showN));
                int from = Mathf.Clamp(_pgOff, 0, Mathf.Max(0, files.Count - showN));
                for (int i = from; i < from + showN && i < files.Count; i++)
                {
                    int idx = i;
                    bool sel = idx == _selMem;
                    string tagName = CleanMemTag(Str(files[i], "file"));
                    string preview = CleanMemBody(Str(files[i], "head"), Str(files[i], "body"));
                    var item = Box(-1, -1, 12f * _k, sel ? new Color(acc.r, acc.g, acc.b, 0.20f) : Color.white);
                    Border(item, 1.2f, sel ? acc : new Color(accDeep.r, accDeep.g, accDeep.b, 0.16f));
                    item.style.paddingLeft = item.style.paddingRight = 10f * _k;
                    item.style.paddingTop = item.style.paddingBottom = 7f * _k;
                    item.style.marginTop = 6f * _k;
                    item.Add(L("【" + tagName + "】", 10.5f, sel ? accDeep : Ink, true));
                    item.Add(WrapTxt(preview, 10f, Ink2, false, 30));
                    Click(item, () => { _selMem = idx; RenderPage(); });
                    cMem.Add(item);
                }
                var bodyBox = Box(-1, -1, 14f * _k, DarkCard);
                bodyBox.style.paddingLeft = bodyBox.style.paddingRight = 11f * _k;
                bodyBox.style.paddingTop = bodyBox.style.paddingBottom = 9f * _k;
                bodyBox.style.marginTop = 8f * _k;
                string selTag = CleanMemTag(Str(files[_selMem], "file"));
                string selBody = CleanMemBody(Str(files[_selMem], "head"), Str(files[_selMem], "body"));
                bodyBox.Add(L("✦ " + selTag + " · 记忆详情", 10f, Rgb(255, 168, 232), true));
                bodyBox.Add(WrapTxt(selBody, 10.2f, Ink, false, 104));
                cMem.Add(bodyBox);
            }
            else
            {
                string idxTxt = Regex.Replace(Str(mem, "index"), @"\[([^\]]*)\]\([^)]*\)", "$1");
                cMem.Add(WrapTxt(idxTxt.Length > 0 ? idxTxt : "还没有记录", 11f, Ink2, false, 160));
            }

            // 右翼上：她主动找你的记录
            var cReach = Card(_bentoHost, 194f, false);
            cReach.Add(L("她主动找你的记录", 12.5f, Ink, true));
            var reach = Tk(td, "reach") as JArray;
            if (reach == null || reach.Count == 0) cReach.Add(WrapTxt("今天还没有主动发消息", 11f, Ink2, false, 30));
            else
                for (int i = reach.Count - 1; i >= 0 && i >= reach.Count - 2; i--)
                {
                    var rb = Box(-1, -1, 12f * _k, Color.white);
                    Border(rb, 1f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.16f));
                    rb.style.paddingLeft = rb.style.paddingRight = 10f * _k;
                    rb.style.paddingTop = rb.style.paddingBottom = 7f * _k;
                    rb.style.marginTop = 7f * _k;
                    rb.Add(WrapTxt(Str(reach[i], "ts") + " · " + (IsOfficeMode() ? "[办公模式已隐藏文本]" : Str(reach[i], "text")), 10.8f, Ink, false, 52));
                    cReach.Add(rb);
                }

            // 右翼下：暗色最新对话摘录（倒序优先私聊与她的回复）
            var cTalk = Card(_bentoHost, 238f, true);
            var tHd = RowFlex(cTalk);
            tHd.Add(L("最新对话摘录", 12f, Ink, true));
            tHd.Add(L(Bool(td, "fallback") ? "显示 " + Str(td, "shown_date") : Str(td, "date"), 10f, Rgb(232, 212, 248, 0.75f), false));
            var talks = Tk(td, "talks") as JArray;
            if (talks == null || talks.Count == 0) cTalk.Add(WrapTxt("还没有互动记录", 10.8f, Rgb(246, 232, 255, 0.85f), false, 30));
            else
            {
                var picks = new List<JToken>();
                for (int i = talks.Count - 1; i >= 0 && picks.Count < 4; i--)
                    if (Str(talks[i], "she").Length > 0 || Str(talks[i], "chat") == "私聊") picks.Add(talks[i]);
                for (int i = talks.Count - 1; i >= 0 && picks.Count < 4; i--)
                    if (!picks.Contains(talks[i])) picks.Add(talks[i]);
                for (int i = 0; i < picks.Count; i++)
                {
                    var tk = picks[i];
                    string chat = Str(tk, "chat"), he = Str(tk, "he"), she = Str(tk, "she"), whoName = Str(tk, "who");
                    string msg;
                    if (IsOfficeMode() && chat == "私聊") msg = "[私聊已打码]";
                    else if (she.Length > 0 && he.Length > 0) msg = "她：" + she + "（回：" + Cut(he, 10) + "）";
                    else if (she.Length > 0) msg = "她：" + she;
                    else msg = (chat == "群聊" && whoName.Length > 0 ? whoName : "你") + "：" + he;
                    cTalk.Add(WrapTxt(Str(tk, "ts") + " [" + chat + "] " + msg, 10.5f, Rgb(246, 232, 255, 0.92f), false, 48));
                }
            }
        }

        // ---------------- 3. 成长轨迹（绝区零 2×2 勋章格 + 好奇心 + 周报 + 独白）----------------

        void BuildGrowthPage(bool loading)
        {
            var g = _bd.Get("growth");
            if (g == null) { LoadingOrEmpty("成长轨迹", loading); return; }

            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            // 左翼上：绝区零风格 2×2 累计数字徽章
            var badgeGrid = new VisualElement { pickingMode = PickingMode.Ignore };
            badgeGrid.style.flexDirection = FlexDirection.Row;
            badgeGrid.style.flexWrap = Wrap.Wrap;
            badgeGrid.style.justifyContent = Justify.SpaceBetween;
            badgeGrid.style.marginBottom = 4f * _k;
            string[][] keys = {
                new[] { "experiences", "经历累计" },
                new[] { "reflections", "思考反思" },
                new[] { "examples", "表达范例" },
                new[] { "curiosity", "好奇探索" }
            };
            for (int i = 0; i < keys.Length; i++)
            {
                bool hero = i == 0;
                var v = Tk(g, "counts." + keys[i][0]);
                var tile = Box(130f * _k, 90f * _k, 18f * _k, hero ? acc : DarkCard);
                Border(tile, 1.3f, hero ? Rgb(255, 255, 255, 0.8f) : Rgb(255, 255, 255, 0.24f));
                tile.style.paddingLeft = tile.style.paddingRight = 12f * _k;
                tile.style.paddingTop = tile.style.paddingBottom = 10f * _k;
                tile.style.justifyContent = Justify.SpaceBetween;
                tile.style.marginBottom = 10f * _k;
                tile.Add(L(keys[i][1] + (hero ? " ↗" : ""), 10f, hero ? Color.white : Ink2, true));
                tile.Add(L(v != null ? ToF(v).ToString("0") : "0", 24f, hero ? Color.white : Ink, true));
                badgeGrid.Add(tile);
            }
            _leftHost.Add(badgeGrid);

            // 左翼下：好奇心贴纸墙
            var cCur = Card(_leftHost, 240f, false);
            cCur.Add(L("好奇心 & 最近在学", 12f, Ink, true));
            var cur = Tk(g, "curiosity") as JArray;
            var cw = new VisualElement { pickingMode = PickingMode.Ignore };
            cw.style.flexDirection = FlexDirection.Row; cw.style.flexWrap = Wrap.Wrap; cw.style.marginTop = 8f * _k;
            if (cur != null)
                for (int i = 0; i < cur.Count && i < 6; i++)
                    Tag(cw, "[" + Str(cur[i], "channel") + "] " + Cut(Str(cur[i], "topic"), 13), i == 0);
            cCur.Add(cw);

            // 右翼上：周报摘要 + 此刻关注
            var cWk = Card(_bentoHost, 280f, false);
            Border(cWk, 1.8f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.42f));
            cWk.Add(L("小祈周报 & 此刻着迷", 12.5f, Ink, true));
            string wk = Str(g, "weekly"), self = Str(g, "self_now");
            var wb = Box(-1, -1, 14f * _k, Color.white);
            wb.style.paddingLeft = wb.style.paddingRight = 12f * _k; wb.style.paddingTop = wb.style.paddingBottom = 9f * _k; wb.style.marginTop = 8f * _k;
            wb.Add(L("周报摘要", 10.5f, accDeep, true));
            wb.Add(WrapTxt(wk.Length > 0 ? wk : "暂无本周总结", 10.8f, Ink, false, 88));
            cWk.Add(wb);
            var sb = Box(-1, -1, 14f * _k, Color.white);
            sb.style.paddingLeft = sb.style.paddingRight = 12f * _k; sb.style.paddingTop = sb.style.paddingBottom = 9f * _k; sb.style.marginTop = 8f * _k;
            sb.Add(L("此刻关注", 10.5f, accDeep, true));
            sb.Add(WrapTxt(self.Length > 0 ? self : "还没有", 10.8f, Ink, false, 72));
            cWk.Add(sb);

            // 右翼下：暗色内心独白与语感旋钮
            var cTh = Card(_bentoHost, 152f, true);
            cTh.Add(L("内心独白 & 语感旋钮", 12f, Ink, true));
            var ths = Tk(g, "thoughts") as JArray;
            if (ths != null && ths.Count > 0)
                cTh.Add(WrapTxt("“" + Str(ths[0], "thought") + "”", 10.5f, Rgb(246, 232, 255, 0.92f), false, 68));
            var kb = Tk(g, "knobs");
            if (kb != null)
                cTh.Add(WrapTxt("话长 ×" + Num(kb, "len_x", 1f).ToString("0.00") + " · 靠拢 ×" + Num(kb, "kao_x", 1f).ToString("0.00"), 10f, Rgb(255, 168, 232), true, 40));
        }

        // ---------------- 4. Brain 轻控（高频模式开关 + 轻指令 + Web 外跳门户）----------------

        void BuildBrainPage()
        {
            var st = _bd.Get("state");
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);

            // 左翼上：场景模式切换
            var cMode = Card(_leftHost, 230f, false);
            cMode.Add(L("场景模式（一键切换）", 12.5f, Ink, true));
            var cfg = XiaoqiRuntime.Config;
            bool isOff = IsOfficeMode();
            bool isDnd = XiaoqiRuntime.Dnd;
            AddModeOpt(cMode, "🏠 居家模式", "显示私聊气泡与完整同步", !isOff && !isDnd, () =>
            {
                if (cfg == null) return;
                cfg.token = HomeToken; cfg.bubble = true; XiaoqiRuntime.ClearQuiet(); XiaoqiConfig.Save(cfg);
                if (_modeBtnLabel != null) _modeBtnLabel.text = "🏠 居家模式";
                Toast("已切至居家模式"); RenderPage();
            });
            AddModeOpt(cMode, "💼 办公防社死", "隐藏私聊文本，只留动作", isOff, () =>
            {
                if (cfg == null) return;
                cfg.token = OfficeToken; cfg.bubble = false; XiaoqiConfig.Save(cfg);
                if (_modeBtnLabel != null) _modeBtnLabel.text = "💼 办公模式";
                Toast("已切至办公防社死模式"); RenderPage();
            });
            AddModeOpt(cMode, "🌙 安静勿扰 30分", "暂停主动弹窗打扰", isDnd, () =>
            {
                XiaoqiRuntime.SetQuietMinutes(30);
                Toast("已开启安静模式 30 分钟"); RenderPage();
            });

            // 左翼下：4 个轻触发钮（2×2 稳定双排）
            var cAct = Card(_leftHost, 202f, false);
            cAct.Add(L("Brain 轻指令触发", 12f, Ink, true));
            if (_brainStatus.Length > 0) cAct.Add(L(_brainStatus, 10f, accDeep, true));
            var acts = new VisualElement { pickingMode = PickingMode.Ignore };
            acts.style.flexDirection = FlexDirection.Row; acts.style.flexWrap = Wrap.Wrap; acts.style.marginTop = 8f * _k;
            AddActBtn(acts, "☀ 叫醒她", () => _bd.TriggerBrainAction("wake", msg => { _brainStatus = msg; Toast(msg); RenderPage(); }));
            AddActBtn(acts, "📖 读一篇", () => _bd.TriggerBrainAction("read", msg => { _brainStatus = msg; Toast(msg); RenderPage(); }));
            AddActBtn(acts, "🎬 抓学习", () => _bd.TriggerBrainAction("learn", msg => { _brainStatus = msg; Toast(msg); RenderPage(); }));
            AddActBtn(acts, "👁 看屏幕", () => XqScreenCompanion.PushNow(_bd, msg => { _brainStatus = msg; Toast(msg); RenderPage(); }));
            cAct.Add(acts);

            // 右翼上：暗色运行状态 + 九维低谷提醒
            var cStat = Card(_bentoHost, 146f, true);
            cStat.Add(L("● 云端 Brain 运行中（思考层）", 12.5f, Ink, true));
            float tok = Num(st, "tokens", 0f), cap = Mathf.Max(1f, Num(st, "cap", 2000000f));
            cStat.Add(L("今日 Token：" + (tok / 10000f).ToString("0.1") + " 万 / " + Mathf.RoundToInt(cap / 10000f) + " 万 · 当前情绪：" + Str(st, "mood"), 10.8f, Rgb(232, 212, 248, 0.9f), false));
            var bar = Box(380f * _k, 8f * _k, 4f * _k, Rgb(255, 255, 255, 0.18f));
            bar.style.marginTop = 12f * _k;
            bar.Add(Box(380f * _k * Mathf.Clamp01(tok / cap), 8f * _k, 4f * _k, acc));
            cStat.Add(bar);

            // 右翼下：重度 Web 工具门户（2×2 完整卡片）
            var cWeb = Card(_bentoHost, 286f, false);
            Border(cWeb, 1.8f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.42f));
            cWeb.Add(L("Web 完整控制台（重度后台外跳）", 12.5f, Ink, true));
            cWeb.Add(WrapTxt("复杂调参、地图轨迹、画图工作流保留在浏览器打开：", 10f, Ink2, false, 44));
            var wg = new VisualElement { pickingMode = PickingMode.Ignore };
            wg.style.flexDirection = FlexDirection.Row; wg.style.flexWrap = Wrap.Wrap; wg.style.marginTop = 10f * _k;
            AddPortal(wg, "🗺 薄扶林地图", "实时定位与轨迹", "/p/map");
            AddPortal(wg, "🎨 自拍画图台", "ComfyUI 工作流", "/p/draw");
            AddPortal(wg, "⚙ 完整 Brain 设置", "8 大类全量参数", "/p/set");
            AddPortal(wg, "🩺 运维六层快照", "状态自检 /snapshot", "/snapshot");
            cWeb.Add(wg);
        }

        // ---------------- 记忆数据清洗与通用卡片小件 ----------------

        static string CleanMemTag(string file)
        {
            if (string.IsNullOrEmpty(file)) return "日常记忆";
            string f = file.ToLowerInvariant();
            if (f.StartsWith("user")) return "关于你";
            if (f.StartsWith("ask")) return "群聊分寸";
            if (f.StartsWith("avoid")) return "相处默契";
            if (f.StartsWith("assistant")) return "自我设定";
            if (f.StartsWith("search") || f.StartsWith("learn") || f.StartsWith("check")) return "对话习惯";
            return "长期记忆";
        }

        static string CleanMemBody(string head, string body)
        {
            string raw = !string.IsNullOrEmpty(body) ? body : (head ?? "");
            if (string.IsNullOrEmpty(raw)) return "暂无内容";
            string s = Regex.Replace(raw, @"^---\s*\r?\n[\s\S]*?\r?\n---\s*", "");
            var desc = Regex.Match(s, @"description:\s*(.+?)(?=\r?\n\s*(?:metadata|type|triggers|tags|name):|$)", RegexOptions.Singleline);
            if (desc.Success)
                s = desc.Groups[1].Value;
            else
            {
                s = Regex.Replace(s, @"(?m)^\s*(?:name|metadata|type|tags|triggers):\s*.*$", "");
            }
            s = Regex.Replace(s, @"\[([^\]]*)\]\([^)]*\)", "$1");
            s = s.Replace("**", "").Replace("`", "");
            s = Regex.Replace(s, @"(?m)^\s*[-*#]+\s*", "");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s.Length > 0 ? s : "暂无详情";
        }

        static int NowMin()
        {
            int m = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            return DateTime.Now.Hour < 3 ? m + 1440 : m;
        }

        static bool IsMainBenchSeg(JToken s)
        {
            if (s == null) return false;
            if (Str(s, "layer") == "fill") return false;
            string act = Str(s, "act");
            if (act == "空闲" || act == "填充" || act == "走路") return false;
            string nm = Str(s, "name");
            if (nm.Contains("在宿舍待着") || nm.Contains("在宿舍磨蹭")) return false;
            return true;
        }

        static List<JToken> PickMainSegs(JArray segs, int limit)
        {
            var res = new List<JToken>();
            if (segs == null || segs.Count == 0) return res;
            int now = NowMin();
            int nowIdx = -1, lastPassed = 0;
            for (int i = 0; i < segs.Count; i++)
            {
                int sa = (int)Num(segs[i], "a", 0f), sb = (int)Num(segs[i], "b", 0f);
                if (sa <= now && now < Mathf.Max(sb, sa + 1)) { nowIdx = i; break; }
                if (sa <= now) lastPassed = i;
            }
            if (nowIdx < 0) nowIdx = lastPassed;

            for (int j = nowIdx - 1; j >= 0; j--)
                if (IsMainBenchSeg(segs[j])) { res.Add(segs[j]); break; }
            if (nowIdx >= 0 && nowIdx < segs.Count) res.Add(segs[nowIdx]);
            for (int j = nowIdx + 1; j < segs.Count && res.Count < limit; j++)
                if (IsMainBenchSeg(segs[j])) res.Add(segs[j]);
            if (res.Count < 2 && nowIdx + 1 < segs.Count) res.Add(segs[nowIdx + 1]);
            return res;
        }

        void AddCleanNoteRow(VisualElement parent, string tag, string text, Action act)
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var row = Box(-1, -1, 14f * _k, Color.white);
            Border(row, 1f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.16f));
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.FlexStart;
            row.style.paddingLeft = row.style.paddingRight = 10f * _k;
            row.style.paddingTop = row.style.paddingBottom = 8f * _k;
            row.style.marginTop = 8f * _k;

            var badge = Box(-1, 20f * _k, 8f * _k, new Color(acc.r, acc.g, acc.b, 0.18f));
            badge.style.paddingLeft = badge.style.paddingRight = 7f * _k;
            badge.style.justifyContent = Justify.Center;
            badge.style.marginRight = 8f * _k;
            badge.style.flexShrink = 0f;
            badge.Add(L(tag, 9.5f, accDeep, true));

            var txt = WrapTxt(text, 10.3f, Ink, false, 44);
            txt.style.marginTop = 0f; txt.style.flexShrink = 1f;
            row.Add(badge); row.Add(txt);
            Click(row, act);
            parent.Add(row);
        }

        VisualElement TickStrip(int count, int lit)
        {
            var r = new VisualElement { pickingMode = PickingMode.Ignore };
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.justifyContent = Justify.SpaceBetween;
            r.style.marginTop = 6f * _k;
            for (int i = 0; i < count; i++)
            {
                var t = Box(2f * _k, 8f * _k, 1f * _k, new Color(1f, 1f, 1f, i < lit ? 0.85f : 0.25f));
                r.Add(t);
            }
            return r;
        }

        void LoadingOrEmpty(string name, bool loading)
        {
            var card = Card(_bentoHost, 200f, false);
            card.Add(L(loading ? "正在翻看小祈的" + name + "…" : "暂时连不上服务器", 15f, Ink, true));
            card.Add(WrapTxt(loading ? "并发同步云端数据中，请稍候片刻（有本地缓存时会秒出内容）。" : "请确认 Tailscale 在线，或点击右上「↻」重试。", 11.5f, Ink2, false, 80));
        }

        VisualElement Card(VisualElement parent, float h, bool dark)
        {
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.style.height = h * _k;
            card.style.marginBottom = 12f * _k;
            Round(card, 22f * _k);
            card.style.backgroundColor = dark ? DarkCard : LilacCard;
            Border(card, 1.3f, dark ? Rgb(255, 255, 255, 0.24f) : Color.white);
            card.style.paddingLeft = card.style.paddingRight = 14f * _k;
            card.style.paddingTop = card.style.paddingBottom = 12f * _k;
            parent.Add(card);
            return card;
        }

        VisualElement RowFlex(VisualElement parent)
        {
            var r = new VisualElement { pickingMode = PickingMode.Ignore };
            r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
            r.style.justifyContent = Justify.SpaceBetween; r.style.flexWrap = Wrap.Wrap;
            parent.Add(r);
            return r;
        }

        void Tag(VisualElement parent, string text, bool accent)
        {
            if (string.IsNullOrEmpty(text)) return;
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var p = new VisualElement { pickingMode = PickingMode.Ignore };
            Round(p, 11f * _k);
            p.style.backgroundColor = accent ? new Color(acc.r, acc.g, acc.b, 0.22f) : Color.white;
            p.style.paddingLeft = p.style.paddingRight = 8f * _k;
            p.style.paddingTop = p.style.paddingBottom = 3f * _k;
            p.style.marginRight = 5f * _k; p.style.marginBottom = 4f * _k;
            p.Add(L(Cut(text, 20), 9.8f, accent ? accDeep : Ink, accent));
            parent.Add(p);
        }

        void AddModeOpt(VisualElement parent, string title, string sub, bool active, Action act)
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var b = Box(-1, 46f * _k, 14f * _k, active ? new Color(acc.r, acc.g, acc.b, 0.22f) : Color.white);
            Border(b, 1.2f, active ? acc : new Color(accDeep.r, accDeep.g, accDeep.b, 0.16f));
            b.style.paddingLeft = b.style.paddingRight = 10f * _k; b.style.justifyContent = Justify.Center; b.style.marginTop = 7f * _k;
            b.Add(L(title, 10.8f, active ? accDeep : Ink, true));
            b.Add(L(sub, 9.2f, Ink2, false));
            Click(b, act);
            parent.Add(b);
        }

        void AddActBtn(VisualElement parent, string text, Action act)
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var b = Box(112f * _k, 38f * _k, 14f * _k, Color.white);
            Border(b, 1.2f, new Color(acc.r, acc.g, acc.b, 0.50f));
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.marginRight = 6f * _k; b.style.marginBottom = 8f * _k;
            b.Add(L(text, 10.5f, Ink, true));
            Click(b, act);
            parent.Add(b);
        }

        void AddPortal(VisualElement parent, string title, string sub, string path)
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var b = Box(184f * _k, 86f * _k, 16f * _k, Color.white);
            Border(b, 1.2f, new Color(accDeep.r, accDeep.g, accDeep.b, 0.20f));
            b.style.paddingLeft = b.style.paddingRight = 10f * _k; b.style.justifyContent = Justify.Center;
            b.style.marginRight = 8f * _k; b.style.marginBottom = 8f * _k;
            b.Add(L(title, 11f, Ink, true));
            b.Add(WrapTxt(sub, 9.5f, Ink2, false, 20));
            b.Add(L("浏览器打开 ↗", 9.5f, accDeep, true));
            Click(b, () =>
            {
                var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
                string srv = cfg != null ? (cfg.server ?? "").TrimEnd('/') : "";
                if (!string.IsNullOrEmpty(srv)) { Application.OpenURL(srv + path); Toast("已在浏览器打开 " + path); }
            });
            parent.Add(b);
        }

        Label WrapTxt(string s, float size, Color col, bool bold, int maxChars)
        {
            var l = L(Cut(s, maxChars), size, col, bold);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 3f * _k;
            return l;
        }

        void Click(VisualElement el, Action act)
        {
            if (el != null && act != null) _pageClicks.Add(new KeyValuePair<VisualElement, Action>(el, act));
        }

        // ---------------- JSON 解析工具 ----------------

        static JToken Tk(JToken t, string path)
        {
            if (t == null) return null;
            try { var x = t.SelectToken(path); return (x == null || x.Type == JTokenType.Null) ? null : x; }
            catch (Exception) { return null; }
        }

        static string Str(JToken t, string path)
        {
            var x = Tk(t, path);
            if (x == null) return "";
            return x.Type == JTokenType.String ? ((string)x ?? "") : x.ToString();
        }

        static float ToF(JToken x)
        {
            if (x == null) return 0f;
            if (x.Type == JTokenType.Integer || x.Type == JTokenType.Float) return (float)x;
            float f; return float.TryParse(x.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : 0f;
        }

        static float Num(JToken t, string path, float d = -1f)
        {
            var x = Tk(t, path);
            if (x == null) return d;
            if (x.Type == JTokenType.Integer || x.Type == JTokenType.Float) return (float)x;
            float f; return float.TryParse(x.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : d;
        }

        static bool Bool(JToken t, string path)
        {
            var x = Tk(t, path);
            return x != null && x.Type == JTokenType.Boolean && (bool)x;
        }

        static string Cut(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, Math.Max(1, n - 1)) + "…";
        }

        static string Join(string sep, params string[] parts)
        {
            var l = new List<string>();
            foreach (var p in parts) if (!string.IsNullOrEmpty(p)) l.Add(p);
            return string.Join(sep, l.ToArray());
        }
    }
}
