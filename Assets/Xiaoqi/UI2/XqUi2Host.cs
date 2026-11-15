// XQ: G57 V1 — UI Toolkit today card and 「她开口」 bubble in the pet window.
// Click-through is UniWindowController.HitTestByOpaquePixel (scene hitTestType = Opacity).
// This host registers UniWindowController.ExtraHitTest so a picked solid element counts.
using System;
using System.Collections;
using System.IO;
using Kirurobo;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Xiaoqi.Spike;
using Xiaoqi.UI;

namespace Xiaoqi.UI2
{
    public class XqUi2Host : MonoBehaviour
    {
        const string None = "还没有";

        [Serializable]
        class DayDto
        {
            public string now;
            public string mood;
            public int now_min;
            public SegDto[] segs;
        }

        [Serializable]
        class SegDto
        {
            public int a, b;
            public string name, act;
        }

        PanelSettings _panelSettings;
        UIDocument _doc;
        VisualElement _cardInst;
        VisualElement _bubbleInst;
        VisualElement _cardAnim;
        VisualElement _bubbleAnim;
        HeadAnchor _head;
        DayDto _day;
        bool _built;
        bool _cardSolid = true;
        bool _cardClosing;
        bool _dayMissLogged;
        int _anchorState;
        bool _hitLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != "Mate Engine Main") return;
            if (FindFirstObjectByType<XqUi2Host>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("XqUi2Host");
            go.AddComponent<XqUi2Host>();
        }

        IEnumerator Start()
        {
            for (int i = 0; i < 180 && XiaoqiRuntime.Config == null; i++)
                yield return null;
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            if (cfg == null || !cfg.ui2)
            {
                Debug.Log("XQ_UI2 off");
                Destroy(gameObject);
                yield break;
            }
            var systems = FindFirstObjectByType<XiaoqiSystems>();
            if (systems != null) transform.SetParent(systems.transform, false);
            else DontDestroyOnLoad(gameObject);
            bool built;
            try { built = Build(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                built = false;
            }
            if (!built)
            {
                Destroy(gameObject);
                yield break;
            }
            StartCoroutine(FetchDay());
        }

        void OnDestroy()
        {
            XiaoqiLink.OnStage -= OnStage;
            XiaoqiFeed.FeedChanged -= OnFeed;
            if (UniWindowController.ExtraHitTest == Hit)
                UniWindowController.ExtraHitTest = null;
            if (_panelSettings != null) Destroy(_panelSettings);
        }

        bool Build()
        {
            var cardTree = Resources.Load<VisualTreeAsset>("UI2/TodayCard");
            var bubbleTree = Resources.Load<VisualTreeAsset>("UI2/ChatBubble");
            if (cardTree == null || bubbleTree == null)
            {
                Debug.LogWarning("XQ_UI2 missing UXML");
                return false;
            }

            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.hideFlags = HideFlags.DontSave;
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _panelSettings.scale = 1f;
            _panelSettings.sortingOrder = 500;
            // XQ: a panel clear would paint the whole framebuffer and make every pixel solid.
            _panelSettings.clearColor = false;
            _panelSettings.clearDepthStencil = false;

            var go = new GameObject("XqUi2Document");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            _doc = go.AddComponent<UIDocument>();
            _doc.panelSettings = _panelSettings;
            go.SetActive(true);

            var root = _doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("XQ_UI2 no panel root");
                return false;
            }
            root.pickingMode = PickingMode.Ignore;

            var sheet = Resources.Load<StyleSheet>("UI2/xq");
            _cardInst = Prepare(cardTree, sheet, out _cardAnim);
            _bubbleInst = Prepare(bubbleTree, sheet, out _bubbleAnim);
            root.Add(_cardInst);
            root.Add(_bubbleInst);

            var close = _cardInst.Q<Button>("today-close");
            if (close != null) close.clicked += CloseCard;
            else Debug.LogWarning("XQ_UI2 close button missing");

            XiaoqiLink.OnStage += OnStage;
            XiaoqiFeed.FeedChanged += OnFeed;
            UniWindowController.ExtraHitTest = Hit;
            Paint();
            StartCoroutine(Enter(_cardAnim));
            StartCoroutine(Enter(_bubbleAnim));
            _built = true;

            var uw = UniWindowController.current;
            Debug.Log("XQ_UI2 on hit=" + (uw != null ? uw.hitTestType.ToString() : "no-window"));
            return true;
        }

