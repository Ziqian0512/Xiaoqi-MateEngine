using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xiaoqi.Host;

namespace Xiaoqi.UI
{
    /// <summary>G55 §4：B2 半月弧右键菜单。为她头侧一列白底文字胶囊。</summary>
    public class XqMenu : MonoBehaviour
    {
        private const float RadiusPx = 150f;
        private const float ArcRadiusOffset = 14f;

        private class Slot
        {
            public string Id;
            public string Text;
            public XqUi.CapsuleView View;
            public CanvasGroup Group;
            public Vector2 BasePos;
            public float Hover;
            public bool Gray;
            public string Hint;
            public Action Act;
        }

        private Canvas _canvas;
        private RectTransform _canvasRt;
        private GameObject _go;
        private RectTransform _root;
        private CanvasGroup _group;

        private XqUi.CapsuleView _status;
        private CanvasGroup _statusGroup;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Slot> _sub = new List<Slot>();
        private string _subKind = "";
        private Text _settings;
        private CanvasGroup _settingsGroup;
        private Image _underline;
        private Image _arcRight, _arcLeft;
        private CanvasGroup _arcGroup;
        private CanvasGroup _arcGroupL;
        private GameObject _dotsRoot;
        private CanvasGroup _dotsGroup;
        private readonly List<Image> _dots = new List<Image>();
        private Sprite _dotSprite;
        private float _dotR;
        private int _dotCount;
        private Text _hint;
        private CanvasGroup _hintGroup;

        private bool _open, _closing, _left;
        private bool _loggedAlpha;
        private bool _settled;
        private bool _demo;
        private bool _demoMode;
        private float _demoAt;
        private bool _shot;
        private string _shotPath;
        private bool _shotStarted;
        private string _demoSub;
        private Vector2 _forced;
        private bool _forceCenter;
        private float _since;
        private float _s = 1f;
        private Vector2 _center;
        private float _maxWidth;
        private float _capH;
        private float _avatarScale = 1f;
        private float _r;
        private float _menuScale = 1f;
        private Image _link;

        private static XqMenu _instance;

        /// <summary>半弧菜单开着（含关闭动画那 0.12 秒）。桌宠界面那一层据此让开。</summary>
        public static bool IsOpen { get { return (_instance != null && _instance._open) || Xiaoqi.UI2.XqGlassMenu.IsOpen; } }
        /// <summary>最近一次真正关掉的帧号，用来挡「同一下点击既关菜单又出今天卡」。</summary>
        public static int LastCloseFrame = -100;
        /// <summary>玻璃版右键菜单（XqGlassMenu）接管后，旧菜单不再响应右键。</summary>
        public static bool Replaced;

        /// <summary>今天卡走新的玻璃版；找不到桌宠界面才退回旧卡。</summary>
        private static void OpenToday()
        {
            if (!Xiaoqi.UI2.XqUi2Sample.OpenToday()) Xiaoqi.Spike.TodayCard.DebugOpen();
        }

        private void Awake()
        {
            _instance = this;
            Build();
            _go.SetActive(false);
        }

        private void Start()
        {
            PetHost.I?.SuppressHostMenu();
            PetHost.I?.RegisterMenuBlocker(() => IsOpen);
            _demo = Array.IndexOf(Environment.GetCommandLineArgs(), "-menu-demo") >= 0;
            _shot = Array.IndexOf(Environment.GetCommandLineArgs(), "-menu-shot") >= 0;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo-doing") >= 0) _demoSub = "doing";
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo-quiet") >= 0) _demoSub = "quiet";
            if (_shot) _demo = true;
            _demoMode = _demo || _shot;
            if (_demo) _demoAt = Time.unscaledTime + 2.5f;
            if (_shot)
            {
                var dir = System.IO.Path.Combine(Application.persistentDataPath, "shots");
                System.IO.Directory.CreateDirectory(dir);
                _shotPath = System.IO.Path.Combine(dir, "menu.png");
            }            var cargs = Environment.GetCommandLineArgs();
            int ci = Array.IndexOf(cargs, "-capture");
            if (ci >= 0 && ci + 1 < cargs.Length)
                StartCoroutine(CaptureRoutine(cargs[ci + 1]));
            int fi2 = Array.IndexOf(cargs, "-frames");
            if (fi2 >= 0 && fi2 + 1 < cargs.Length)
                StartCoroutine(FramesRoutine(cargs[fi2 + 1]));
            if (Array.IndexOf(cargs, "-demo-a4") >= 0)
                StartCoroutine(DemoA4());
            if (Array.IndexOf(cargs, "-demo-a6") >= 0)
                StartCoroutine(DemoA6());
            if (Array.IndexOf(cargs, "-demo-idle") >= 0)
                StartCoroutine(DemoIdle());
            if (Array.IndexOf(cargs, "-demo-chibi") >= 0)
                StartCoroutine(DemoChibi());
            if (Array.IndexOf(cargs, "-demo-dance") >= 0)
                StartCoroutine(DemoDance());
        }

        // ------------------------------------------------------------- build

