using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Xiaoqi.Host;
using Xiaoqi.UI;

namespace Xiaoqi.Spike
{
    public class TodayCard : MonoBehaviour
    {
        [Serializable] private class DayData
        {
            public int v; public string date; public string now; public int now_min;
            public Seg[] segs; public Mark[] marks; public string mood; public Body body;
        }
        [Serializable] private class Seg { public int a, b; public string start, end, name, act, where; }
        [Serializable] private class Mark { public string at; public int min; public string emo; public float delta; public string note, src; }
        [Serializable] private class Body { public float sleepy, stamina, social; }

        // P1-3c 玻璃今天卡接管单击。这张 uGUI 卡留着当备份，点击不再打开它。
        private const bool EnableClickOpen = false;
        private const float CardWidth = 360f;

        private bool _open;
        private DayData _data;
        private string _error;
        private float _nextRefresh;
        private Vector2 _downPos;
        private float _downAt;
        private bool _downOnPet;

        private Canvas _canvas;
        private GameObject _go;
        private RectTransform _card;
        private RectTransform _content;
        private float _cursor;
        private float _cardW;
        private float _contentW;

        private void Awake()
        {
            Build();
            _go.SetActive(false);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo-today") >= 0)
                StartCoroutine(DemoOpen());
        }

        private IEnumerator DemoOpen()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            Open();
        }

