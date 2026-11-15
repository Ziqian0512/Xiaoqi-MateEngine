using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-3c 玻璃今天卡。数据和旧 TodayCard 同一条 /api/body/day（含 -fake-day）。
    /// 没有日程就只显示灰字，不编时间表。
    /// </summary>
    public class XqTodayCard
    {
        [Serializable] class DayData
        {
            public int v;
            public string date;
            public string now;
            public int now_min;
            public Seg[] segs;
        }
        [Serializable] class Seg
        {
            public int a, b;
            public string start, end, name, act, where;
        }

        public VisualElement Root { get; private set; }
        public bool IsOpen { get; private set; }

        VisualElement _rows;
        Label _date;
        Label _empty;
        VisualElement _nextBar;
        Label _nextLine;
        MonoBehaviour _host;
        Coroutine _poll;

        public void Build(VisualElement parent)
        {
            Root = Glass();
            Root.style.display = DisplayStyle.None;
            var body = Root.Q(className: "xq-body");

            var title = new Label("今日日程");
            title.AddToClassList("xq-today-title");
            title.pickingMode = PickingMode.Ignore;
            _date = new Label("");
            _date.AddToClassList("xq-today-date");
            _date.pickingMode = PickingMode.Ignore;
            _rows = new VisualElement { pickingMode = PickingMode.Ignore };
            _rows.AddToClassList("xq-today-rows");
            _empty = new Label("");
            _empty.AddToClassList("xq-today-empty");
            _empty.pickingMode = PickingMode.Ignore;
            _empty.style.display = DisplayStyle.None;

            _nextBar = new VisualElement { pickingMode = PickingMode.Ignore };
            _nextBar.AddToClassList("xq-today-next");
            var kicker = new Label("下一件");
            kicker.AddToClassList("xq-today-kicker");
            kicker.pickingMode = PickingMode.Ignore;
            _nextLine = new Label("");
            _nextLine.AddToClassList("xq-today-next-line");
            _nextLine.pickingMode = PickingMode.Ignore;
            _nextBar.Add(kicker);
            _nextBar.Add(_nextLine);
            _nextBar.style.display = DisplayStyle.None;

            body.Add(title);
            body.Add(_date);
            body.Add(_rows);
            body.Add(_empty);
            body.Add(_nextBar);
            parent.Add(Root);
        }

        public bool Hit(VisualElement picked)
        {
            for (var e = picked; e != null; e = e.parent)
                if (e == Root) return true;
            return false;
        }

        public void Open(MonoBehaviour host)
        {
            if (Root == null || host == null) return;
            _host = host;
            IsOpen = true;
            Root.style.display = DisplayStyle.Flex;
            ShowPending();
            if (_poll != null) host.StopCoroutine(_poll);
            _poll = host.StartCoroutine(Poll());
            Debug.Log("XQ_TODAY open");
        }

        public void Close()
        {
            if (!IsOpen && (Root == null || Root.resolvedStyle.display == DisplayStyle.None)) return;
            IsOpen = false;
            if (_host != null && _poll != null) _host.StopCoroutine(_poll);
            _poll = null;
            if (Root != null) Root.style.display = DisplayStyle.None;
        }

        IEnumerator Poll()
        {
            while (IsOpen)
            {
                yield return FetchOnce();
                float t = 0f;
                while (IsOpen && t < 30f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        IEnumerator FetchOnce()
        {
            var args = Environment.GetCommandLineArgs();
            int fi = Array.IndexOf(args, "-fake-day");
            if (fi >= 0 && fi + 1 < args.Length && System.IO.File.Exists(args[fi + 1]))
            {
                DayData fake = null;
                try { fake = JsonUtility.FromJson<DayData>(System.IO.File.ReadAllText(args[fi + 1])); }
                catch (Exception e) { Debug.Log("XQ_TODAY fake " + e.Message); }
                Apply(fake);
                yield break;
            }

            var cfg = XiaoqiRuntime.Config;
            if (cfg == null || string.IsNullOrEmpty(cfg.server))
            {
                Debug.Log("XQ_TODAY no server");
                Apply(null);
                yield break;
            }
            var url = cfg.server.TrimEnd('/') + "/api/body/day";
            using (var req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("XQ_TODAY fail " + req.error);
                    Apply(null);
                    yield break;
                }
                DayData data = null;
                try { data = JsonUtility.FromJson<DayData>(req.downloadHandler.text); }
                catch (Exception e) { Debug.Log("XQ_TODAY parse " + e.Message); }
                Apply(data);
            }
        }

        void ShowPending()
        {
            _rows.Clear();
            _date.text = "";
            _empty.text = "正在看今天…";
            _empty.style.display = DisplayStyle.Flex;
            _nextBar.style.display = DisplayStyle.None;
        }

        void Apply(DayData data)
        {
            if (!IsOpen || _rows == null) return;
            bool any = data != null && data.segs != null && data.segs.Length > 0;
            _date.text = data != null && !string.IsNullOrEmpty(data.date) ? data.date : "";
            _rows.Clear();
            if (!any)
            {
                _empty.text = "今天还没有安排";
                _empty.style.display = DisplayStyle.Flex;
                _nextBar.style.display = DisplayStyle.None;
                return;
            }

            _empty.style.display = DisplayStyle.None;
            int now = data.now_min;
            Seg upcoming = null;
            foreach (var s in data.segs)
            {
                if (s == null) continue;
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("xq-today-row");
                bool past = now > 0 && now >= s.b;
                bool current = now > 0 && now >= s.a && now < s.b;
                if (past) row.AddToClassList("xq-past");
                if (current) row.AddToClassList("xq-now");
                if (upcoming == null && now > 0 && s.a > now) upcoming = s;

                var time = new Label(When(s));
                time.AddToClassList("xq-today-time");
                time.pickingMode = PickingMode.Ignore;
                var name = new Label(What(s));
                name.AddToClassList("xq-today-name");
                name.pickingMode = PickingMode.Ignore;
                row.Add(time);
                row.Add(name);
                _rows.Add(row);
            }

            _nextBar.style.display = DisplayStyle.Flex;
            if (upcoming == null) _nextLine.text = "还没有";
            else
            {
                string when = When(upcoming);
                string what = What(upcoming);
                _nextLine.text = string.IsNullOrEmpty(what) ? when : (string.IsNullOrEmpty(when) ? what : when + "  " + what);
            }
        }

        static string When(Seg s)
        {
            if (!string.IsNullOrEmpty(s.start) && !string.IsNullOrEmpty(s.end)) return s.start + "–" + s.end;
            if (!string.IsNullOrEmpty(s.start)) return s.start;
            return s.end ?? "";
        }

        static string What(Seg s)
        {
            if (!string.IsNullOrEmpty(s.name)) return s.name;
            if (!string.IsNullOrEmpty(s.act)) return s.act;
            if (s.where == "dorm") return "宿舍";
            if (s.where == "out") return "外出";
            return s.where ?? "";
        }

        static VisualElement Glass()
        {
            var host = new VisualElement { name = "xq-today", pickingMode = PickingMode.Ignore };
            host.AddToClassList("xq-today");
            var shadow = new VisualElement();
            shadow.AddToClassList("xq-shadow");
            var body = new VisualElement { name = "xq-body" };
            body.AddToClassList("xq-body");
            var glass = new VisualElement();
            glass.AddToClassList("xq-glass");
            var hi = new VisualElement();
            hi.AddToClassList("xq-hi");
            body.Add(glass);
            body.Add(hi);
            host.Add(shadow);
            host.Add(body);
            return host;
        }
    }
}