        private void Build()
        {
            _s = XqStyle.Scale;
            _capH = XqStyle.Fr(XqStyle.CapsuleHeight);

            var canvasGo = new GameObject("XqMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 330;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasRt = canvasGo.GetComponent<RectTransform>();

            _go = new GameObject("XqMenu", typeof(RectTransform), typeof(CanvasGroup));
            _go.transform.SetParent(canvasGo.transform, false);
            _root = (RectTransform)_go.transform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;
            _group = _go.GetComponent<CanvasGroup>();

            _dotsRoot = new GameObject("ArcDots", typeof(RectTransform), typeof(CanvasGroup));
            _dotsRoot.transform.SetParent(_root, false);
            var drt = (RectTransform)_dotsRoot.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = Vector2.zero;
            _dotsGroup = _dotsRoot.GetComponent<CanvasGroup>();
            _dotsGroup.alpha = 0f;
            _dotSprite = XqUi.Round(XqStyle.PinkGuide, XqStyle.PinkGuide, 4f);
            _dotR = XqStyle.Fr(RadiusPx - ArcRadiusOffset);
            float arcLen = 140f * Mathf.Deg2Rad * _dotR;
            _dotCount = Mathf.FloorToInt(arcLen / XqStyle.Fr(6f));
            for (int i = 0; i < _dotCount; i++)
            {
                var d = new GameObject("Dot" + i, typeof(RectTransform));
                d.transform.SetParent(_dotsRoot.transform, false);
                var img = d.AddComponent<Image>();
                img.sprite = _dotSprite;
                img.color = XqStyle.PinkGuide;
                img.raycastTarget = false;
                var rt = d.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(XqStyle.Fr(3f), XqStyle.Fr(3f));
                _dots.Add(img);
            }

            _status = XqUi.Capsule(_root, "", null);
            _statusGroup = _status.Root.gameObject.AddComponent<CanvasGroup>();
            _status.Bg.sprite = XqUi.Round(XqStyle.PinkFill, XqStyle.PinkFill, 16f);
            _status.Text.color = XqStyle.InkSoft;
            _status.Text.fontSize = XqStyle.Px(XqStyle.FontStatus);
            _status.Bg.raycastTarget = true;
            _status.Hover.OnClick = () => { Close("action"); OpenToday(); };

            AddSlot("today", "今天", false, null, () => { Close("action"); OpenToday(); });
            AddSlot("say", "说句话", true, "还没开通", null);
            AddSlot("doing", "让她…", false, null, () => ToggleSub("doing"));
            AddSlot("quiet", "安静一会", false, null, () => ToggleSub("quiet"));
            AddSlot("live", "直播间", true, "还在做", null);

            _settings = XqUi.Label(_root, "设置", XqStyle.Px(XqStyle.FontSmall), XqStyle.Muted,
                TextAnchor.MiddleCenter, XqStyle.FontSmall + 8f);
            _settings.raycastTarget = true;
            _settingsGroup = _settings.gameObject.AddComponent<CanvasGroup>();
            var sh = _settings.gameObject.AddComponent<XqHover>();
            sh.OnEnter = () => { _settings.color = XqStyle.Ink; _underline.enabled = true; };
            sh.OnExit = () => { _settings.color = XqStyle.Muted; _underline.enabled = false; };
            sh.OnClick = () => { Close("action"); XiaoqiSettings.Open(); };

            var line = new GameObject("Underline", typeof(RectTransform));
            line.transform.SetParent(_settings.transform, false);
            _underline = line.AddComponent<Image>();
            _underline.color = XqStyle.Ink;
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0f);
            lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(XqStyle.Fr(28f), 1f);
            lrt.anchoredPosition = new Vector2(0f, 0f);
            _underline.enabled = false;

            _hint = XqUi.Label(_root, "", XqStyle.Px(XqStyle.FontSmall), XqStyle.Muted, TextAnchor.MiddleCenter, 18f);
            _hintGroup = _hint.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.alpha = 0f;
        }

        private void AddSlot(string id, string text, bool gray, string hint, Action act)
        {
            var cap = XqUi.Capsule(_root, text, null);
            var grp = cap.Root.gameObject.AddComponent<CanvasGroup>();
            var slot = new Slot
            {
                Id = id, Text = text, View = cap, Group = grp, Gray = gray, Hint = hint, Act = act
            };
            if (gray)
            {
                cap.Bg.sprite = XqUi.Round(XqStyle.GrayFill, XqStyle.GrayLine, 18f);
                cap.Text.color = XqStyle.GrayText;
            }
            cap.Hover.OnEnter = () => OnEnter(slot);
            cap.Hover.OnExit = () => OnExit(slot);
            cap.Hover.OnDown = () => cap.Root.localScale = Vector3.one * 0.96f;
            cap.Hover.OnUp = () => cap.Root.localScale = Vector3.one;
            cap.Hover.OnClick = () =>
            {
                if (slot.Gray) { Debug.Log("XQ_MENU gray " + slot.Id); return; }
                slot.Act?.Invoke();
            };
            _slots.Add(slot);
        }

        // ---------------------------------------------------------- submenus

        private void ToggleSub(string kind)
        {
            if (_subKind == kind) { ClearSub(); return; }
            ClearSub();
            _subKind = kind;
            if (kind == "doing") BuildDoing();
            else if (kind == "quiet") BuildQuiet();
            else if (kind == "dance") BuildDance();
            else if (kind == "idle") BuildIdle();
            PositionSub();
            SetDim(true);
            StartCoroutine(SubIn());
        }

        private IEnumerator CaptureRoutine(string path)
        {
            yield return new WaitForSecondsRealtime(4.5f);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("XQ_CAPTURE " + path);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        private IEnumerator FramesRoutine(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            for (int i = 0; i < 60; i++)
            {
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "f" + i.ToString("000") + ".png"));
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Debug.Log("XQ_FRAMES done " + dir);
            yield return new WaitForSecondsRealtime(0.6f);
            Application.Quit();
        }