        private void Build()
        {
            var canvasGo = new GameObject("XqTodayCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 315;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _go = new GameObject("XqTodayCard", typeof(RectTransform));
            _go.transform.SetParent(canvasGo.transform, false);
            _card = (RectTransform)_go.transform;
            _card.anchorMin = _card.anchorMax = new Vector2(0f, 1f);
            _card.pivot = new Vector2(0f, 1f);
            _cardW = XqStyle.Fr(CardWidth);
            _contentW = _cardW - XqStyle.Fr(32f);
            _card.sizeDelta = new Vector2(_cardW, 0f);

            var bg = _card.gameObject.AddComponent<Image>();
            bg.color = XqStyle.Paper;
            bg.raycastTarget = true;

            var close = XqUi.IconButton(_card, "×", Close);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-XqStyle.Fr(8f), -XqStyle.Fr(6f));

            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.transform.SetParent(_card, false);
            _content.anchorMin = _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);
            _content.sizeDelta = new Vector2(_contentW, 0f);
            _content.anchoredPosition = new Vector2(XqStyle.Fr(16f), -XqStyle.Fr(14f));
        }

        // ------------------------------------------------------------- input

        private void Update()
        {
            if (_open && Xiaoqi.Host.PetHost.I != null && !Xiaoqi.Host.PetHost.I.AvatarVisible) { Close(); }
            if (!XiaoqiRuntime.Config.emotion_line) return;
            if (!EnableClickOpen) return;

            if (Input.GetMouseButtonDown(0))
            {
                _downPos = Input.mousePosition;
                _downAt = Time.unscaledTime;
                _downOnPet = HitPet(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(0))
            {
                var dt = Time.unscaledTime - _downAt;
                var moved = Vector2.Distance(Input.mousePosition, _downPos);
                bool dragging = PetHost.I != null && PetHost.I.IsDragging;
                bool click = _downOnPet && dt <= 0.3f && moved <= 6f && !dragging;
                if (click)
                {
                    if (_open) Close();
                    else Open();
                }
            }
            if (_open && Input.GetKeyDown(KeyCode.Escape)) Close();
            if (_open && Time.unscaledTime >= _nextRefresh) StartCoroutine(Fetch());
            if (_open && Input.GetMouseButtonDown(0) && !PointerInsideCard()) Close();
        }

        private bool PointerInsideCard()
        {
            return _card != null && RectTransformUtility.RectangleContainsScreenPoint(_card, Input.mousePosition, null);
        }

        private bool HitPet(Vector3 screen)
        {
            if (PetHost.I == null) return false;
            var r = PetHost.I.ScreenRect();
            return r.width > 1f && r.height > 1f && r.Contains(new Vector2(screen.x, screen.y));
        }

        public static void DebugOpen()
        {
            var c = FindFirstObjectByType<TodayCard>(FindObjectsInactive.Include);
            if (c != null) c.Open();
        }

        public static void DebugClose()
        {
            var c = FindFirstObjectByType<TodayCard>(FindObjectsInactive.Include);
            if (c != null) c.Close();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !ShotMode()) Close();
        }

        private static bool ShotMode()
        {
            var a = Environment.GetCommandLineArgs();
            return Array.IndexOf(a, "-menu-shot") >= 0 || Array.IndexOf(a, "-demo-today") >= 0;
        }

        // -------------------------------------------------------------- open

        public void Open()
        {
            XiaoqiAbout.Hide();
            _open = true;
            _data = null;
            _error = null;
            _nextRefresh = 0f;
            _go.SetActive(true);
            Refresh();
            Debug.Log("XQ_TODAY open go=" + _go.activeInHierarchy + " canvas=" + (_canvas != null && _canvas.isActiveAndEnabled)
                      + " rect=" + _card.rect + " pos=" + _card.anchoredPosition);
        }

        public void Close()
        {
            _open = false;
            _go.SetActive(false);
        }

        private void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);
            _cursor = 0f;

            LabelAt("今天", XqStyle.FontCardTitle, XqStyle.Ink, 24f);
            if (!string.IsNullOrEmpty(_error)) LabelAt(_error, XqStyle.FontSmall, XqStyle.Muted, 18f);
            else if (_data == null) LabelAt("正在看今天…", XqStyle.FontSmall, XqStyle.Muted, 18f);
            else
            {
                LabelAt((_data.date ?? "") + "   心情 " + (_data.mood ?? ""), XqStyle.FontSmall, XqStyle.Muted, 18f);
                AddSchedule();
                AddMarks();
                AddBody();
            }

            _cursor += XqStyle.Fr(6f);
            _content.sizeDelta = new Vector2(_contentW, _cursor);
            _card.sizeDelta = new Vector2(_cardW, _cursor + XqStyle.Fr(28f));
            LayoutCard();
        }

        private RectTransform Row(float h)
        {
            var rt = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(_content, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(_contentW, h);
            rt.anchoredPosition = new Vector2(0f, -_cursor);
            _cursor += h + XqStyle.Fr(8f);
            return rt;
        }

        private Text LabelAt(string text, int size, Color color, float height)
        {
            var rt = Row(XqStyle.Fr(height));
            var t = XqUi.Label(rt, text, size, color, TextAnchor.MiddleLeft, height);
            var trt = t.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(_contentW, XqStyle.Fr(height));
            trt.anchoredPosition = Vector2.zero;
            return t;
        }

        private void LayoutCard()
        {
            float h = _cursor + XqStyle.Fr(28f);
            float w = _cardW;
            float sw = Screen.width, sh = Screen.height;
            var r = PetHost.I != null ? PetHost.I.ScreenRect() : new Rect(sw * 0.5f, sh * 0.5f, 0f, 0f);
            float x;
            if (r.xMax + XqStyle.Fr(16f) + w <= sw - XqStyle.Fr(8f)) x = r.xMax + XqStyle.Fr(16f);
            else if (r.xMin - XqStyle.Fr(16f) - w >= XqStyle.Fr(8f)) x = r.xMin - XqStyle.Fr(16f) - w;
            else x = Mathf.Clamp(r.center.x - w * 0.5f, XqStyle.Fr(8f), sw - w - XqStyle.Fr(8f));

            float top = Mathf.Clamp(r.center.y + h * 0.5f, h + XqStyle.Fr(8f), sh - XqStyle.Fr(8f));
            _card.anchoredPosition = new Vector2(x, top - sh);
        }

        // ------------------------------------------------------------ pieces

        private void AddSchedule()
        {
            LabelAt("日程", XqStyle.FontSmall, XqStyle.Muted, 16f);
            var row = Row(XqStyle.Fr(16f));
            float w = _contentW;
            int from = int.MaxValue, to = int.MinValue;
            foreach (var s in _data.segs ?? new Seg[0])
            {
                from = Mathf.Min(from, s.a);
                to = Mathf.Max(to, s.b);
            }
            if (to <= from) to = from + 1;

            foreach (var s in _data.segs ?? new Seg[0])
            {
                float x0 = (s.a - from) / (float)(to - from) * w;
                float x1 = (s.b - from) / (float)(to - from) * w;
                var segGo = new GameObject("Seg", typeof(RectTransform));
                segGo.transform.SetParent(row, false);
                var img = segGo.AddComponent<Image>();
                int pw = Mathf.Max(6, Mathf.RoundToInt(x1 - x0));
                img.sprite = XqUi.Pill(pw, Mathf.RoundToInt(XqStyle.Fr(10f)),
                    s.where == "dorm" ? XqStyle.PinkGuide : XqStyle.GrayLine, XqStyle.PinkGuide);
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                var rt = (RectTransform)segGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(Mathf.Max(6f, x1 - x0), XqStyle.Fr(10f));
                rt.anchoredPosition = new Vector2(x0, -XqStyle.Fr(2f));
            }

            if (_data.now_min > 0)
            {
                float t = Mathf.Clamp01((_data.now_min - from) / (float)(to - from));
                var nowGo = new GameObject("Now", typeof(RectTransform));
                nowGo.transform.SetParent(row, false);
                var img = nowGo.AddComponent<Image>();
                img.color = XqStyle.Ink;
                img.raycastTarget = false;
                var rt = (RectTransform)nowGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(1f, XqStyle.Fr(13f));
                rt.anchoredPosition = new Vector2(t * w, 0f);
            }
        }

        private void AddMarks()
        {
            LabelAt("情绪点", XqStyle.FontSmall, XqStyle.Muted, 16f);
            var marks = _data.marks ?? new Mark[0];
            if (marks.Length == 0)
            {
                LabelAt("今天还没有什么特别的", XqStyle.FontSmall, XqStyle.Body, 18f);
                return;
            }
            int shown = 0;
            for (int i = marks.Length - 1; i >= 0 && shown < 6; i--, shown++)
            {
                var m = marks[i];
                var row = Row(XqStyle.Fr(20f));
                var time = XqUi.Label(row, m.at ?? "", XqStyle.FontSmall, XqStyle.Muted, TextAnchor.MiddleLeft, 18f);
                Place(time.rectTransform, 0f, XqStyle.Fr(46f));
                var emo = XqUi.Label(row, m.emo ?? "", XqStyle.FontSmall, XqStyle.Ink, TextAnchor.MiddleLeft, 18f);
                Place(emo.rectTransform, XqStyle.Fr(48f), XqStyle.Fr(56f));
                var note = XqUi.Label(row, m.note ?? "", XqStyle.FontSmall, XqStyle.Body, TextAnchor.MiddleLeft, 18f);
                Place(note.rectTransform, XqStyle.Fr(106f), _contentW - XqStyle.Fr(106f));
            }
        }

        private static void Place(RectTransform rt, float x, float w)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, XqStyle.Fr(18f));
            rt.anchoredPosition = new Vector2(x, 0f);
        }

        private void AddBody()
        {
            if (_data.body == null) return;
            LabelAt("身体", XqStyle.FontSmall, XqStyle.Muted, 16f);
            AddBar("困意", _data.body.sleepy);
            AddBar("体力", _data.body.stamina);
            AddBar("社交", _data.body.social);
        }

        private void AddBar(string label, float value)
        {
            var row = Row(XqStyle.Fr(22f));
            var name = XqUi.Label(row, label, XqStyle.FontSmall, XqStyle.Body, TextAnchor.MiddleLeft, 18f);
            Place(name.rectTransform, 0f, XqStyle.Fr(40f));

            float w = _contentW - XqStyle.Fr(100f);
            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(row, false);
            var timg = track.AddComponent<Image>();
            timg.sprite = XqUi.Pill(Mathf.RoundToInt(w), Mathf.RoundToInt(XqStyle.Fr(8f)), XqStyle.PinkGuide, XqStyle.PinkGuide);
            timg.type = Image.Type.Simple;
            timg.raycastTarget = false;
            var trt = (RectTransform)track.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(w, XqStyle.Fr(8f));
            trt.anchoredPosition = new Vector2(XqStyle.Fr(44f), -XqStyle.Fr(7f));

            float fillW = Mathf.Clamp01(value / 100f) * w;
            if (fillW > 2f)
            {
                var fill = new GameObject("Fill", typeof(RectTransform));
                fill.transform.SetParent(track.transform, false);
                var fimg = fill.AddComponent<Image>();
                fimg.sprite = XqUi.Pill(Mathf.RoundToInt(fillW), Mathf.RoundToInt(XqStyle.Fr(8f)), XqStyle.PinkStrong, XqStyle.PinkStrong);
                fimg.type = Image.Type.Simple;
                fimg.raycastTarget = false;
                var frt = (RectTransform)fill.transform;
                frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f);
                frt.pivot = new Vector2(0f, 0.5f);
                frt.sizeDelta = new Vector2(fillW, XqStyle.Fr(8f));
                frt.anchoredPosition = Vector2.zero;
            }

            var val = XqUi.Label(row, Mathf.RoundToInt(value).ToString(), XqStyle.FontSmall, XqStyle.Muted, TextAnchor.MiddleRight, 18f);
            var vrt = val.rectTransform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(1f, 1f);
            vrt.sizeDelta = new Vector2(XqStyle.Fr(40f), XqStyle.Fr(18f));
            vrt.anchoredPosition = Vector2.zero;
        }

        // ------------------------------------------------------------- fetch

        private IEnumerator Fetch()
        {
            _nextRefresh = Time.unscaledTime + 30f;
            var args = Environment.GetCommandLineArgs();
            var fi = Array.IndexOf(args, "-fake-day");
            if (fi >= 0 && fi + 1 < args.Length && System.IO.File.Exists(args[fi + 1]))
            {
                try { _data = JsonUtility.FromJson<DayData>(System.IO.File.ReadAllText(args[fi + 1])); }
                catch (Exception e) { _error = e.Message; }
                Refresh();
                yield break;
            }
            var cfg = XiaoqiRuntime.Config;
            var url = (cfg.server ?? "").TrimEnd('/') + "/api/body/day";
            using (var req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { _data = JsonUtility.FromJson<DayData>(req.downloadHandler.text); _error = null; }
                    catch (Exception e) { _error = e.Message; }
                }
                else _error = "连不上";
            }
            Refresh();
        }
    }
}
