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
        private float _demoAt;
        private bool _shot;
        private string _shotPath;
        private float _since;
        private float _s = 1f;
        private Vector2 _center;
        private float _maxWidth;
        private float _capH;

        private static XqMenu _instance;

        private void Awake()
        {
            _instance = this;
            Build();
            _go.SetActive(false);
        }

        private void Start()
        {
            PetHost.I?.SuppressHostMenu();
            PetHost.I?.RegisterMenuBlocker(() => _open);
            _demo = Array.IndexOf(Environment.GetCommandLineArgs(), "-menu-demo") >= 0;
            _shot = Array.IndexOf(Environment.GetCommandLineArgs(), "-menu-shot") >= 0;
            if (_shot) _demo = true;
            if (_demo) _demoAt = Time.unscaledTime + 2.5f;
            if (_shot)
            {
                var dir = System.IO.Path.Combine(Application.persistentDataPath, "shots");
                System.IO.Directory.CreateDirectory(dir);
                _shotPath = System.IO.Path.Combine(dir, "menu.png");
            }
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
            _status.Hover.OnClick = () => { Close("action"); Xiaoqi.Spike.TodayCard.DebugOpen(); };

            AddSlot("today", "今天", false, null, () => { Close("action"); Xiaoqi.Spike.TodayCard.DebugOpen(); });
            AddSlot("say", "说句话", true, "还没开通", null);
            AddSlot("doing", "让她…", false, null, () => Debug.Log("XQ_MENU doing"));
            AddSlot("quiet", "安静一会", false, null, () => Debug.Log("XQ_MENU quiet"));
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

        private Image NewArc(bool right)
        {
            var go = new GameObject(right ? "ArcR" : "ArcL", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = ArcSprite(right);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float d = XqStyle.Fr(2f * ((RadiusPx - ArcRadiusOffset) + 24f));
            rt.sizeDelta = new Vector2(d, d);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        private Sprite ArcSprite(bool right)
        {
            float r = XqStyle.Fr(RadiusPx - ArcRadiusOffset);
            int size = Mathf.CeilToInt((r + XqStyle.Fr(4f)) * 2f);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            float dotLen = XqStyle.Fr(3f);
            float period = dotLen * 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (Mathf.Abs(dist - r) > XqStyle.Fr(0.7f)) continue;
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                float a = right ? ang : 180f - ang;
                if (a > 70f || a < -70f) continue;
                float arc = Mathf.Abs(a) * Mathf.Deg2Rad * r;
                if (Mathf.Repeat(arc, period) > dotLen) continue;
                px[y * size + x] = XqStyle.PinkGuide;
            }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return sp;
        }

        // ------------------------------------------------------------- input

        private void Update()
        {
            if (!_open)
            {
                if (_demo && Time.unscaledTime >= _demoAt) { _demo = false; Open(); return; }
                if (Input.GetMouseButtonDown(1) && HitPet()) Open();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) { Close("esc"); return; }
            if (Input.GetMouseButtonDown(1)) { Close("right"); return; }
            if (Input.GetMouseButtonDown(0) && !IsPointerOverMenu()) { Close("blank"); return; }

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
            if (!_loggedAlpha && _since > 0.6f)
            {
                _loggedAlpha = true;
                Debug.Log("XQ_MENU alpha group=" + _group.alpha + " slot0=" + _slots[0].Group.alpha
                          + " bg=" + _slots[0].View.Bg.color + " scale=" + _root.localScale);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_demo || _shot) return;
            if (!hasFocus && _open) Close("focus");
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
            var head = FindFirstObjectByType<Xiaoqi.Spike.HeadAnchor>(FindObjectsInactive.Include);
            if (head != null && head.ScreenPoint != Vector2.zero) _center = head.ScreenPoint;
            else
            {
                _center = Input.mousePosition;
                Debug.Log("XQ_MENU no_head");
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
            _root.localScale = Vector3.one;
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
            if (_shot) StartCoroutine(ShotRoutine());
        }

        private IEnumerator ShotRoutine()
        {
            yield return new WaitForSecondsRealtime(1.0f);
            Debug.Log("XQ_SHOT alpha=" + _group.alpha + " since=" + _since + " settled=" + _settled
                      + " slot0=" + _slots[0].Group.alpha);
            ScreenCapture.CaptureScreenshot(_shotPath);
            Debug.Log("XQ_MENU shot " + _shotPath);
            yield return new WaitForSecondsRealtime(0.6f);
            Application.Quit();
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
            _status.Text.text = text;
        }

        private void Layout()
        {
            float w = Screen.width, h = Screen.height;
            float R = XqStyle.Fr(RadiusPx);

            // capsule widths depend on text; measure once
            _maxWidth = 0f;
            foreach (var s in _slots)
            {
                float tw = s.View.Text.preferredWidth + XqStyle.Fr(2f * XqStyle.CapsulePadX);
                float cw = Mathf.Max(XqStyle.Fr(XqStyle.CapsuleMinWidth), tw);
                s.View.Root.sizeDelta = new Vector2(cw, _capH);
                int pw = Mathf.RoundToInt(cw), ph = Mathf.RoundToInt(_capH);
                var ui = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                s.View.Bg.sprite = ui;
                s.View.Bg.type = Image.Type.Sliced;
                s.View.Bg.color = s.Gray ? XqStyle.GrayFill : XqStyle.Paper;
                s.View.HoverBg.sprite = ui;
                s.View.HoverBg.type = Image.Type.Sliced;
                s.View.HoverBg.color = new Color(XqStyle.PinkFill.r, XqStyle.PinkFill.g, XqStyle.PinkFill.b, 0f);
                _maxWidth = Mathf.Max(_maxWidth, cw);
            }

            float statusW = _status.Text.preferredWidth + XqStyle.Fr(2f * XqStyle.CapsulePadX);
            float sw = Mathf.Max(XqStyle.Fr(120f), statusW);
            float sh = XqStyle.Fr(28f);
            _status.Root.sizeDelta = new Vector2(sw, sh);
            _status.Bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            _status.Bg.type = Image.Type.Sliced;
            _status.Bg.color = XqStyle.PinkFill;

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
            _since += Time.unscaledDeltaTime;
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
                _root.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, k);
                yield return null;
            }
            _go.SetActive(false);
            _open = false;
            _closing = false;
        }
    }
}