        private IEnumerator DemoA4()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (PetHost.I != null)
            {
                var r = PetHost.I.ScreenRect();
                var inside = r.center;
                var outside = new Vector2(r.xMax + XqStyle.Fr(220f), r.center.y);
                Debug.Log("XQ_HITTEST inside=" + r.Contains(inside) + " outside=" + r.Contains(outside)
                          + " rect=" + r);
            }
            Open();
            yield return new WaitForSecondsRealtime(1.2f);
            Close("esc");
            yield return new WaitForSecondsRealtime(0.8f);
            Open();
            yield return new WaitForSecondsRealtime(0.8f);
            Close("blank");
            yield return new WaitForSecondsRealtime(0.8f);
            Open();
            yield return new WaitForSecondsRealtime(0.8f);
            Close("right");
            yield return new WaitForSecondsRealtime(0.8f);
            Open();
            yield return new WaitForSecondsRealtime(0.8f);
            Close("focus");
            Debug.Log("XQ_DEMO_A4 done");
        }

        private IEnumerator DemoA6()
        {
            yield return new WaitForSecondsRealtime(2f);
            Xiaoqi.Spike.PetPresence.Nap(0.1f);
            yield return new WaitForSecondsRealtime(8f);
            Xiaoqi.Spike.PetPresence.Nap(1f);
            yield return new WaitForSecondsRealtime(1.5f);
            Xiaoqi.Spike.PetPresence.Wake();
            Debug.Log("XQ_DEMO_A6 done");
        }

        private IEnumerator DemoIdle()
        {
            yield return new WaitForSecondsRealtime(3.2f);
            ToggleSub("doing");
            yield return new WaitForSecondsRealtime(0.6f);
            ToggleSub("idle");
            yield return new WaitForSecondsRealtime(0.5f);
            if (PetHost.I != null) PetHost.I.RunAction("pose_next");
            yield return new WaitForSecondsRealtime(0.4f);
            if (PetHost.I != null) PetHost.I.RunAction("pose_next");
            Debug.Log("XQ_DEMO_IDLE done");
        }

        private IEnumerator DemoDance()
        {
            yield return new WaitForSecondsRealtime(3.2f);
            ToggleSub("doing");
            yield return new WaitForSecondsRealtime(0.5f);
            ToggleSub("dance");
            yield return new WaitForSecondsRealtime(0.2f);
            if (PetHost.I != null) PetHost.I.RunAction("dance_2");
            Debug.Log("XQ_DEMO_DANCE done");
        }

