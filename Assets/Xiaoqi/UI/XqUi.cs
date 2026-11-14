using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xiaoqi.UI
{
    public class XqHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Action OnEnter, OnExit, OnDown, OnUp, OnClick;
        public void OnPointerEnter(PointerEventData e) => OnEnter?.Invoke();
        public void OnPointerExit(PointerEventData e) => OnExit?.Invoke();
        public void OnPointerDown(PointerEventData e) => OnDown?.Invoke();
        public void OnPointerUp(PointerEventData e) => OnUp?.Invoke();
        public void OnPointerClick(PointerEventData e) => OnClick?.Invoke();
    }

    /// <summary>G55 §3.4：纯代码 uGUI 控件。尺寸都按 XqStyle.Scale 放大。</summary>
    public static class XqUi
    {
        private static Font _font;
        private static Sprite _uiSprite;
        private static bool _uiSpriteTried;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>Unity 内置 UI 九宫格贴图（白色，可就地染色）。</summary>
        public static Sprite UiSprite
        {
            get
            {
                if (_uiSprite != null) return _uiSprite;
                if (_uiSpriteTried) return null;
                _uiSpriteTried = true;
                foreach (var n in new[] { "UI/Skin/UISprite.psd", "UI/Skin/Background.psd", "UI/Skin/Knob.psd" })
                {
                    _uiSprite = Resources.GetBuiltinResource<Sprite>(n);
                    if (_uiSprite != null) { Debug.Log("XQ_UI sprite " + n); break; }
                }
                if (_uiSprite == null) Debug.Log("XQ_UI sprite missing");
                return _uiSprite;
            }
        }

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try
                {
                    _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI" }, 16);
                }
                catch { }
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // ------------------------------------------------------------ sprites

        public static Sprite Round(Color fill, Color line, float radiusPx)
        {
            string key = "r" + ColorKey(fill) + ColorKey(line) + Mathf.RoundToInt(radiusPx * 10);
            if (Sprites.TryGetValue(key, out var s)) return s;
            const int size = 64;
            int r = Mathf.Clamp(Mathf.RoundToInt(radiusPx), 2, 30);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, size, size, r);
                float inside = Mathf.Clamp01(-d + 0.5f);
                float border = Mathf.Clamp01(-Mathf.Abs(d) + 0.75f);
                Color c = Color.Lerp(fill, line, border);
                float a = Mathf.Lerp(0f, fill.a, inside);
                if (border > 0f) a = Mathf.Max(a, Mathf.Lerp(0f, line.a, border));
                px[y * size + x] = c * 1f;
                px[y * size + x].a = (byte)(Mathf.Clamp01(a) * 255f);
            }
            tex.SetPixels32(px);
tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
            Sprites[key] = s;
            return s;
        }

        /// <summary>整块精确尺寸的胶囊贴图（不用九宫格，圆角不会被拉方）。</summary>
        public static Sprite Pill(int w, int h, Color fill, Color line)
        {
            w = Mathf.Max(8, w);
            h = Mathf.Max(8, h);
            string key = "p" + w + "x" + h + ColorKey(fill) + ColorKey(line);
            if (Sprites.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            float r = h * 0.5f - 1f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, w, h, r);
                float inside = Mathf.Clamp01(-d + 0.5f);
                float border = Mathf.Clamp01(-Mathf.Abs(d + 0.6f) + 0.9f);
                Color c = Color.Lerp(fill, line, border);
                float a = Mathf.Max(fill.a * inside, line.a * border);
                c.a = a;
                px[y * w + x] = c;
            }
            tex.SetPixels32(px);
tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Sprites[key] = s;
            return s;
        }

        public static Sprite Shadow(float radiusPx)
        {
            string key = "sh" + Mathf.RoundToInt(radiusPx);
            if (Sprites.TryGetValue(key, out var s)) return s;
            const int size = 64;
            int r = Mathf.Clamp(Mathf.RoundToInt(radiusPx), 2, 28);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, size, size, r);
                float a = Mathf.Clamp01(1f - d / 8f) * 0.10f;
                px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
            Sprites[key] = s;
            return s;
        }

        private static string ColorKey(Color c) =>
            Mathf.RoundToInt(c.r * 255) + "_" + Mathf.RoundToInt(c.g * 255) + "_" +
            Mathf.RoundToInt(c.b * 255) + "_" + Mathf.RoundToInt(c.a * 100);

        private static float RoundRectDist(float px, float py, float w, float h, float r)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float qx = Mathf.Abs(px - cx) - (cx - r);
            float qy = Mathf.Abs(py - cy) - (cy - r);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        // --------------------------------------------------------- primitives

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        public static Image Panel(RectTransform rt, Sprite sprite, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, float height = 20f)
        {
            var rt = Node(parent, "Label");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = Mathf.RoundToInt(size * XqStyle.Scale);
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(height);
            le.minHeight = XqStyle.Fr(height);
            return t;
        }

        public class CapsuleView
        {
            public RectTransform Root;
            public Image Bg;
            public Image HoverBg;
            public Text Text;
            public XqHover Hover;
            public bool Active = true;
        }

        public static CapsuleView Capsule(Transform parent, string text, Action onClick = null, bool dark = false)
        {
            var rt = Node(parent, "Capsule");
            var bg = Panel(rt, Round(XqStyle.Paper, XqStyle.PinkLine, 18f), Color.white);
            bg.raycastTarget = true;

            var hoverRt = Node(rt, "HoverBg");
            hoverRt.anchorMin = Vector2.zero;
            hoverRt.anchorMax = Vector2.one;
            hoverRt.offsetMin = Vector2.zero;
            hoverRt.offsetMax = Vector2.zero;
            var hoverImg = Panel(hoverRt, Round(XqStyle.PinkFill, XqStyle.PinkStrong, 18f), new Color(1f, 1f, 1f, 0f));
            hoverImg.raycastTarget = false;

            var t = Label(rt, text, XqStyle.FontCapsule, dark ? XqStyle.GrayText : XqStyle.Ink, TextAnchor.MiddleCenter, 0f);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(XqStyle.Fr(18f), 0f);
            t.rectTransform.offsetMax = new Vector2(-XqStyle.Fr(18f), 0f);

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(XqStyle.CapsuleHeight);
            le.minHeight = XqStyle.Fr(XqStyle.CapsuleHeight);

            var hover = rt.gameObject.AddComponent<XqHover>();
            if (onClick != null) hover.OnClick = onClick;

            return new CapsuleView { Root = rt, Bg = bg, HoverBg = hoverImg, Text = t, Hover = hover };
        }

        public static RectTransform Card(Transform parent, string title, float widthPx)
        {
            var rt = Node(parent, "Card");
            var img = Panel(rt, Round(XqStyle.Paper, XqStyle.GrayLine, XqStyle.CardRadius), Color.white);
            img.raycastTarget = true;
            rt.sizeDelta = new Vector2(XqStyle.Fr(widthPx), 0f);

            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(XqStyle.Px(16), XqStyle.Px(16), XqStyle.Px(14), XqStyle.Px(14));
            vlg.spacing = XqStyle.Fr(10f);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (!string.IsNullOrEmpty(title))
                Label(rt, title, XqStyle.FontCardTitle, XqStyle.Ink, TextAnchor.MiddleLeft, 24f);

            return rt;
        }

        public class SwitchView
        {
            public RectTransform Root;
            public Image Track, Knob;
            public Func<bool> Get;
            public Action<bool> Set;
        }

        public static SwitchView Switch(Transform parent, bool value, Action<bool> onChange)
        {
            var rt = Node(parent, "Switch");
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(28f);
            le.minHeight = XqStyle.Fr(28f);

            var trackRt = Node(rt, "Track");
            trackRt.sizeDelta = new Vector2(XqStyle.Fr(44f), XqStyle.Fr(24f));
            trackRt.anchoredPosition = new Vector2(0f, -XqStyle.Fr(2f));
            var track = Panel(trackRt, Round(XqStyle.GrayLine, XqStyle.GrayLine, 12f), Color.white);
            track.raycastTarget = true;

            var knobRt = Node(trackRt, "Knob");
            knobRt.sizeDelta = new Vector2(XqStyle.Fr(18f), XqStyle.Fr(18f));
            var knob = Panel(knobRt, Round(XqStyle.Paper, XqStyle.Paper, 10f), Color.white);
            knob.raycastTarget = false;

            var state = new SwitchView { Root = rt, Track = track, Knob = knob };
            state.Get = () => value;
            state.Set = v =>
            {
                value = v;
                Apply();
                onChange?.Invoke(v);
            };
            void Apply()
            {
                track.sprite = Round(value ? XqStyle.PinkStrong : XqStyle.GrayLine,
                    value ? XqStyle.PinkStrong : XqStyle.GrayLine, 12f);
                knobRt.anchorMin = knobRt.anchorMax = new Vector2(value ? 1f : 0f, 0.5f);
                knobRt.anchoredPosition = new Vector2(value ? -XqStyle.Fr(12f) : XqStyle.Fr(12f), 0f);
            }
            Apply();

            var hover = trackRt.gameObject.AddComponent<XqHover>();
            hover.OnClick = () => state.Set(!value);
            return state;
        }

        public class SegmentedView
        {
            public RectTransform Root;
            public List<CapsuleView> Items = new List<CapsuleView>();
            public Action<int> Changed;
            public int Index;

            public void SetIndex(int i)
            {
                Index = i;
                for (int k = 0; k < Items.Count; k++) Apply(k == i, Items[k]);
                Changed?.Invoke(i);
            }
        }

        private static void Apply(bool on, CapsuleView v)
        {
            v.Bg.sprite = Round(on ? XqStyle.PinkFill : XqStyle.Paper, on ? XqStyle.PinkStrong : XqStyle.PinkLine, 18f);
            v.Text.color = on ? XqStyle.PinkStrong : XqStyle.Ink;
        }

        public static SegmentedView Segmented(Transform parent, string[] options, int index, Action<int> onChange)
        {
            var rt = Node(parent, "Segmented");
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = XqStyle.Fr(8f);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(XqStyle.CapsuleHeight);
            le.minHeight = XqStyle.Fr(XqStyle.CapsuleHeight);

            var view = new SegmentedView { Root = rt, Changed = onChange };
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var cap = Capsule(rt, options[i], null);
                cap.Hover.OnClick = () => view.SetIndex(idx);
                var cw = cap.Root.gameObject.GetComponent<LayoutElement>();
                var textWidth = options[i].Length * XqStyle.FontCapsule * XqStyle.Scale * 1.0f + XqStyle.Fr(34f);
                cw.preferredWidth = Mathf.Max(XqStyle.Fr(52f), textWidth);
                view.Items.Add(cap);
                Apply(i == index, cap);
            }
            view.Index = index;
            return view;
        }

        public class SliderView
        {
            public RectTransform Root;
            public Slider Slider;
            public Text Value;
        }

        public static SliderView SliderRow(Transform parent, float min, float max, float value, string format, Action<float> onChange)
        {
            var rt = Node(parent, "SliderRow");
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(30f);
            le.minHeight = XqStyle.Fr(30f);

            var trackRt = Node(rt, "Track");
            trackRt.sizeDelta = new Vector2(XqStyle.Fr(220f), XqStyle.Fr(6f));
            trackRt.anchoredPosition = new Vector2(0f, -XqStyle.Fr(12f));
            var track = Panel(trackRt, Round(XqStyle.PinkGuide, XqStyle.PinkGuide, 3f), Color.white);

            var fillArea = Node(trackRt, "Fill Area");
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;
            var fill = Node(fillArea, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Panel(fill, Round(XqStyle.PinkStrong, XqStyle.PinkStrong, 3f), Color.white);

            var handle = Node(trackRt, "Handle");
            handle.sizeDelta = new Vector2(XqStyle.Fr(16f), XqStyle.Fr(16f));
            var handleImg = Panel(handle, Round(XqStyle.Paper, XqStyle.PinkStrong, 8f), Color.white);

            var slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));

            var val = Label(rt, value.ToString(format), XqStyle.FontSmall, XqStyle.Muted, TextAnchor.MiddleRight, 20f);
            val.rectTransform.anchorMin = new Vector2(1f, 1f);
            val.rectTransform.anchorMax = new Vector2(1f, 1f);
            val.rectTransform.pivot = new Vector2(1f, 1f);
            val.rectTransform.sizeDelta = new Vector2(XqStyle.Fr(70f), XqStyle.Fr(20f));
            val.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            return new SliderView { Root = rt, Slider = slider, Value = val };
        }

        public class InputView
        {
            public RectTransform Root;
            public InputField Field;
        }

        public static InputView TextInput(Transform parent, string value, bool password, Action<string> onChange)
        {
            var rt = Node(parent, "Input");
            var bg = Panel(rt, Round(XqStyle.GrayFill, XqStyle.GrayLine, 8f), Color.white);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(32f);
            le.minHeight = XqStyle.Fr(32f);

            var textRt = Node(rt, "Text");
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(XqStyle.Fr(10f), 0f);
            textRt.offsetMax = new Vector2(-XqStyle.Fr(10f), 0f);
            var text = textRt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = Mathf.RoundToInt(XqStyle.FontBody * XqStyle.Scale);
            text.color = XqStyle.Body;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var field = rt.gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.text = value ?? "";
            field.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            field.targetGraphic = bg;
            field.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return new InputView { Root = rt, Field = field };
        }

        public static Button IconButton(Transform parent, string glyph, Action onClick)
        {
            var rt = Node(parent, "IconButton");
            rt.sizeDelta = new Vector2(XqStyle.Fr(28f), XqStyle.Fr(28f));
            var img = Panel(rt, Round(new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f), 14f), Color.white);
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = Label(rt, glyph, XqStyle.FontBody, XqStyle.Muted, TextAnchor.MiddleCenter, 0f);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            return btn;
        }
    }
}