        VisualElement Prepare(VisualTreeAsset tree, StyleSheet sheet, out VisualElement anim)
        {
            var inst = tree.Instantiate();
            if (sheet != null && !inst.styleSheets.Contains(sheet)) inst.styleSheets.Add(sheet);
            inst.pickingMode = PickingMode.Ignore;
            inst.style.position = Position.Absolute;
            inst.style.left = 0;
            inst.style.top = 0;
            inst.style.width = StyleKeyword.Auto;
            inst.style.height = StyleKeyword.Auto;
            inst.style.flexGrow = 0;
            inst.style.overflow = Overflow.Visible;
            ArmPicking(inst);
            var font = XqUi.Font;
            if (font != null) ApplyFont(inst, FontDefinition.FromFont(font));
            Blank(inst);
            anim = inst.Q(className: "xq-anim") ?? inst;
            anim.AddToClassList("xq-enter");
            return inst;
        }

        static void ArmPicking(VisualElement e)
        {
            if (e == null) return;
            bool solid = e.ClassListContains("xq-card")
                || e.ClassListContains("xq-bubble")
                || e.ClassListContains("xq-bubble__tail")
                || e is Button;
            e.pickingMode = solid ? PickingMode.Position : PickingMode.Ignore;
            for (int i = 0; i < e.childCount; i++) ArmPicking(e[i]);
        }

        static void ApplyFont(VisualElement e, FontDefinition def)
        {
            if (e is TextElement text) text.style.unityFontDefinition = def;
            for (int i = 0; i < e.childCount; i++) ApplyFont(e[i], def);
        }

        static void Blank(VisualElement tree)
        {
            string[] names = { "today-clock", "today-line", "today-doing", "today-mood", "bubble-text" };
            for (int i = 0; i < names.Length; i++)
            {
                var label = tree.Q<Label>(names[i]);
                if (label == null) continue;
                label.text = None;
                label.AddToClassList("xq-missing");
            }
        }

        IEnumerator Enter(VisualElement anim)
        {
            // One presented frame at opacity 0 / +8px, then drop the class so the 0.25s transition runs.
            yield return null;
            yield return new WaitForEndOfFrame();
            if (anim == null) yield break;
            if (anim == _cardAnim && _cardClosing) yield break;
            anim.RemoveFromClassList("xq-enter");
        }

        void CloseCard()
        {
            if (_cardClosing) return;
            _cardClosing = true;
            if (_cardAnim != null) _cardAnim.AddToClassList("xq-enter");
            StartCoroutine(HideCard());
        }

        IEnumerator HideCard()
        {
            yield return new WaitForSecondsRealtime(0.26f);
            _cardSolid = false;
            if (_cardInst != null) _cardInst.style.display = DisplayStyle.None;
        }

        void OnStage(StageData _) { if (_built) Paint(); }
        void OnFeed() { if (_built) Paint(); }

        void Paint()
        {
            var stage = XiaoqiLink.Current;
            string clock = First(DayNow(), ClockOf(stage != null ? stage.ts : null));
            string mood = First(DayMood(), stage != null && stage.face != null ? stage.face.top : null);
            string doing = First(XiaoqiFeed.LatestDoing, DoingOf(stage), DayDoing());
            // No summary field exists. Do not compose a sentence from the other fields.
            string line = stage != null && stage.@event != null ? stage.@event.text : null;
            Set(_cardInst, "today-clock", clock);
            Set(_cardInst, "today-line", line);
            Set(_cardInst, "today-doing", doing);
            Set(_cardInst, "today-mood", mood);
            Set(_bubbleInst, "bubble-text", First(XiaoqiFeed.LatestText, line));
        }

        string DayNow() { return _day != null ? _day.now : null; }
        string DayMood() { return _day != null ? _day.mood : null; }

        string DayDoing()
        {
            if (_day == null || _day.segs == null) return null;
            int now = _day.now_min;
            for (int i = 0; i < _day.segs.Length; i++)
            {
                var s = _day.segs[i];
                if (s == null || now < s.a || now >= s.b) continue;
                if (!string.IsNullOrWhiteSpace(s.act)) return s.act;
                if (!string.IsNullOrWhiteSpace(s.name)) return s.name;
            }
            return null;
        }

        static string DoingOf(StageData stage)
        {
            if (stage == null) return null;
            if (stage.seg != null)
            {
                if (!string.IsNullOrWhiteSpace(stage.seg.act)) return stage.seg.act;
                if (!string.IsNullOrWhiteSpace(stage.seg.name)) return stage.seg.name;
            }
            if (!string.IsNullOrWhiteSpace(stage.pose)) return stage.pose;
            return null;
        }