        private IEnumerator DemoChibi()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (PetHost.I != null) Debug.Log("XQ_DEMO_CHIBI " + PetHost.I.RunAction("chibi"));
        }

        private IEnumerator SubIn()
        {
            float t = 0f;
            while (t < XqStyle.InSec)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < _sub.Count; i++)
                {
                    var s = _sub[i];
                    if (s.View == null) continue;
                    float d = Mathf.Clamp01((t - i * XqStyle.StaggerSec) / XqStyle.InSec);
                    float e = 1f - Mathf.Pow(1f - d, 3f);
                    s.Group.alpha = e;
                    s.View.Root.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
                }
                yield return null;
            }
            foreach (var s in _sub) if (s.View != null) { s.Group.alpha = 1f; s.View.Root.localScale = Vector3.one; }
        }

        private Slot AnchorSlot(string id)
        {
            return _slots.Find(s => s.Id == id);
        }

        private void BuildDoing()
        {
            var st = XiaoqiLink.Current;
            bool outside = st != null && st.where != "dorm";
            string why = (st != null && st.seg != null ? st.seg.name : "") + "，回来再说";
            bool asleep = Xiaoqi.Spike.PetPresence.IsAsleep;

            AddSub("回到中间", null, false, () =>
            {
                PetHost.I?.ExitWindowSit();
                PetHost.I?.MoveToCenter();
                Close("action");
            });
            AddSub(asleep ? "叫醒她" : "睡一会", outside ? why : null, outside, () =>
            {
                if (asleep) Xiaoqi.Spike.PetPresence.Wake();
                else Xiaoqi.Spike.PetPresence.Nap(XiaoqiRuntime.Config != null ? XiaoqiRuntime.Config.nap_min : 20);
                Close("action");
            });
            if (PetHost.I != null)
            {
                foreach (var act in PetHost.I.Actions)
                {
                    if (act.Id.StartsWith("idle_") || act.Id.StartsWith("dance_")) continue;
                    var a = act;
                    AddSub(a.Label, null, false, () =>
                    {
                        if (!PetHost.I.RunAction(a.Id)) Debug.Log("XQ_MENU action failed " + a.Id);
                        Close("action");
                    });
                }


            }

            bool hasDance = PetHost.I != null && PetHost.I.Dances.Count > 0;
            if (!hasDance && PetHost.I != null)
                foreach (var act in PetHost.I.Actions)
                    if (act.Id.StartsWith("dance_")) { hasDance = true; break; }
            if (hasDance)
            {
                AddSub("跳舞 ▸", outside ? why : null, outside, () => ToggleSub("dance"));
                _sub[_sub.Count - 1].Id = "dance";
            }

            // P2/P3：三种形态入口。只加项；工作台在 P2 通过前是灰的（XqMode.BenchAvailable）
            var cur = Xiaoqi.Mode.XqMode.Current;
            if (cur != Xiaoqi.Mode.XqView.Pet)
                AddSub("桌宠", null, false, () => { Close("action"); Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Pet); });
            if (cur != Xiaoqi.Mode.XqView.Close)
                AddSub("近景", null, false, () => { Close("action"); Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Close); });
            if (cur != Xiaoqi.Mode.XqView.Bench)
            {
                bool benchOff = !Xiaoqi.Mode.XqMode.BenchAvailable;
                AddSub("工作台", benchOff ? "还没开放" : null, benchOff,
                    () => { Close("action"); Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Bench); });
            }
        }

        private void BuildIdle()
        {
            if (PetHost.I == null) return;
            foreach (var act in PetHost.I.Actions)
            {
                if (!act.Id.StartsWith("idle_")) continue;
                var a = act;
                AddSub(a.Label, null, false, () =>
                {
                    PetHost.I.RunAction(a.Id);
                    Close("action");
                });
            }
        }

        private void BuildQuiet()
        {
            AddSub("30 分钟", null, false, () => { XiaoqiRuntime.SetQuietMinutes(30); Close("action"); });
            AddSub("1 小时", null, false, () => { XiaoqiRuntime.SetQuietMinutes(60); Close("action"); });
            AddSub("到明早 8 点", null, false, () => { XiaoqiRuntime.SetQuietTomorrow8(); Close("action"); });
            AddSub("一直", null, false, () => { XiaoqiRuntime.SetQuietForever(); Close("action"); });
            if (XiaoqiRuntime.Dnd)
                AddSub("不安静了", null, false, () => { XiaoqiRuntime.ClearQuiet(); Close("action"); });
        }

        private void BuildDance()
        {
            foreach (var name in PetHost.I.Dances)
            {
                string dance = name;
                AddSub(name, null, false, () =>
                {
                    if (PetHost.I.PlayDance(dance)) Close("action");
                    else Debug.Log("XQ_MENU dance failed " + dance);
                });
            }
            foreach (var act in PetHost.I.Actions)
            {
                if (!act.Id.StartsWith("dance_")) continue;
                var a = act;
                AddSub(a.Label, null, false, () =>
                {
                    PetHost.I.RunAction(a.Id);
                    Close("action");
                });
            }
            if (PetHost.I.IsDancing)
                AddSub("停下", null, false, () => { PetHost.I.StopDance(); Close("action"); });
        }

        private void AddSub(string label, string hint, bool gray, Action act)
        {
            var cap = XqUi.Capsule(_root, label, null);
            var grp = cap.Root.gameObject.AddComponent<CanvasGroup>();
            var slot = new Slot { Id = "sub", Text = label, View = cap, Group = grp, Gray = gray, Hint = hint, Act = act };
            if (gray) cap.Text.color = XqStyle.GrayText;
            cap.Hover.OnEnter = () => OnEnter(slot);
            cap.Hover.OnExit = () => OnExit(slot);
            cap.Hover.OnDown = () => cap.Root.localScale = Vector3.one * 0.96f;
            cap.Hover.OnUp = () => cap.Root.localScale = Vector3.one;
            cap.Hover.OnClick = () =>
            {
                if (slot.Gray) { Debug.Log("XQ_MENU gray sub"); return; }
                slot.Act?.Invoke();
            };
            _sub.Add(slot);
        }

        private void PositionSub()
        {
            float gap = XqStyle.Fr(6f);
            float step = XqStyle.Fr(44f);
            string anchorId = _subKind == "quiet" ? "quiet" : "doing";
            var anchor = AnchorSlot(anchorId);
            if (anchor == null) return;

            var anchorRect = anchor.View.Root;
            float anchorW = anchorRect.sizeDelta.x;
            float baseX = _left ? anchor.BasePos.x - anchorW - gap : anchor.BasePos.x + anchorW + gap;
            float centerY = anchor.BasePos.y;

            if (_subKind == "dance")
            {
                var danceAnchor = _sub.Find(s => s.Id == "dance");
                if (danceAnchor != null)
                {
                    baseX = _left ? danceAnchor.BasePos.x - danceAnchor.View.Root.sizeDelta.x - gap
                                  : danceAnchor.BasePos.x + danceAnchor.View.Root.sizeDelta.x + gap;
                    centerY = danceAnchor.BasePos.y;
                }
            }

            float base0 = baseX;
            int cols = _sub.Count > 8 ? 2 : 1;
            int perCol = Mathf.CeilToInt(_sub.Count / (float)cols);
            float colStep = XqStyle.Fr(160f);
            float half = (perCol - 1) * step * 0.5f;
            float minY = half + XqStyle.Fr(8f) - _center.y;
            float maxY = Screen.height - half - XqStyle.Fr(8f) - _center.y;
            centerY = Mathf.Clamp(centerY, minY, maxY);

            for (int i = 0; i < _sub.Count; i++)
            {
                var s = _sub[i];
                float tw = s.View.Text.preferredWidth + XqStyle.Fr(2f * XqStyle.CapsulePadX);
                float w = Mathf.Max(XqStyle.Fr(96f), tw);
                s.View.Root.sizeDelta = new Vector2(w, _capH);
                int col = cols == 1 ? 0 : i / perCol;
                int row = cols == 1 ? i : i % perCol;
                baseX = base0 + (_left ? -col * colStep : col * colStep);
                int pw2 = Mathf.RoundToInt(w), ph2 = Mathf.RoundToInt(_capH);
                s.View.Bg.sprite = XqUi.Pill(pw2, ph2, s.Gray ? XqStyle.GrayFill : XqStyle.Paper,
                    s.Gray ? XqStyle.GrayLine : XqStyle.PinkLine);
                s.View.Bg.type = Image.Type.Simple;
                s.View.Bg.color = Color.white;
                s.View.HoverBg.sprite = XqUi.Pill(pw2, ph2, XqStyle.PinkFill, XqStyle.PinkStrong);
                s.View.HoverBg.type = Image.Type.Simple;
                s.View.HoverBg.color = new Color(XqStyle.PinkFill.r, XqStyle.PinkFill.g, XqStyle.PinkFill.b, 0f);

                var rt = s.View.Root;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(_left ? 1f : 0f, 0.5f);
                float y = centerY + ((perCol - 1) * 0.5f - row) * step;
                s.BasePos = new Vector2(baseX, y);
                rt.anchoredPosition = s.BasePos;

                rt.localScale = Vector3.one;
                s.Group.alpha = 1f;
            }

            if (_link == null)
            {
                var lo = new GameObject("Link", typeof(RectTransform));
                lo.transform.SetParent(_root, false);
                _link = lo.AddComponent<Image>();
                _link.raycastTarget = false;
            }
            _link.gameObject.SetActive(true);
            _link.color = XqStyle.PinkGuide;
            var lrt = _link.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            float parentEdge = _left ? anchor.BasePos.x - anchorW : anchor.BasePos.x + anchorW;
            float linkLen = Mathf.Abs(base0 - parentEdge);
            lrt.sizeDelta = new Vector2(Mathf.Max(2f, linkLen), XqStyle.Fr(2f));
            lrt.anchoredPosition = new Vector2(Mathf.Min(parentEdge, base0), centerY - XqStyle.Fr(1f));
        }

        private void ClearSub()
        {
            foreach (var s in _sub)
                if (s.View != null) Destroy(s.View.Root.gameObject);
            _sub.Clear();
            _subKind = "";
            if (_link != null) _link.gameObject.SetActive(false);
            _hintGroup.alpha = 0f;
            SetDim(false);
        }

        private void SetDim(bool dim)
        {
            float a = dim ? 0.72f : 1f;
            foreach (var s in _slots)
            {
                s.Group.alpha = a;
                s.View.Bg.color = Color.white;
            }
            if (dim)
            {
                var par = AnchorSlot(_subKind == "quiet" ? "quiet" : "doing");
                if (par != null)
                {
                    par.Group.alpha = 1f;
                    par.View.Bg.color = XqStyle.PinkFill;
                }
            }
            _statusGroup.alpha = a;
        }


        // ------------------------------------------------------------- input

        private void Update()
        {
            if (!_open)
            {
                if (_demo && Time.unscaledTime >= _demoAt) { _demo = false; Open(); return; }
                if (Input.GetMouseButtonDown(1) && HitPet() && !Replaced) Open();
                return;
            }

            _since += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!string.IsNullOrEmpty(_subKind)) { ClearSub(); return; }
                Close("esc");
                return;
            }
            if (Input.GetMouseButtonDown(1)) { Close("right"); return; }
            if (Input.GetMouseButtonDown(0) && !IsPointerOverMenu()) { Close("blank"); return; }

            if (PetHost.I != null && !PetHost.I.AvatarVisible) { Close("hidden"); return; }
            FollowHead();

            if (_since < XqStyle.InSec * 1.2f) AnimateOpen();
            else if (!_settled)
            {
                _settled = true;
                _group.alpha = 1f;
                _statusGroup.alpha = 1f;
                _settingsGroup.alpha = 1f;
                _dotsGroup.alpha = 0.9f;
                foreach (var s in _slots) s.Group.alpha = 1f;
            }
            AnimateHover();
            if (!string.IsNullOrEmpty(_demoSub) && _since > 0.3f)
            {
                var kind = _demoSub;
                _demoSub = "";
                ToggleSub(kind);
            }
            if (!_loggedAlpha && _since > 0.6f)
            {
                _loggedAlpha = true;
                Debug.Log("XQ_MENU alpha group=" + _group.alpha + " slot0=" + _slots[0].Group.alpha
                          + " bg=" + _slots[0].View.Bg.color + " scale=" + _root.localScale);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // 菜单不再因窗口失焦关闭：宠物窗口经常被鼠标移出就失焦，会误关。
        }

        private bool IsPointerOverMenu()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return true;
            return false;
        }

        private void OnEnter(Slot s)
        {
            if (s.Gray)
            {
                _hint.text = s.Hint ?? "";
                _hintGroup.alpha = 1f;
                var p = s.BasePos;
                var dir = p.sqrMagnitude > 1f ? p.normalized : Vector2.right;
                _hint.rectTransform.anchorMin = _hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _hint.rectTransform.anchoredPosition = p + dir * XqStyle.Fr(48f);
            }
            s.Hover = 0f;
        }

        private void OnExit(Slot s)
        {
            if (s.Gray) _hintGroup.alpha = 0f;
        }

        private void AnimateHover()
        {
            foreach (var s in _slots)
            {
                var rt = s.View.Root;
                float target = RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null) ? 1f : 0f;
                s.Hover = Mathf.MoveTowards(s.Hover, target, Time.unscaledDeltaTime / XqStyle.HoverSec);
                float h = s.Hover;
                if (!s.Gray)
                {
                    var c = s.View.HoverBg.color;
                    c.a = h;
                    s.View.HoverBg.color = c;
                    s.View.Text.color = Color.Lerp(XqStyle.Ink, XqStyle.PinkStrong, h * 0.4f);
                }
                var dir = s.BasePos.sqrMagnitude > 1f ? s.BasePos.normalized : Vector2.zero;
                if (_since >= XqStyle.InSec)
                    rt.anchoredPosition = s.BasePos + dir * (XqStyle.Fr(4f) * h);
            }
        }

        // -------------------------------------------------------------- open

        private bool HitPet()
        {
            if (PetHost.I == null) return false;
            var r = PetHost.I.ScreenRect();
            if (r.width < 1f || r.height < 1f) return false;
            return r.Contains((Vector2)Input.mousePosition);
        }

        private void Open()
        {
            if (_forceCenter)
            {
                _forceCenter = false;
                _center = _forced;
            }
            else
            {
                var openRect = StableRect();
                float openBody = openRect.height > 10f ? openRect.height : Screen.height * 0.45f;
                if (openRect.height > 10f) _center = StableCenter(openRect);
                else
                {
                    _center = Input.mousePosition;
                    Debug.Log("XQ_MENU no_head");
                }
            }

            if (Screen.width < 600 || Screen.height < 500)
                Debug.Log("XQ_MENU_SMALL w=" + Screen.width + " h=" + Screen.height);

            SetStatus();
            Layout();

            _go.SetActive(true);
            _open = true;
            _closing = false;
            _since = 0f;
            _loggedAlpha = false;
            _settled = false;
            _group.alpha = 1f;
            _statusGroup.alpha = 1f;
            _settingsGroup.alpha = 1f;
            _dotsGroup.alpha = 0.9f;
            foreach (var s in _slots) s.Group.alpha = 1f;
            _root.localScale = Vector3.one * _menuScale;
            Debug.Log("XQ_MENU open side=" + (_left ? "L" : "R") + " c=(" + _center.x.ToString("0") + "," +
                      _center.y.ToString("0") + ") s=" + _s.ToString("0.00"));
            Debug.Log("XQ_MENU rect cap=" + _slots[0].View.Root.rect.size + " status=" + _status.Root.rect.size
                      + " dpi=" + Screen.dpi + " canvas=" + _canvasRt.rect.size);
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                Debug.Log("XQ_MENU slot " + s.Id + " size=" + s.View.Root.rect.size + " pos=" + s.View.Root.anchoredPosition
                          + " bg=" + s.View.Bg.color + " sprite=" + (s.View.Bg.sprite != null));
            }
            Debug.Log("XQ_MENU status pos=" + _status.Root.anchoredPosition + " dots=" + _dots.Count);
            if (_shot && !_shotStarted)
            {
                _shotStarted = true;
                StartCoroutine(ShotRoutine());
            }
        }

        private void FollowHead()
        {
            var br = StableRect();
            float bodyH = br.height > 10f ? br.height : Screen.height * 0.45f;

            float target = Mathf.Clamp(bodyH / XqStyle.Fr(380f), 0.7f, 1.15f);
            if (Mathf.Abs(target - _menuScale) > 0.02f)
            {
                _menuScale = Mathf.Lerp(_menuScale, target, 0.25f);
                _root.localScale = Vector3.one * _menuScale;
            }

            // 中心取稳定外框（死区加平滑），不再逐帧跟头骨——待机一抬手菜单就跟着晃，点不上
            Vector2 c = br.height > 10f ? StableCenter(br) : _center;
            if (float.IsNaN(c.x) || float.IsNaN(c.y)) return;
            c.x = Mathf.Clamp(c.x, XqStyle.Fr(40f), Screen.width - XqStyle.Fr(40f));
            c.y = Mathf.Clamp(c.y, XqStyle.Fr(20f), Screen.height - XqStyle.Fr(20f));

            float wantR = BaseRadius();
            if (Mathf.Abs(wantR - _r) > 2f)
            {
                _center = c;
                Layout();
                if (!string.IsNullOrEmpty(_subKind)) PositionSub();
                ApplyVisible();
                return;
            }
            if ((c - _center).sqrMagnitude > 1f)
            {
                _center = c;
                _root.anchoredPosition = _center - new Vector2(Screen.width, Screen.height) * 0.5f;
            }
        }
        private float BaseRadius()
        {
            var r = StableRect();
            float bodyH = r.height > 10f ? r.height : Screen.height * 0.45f;
            float screenR = Mathf.Clamp(bodyH * 0.34f, XqStyle.Fr(70f), XqStyle.Fr(200f));
            return screenR / Mathf.Max(0.1f, _menuScale);
        }

        private static Vector2 MenuCenter(Vector2 head, float bodyH)
        {
            return new Vector2(head.x, head.y - bodyH * 0.30f);
        }

        private static Rect StableRect()
        {
            var r = Xiaoqi.UI2.XqBodyAnchor.Stable();
            if (r.height > 10f) return r;
            return PetHost.I != null ? PetHost.I.ScreenRect() : new Rect(0, 0, Screen.width, Screen.height * 0.45f);
        }

        // 原来是「头骨 +0.28m 的屏幕点」再下移 0.30 身高；头顶大约就是外框上沿，等价写成这样
        private static Vector2 StableCenter(Rect body)
        {
            return MenuCenter(new Vector2(body.center.x, body.yMax), body.height);
        }

        private void ApplyVisible()
        {
            _group.alpha = 1f;
            _statusGroup.alpha = _subKind == "" ? 1f : 0.72f;
            _settingsGroup.alpha = 1f;
            _dotsGroup.alpha = 0.9f;
            foreach (var s in _slots)
            {
                s.Group.alpha = _subKind == "" ? 1f : 0.72f;
                s.View.Root.localScale = Vector3.one;
                s.View.Root.anchoredPosition = s.BasePos;
            }
        }

        private IEnumerator ShotRoutine()
        {
            yield return new WaitForSecondsRealtime(0.9f);
            Shot("01-menu-right");
            yield return new WaitForSecondsRealtime(0.3f);

            ToggleSub("doing");
            yield return new WaitForSecondsRealtime(0.5f);
            Shot("02-doing");
            yield return new WaitForSecondsRealtime(0.3f);
            ClearSub();

            ToggleSub("quiet");
            yield return new WaitForSecondsRealtime(0.5f);
            Shot("03-quiet");
            yield return new WaitForSecondsRealtime(0.3f);
            ClearSub();

            Close("action");
            yield return new WaitForSecondsRealtime(0.3f);
            _forced = new Vector2(Screen.width - XqStyle.Fr(40f), Screen.height * 0.55f);
            _forceCenter = true;
            Open();
            yield return new WaitForSecondsRealtime(0.5f);
            Shot("04-menu-left");
            yield return new WaitForSecondsRealtime(0.3f);

            Close("action");
            yield return new WaitForSecondsRealtime(0.2f);

            Xiaoqi.Spike.TodayCard.DebugOpen();
            yield return new WaitForSecondsRealtime(0.8f);
            Shot("05-today");
            yield return new WaitForSecondsRealtime(0.3f);
            Xiaoqi.Spike.TodayCard.DebugClose();
            yield return new WaitForSecondsRealtime(0.3f);

            XiaoqiSettings.Open();
            yield return new WaitForSecondsRealtime(0.6f);
            Shot("06-settings");
            yield return new WaitForSecondsRealtime(0.3f);
            var s = FindFirstObjectByType<XiaoqiSettings>(FindObjectsInactive.Include);
            if (s != null) s.Close();
            yield return new WaitForSecondsRealtime(0.3f);

            XiaoqiAbout.Show();
            yield return new WaitForSecondsRealtime(0.6f);
            Shot("07-about");
            yield return new WaitForSecondsRealtime(0.3f);
            XiaoqiAbout.Hide();
            yield return new WaitForSecondsRealtime(0.4f);

            Debug.Log("XQ_SHOT done");
            Application.Quit();
        }

        private void Shot(string name)
        {
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "shots");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            var sb = new System.Text.StringBuilder("XQ_SHOT " + path);
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                sb.Append(" | " + c.name + ":" + c.sortingOrder + ":" + c.isActiveAndEnabled);
            Debug.Log(sb.ToString());
        }

        private static string PathOf(Transform t)
        {
            var s = t.name;
            while (t.parent != null && t.parent.name != "XqMenu")
            {
                t = t.parent;
                s = t.name + "/" + s;
            }
            return s;
        }

        private void SetStatus()
        {
            var s = XiaoqiLink.Current;
            string text;
            if (s == null) text = "连不上服务器";
            else
            {
                if (Xiaoqi.Spike.PetPresence.IsAsleep) text = "睡着了";
                else
                {
                    var segName = s.seg != null ? s.seg.name : "";
                    text = s.where != "dorm"
                        ? segName + (s.seg != null && !string.IsNullOrEmpty(s.seg.end) ? " · " + s.seg.end + " 回" : "")
                        : segName;
                }
                if (s.face != null && !string.IsNullOrEmpty(s.face.top) && s.face.top != "平常" && s.face.level >= 0.25f)
                    text += " · " + (s.face.level < 0.5f ? "有点" : "") + s.face.top;
            }
            if (XiaoqiRuntime.IsQuiet()) text += " · 安静中";
            _status.Text.text = text;

            var q = AnchorSlot("quiet");
            if (q != null)
            {
                if (XiaoqiRuntime.IsQuiet())
                {
                    var until = XiaoqiRuntime.Config != null ? XiaoqiRuntime.Config.dnd_until : "";
                    q.View.Text.text = until == "forever" ? "安静中" : XiaoqiRuntime.QuietLabel();
                    q.View.Text.color = XqStyle.PinkStrong;
                }
                else
                {
                    q.View.Text.text = "安静一会";
                    q.View.Text.color = XqStyle.Ink;
                }
            }
        }

        private void Layout()
        {
            float w = Screen.width, h = Screen.height;
            float R = BaseRadius();
            _r = R;

            // capsule widths depend on text; measure once
            _maxWidth = 0f;
            foreach (var s in _slots)
            {
                float tw = s.View.Text.preferredWidth + XqStyle.Fr(2f * XqStyle.CapsulePadX);
                float cw = Mathf.Max(XqStyle.Fr(XqStyle.CapsuleMinWidth), tw);
                s.View.Root.sizeDelta = new Vector2(cw, _capH);
                int pw2 = Mathf.RoundToInt(cw), ph2 = Mathf.RoundToInt(_capH);
                s.View.Bg.sprite = XqUi.Pill(pw2, ph2, s.Gray ? XqStyle.GrayFill : XqStyle.Paper,
                    s.Gray ? XqStyle.GrayLine : XqStyle.PinkLine);
                s.View.Bg.type = Image.Type.Simple;
                s.View.Bg.color = Color.white;
                s.View.HoverBg.sprite = XqUi.Pill(pw2, ph2, XqStyle.PinkFill, XqStyle.PinkStrong);
                s.View.HoverBg.type = Image.Type.Simple;
                s.View.HoverBg.color = new Color(XqStyle.PinkFill.r, XqStyle.PinkFill.g, XqStyle.PinkFill.b, 0f);
                _maxWidth = Mathf.Max(_maxWidth, cw);
            }

            float statusW = _status.Text.preferredWidth + XqStyle.Fr(2f * XqStyle.CapsulePadX);
            float sw = Mathf.Max(XqStyle.Fr(120f), statusW);
            float sh = XqStyle.Fr(28f);
            _status.Root.sizeDelta = new Vector2(sw, sh);
            _status.Bg.sprite = XqUi.Pill(Mathf.RoundToInt(sw), Mathf.RoundToInt(sh), XqStyle.PinkFill, XqStyle.PinkFill);
            _status.Bg.type = Image.Type.Simple;
            _status.Bg.color = Color.white;

            _left = _center.x + R + _maxWidth + XqStyle.Fr(24f) > w;

            // vertical clamp for the whole group
            float statusH = XqStyle.Fr(28f);
            float groupTop = Mathf.Sin(60f * Mathf.Deg2Rad) * R + _capH * 0.5f + XqStyle.Fr(12f) + statusH;
            float groupBottom = -(Mathf.Sin(60f * Mathf.Deg2Rad) * R + _capH * 0.5f + XqStyle.Fr(23f));
            float cy = Mathf.Clamp(_center.y, -groupBottom + XqStyle.Fr(8f), h - groupTop - XqStyle.Fr(8f));
            _center.y = cy;

            var canvasHalf = new Vector2(w, h) * 0.5f;
            _root.anchoredPosition = _center - canvasHalf;

            float[] angles = _left ? new[] { 120f, 150f, 180f, 210f, 240f } : new[] { 60f, 30f, 0f, -30f, -60f };
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                float a = angles[i] * Mathf.Deg2Rad;
                var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R;
                s.BasePos = p;
                var rt = s.View.Root;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(_left ? 1f : 0f, 0.5f);
                rt.anchoredPosition = p;
                rt.localScale = Vector3.one;
                s.Group.alpha = 0f;
            }

            // status sits above today, aligned to its outer-start edge
            var today = _slots[0];
            float sx = _left ? today.BasePos.x - today.View.Root.sizeDelta.x : today.BasePos.x;
            _status.Root.anchorMin = _status.Root.anchorMax = new Vector2(0.5f, 0.5f);
            _status.Root.pivot = new Vector2(_left ? 1f : 0f, 0.5f);
            _status.Root.anchoredPosition = new Vector2(sx, today.BasePos.y + _capH * 0.5f + XqStyle.Fr(12f) + XqStyle.Fr(14f));
            _statusGroup.alpha = 0f;

            // settings under live
            var live = _slots[_slots.Count - 1];
            float lx = _left ? live.BasePos.x - live.View.Root.sizeDelta.x : live.BasePos.x + live.View.Root.sizeDelta.x;
            _settings.rectTransform.anchorMin = _settings.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _settings.rectTransform.pivot = new Vector2(_left ? 0f : 1f, 0.5f);
            _settings.rectTransform.anchoredPosition = new Vector2(lx, live.BasePos.y - _capH * 0.5f - XqStyle.Fr(14f));
            _settingsGroup.alpha = 0f;

            _hint.rectTransform.anchorMin = _hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            LayoutDots(_left);
            _dotsGroup.alpha = 0f;
        }

        private void LayoutDots(bool left)
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                float t = _dotCount <= 1 ? 0.5f : i / (float)(_dotCount - 1);
                float ang = Mathf.Lerp(70f, -70f, t);
                if (left) ang = 180f - ang;
                float rad = ang * Mathf.Deg2Rad;
                _dots[i].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _dotR;
            }
        }

        private void AnimateOpen()
        {
            float t = Mathf.Clamp01(_since / XqStyle.InSec);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                float d = Mathf.Clamp01((_since - i * XqStyle.StaggerSec) / XqStyle.InSec);
                float e = 1f - Mathf.Pow(1f - d, 3f);
                s.View.Root.anchoredPosition = Vector2.Lerp(s.BasePos * 0.6f, s.BasePos, e);
                s.View.Root.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
            }
        }

        private void Close(string by)
        {
            if (!_open || _closing) return;
            ClearSub();
            _closing = true;
            Debug.Log("XQ_MENU close by=" + by);
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            float t = 0f;
            float a0 = _group.alpha;
            while (t < XqStyle.OutSec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / XqStyle.OutSec);
                _group.alpha = Mathf.Lerp(a0, 0f, k);
                _root.localScale = Vector3.one * (_menuScale * Mathf.Lerp(1f, 0.94f, k));
                yield return null;
            }
            _go.SetActive(false);
            _open = false;
            _closing = false;
            LastCloseFrame = Time.frameCount;
        }
    }
}
