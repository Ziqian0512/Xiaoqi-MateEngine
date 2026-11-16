using System;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Spike;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-3 桌宠界面。尺寸是样式稿 px，由 PanelSettings.scale（身高/364）变成物理像素。
    /// 空像素不设背景，穿透仍走 ME 的 opacity hit-test。
    /// </summary>
    public class XqPetHud
    {
        const float FadeSec = 0.6f;
        const float IdleSec = 8f;
        const float NearPx = 400f;

        public VisualElement Root { get; private set; }
        public VisualElement Bubble { get; private set; }

        VisualElement _next;
        VisualElement _here;
        VisualElement _ringHost;
        XqArc _arc;
        Label _bubbleText;
        Label _nextLine;
        Label _hereLine;
        Label _ringWord;
        VisualElement _gate;
        VisualElement _entries;
        VisualElement _quiet;
        Label _quietWord;
        Label _ringDetail;
        XqTailDown _tail;
        float _menu;
        float _menuFrom;
        float _menuTarget;
        float _menuT0;
        bool _menuOpen;
        bool _near;
        bool _ringOpen;
        float _chipFrom;
        float _chipTarget = 1f;
        float _chipT0;
        bool _chipReady;
        XqTodayCard _today;
        bool _armPet;
        Vector3 _armPos;
        float _armAt;
        string _say;
        float _sayUntil;
        bool _sayOpen;
        float _chip;
        float _chipVel;
        float _lastOp;
        bool _wasNear;

        public void Attach(VisualElement parent, Font font)
        {
            Root = new VisualElement { name = "xq-hud", pickingMode = PickingMode.Ignore };
            Root.AddToClassList("xq-hud");
            if (font != null) Root.style.unityFont = font;

            Bubble = Glass("xq-bubble-real", false);
            _tail = new XqTailDown();
            _tail.AddToClassList("xq-tail-down");
            _bubbleText = new Label();
            _bubbleText.AddToClassList("xq-copy");
            Bubble.Q(className: "xq-body").Add(_bubbleText);
            Bubble.Add(_tail);

            _next = Glass("xq-cap", false);
            _next.AddToClassList("xq-next");
            _next.Q(className: "xq-body").Add(Icon("clock"));
            _nextLine = Column(_next, "下一件事", "");

            _here = Glass("xq-cap", false);
            _here.Q(className: "xq-body").Add(Icon("book"));
            _hereLine = Column(_here, "她在", "还没有");

            _ringHost = Glass("xq-ring", true);
            _arc = new XqArc();
            _arc.AddToClassList("xq-arc");
            _ringWord = new Label("还没有");
            _ringWord.AddToClassList("xq-ring-word");
            var ringBody = _ringHost.Q(className: "xq-body");
            ringBody.Add(_arc);
            ringBody.Add(_ringWord);

            _gate = Glass("xq-gate", true);
            var mark = new XqExpand();
            mark.AddToClassList("xq-gate-mark");
            mark.pickingMode = PickingMode.Ignore;
            _gate.Q(className: "xq-body").Add(mark);
            _gate.Q(className: "xq-body").RegisterCallback<ClickEvent>(_ => ToggleEntries());

            _entries = new VisualElement { pickingMode = PickingMode.Ignore };
            _entries.AddToClassList("xq-entries");
            _entries.Add(Entry("桌宠", true, () => { SetView("pet"); SetMenu(false); }));
            _entries.Add(Soon("近景"));
            _entries.Add(Soon("工作台"));
            _entries.style.display = DisplayStyle.None;

            _quiet = new VisualElement { pickingMode = PickingMode.Ignore };
            _quiet.AddToClassList("xq-quiet");
            var shade = new Label("");
            shade.AddToClassList("xq-quiet-shade");
            shade.pickingMode = PickingMode.Ignore;
            _quietWord = new Label("");
            _quietWord.AddToClassList("xq-quiet-word");
            _quietWord.pickingMode = PickingMode.Ignore;
            _quiet.Add(shade);
            _quiet.Add(_quietWord);
            _quiet.style.display = DisplayStyle.None;

            _ringDetail = new Label("");
            _ringDetail.AddToClassList("xq-ring-detail");
            _ringDetail.style.display = DisplayStyle.None;
            ringBody.RegisterCallback<ClickEvent>(_ => ToggleRing());

            Root.Add(Bubble);
            Root.Add(_quiet);
            Root.Add(_next);
            Root.Add(_here);
            Root.Add(_ringHost);
            Root.Add(_ringDetail);
            Root.Add(_entries);
            Root.Add(_gate);
            _today = new XqTodayCard();
            _today.Build(Root);
            parent.Add(Root);

            XiaoqiLink.OnStage += _ => Refresh();
            XiaoqiFeed.OnSay += OnSay;
            if (!string.IsNullOrEmpty(XiaoqiFeed.LatestSay)) OnSay(XiaoqiFeed.LatestSay);
            _lastOp = Time.unscaledTime;
            Refresh();
        }

        public void Tick(IPanel panel, float scale)
        {
            if (Root == null || panel == null) return;
            Refresh();
            NoteActivity();
            Animate(scale);
            Place(panel, scale);
            if (_tail != null) _tail.SetColor(new Color(1f, 1f, 1f, 0.9f));
        }

        public bool HitBubble(VisualElement picked)
        {
            return Inside(Bubble, picked);
        }

        public void ToggleSay()
        {
            _sayOpen = !_sayOpen;
            Bubble.EnableInClassList("xq-open", _sayOpen);
            NoteOperation();
        }

        public void HandlePointer(MonoBehaviour host)
        {
            if (Root == null || Root.panel == null || _today == null) return;
            var picked = Root.panel.Pick(RuntimePanelUtils.ScreenToPanel(Root.panel, Input.mousePosition));
            if (_today.IsOpen && Input.GetKeyDown(KeyCode.Escape)) _today.Close();
            if (Input.GetMouseButtonDown(0))
            {
                if (HitBubble(picked)) ToggleSay();
                bool chrome = BlocksChrome(picked);
                bool onHer = OnBody() && !chrome;
                if (_today.IsOpen)
                {
                    if (_today.Hit(picked)) _today.Close();
                    else if (!chrome && !onHer) _today.Close();
                }
                _armPet = onHer;
                _armPos = Input.mousePosition;
                _armAt = Time.unscaledTime;
            }
            if (Input.GetMouseButtonUp(0) && _armPet)
            {
                _armPet = false;
                float moved = Vector2.Distance(Input.mousePosition, _armPos);
                bool click = Time.unscaledTime - _armAt <= 0.35f && moved <= 8f;
                if (!click) return;
                if (_today.IsOpen) _today.Close();
                else _today.Open(host);
            }
        }

        bool BlocksChrome(VisualElement picked)
        {
            return Inside(Bubble, picked) || Inside(_next, picked) || Inside(_here, picked)
                || Inside(_ringHost, picked) || Inside(_ringDetail, picked) || Inside(_gate, picked)
                || Inside(_entries, picked) || Inside(_quiet, picked)
                || (_today != null && _today.Hit(picked));
        }

        static bool OnBody()
        {
            var r = Body();
            return r.width > 2f && r.height > 2f && r.Contains((Vector2)Input.mousePosition);
        }

        void OnSay(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (XiaoqiRuntime.Config != null && !XiaoqiRuntime.Config.bubble) return;
            if (XiaoqiRuntime.IsQuiet()) return;
            _say = text;
            _sayOpen = false;
            Bubble.EnableInClassList("xq-open", false);
            _sayUntil = Time.unscaledTime + 7f;
            if (_bubbleText != null) _bubbleText.text = text;
        }

        void Refresh()
        {
            var s = XiaoqiLink.Current;
            bool asleep = PetPresence.IsAsleep || (s != null && s.pose == "sleep");
            // XQ: P1-6 「路上」(transit) 也算她不在宿舍：样子和外出一样
            bool away = s != null && !string.IsNullOrEmpty(s.where) && s.where != "dorm";

            bool showNext = s != null && s.next != null && HasNext(s.next);
            _next.style.display = showNext ? DisplayStyle.Flex : DisplayStyle.None;
            if (showNext) _nextLine.text = NextLine(s.next);

            if (asleep)
            {
                string place = s != null ? Where(s.where) : "";
                _hereLine.text = string.IsNullOrEmpty(place) ? "睡着了" : "睡着了 · " + place;
            }
            else if (s == null)
                _hereLine.text = "还没有";
            else if (away)
                _hereLine.text = AwayLine(s);
            else
            {
                string place = Where(s.where);
                string act = s.seg != null ? (string.IsNullOrEmpty(s.seg.name) ? s.seg.act : s.seg.name) : "";
                _hereLine.text = string.IsNullOrEmpty(place) && string.IsNullOrEmpty(act)
                    ? "还没有"
                    : (string.IsNullOrEmpty(act) ? place : place + " · " + act);
            }

            if (s == null || s.body == null)
            {
                _ringWord.text = "还没有";
                _arc.Amount = 0f;
                _arc.MarkDirtyRepaint();
            }
            else
            {
                // 假数据里 stamina/sleepy 是 0–100。字的阈值写在回报里，等 WB 确认量纲。
                float st = Mathf.Clamp(s.body.stamina, 0f, 100f);
                _arc.Amount = st / 100f;
                _arc.MarkDirtyRepaint();
                if (s.body.sleepy >= 70f) _ringWord.text = "困";
                else if (st <= 35f) _ringWord.text = "累";
                else _ringWord.text = "精神";
            }

            bool bubbleOff = XiaoqiRuntime.Config != null && !XiaoqiRuntime.Config.bubble;
            bool quiet = XiaoqiRuntime.IsQuiet();
            bool showSay = !quiet && !bubbleOff && !string.IsNullOrEmpty(_say) && Time.unscaledTime < _sayUntil;
            Bubble.style.display = showSay ? DisplayStyle.Flex : DisplayStyle.None;
            if (showSay && _bubbleText.text != _say) _bubbleText.text = _say;

            if (quiet)
            {
                _next.style.display = DisplayStyle.None;
                _here.style.display = DisplayStyle.None;
                _ringHost.style.display = DisplayStyle.None;
                _ringDetail.style.display = DisplayStyle.None;
                _quiet.style.display = DisplayStyle.Flex;
                string q = XiaoqiRuntime.QuietLabel();
                _quietWord.text = q;
                _quiet.Q<Label>(className: "xq-quiet-shade").text = q;
            }
            else
            {
                _quiet.style.display = DisplayStyle.None;
                _here.style.display = DisplayStyle.Flex;
                if (asleep || away)
                {
                    _next.style.display = DisplayStyle.None;
                    _ringHost.style.display = DisplayStyle.None;
                    _ringDetail.style.display = DisplayStyle.None;
                }
                else
                {
                    _ringHost.style.display = DisplayStyle.Flex;
                    _ringDetail.style.display = _ringOpen ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        void NoteActivity()
        {
            if (PetHost.I != null && PetHost.I.IsDragging) NoteOperation();
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
            {
                var r = Body();
                if (r.width > 2f && r.Contains((Vector2)Input.mousePosition)) NoteOperation();
            }
            var body = Body();
            if (body.width > 2f)
            {
                var p = (Vector2)Input.mousePosition;
                float dx = Mathf.Max(body.xMin - p.x, 0f, p.x - body.xMax);
                float dy = Mathf.Max(body.yMin - p.y, 0f, p.y - body.yMax);
                bool near = dx * dx + dy * dy <= NearPx * NearPx || body.Contains(p);
                if (near && !_wasNear) NoteOperation();
                _wasNear = near;
                _near = near;
            }
        }

        void NoteOperation() { _lastOp = Time.unscaledTime; }

        void Animate(float scale)
        {
            string mode = XiaoqiRuntime.Config != null ? XiaoqiRuntime.Config.chips : "fade";
            if (string.IsNullOrEmpty(mode)) mode = "fade";
            float target = 1f;
            if (mode == "off") target = 0f;
            else if (mode == "stay") target = 1f;
            else target = Time.unscaledTime - _lastOp > IdleSec ? 0f : 1f;

            if (!_chipReady)
            {
                _chip = _chipFrom = _chipTarget = target;
                _chipT0 = Time.unscaledTime;
                _chipReady = true;
            }
            else if (!Mathf.Approximately(target, _chipTarget))
            {
                _chipFrom = _chip;
                _chipTarget = target;
                _chipT0 = Time.unscaledTime;
            }
            float u = Mathf.Clamp01((Time.unscaledTime - _chipT0) / FadeSec);
            _chip = Mathf.Lerp(_chipFrom, _chipTarget, Ease(u));
            ApplyChip(_next, 0f, scale);
            ApplyChip(_here, -1.7f, scale);
            ApplyChip(_ringHost, -3.4f, scale);

            float want = _menuOpen ? 1f : 0f;
            if (!Mathf.Approximately(want, _menuTarget))
            {
                _menuFrom = _menu;
                _menuTarget = want;
                _menuT0 = Time.unscaledTime;
            }
            float mu = Mathf.Clamp01((Time.unscaledTime - _menuT0) / 0.25f);
            _menu = Mathf.Lerp(_menuFrom, _menuTarget, Ease(mu));
        }

        void ToggleEntries()
        {
            SetMenu(!_menuOpen);
            NoteOperation();
        }

        void SetMenu(bool open)
        {
            _menuOpen = open;
        }

        void ToggleRing()
        {
            _ringOpen = !_ringOpen;
            NoteOperation();
            if (!_ringOpen)
            {
                _ringDetail.style.display = DisplayStyle.None;
                return;
            }
            var s = XiaoqiLink.Current;
            if (s == null || s.body == null) _ringDetail.text = "还没有";
            else
            {
                int n = Mathf.RoundToInt(Mathf.Clamp(s.body.stamina, 0f, 100f));
                string mood = s.face != null ? s.face.top : "";
                _ringDetail.text = string.IsNullOrEmpty(mood) ? "精力 " + n : mood + " · 精力 " + n;
            }
            _ringDetail.style.display = DisplayStyle.Flex;
        }

        static void SetView(string view)
        {
            Debug.Log("XQ_VIEW view=" + view);
        }

        static VisualElement Entry(string label, bool on, Action act)
        {
            var el = new Label(label);
            el.AddToClassList("xq-entry");
            el.AddToClassList(on ? "xq-entry-on" : "xq-entry-soon");
            if (on) el.RegisterCallback<ClickEvent>(_ => act());
            return el;
        }

        static VisualElement Soon(string label)
        {
            var host = new VisualElement();
            host.AddToClassList("xq-entry");
            host.AddToClassList("xq-entry-soon");
            var text = new Label(label);
            text.pickingMode = PickingMode.Ignore;
            var tip = new Label("下一阶段开放");
            tip.AddToClassList("xq-soon-tip");
            tip.pickingMode = PickingMode.Ignore;
            host.Add(text);
            host.Add(tip);
            host.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            host.RegisterCallback<MouseEnterEvent>(_ => tip.EnableInClassList("xq-tip-on", true));
            host.RegisterCallback<MouseLeaveEvent>(_ => tip.EnableInClassList("xq-tip-on", false));
            return host;
        }

        void ApplyChip(VisualElement el, float phase, float scale)
        {
            if (el == null || el.resolvedStyle.display == DisplayStyle.None) return;
            float bob = Mathf.Sin((Time.unscaledTime + phase) * (Mathf.PI * 2f / 7f)) * 5f * scale;
            el.style.opacity = _chip;
            el.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.96f, 1f, _chip));
            el.style.translate = new Translate(Mathf.Lerp(14f * scale, 0f, _chip), bob, 0f);
        }

        void Place(IPanel panel, float scale)
        {
            var r = Body();
            if (r.height < 8f) return;
            float H = r.height;
            float W = r.width > 2f ? r.width : H * 0.58f;
            float cx = r.x + r.width * 0.5f;
            float top = r.yMax;
            float left = r.xMin;

            PlaceBox(panel, Bubble, cx + 0.28f * W - Width(Bubble, 236f * scale), top + 8f * scale);
            float y44 = top - 0.44f * H;
            float y61 = top - 0.61f * H;
            float y78 = top - 0.78f * H;
            PlaceRight(panel, _next, left - 1.0f * W, y44);
            PlaceRight(panel, _here, left - 0.8f * W, y61);
            PlaceRight(panel, _ringHost, left - 0.26f * W, y78);
            KeepOffFace(panel, _next, r, H);
            KeepOffFace(panel, _here, r, H);
            KeepOffFace(panel, _ringHost, r, H);
            float mid82 = top - 0.82f * H;
            float gw = Width(_gate, 40f * scale);
            float gh = Height(_gate, 40f * scale);
            float gateRight = r.xMax + 6f * scale;
            PlaceBox(panel, _gate, gateRight - gw, mid82 - gh * 0.5f);
            if (_entries != null)
            {
                float ew = Width(_entries, 72f);
                float eh = Height(_entries, 108f);
                float above = mid82 + gh * 0.5f + 8f * scale + eh;
                var ep = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(gateRight - ew, above));
                _entries.style.left = ep.x;
                _entries.style.top = ep.y;
                _entries.style.translate = new Translate((1f - _menu) * 10f * scale, 0f, 0f);
                _entries.style.opacity = _menu;
                _entries.style.display = _menu > 0.02f ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_quiet != null && _quiet.resolvedStyle.display != DisplayStyle.None)
            {
                float qw = Width(_quiet, 120f);
                var qp = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(cx - qw * 0.5f, top + 28f * scale));
                _quiet.style.left = qp.x;
                _quiet.style.top = qp.y;
            }
            if (_ringDetail != null && _ringOpen && _ringHost != null)
            {
                float dw = Width(_ringDetail, 96f);
                float ringRight = left - 0.26f * W;
                var dp = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(ringRight - Width(_ringHost, 64f) - dw - 8f, y78 + 16f));
                _ringDetail.style.left = dp.x;
                _ringDetail.style.top = dp.y;
            }
            bool showGate = _near || _menuOpen;
            if (_gate != null)
            {
                _gate.style.opacity = showGate ? 1f : 0f;
                _gate.style.display = showGate ? DisplayStyle.Flex : DisplayStyle.None;
            }
            PlaceToday(panel, r, scale);
        }

        void PlaceToday(IPanel panel, Rect body, float scale)
        {
            if (_today == null || !_today.IsOpen || _today.Root == null) return;
            var card = _today.Root;
            float cw = Width(card, 280f * scale);
            float ch = Height(card, 180f * scale);
            float left = body.xMin - 16f * scale - cw;
            float bottom = body.center.y - ch * 0.5f;
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(left, bottom + ch));
            var origin = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            var span = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(cw, ch));
            float pw = Mathf.Abs(span.x - origin.x);
            float ph = Mathf.Abs(span.y - origin.y);
            float limitW = Root != null ? Root.worldBound.width : 0f;
            float limitH = Root != null ? Root.worldBound.height : 0f;
            if (limitW > 16f) p.x = Mathf.Clamp(p.x, 8f, Mathf.Max(8f, limitW - pw - 8f));
            if (limitH > 16f) p.y = Mathf.Clamp(p.y, 8f, Mathf.Max(8f, limitH - ph - 8f));
            card.style.left = p.x;
            card.style.top = p.y;
        }

        static void KeepOffFace(IPanel panel, VisualElement el, Rect body, float H)
        {
            if (el == null || el.resolvedStyle.display == DisplayStyle.None) return;
            var face = new Rect(body.xMin, body.yMax - H * 0.28f, body.width, H * 0.28f);
            var b = el.worldBound;
            if (b.width < 2f || !b.Overlaps(PanelRect(panel, face))) return;
            float shift = PanelRect(panel, face).xMin - b.xMax - 8f;
            if (shift < 0f) el.style.left = el.resolvedStyle.left + shift;
        }

        static Rect PanelRect(IPanel panel, Rect screen)
        {
            var a = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.xMin, screen.yMax));
            var c = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.xMax, screen.yMin));
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static void PlaceBox(IPanel panel, VisualElement el, float screenLeft, float screenBottom)
        {
            if (el == null) return;
            float h = Height(el, 48f);
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenLeft, screenBottom + h));
            el.style.left = p.x;
            el.style.top = p.y;
        }

        static void PlaceRight(IPanel panel, VisualElement el, float screenRight, float screenMidY)
        {
            if (el == null) return;
            float w = Width(el, 120f);
            float h = Height(el, 46f);
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenRight - w, screenMidY + h * 0.5f));
            el.style.left = p.x;
            el.style.top = p.y;
        }

        static float Width(VisualElement el, float fallback)
        {
            float w = el.resolvedStyle.width;
            return float.IsNaN(w) || w < 2f ? fallback : w;
        }

        static float Height(VisualElement el, float fallback)
        {
            float h = el.resolvedStyle.height;
            return float.IsNaN(h) || h < 2f ? fallback : h;
        }

        static Rect Body()
        {
            if (PetHost.I == null) return new Rect(0, 0, 0, 0);
            return PetHost.I.ScreenRect();
        }

        static bool HasNext(StageData.StageNext n)
        {
            return !string.IsNullOrEmpty(n.at) || !string.IsNullOrEmpty(n.where)
                || !string.IsNullOrEmpty(n.spot) || !string.IsNullOrEmpty(n.pose);
        }

        static string NextLine(StageData.StageNext n)
        {
            string place = Where(n.where);
            if (string.IsNullOrEmpty(place)) place = n.spot ?? "";
            if (!string.IsNullOrEmpty(n.at) && !string.IsNullOrEmpty(place)) return n.at + "  " + place;
            if (!string.IsNullOrEmpty(n.at)) return n.at;
            return place;
        }

        // XQ: P1-6 §3.4：「她出门了 · 约 18:00 回来」。回来的时间取下一段的开始时间（和头顶标签同口径），没有就不写。
        static string AwayLine(StageData s)
        {
            string back = s != null && s.next != null ? s.next.at : "";
            return string.IsNullOrEmpty(back) ? "她出门了" : "她出门了 · 约 " + back + " 回来";
        }

        static string Where(string where)
        {
            if (string.IsNullOrEmpty(where)) return "";
            if (where == "dorm") return "宿舍";
            if (where == "out") return "外出";
            return where;
        }

        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        static bool Inside(VisualElement root, VisualElement picked)
        {
            for (var e = picked; e != null; e = e.parent)
                if (e == root) return true;
            return false;
        }

        static VisualElement Glass(string cls, bool emph)
        {
            var host = new VisualElement { pickingMode = PickingMode.Ignore };
            host.AddToClassList(cls);
            var shadow = new VisualElement();
            shadow.AddToClassList("xq-shadow");
            var body = new VisualElement { name = "xq-body" };
            body.AddToClassList("xq-body");
            var glass = new VisualElement();
            glass.AddToClassList("xq-glass");
            if (emph) glass.AddToClassList("xq-glass-emph");
            var hi = new VisualElement();
            hi.AddToClassList("xq-hi");
            body.Add(glass);
            body.Add(hi);
            host.Add(shadow);
            host.Add(body);
            return host;
        }

        static Label Column(VisualElement host, string kicker, string line)
        {
            var col = new VisualElement();
            col.AddToClassList("xq-cap-col");
            var a = new Label(kicker);
            a.AddToClassList("xq-cap-kicker");
            var b = new Label(line);
            b.AddToClassList("xq-cap-line");
            col.Add(a);
            col.Add(b);
            host.Q(className: "xq-body").Add(col);
            return b;
        }

        static VisualElement Icon(string kind)
        {
            var icon = new XqIcon(kind);
            icon.AddToClassList("xq-icon");
            return icon;
        }

        sealed class XqTailDown : VisualElement
        {
            Color _c = new Color(1f, 1f, 1f, 0.9f);
            public XqTailDown()
            {
                generateVisualContent += Draw;
                pickingMode = PickingMode.Position;
            }
            public void SetColor(Color c)
            {
                if (_c == c) return;
                _c = c;
                MarkDirtyRepaint();
            }
            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 1f || h < 1f) return;
                var p = ctx.painter2D;
                p.fillColor = _c;
                p.BeginPath();
                p.MoveTo(new Vector2(0f, 0f));
                p.LineTo(new Vector2(w, 0f));
                p.LineTo(new Vector2(w * 0.5f, h));
                p.ClosePath();
                p.Fill();
            }
        }

        sealed class XqArc : VisualElement
        {
            public float Amount;
            public XqArc()
            {
                generateVisualContent += Draw;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 7;
                style.top = 7;
                style.width = 50;
                style.height = 50;
            }
            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 4f) return;
                var c = new Vector2(w * 0.5f, h * 0.5f);
                float rad = Mathf.Min(w, h) * 0.5f - 2f;
                var p = ctx.painter2D;
                p.lineWidth = 4f;
                p.strokeColor = new Color(1f, 1f, 1f, 0.55f);
                p.BeginPath();
                p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(359f));
                p.Stroke();
                if (Amount > 0.01f)
                {
                    p.strokeColor = new Color(1f, 0.56f, 0.72f, 1f);
                    p.BeginPath();
                    p.Arc(c, rad, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * Mathf.Clamp01(Amount)));
                    p.Stroke();
                }
            }
        }

        sealed class XqIcon : VisualElement
        {
            readonly string _kind;
            public XqIcon(string kind)
            {
                _kind = kind;
                generateVisualContent += Draw;
                pickingMode = PickingMode.Ignore;
            }
            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 4f) return;
                var p = ctx.painter2D;
                var c = new Vector2(w * 0.5f, h * 0.5f);
                p.strokeColor = new Color(0.84f, 0.28f, 0.5f, 1f);
                p.lineWidth = 1.6f;
                if (_kind == "clock")
                {
                    p.BeginPath();
                    p.Arc(c, w * 0.28f, Angle.Degrees(0), Angle.Degrees(359));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(c);
                    p.LineTo(c + new Vector2(0f, -w * 0.16f));
                    p.MoveTo(c);
                    p.LineTo(c + new Vector2(w * 0.12f, 0f));
                    p.Stroke();
                }
                else
                {
                    p.BeginPath();
                    p.MoveTo(new Vector2(w * 0.28f, h * 0.3f));
                    p.LineTo(new Vector2(w * 0.72f, h * 0.3f));
                    p.LineTo(new Vector2(w * 0.72f, h * 0.72f));
                    p.LineTo(new Vector2(w * 0.28f, h * 0.72f));
                    p.ClosePath();
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(new Vector2(w * 0.28f, h * 0.48f));
                    p.LineTo(new Vector2(w * 0.72f, h * 0.48f));
                    p.Stroke();
                }
            }
        }

        sealed class XqExpand : VisualElement
        {
            static readonly CustomStyleProperty<Color> Ink = new CustomStyleProperty<Color>("--xq-accent-ink");
            Color _c = new Color(0.84f, 0.28f, 0.5f, 1f);

            public XqExpand()
            {
                generateVisualContent += Draw;
                pickingMode = PickingMode.Ignore;
                RegisterCallback<CustomStyleResolvedEvent>(_ =>
                {
                    if (customStyle.TryGetValue(Ink, out var c) && c != _c)
                    {
                        _c = c;
                        MarkDirtyRepaint();
                    }
                });
            }

            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 4f || h < 4f) return;
                var p = ctx.painter2D;
                p.strokeColor = _c;
                p.lineWidth = 1.6f;
                float a = w * 0.22f;
                float b = h * 0.22f;
                float m = 1.5f;
                Corner(p, m, m, a, b, 1f, 1f);
                Corner(p, w - m, m, a, b, -1f, 1f);
                Corner(p, m, h - m, a, b, 1f, -1f);
                Corner(p, w - m, h - m, a, b, -1f, -1f);
            }

            static void Corner(Painter2D p, float x, float y, float a, float b, float sx, float sy)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(x + sx * a, y));
                p.LineTo(new Vector2(x, y));
                p.LineTo(new Vector2(x, y + sy * b));
                p.Stroke();
            }
        }
    }
}