        static string ClockOf(string ts)
        {
            if (string.IsNullOrWhiteSpace(ts)) return null;
            int i = ts.IndexOf(' ');
            if (i < 0) i = ts.IndexOf('T');
            if (i < 0 || i + 6 > ts.Length) return null;
            return ts.Substring(i + 1, 5);
        }

        static string First(params string[] values)
        {
            if (values == null) return null;
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i])) return values[i].Trim();
            }
            return null;
        }

        static void Set(VisualElement tree, string name, string value)
        {
            if (tree == null) return;
            var label = tree.Q<Label>(name);
            if (label == null) return;
            bool missing = string.IsNullOrWhiteSpace(value);
            label.text = missing ? None : value;
            if (missing) label.AddToClassList("xq-missing");
            else label.RemoveFromClassList("xq-missing");
        }

        void LateUpdate()
        {
            if (!_built || _doc == null || _doc.rootVisualElement == null) return;
            _doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            var panel = _doc.rootVisualElement.panel;
            if (panel == null) return;
            if (_head == null) _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            bool haveHead = _head != null && _head.ScreenPoint.sqrMagnitude > 1f;
            int anchorState = haveHead ? 2 : 1;
            if (_anchorState != anchorState)
            {
                _anchorState = anchorState;
                Debug.Log(haveHead ? "XQ_UI2 anchor head " + _head.ScreenPoint : "XQ_UI2 anchor fallback");
            }
            Vector2 anchor = haveHead
                ? _head.ScreenPoint
                : new Vector2(Screen.width * 0.62f, Screen.height * 0.58f);
            Place(_cardInst, panel, anchor, 36f, 0f, 340f);
            Place(_bubbleInst, panel, anchor, -16f, 72f, 240f);
        }

        static void Place(VisualElement el, IPanel panel, Vector2 anchor, float dx, float dy, float width)
        {
            if (el == null || panel == null) return;
            float x = anchor.x + dx;
            float y = anchor.y + dy;
            if (x + width > Screen.width - 8f) x = anchor.x - width - 12f;
            x = Mathf.Clamp(x, 8f, Mathf.Max(8f, Screen.width - width - 8f));
            y = Mathf.Clamp(y, 48f, Mathf.Max(48f, Screen.height - 16f));
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(x, y));
            el.style.left = p.x;
            el.style.top = p.y;
        }

        bool Hit(Vector2 screenBottomLeft)
        {
            try
            {
                if (!_built || _doc == null || _doc.rootVisualElement == null) return false;
                var panel = _doc.rootVisualElement.panel;
                if (panel == null) return false;
                var picked = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenBottomLeft));
                if (picked == null) return false;
                if (Inside(picked, "xq-card")) return _cardSolid;
                return Inside(picked, "xq-bubble") || Inside(picked, "xq-bubble__tail");
            }
            catch (Exception e)
            {
                if (!_hitLogged)
                {
                    _hitLogged = true;
                    Debug.LogException(e);
                }
                return false;
            }
        }

        static bool Inside(VisualElement ve, string className)
        {
            for (var e = ve; e != null; e = e.parent)
            {
                if (e.resolvedStyle.display == DisplayStyle.None) return false;
                if (e.ClassListContains(className)) return true;
            }
            return false;
        }

        IEnumerator FetchDay()
        {
            var args = Environment.GetCommandLineArgs();
            int fi = Array.IndexOf(args, "-fake-day");
            if (fi >= 0 && fi + 1 < args.Length && File.Exists(args[fi + 1]))
            {
                try { _day = JsonUtility.FromJson<DayDto>(File.ReadAllText(args[fi + 1])); }
                catch (Exception e) { Debug.LogWarning("XQ_UI2 fake day: " + e.Message); }
                Paint();
                yield break;
            }
            while (true)
            {
                var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
                var server = (cfg.server ?? "").TrimEnd('/');
                if (!string.IsNullOrEmpty(server))
                {
                    using (var req = UnityWebRequest.Get(server + "/api/body/day"))
                    {
                        if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                        req.timeout = 6;
                        yield return req.SendWebRequest();
                        if (req.result == UnityWebRequest.Result.Success)
                        {
                            try
                            {
                                var day = JsonUtility.FromJson<DayDto>(req.downloadHandler.text);
                                if (day != null) _day = day;
                            }
                            catch (Exception e) { Debug.LogWarning("XQ_UI2 day parse: " + e.Message); }
                        }
                        else if (!_dayMissLogged)
                        {
                            _dayMissLogged = true;
                            Debug.Log("XQ_UI2 /api/body/day unavailable");
                        }
                    }
                }
                if (_built) Paint();
                yield return new WaitForSecondsRealtime(30f);
            }
        }
    }
}
