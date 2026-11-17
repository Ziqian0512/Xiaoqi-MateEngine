using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// G74 W6 工作台页签：0 今天（W4 原样） / 1 她（此刻） / 2 生活 / 3 记忆 / 4 成长（只读展示）。
    /// 做法：第 0 页的 6 行内容在构建时摘下来存好；切到别的页就把这 6 行清空重填，切回来再原样放回去。
    /// 数据来自 XqBenchData（docs/BENCH-API.md）；连不上用磁盘缓存，并亮「本地服务没开 · 重试」。
    /// 不用 Overflow.Hidden（ISSUES #22）；列表类内容用「滚轮翻看」的窗口，不滚动裁剪。
    /// </summary>
    public partial class XqBenchPanel
    {
        static readonly string[] PageName = { "今天", "她（此刻）", "生活", "记忆", "成长" };
        const int WinN = 2;                       // 列表行一次显示几条

        int _page;                                // 0..4
        int _pgOff;                               // 滚轮翻看的偏移
        int _pgMax;                               // 本页最大偏移（渲染时算）
        float _pgFade = 1f;
        bool _pgOffline;
        bool _p0Active = true;                    // 6 行里现在放的是不是第 0 页的内容
        XqBenchData _bd;
        VisualElement _snapBtn;
        readonly List<VisualElement>[] _p0kids = new List<VisualElement>[RowN];
        readonly List<XqGlyph> _navGlyph = new List<XqGlyph>();

        bool OfflineNow { get { return _page == 0 ? _offline : _pgOffline; } }

        // ---------------- 接线（主文件里的几个挂钩调这里）----------------

        /// <summary>BuildPanel 里 6 行建好之后调：把第 0 页的内容摘下来存着。</summary>
        void CapturePage0()
        {
            for (int r = 0; r < RowN; r++)
            {
                var l = new List<VisualElement>();
                foreach (var c in _row[r].Children()) l.Add(c);
                _p0kids[r] = l;
            }
            _p0Active = true;
        }

        void PageShow()
        {
            _bd = XqBenchData.Ensure();
            _bd.Changed -= OnBenchData; _bd.Changed += OnBenchData;
            if (_page > 0) _bd.Open(_page);
            _pgFade = 0f;
            RenderPage();
            ApplyNavSel();
        }

        void PageHide()
        {
            if (_bd != null) { _bd.Changed -= OnBenchData; _bd.Stop(); }
        }

        void OnBenchData()
        {
            if (!_shown || _page == 0 || _bd == null) return;
            _pgOffline = _bd.Offline(_page);
            RenderPage();
        }

        void PageTick(float dt)
        {
            _pgFade = Mathf.MoveTowards(_pgFade, 1f, dt * 5f);
        }

        void SelectPage(int p)
        {
            if (p < 0 || p > 4) return;
            if (p == _page) { if (p > 0 && _bd != null) _bd.Refresh(); return; }
            _page = p; _pgOff = 0; _pgFade = 0f;
            Debug.Log("XQ_BENCH page=" + p + " " + PageName[p]);
            if (_bd == null) _bd = XqBenchData.Ensure();
            if (p > 0) { _pgOffline = false; _bd.Open(p); } else _bd.Stop();
            ApplyNavSel();
            RenderPage();
        }

        void ApplyNavSel()
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            for (int n = 0; n < _nav.Count; n++)
            {
                bool sel = n == _page;
                Border(_nav[n], 1f, sel ? new Color(acc.r, acc.g, acc.b, 0.35f) : new Color(1f, 1f, 1f, 0f));
                if (n < _navGlyph.Count && _navGlyph[n] != null)
                {
                    _navGlyph[n].Stroke = sel ? accDeep : new Color(Ink.r, Ink.g, Ink.b, n >= _nav.Count - 1 ? 0.45f : 0.78f);
                    _navGlyph[n].MarkDirtyRepaint();
                }
            }
        }

        /// <summary>每帧（PollSelf 里）：滚轮翻看；返回 true = 这一帧的点击被页内控件吃了。</summary>
        bool PagePoll(bool down)
        {
            if (_page == 0) return false;
            float sc = Input.mouseScrollDelta.y;
            if (Mathf.Abs(sc) > 0.01f && _pgMax > 0 && OverEl(_panel))
            {
                int o = Mathf.Clamp(_pgOff + (sc < 0f ? 1 : -1), 0, _pgMax);
                if (o != _pgOff) { _pgOff = o; RenderPage(); }
            }
            if (down && _snapBtn != null && _snapBtn.panel != null && OverEl(_snapBtn))
            {
                var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
                string server = (cfg.server ?? "").TrimEnd('/');
                Debug.Log("XQ_BENCH snapshot-open server=" + (string.IsNullOrEmpty(server) ? "n" : "y"));
                if (!string.IsNullOrEmpty(server)) Application.OpenURL(server + "/snapshot");
                return true;
            }
            return false;
        }

        // ---------------- 渲染 ----------------

        void RenderPage()
        {
            if (_host == null || _row[0] == null) return;
            _snapBtn = null; _pgMax = 0;
            if (_page == 0)
            {
                if (!_p0Active)
                {
                    for (int r = 0; r < RowN; r++)
                    {
                        _row[r].Clear();
                        if (_p0kids[r] != null) foreach (var c in _p0kids[r]) _row[r].Add(c);
                        _row[r].style.display = DisplayStyle.Flex;
                    }
                    _p0Active = true;
                    Refill();
                }
                return;
            }
            _p0Active = false;
            for (int r = 0; r < RowN; r++) { _row[r].Clear(); _row[r].style.display = DisplayStyle.Flex; }
            if (_bd == null) _bd = XqBenchData.Ensure();
            _pgOffline = _bd.Offline(_page);
            string stampKey = XqBenchData.KeysOf(_page)[0];
            try
            {
                switch (_page)
                {
                    case 1: BuildNow(); break;
                    case 2: BuildLife(); break;
                    case 3: BuildMem(); break;
                    case 4: BuildGrowth(); break;
                }
            }
            catch (Exception e) { Debug.LogWarning("XQ_BENCH page " + _page + ": " + e.Message); }
            _pgOff = Mathf.Clamp(_pgOff, 0, _pgMax);
            var st = _bd.StampOf(stampKey);
            string when = st > DateTime.MinValue ? st.ToString("HH:mm:ss") : "--";
            _metaLabel.text = PageName[_page] + " · " + (_pgOffline ? "缓存 " : "更新 ") + when + (_pgMax > 0 ? " · 滚轮翻看" : "");
        }

        // ---------------- 页：她（此刻）----------------

        void BuildNow()
        {
            var st = _bd.Get("state"); var lf = _bd.Get("life");
            var cur = XiaoqiLink.Current;
            if (st == null && lf == null && cur == null) { Unavail("她（此刻）", "连不上服务器，也没有缓存"); return; }

            // 0 此刻在做
            var c = PRow(0, "此刻在做", "位置、时段、天气");
            var col = Col(c);
            string act = Str(st, "activity"); string place = Str(st, "me.place");
            if (act.Length == 0 && cur != null && cur.seg != null) act = cur.seg.name ?? "";
            if (place.Length == 0) place = Str(lf, "place.where");
            if (place.Length == 0 && cur != null) place = cur.where ?? "";
            col.Add(Txt((act.Length > 0 ? act : "还没有") + (place.Length > 0 ? "  ·  " + place : ""), 14f, Ink, true, 60));
            string seg = "";
            string s0 = Str(st, "segment.start"), s1 = Str(st, "segment.end"), sa = Str(st, "segment.act");
            if (s0.Length > 0) seg = s0 + "-" + s1 + (sa.Length > 0 ? " " + sa : "");
            else if (cur != null && cur.seg != null && !string.IsNullOrEmpty(cur.seg.start)) seg = cur.seg.start + "-" + cur.seg.end;
            string doing = Str(lf, "place.doing");
            string wx = "";
            var w = Tk(lf, "weather");
            if (w != null && w.Type == JTokenType.Object)
                wx = Str(w, "desc") + " " + Num(w, "temp", 0f).ToString("0.#") + "°C · 湿度 " + Num(w, "humidity", 0f).ToString("0") + "%";
            string l2 = Join("  ·  ", seg, doing, wx);
            if (l2.Length > 0) col.Add(Txt(l2, 11f, Ink2, false, 80));

            // 1 心情
            c = PRow(1, "心情", "此刻的情绪象限与想他的程度");
            if (st == null) c.Add(Txt("需要连上服务器", 12f, Ink2, false, 30));
            else
            {
                Pill(c, Str(st, "mood"), true);
                string q = Str(st, "affect.quadrant"); if (q.Length > 0) Pill(c, q, false);
                string lb = Str(st, "label"); string tier = Str(st, "tier");
                if (lb.Length > 0) Pill(c, lb + (tier.Length > 0 ? " · " + tier : ""), false);
                string last = Str(st, "last_owner_at"); if (last.Length > 0) Pill(c, "他上次来：" + Cut(last, 22), false);
            }

            // 2 亲密与安全
            c = PRow(2, "亲密与安全", "百分比，越高越踏实");
            if (st == null) c.Add(Txt("需要连上服务器", 12f, Ink2, false, 30));
            else
            {
                var bars = Col(c);
                Bar(bars, "心情", Num(st, "mood_value"));
                Bar(bars, "亲密", Num(st, "closeness"));
                Bar(bars, "安全感", Num(st, "security"));
            }

            // 3 九维需求
            c = PRow(3, "九维需求", "亮的最强，其余看数字");
            var needs = Tk(st, "needs") as JObject;
            string top = Str(st, "top_dim.k");
            if (needs == null) c.Add(Txt("需要连上服务器", 12f, Ink2, false, 30));
            else
                foreach (var p in needs.Properties())
                    Pill(c, p.Name + " " + Mathf.RoundToInt(ToF(p.Value)), p.Name == top);

            // 4 心跳 / 最近说的
            c = PRow(4, "心跳与最近说的", "她最近一句");
            var col4 = Col(c);
            int bpm = (int)Num(st, "me.bpm", -1f);
            string say = Str(st, "me.say");
            if (say.Length == 0) say = Str(st, "line");
            if (bpm >= 0) col4.Add(Txt("心跳 " + bpm + " bpm  " + Cut(Str(st, "me.bpm_why"), 26), 11f, Ink2, false, 50));
            col4.Add(Txt(say.Length > 0 ? Cut(say, 56) : "还没有", 12f, Ink, false, 60));

            // 5 动态
            c = PRow(5, "最近的动态", "她今天做过的事");
            var feed = Tk(st, "me.feed") as JArray;
            var col5 = Col(c);
            if (feed == null || feed.Count == 0) col5.Add(Txt("还没有", 12f, Ink2, false, 20));
            else
                for (int i = 0; i < feed.Count && i < 3; i++)
                    col5.Add(Txt(Str(feed[i], "t") + "  " + Cut(Str(feed[i], "text"), 40), 11.5f, Bool(feed[i], "now") ? Ink : Ink2, Bool(feed[i], "now"), 60));
        }

        // ---------------- 页：生活 ----------------

        void BuildLife()
        {
            var lf = _bd.Get("life"); var dy = _bd.Get("diary"); var lib = _bd.Get("library"); var pl = _bd.Get("pools"); var ab = _bd.Get("about");
            if (lf == null && dy == null && lib == null) { Unavail("生活", "连不上服务器，也没有缓存"); return; }

            var c = PRow(0, "此刻", "她在哪、在干嘛、外面天气");
            var col = Col(c);
            string where = Str(lf, "place.where"), doing = Str(lf, "place.doing"), since = Str(lf, "place.since");
            col.Add(Txt(where.Length > 0 ? where + (doing.Length > 0 ? "  ·  " + doing : "") + (since.Length > 0 ? "（" + since + " 起）" : "") : "还没有", 13.5f, Ink, true, 60));
            var w = Tk(lf, "weather");
            if (w != null && w.Type == JTokenType.Object)
                col.Add(Txt(Str(w, "desc") + " " + Num(w, "temp", 0f).ToString("0.#") + "°C · 湿度 " + Num(w, "humidity", 0f).ToString("0") + "% · 降水 " + Num(w, "rain", 0f).ToString("0.#") + "mm", 11f, Ink2, false, 60));

            // 1 今天的安排（主线）
            c = PRow(1, "今天的安排", Str(lf, "date"));
            var cards = FilterMain(Tk(lf, "cards") as JArray);
            if (cards.Count == 0) c.Add(Txt("今天还没有排牌", 12f, Ink2, false, 30));
            else
            {
                int nowMin = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
                if (DateTime.Now.Hour < 3) nowMin += 1440;
                int cur = 0;
                for (int i = 0; i < cards.Count; i++) if (ToMin(Str(cards[i], "at")) <= nowMin) cur = i;
                int n = 5;
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, cards.Count - n));
                int from = Mathf.Clamp(cur - 1 + _pgOff, 0, Mathf.Max(0, cards.Count - n));
                for (int i = from; i < from + n && i < cards.Count; i++)
                    Pill(c, Str(cards[i], "at") + " " + Cut(Str(cards[i], "text"), 8), i == cur);
            }

            // 2 日记
            string dstatus = Str(dy, "diary.status");
            c = PRow(2, "日记", dstatus == "fallback" ? "今天是兜底版（模型没写成）" : (dstatus.Length > 0 ? "状态：" + dstatus : "她自己写的一天"));
            var col2 = Col(c);
            var items = new List<JToken>();
            var ia = Tk(dy, "diary.items") as JArray;
            if (ia != null) foreach (var it in ia) if (Str(it, "text").Length > 0) items.Add(it);
            if (items.Count == 0) col2.Add(Txt("还没有", 12f, Ink2, false, 20));
            else
            {
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, items.Count - WinN));
                int f = Mathf.Min(_pgOff, Mathf.Max(0, items.Count - WinN));
                for (int i = f; i < f + WinN && i < items.Count; i++)
                    col2.Add(Txt(Str(items[i], "start") + "  " + Cut(Str(items[i], "text"), 40), 11.5f, Ink, false, 60));
            }

            // 3 自检
            c = PRow(3, "排牌自检", "每天 C1~C22 条规则");
            var checks = Tk(lf, "checks") as JArray;
            if (checks == null) c.Add(Txt("没有数据", 12f, Ink2, false, 20));
            else
            {
                int ok = 0; var bad = new List<string>();
                foreach (var ck in checks) { if (Bool(ck, "ok")) ok++; else bad.Add(Str(ck, "id") + " " + Cut(Str(ck, "msg"), 18)); }
                Pill(c, ok + " / " + checks.Count + " 通过", bad.Count == 0);
                for (int i = 0; i < bad.Count && i < 2; i++) Pill(c, bad[i], false);
            }

            // 4 牌库与选项池
            c = PRow(4, "牌库与选项池", "只读，编辑去网页控制台");
            var libCards = Tk(lib, "cards") as JArray; var places = Tk(lib, "places") as JObject; var acts = Tk(lib, "activities") as JObject;
            if (lib == null && pl == null) c.Add(Txt("没有数据", 12f, Ink2, false, 20));
            else
            {
                if (libCards != null) Pill(c, "牌 " + libCards.Count, false);
                if (acts != null) Pill(c, "活动 " + acts.Count, false);
                if (places != null) Pill(c, "地点 " + places.Count, false);
                if (pl != null) Pill(c, "选项池 " + (int)Num(pl, "check.pools", 0f) + " · 选项 " + (int)Num(pl, "check.options", 0f), false);
                var errs = Tk(pl, "check.errors") as JArray; var errs2 = Tk(lib, "errors") as JArray;
                int ec = (errs != null ? errs.Count : 0) + (errs2 != null ? errs2.Count : 0);
                if (ec > 0) Pill(c, "待修 " + ec, true);
            }

            // 5 关于她
            c = PRow(5, "关于她", "写给她的口味与禁忌");
            var rows = Tk(ab, "rows") as JArray;
            if (ab == null) c.Add(Txt("没有数据", 12f, Ink2, false, 20));
            else if (rows == null || rows.Count == 0) c.Add(Txt("还没写条目", 12f, Ink2, false, 20));
            else
            {
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, rows.Count - WinN));
                int f = Mathf.Min(_pgOff, Mathf.Max(0, rows.Count - WinN));
                var col5 = Col(c);
                for (int i = f; i < f + WinN && i < rows.Count; i++) col5.Add(Txt(Cut(Str(rows[i], "text"), 44), 11.5f, Ink, false, 60));
            }
        }

        // ---------------- 页：记忆 ----------------

        void BuildMem()
        {
            var mem = _bd.Get("memory"); var td = _bd.Get("today");
            if (mem == null && td == null) { Unavail("记忆", "连不上服务器，也没有缓存"); return; }

            var c = PRow(0, "记忆索引", "MEMORY.md 的前几行");
            var col = Col(c);
            var lines = new List<string>();
            foreach (var ln in Str(mem, "index").Split('\n'))
            {
                string t = Regex.Replace(ln, @"\[([^\]]*)\]\([^)]*\)", "$1").Trim().TrimStart('-', ' ');
                if (t.Length > 0) lines.Add(t);
            }
            if (lines.Count == 0) col.Add(Txt("还没有", 12f, Ink2, false, 20));
            else
            {
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, lines.Count - 3));
                int f = Mathf.Min(_pgOff, Mathf.Max(0, lines.Count - 3));
                for (int i = f; i < f + 3 && i < lines.Count; i++) col.Add(Txt(Cut(lines[i], 40), 11f, Ink, false, 60));
            }

            c = PRow(1, "记忆文件", "她记下来的事");
            var files = Tk(mem, "files") as JArray;
            if (files == null || files.Count == 0) c.Add(Txt("还没有", 12f, Ink2, false, 20));
            else
            {
                Pill(c, "共 " + files.Count + " 份", true);
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, files.Count - 3));
                int f = Mathf.Min(_pgOff, Mathf.Max(0, files.Count - 3));
                for (int i = f; i < f + 3 && i < files.Count; i++) Pill(c, Cut(Str(files[i], "file").Replace(".md", ""), 16), false);
            }

            string shown = Str(td, "shown_date");
            bool fb = Bool(td, "fallback");
            string sub = fb && shown.Length > 0 ? "今天还没有，显示 " + shown + " 的" : Str(td, "date");
            c = PRow(2, "今天说了", sub);
            ListRow(c, Tk(td, "talks") as JArray, j => Str(j, "ts") + " " + Str(j, "chat") + "：" + Cut(Str(j, "she").Length > 0 ? "她 " + Str(j, "she") : "他 " + Str(j, "he"), 30));

            c = PRow(3, "笔记", "她给自己记的");
            ListRow(c, Tk(td, "notes") as JArray, j => Str(j, "ts") + " " + Cut(Str(j, "text"), 36));

            c = PRow(4, "主动找你", "她先开口的几次");
            ListRow(c, Tk(td, "reach") as JArray, j => Str(j, "ts") + " " + Cut(Str(j, "text"), 36));

            c = PRow(5, "六层快照", "整页很大，在浏览器里看");
            _snapBtn = Pill(c, "在浏览器打开", true);
        }

        // ---------------- 页：成长 ----------------

        void BuildGrowth()
        {
            var g = _bd.Get("growth");
            if (g == null) { Unavail("成长", "连不上服务器，也没有缓存"); return; }

            var c = PRow(0, "累计", "她攒下的东西");
            string[][] names = { new[] { "experiences", "经历" }, new[] { "reactions", "反应" }, new[] { "reflections", "反思" }, new[] { "examples", "例子" }, new[] { "facts", "事实" }, new[] { "curiosity", "好奇" } };
            foreach (var nm in names)
            {
                var v = Tk(g, "counts." + nm[0]);
                if (v != null) Pill(c, nm[1] + " " + ToF(v).ToString("0"), false);
            }

            c = PRow(1, "最近的想法", "她的反思日记");
            ListRow(c, Tk(g, "thoughts") as JArray, j =>
            {
                string ts = Str(j, "ts");
                if (ts.Length >= 16) ts = ts.Substring(5, 11);      // 11-17 14:25
                return ts + " " + Cut(Str(j, "thought"), 36);
            });

            c = PRow(2, "她学到的", "好 / 坏 / 要教的例子");
            ListRow(c, Tk(g, "examples") as JArray, j =>
            {
                string lab = Str(j, "label");
                string tag = lab == "good" ? "好" : (lab == "bad" ? "差" : "教");
                string rc = Str(j, "reaction");
                return "[" + tag + "] " + Cut(Str(j, "her_line"), 22) + (rc.Length > 0 ? " 回：" + Cut(rc, 12) : "");
            });

            c = PRow(3, "好奇", "她想去看的");
            var cur = Tk(g, "curiosity") as JArray;
            if (cur == null || cur.Count == 0) c.Add(Txt("还没有", 12f, Ink2, false, 20));
            else
            {
                _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, cur.Count - 4));
                int f = Mathf.Min(_pgOff, Mathf.Max(0, cur.Count - 4));
                for (int i = f; i < f + 4 && i < cur.Count; i++) Pill(c, Cut(Str(cur[i], "topic"), 12) + " · " + Str(cur[i], "channel"), false);
            }

            c = PRow(4, "说话的旋钮", "话长 / 靠拢 / 常用表情");
            var kb = Tk(g, "knobs");
            if (kb == null) c.Add(Txt("没有数据", 12f, Ink2, false, 20));
            else
            {
                Pill(c, "话长 ×" + Num(kb, "len_x", 1f).ToString("0.00"), false);
                Pill(c, "靠拢 ×" + Num(kb, "kao_x", 1f).ToString("0.00"), false);
                var sp = Tk(kb, "sticker_pref") as JObject;
                if (sp != null) { int n = 0; foreach (var p in sp.Properties()) { if (n++ >= 3) break; Pill(c, p.Name + " " + ToF(p.Value).ToString("0"), false); } }
            }

            c = PRow(5, "此刻的自述", "周报摘要");
            var col = Col(c);
            string self = Str(g, "self_now"), wk = Str(g, "weekly");
            col.Add(Txt(self.Length > 0 ? Cut(self, 40) : "还没有", 11.5f, Ink, false, 60));
            if (wk.Length > 0) col.Add(Txt("周报：" + Cut(wk, 36), 11f, Ink2, false, 60));
        }

        // ---------------- 通用小件 ----------------

        void Unavail(string title, string msg)
        {
            var c = PRow(0, title, "数据来自服务器控制台");
            c.Add(Txt(msg + "。点右下「重试」，或检查 server 配置。", 12.5f, Ink2, false, 60));
            for (int r = 1; r < RowN; r++) _row[r].style.display = DisplayStyle.None;
        }

        /// <summary>左：标题灰说明；右：控件区。和第 0 页的 Row() 同款。</summary>
        VisualElement PRow(int r, string name, string hint)
        {
            var lab = new VisualElement { pickingMode = PickingMode.Ignore };
            lab.style.width = 220f * _k; lab.style.flexShrink = 0f;
            lab.Add(L(name, 15.5f, Ink, true));
            var h = L(Cut(hint, 22), 11f, Ink2, false); h.style.marginTop = 5f * _k; lab.Add(h);
            var ctl = new VisualElement { pickingMode = PickingMode.Ignore };
            ctl.style.flexGrow = 1f; ctl.style.flexShrink = 1f;
            ctl.style.flexDirection = FlexDirection.Row; ctl.style.flexWrap = Wrap.Wrap;
            ctl.style.alignItems = Align.Center; ctl.style.alignContent = Align.Center;
            ctl.style.maxWidth = 500f * _k;
            _row[r].Add(lab); _row[r].Add(ctl);
            return ctl;
        }

        VisualElement Col(VisualElement parent)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.style.flexDirection = FlexDirection.Column; v.style.flexGrow = 1f; v.style.flexShrink = 1f;
            v.style.maxWidth = 496f * _k;
            parent.Add(v);
            return v;
        }

        Label Txt(string s, float size, Color col, bool bold, int maxChars)
        {
            var l = L(Cut(s, maxChars), size, col, bold);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.maxWidth = 490f * _k;
            l.style.marginBottom = 2f * _k;
            return l;
        }

        VisualElement Pill(VisualElement parent, string text, bool accent)
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var p = new VisualElement { pickingMode = PickingMode.Ignore };
            Round(p, 13f * _k);
            p.style.backgroundColor = accent ? new Color(acc.r, acc.g, acc.b, 0.26f) : new Color(1f, 1f, 1f, 0.55f);
            p.style.paddingLeft = p.style.paddingRight = 10f * _k;
            p.style.paddingTop = p.style.paddingBottom = 4f * _k;
            p.style.marginRight = 6f * _k; p.style.marginTop = 2f * _k; p.style.marginBottom = 2f * _k;
            p.style.flexShrink = 0f;
            p.Add(L(Cut(text, 30), 11.5f, accent ? accDeep : Ink, accent));
            parent.Add(p);
            return p;
        }

        void Bar(VisualElement parent, string name, float v)
        {
            Color acc, accDeep, glassBg, railBg, trackBg;
            ThemePalette(out acc, out accDeep, out glassBg, out railBg, out trackBg);
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.marginBottom = 3f * _k;
            var nm = L(name, 11f, Ink2, false); nm.style.width = 54f * _k;
            var tr = new VisualElement { pickingMode = PickingMode.Ignore };
            tr.style.width = 250f * _k; tr.style.height = 6f * _k; tr.style.backgroundColor = trackBg; Round(tr, 3f * _k);
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(fill, 0, 0, 250f * _k * Mathf.Clamp01(v < 0f ? 0f : v / 100f), 6f * _k);
            fill.style.backgroundColor = acc; Round(fill, 3f * _k);
            tr.Add(fill);
            var val = L(v < 0f ? "--" : Mathf.RoundToInt(v).ToString(), 11.5f, accDeep, true); val.style.marginLeft = 10f * _k;
            row.Add(nm); row.Add(tr); row.Add(val);
            parent.Add(row);
        }

        /// <summary>列表行：最多 WinN 条，滚轮翻看；空列表写「还没有」。</summary>
        void ListRow(VisualElement c, JArray arr, Func<JToken, string> fmt)
        {
            var col = Col(c);
            if (arr == null || arr.Count == 0) { col.Add(Txt("还没有", 12f, Ink2, false, 20)); return; }
            _pgMax = Mathf.Max(_pgMax, Mathf.Max(0, arr.Count - WinN));
            int f = Mathf.Min(_pgOff, Mathf.Max(0, arr.Count - WinN));
            for (int i = f; i < f + WinN && i < arr.Count; i++) col.Add(Txt(fmt(arr[i]), 11.5f, Ink, false, 70));
            if (arr.Count > WinN) col.Add(Txt("共 " + arr.Count + " 条", 10.5f, Ink3, false, 20));
        }

        static List<JToken> FilterMain(JArray a)
        {
            var l = new List<JToken>();
            if (a == null) return l;
            foreach (var x in a) if (Str(x, "layer") == "主") l.Add(x);
            if (l.Count == 0) foreach (var x in a) l.Add(x);
            return l;
        }

        static int ToMin(string hhmm)
        {
            if (string.IsNullOrEmpty(hhmm) || hhmm.Length < 4) return 0;
            int h, m;
            var p = hhmm.Split(':');
            if (p.Length < 2 || !int.TryParse(p[0], out h) || !int.TryParse(p[1], out m)) return 0;
            int v = h * 60 + m;
            return h < 3 ? v + 1440 : v;       // 03:00 换天：凌晨算前一天的尾巴
        }

        // ---------------- JSON 取值 ----------------

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
            if (x == null) return false;
            return x.Type == JTokenType.Boolean && (bool)x;
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
