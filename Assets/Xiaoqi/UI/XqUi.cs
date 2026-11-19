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

    /// <summary>主题 A：按下轻微内缩（视觉反馈，不改变任何点击逻辑）。</summary>
    public class XqPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 _base = Vector3.one;
        private bool _down;

        private void Release()
        {
            if (!_down) return;
            _down = false;
            transform.localScale = _base;
        }

        public void OnPointerDown(PointerEventData e)
        {
            _base = transform.localScale;
            _down = true;
            transform.localScale = _base * XqStyle.PressScale;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();
        private void OnDisable() => Release();
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

        /// <summary>填充 + 内侧描边（strokePx 为描边像素宽），抗锯齿由 0.5px 覆盖率给出。</summary>
        private static Color32 Shade(Color fill, Color line, float d, float stroke)
        {
            float shape = Mathf.Clamp01(-d + 0.5f);
            float inner = stroke <= 0f ? shape : Mathf.Clamp01(-(d + stroke) + 0.5f);
            float ring = Mathf.Max(0f, shape - inner);
            float fw = inner * fill.a, lw = ring * line.a;
            float sum = fw + lw;
            if (sum <= 0.0001f) return new Color32(0, 0, 0, 0);
            Color c = (fill * fw + line * lw) / sum;
            c.a = Mathf.Clamp01(Mathf.Max(fw, lw));
            return c;
        }

        public static Sprite Round(Color fill, Color line, float radiusPx, float strokePx = 1.5f)
        {
            string key = "r" + ColorKey(fill) + ColorKey(line) + Mathf.RoundToInt(radiusPx * 10)
                         + "s" + Mathf.RoundToInt(strokePx * 10);
            if (Sprites.TryGetValue(key, out var s)) return s;
            const int size = 64;
            int r = Mathf.Clamp(Mathf.RoundToInt(radiusPx), 2, 30);
            float stroke = Mathf.Max(0f, strokePx);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, size, size, r);
                px[y * size + x] = Shade(fill, line, d, stroke);
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
        public static Sprite Pill(int w, int h, Color fill, Color line, float strokePx = 1f)
        {
            w = Mathf.Max(8, w);
            h = Mathf.Max(8, h);
            string key = "p" + w + "x" + h + ColorKey(fill) + ColorKey(line) + "s" + Mathf.RoundToInt(strokePx * 10);
            if (Sprites.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            float r = h * 0.5f - 1f;
            float stroke = Mathf.Max(0f, strokePx);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, w, h, r);
                px[y * w + x] = Shade(fill, line, d, stroke);
            }
            tex.SetPixels32(px);
tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Sprites[key] = s;
            return s;
        }

        /// <summary>主题 B：精确尺寸的圆角矩形（任意半径，圆角不会被九宫格拉扁）。面板/底板用。</summary>
        public static Sprite Rounded(int w, int h, float radiusPx, Color fill, Color line, float strokePx = 1f)
        {
            w = Mathf.Max(8, w);
            h = Mathf.Max(8, h);
            string key = "q" + w + "x" + h + "r" + Mathf.RoundToInt(radiusPx * 10)
                         + ColorKey(fill) + ColorKey(line) + "s" + Mathf.RoundToInt(strokePx * 10);
            if (Sprites.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            float r = Mathf.Clamp(radiusPx, 1f, Mathf.Min(w, h) * 0.5f);
            float stroke = Mathf.Max(0f, strokePx);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, w, h, r);
                px[y * w + x] = Shade(fill, line, d, stroke);
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Sprites[key] = s;
            return s;
        }

        /// <summary>柔和外投影蒙版（白色 + 渐隐 alpha，颜色由 Image.color 给）。</summary>
        public static Sprite Shadow(float radiusPx)
        {
            string key = "sh" + Mathf.RoundToInt(radiusPx) + "b" + Mathf.RoundToInt(XqStyle.ShadowBlur * 10);
            if (Sprites.TryGetValue(key, out var s)) return s;
            const int size = 64;
            int r = Mathf.Clamp(Mathf.RoundToInt(radiusPx), 2, 28);
            float blur = Mathf.Max(1f, XqStyle.ShadowBlur);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, size, size, r);
                float a = Mathf.Clamp01(1f - d / blur);
                a *= a;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
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

        // ------------------------------------- 主题 C：一级项 14px 细线图标

        /// <summary>
        /// 主题 C：一级项左侧的细线图标。全部在 14×14 设计坐标里**用代码光栅化**（4× 超采样），
        /// 不依赖任何贴图资源；贴图是白色 + alpha 覆盖率，颜色由 Image.color 给，只取 XqStyle 的三档。
        /// </summary>
        public static class Icon
        {
            public const string Today = "today";
            public const string Say = "say";
            public const string Doing = "doing";
            public const string Quiet = "quiet";
            public const string Live = "live";
            public const string Settings = "settings";

            private const float BoxSize = 14f; // 设计坐标：14×14，y 向下
            private const int Super = 4;       // 超采样倍数

            public static Sprite Get(string kind)
            {
                int px = Mathf.Max(10, XqStyle.Px(XqStyle.IconGeo.Size));
                string key = "ic" + kind + px;
                if (Sprites.TryGetValue(key, out var hit)) return hit;

                int n = px * Super;
                float k = n / BoxSize;
                var cov = new float[n * n];
                switch (kind)
                {
                    case Say: ArtSay(cov, n, k); break;
                    case Doing: ArtDoing(cov, n, k); break;
                    case Quiet: ArtQuiet(cov, n, k); break;
                    case Live: ArtLive(cov, n, k); break;
                    case Settings: ArtSettings(cov, n, k); break;
                    default: ArtToday(cov, n, k); break;
                }

                var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
                var outPx = new Color32[px * px];
                for (int y = 0; y < px; y++)
                for (int x = 0; x < px; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < Super; sy++)
                    {
                        int dr = (px - 1 - y) * Super + sy; // 设计 y 向下，纹理 y 向上
                        int row = dr * n;
                        for (int sx = 0; sx < Super; sx++) a += cov[row + x * Super + sx];
                    }
                    a /= Super * Super;
                    outPx[y * px + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
                tex.SetPixels32(outPx);
                tex.Apply();
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var s = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect);
                Sprites[key] = s;
                return s;
            }

            /// <summary>挂一枚图标节点到 parent 左上（以 parent 左中为锚，向内缩 InsetX）。</summary>
            public static Image Attach(Transform parent, string kind, Color tint)
            {
                var rt = Node(parent, "Icon");
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(XqStyle.Fr(XqStyle.IconGeo.Size), XqStyle.Fr(XqStyle.IconGeo.Size));
                rt.anchoredPosition = new Vector2(XqStyle.Fr(XqStyle.IconGeo.InsetX), 0f);
                var img = Panel(rt, Get(kind), tint);
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.raycastTarget = false;
                return img;
            }

            // -------------------------------------------------------- 光栅化

            private static void Ink(float[] cov, int n, float k, Func<float, float, float> dist)
            {
                float hw = XqStyle.IconGeo.Stroke * 0.5f;
                float cutoff = hw + 1f;
                for (int y = 0; y < n; y++)
                {
                    float py = (y + 0.5f) / k;
                    int row = y * n;
                    for (int x = 0; x < n; x++)
                    {
                        float d = dist((x + 0.5f) / k, py);
                        if (d > cutoff) continue;
                        float a = Mathf.Clamp01((hw - d) * k + 0.5f);
                        int i = row + x;
                        if (a > cov[i]) cov[i] = a;
                    }
                }
            }

            private static void Fill(float[] cov, int n, float k, Func<float, float, float> signed)
            {
                for (int y = 0; y < n; y++)
                {
                    float py = (y + 0.5f) / k;
                    int row = y * n;
                    for (int x = 0; x < n; x++)
                    {
                        if (signed((x + 0.5f) / k, py) >= 0f) continue;
                        int i = row + x;
                        cov[i] = 1f;
                    }
                }
            }

            /// <summary>按“洞”挖掉：signed 为负表示洞内。</summary>
            private static void Cut(float[] cov, int n, float k, Func<float, float, float> signed)
            {
                for (int y = 0; y < n; y++)
                {
                    float py = (y + 0.5f) / k;
                    int row = y * n;
                    for (int x = 0; x < n; x++)
                    {
                        float d = signed((x + 0.5f) / k, py);
                        if (d > 0.5f) continue;
                        float m = Mathf.Clamp01(-d * k + 0.5f);
                        int i = row + x;
                        cov[i] *= 1f - m;
                    }
                }
            }

            private static float Seg(float px, float py, float ax, float ay, float bx, float by)
            {
                float vx = bx - ax, vy = by - ay;
                float wx = px - ax, wy = py - ay;
                float t = Mathf.Clamp01((wx * vx + wy * vy) / Mathf.Max(1e-6f, vx * vx + vy * vy));
                float dx = wx - vx * t, dy = wy - vy * t;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            /// <summary>闭合折线（pts 为 x,y 成对）。</summary>
            private static float Poly(float px, float py, float[] pts)
            {
                float best = 999f;
                int m = pts.Length / 2;
                for (int i = 0; i < m; i++)
                {
                    int j = (i + 1) % m;
                    float d = Seg(px, py, pts[i * 2], pts[i * 2 + 1], pts[j * 2], pts[j * 2 + 1]);
                    if (d < best) best = d;
                }
                return best;
            }

            /// <summary>圆角矩形的带符号距离（负为内部）。</summary>
            private static float Box(float px, float py, float x0, float y0, float x1, float y1, float r)
            {
                float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
                float hx = (x1 - x0) * 0.5f - r, hy = (y1 - y0) * 0.5f - r;
                float qx = Mathf.Abs(px - cx) - hx;
                float qy = Mathf.Abs(py - cy) - hy;
                float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            }

            private static float Ring(float px, float py, float cx, float cy, float r)
            {
                float dx = px - cx, dy = py - cy;
                return Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - r);
            }

            private static float Disk(float px, float py, float cx, float cy, float r)
            {
                float dx = px - cx, dy = py - cy;
                return Mathf.Sqrt(dx * dx + dy * dy) - r;
            }

            // ---------------------------------------------------------- 图形

            /// <summary>今天：日历（外框 + 两枚挂耳 + 表头线 + 两点）。</summary>
            private static void ArtToday(float[] cov, int n, float k)
            {
                Ink(cov, n, k, (x, y) => Mathf.Abs(Box(x, y, 1.8f, 3.5f, 12.2f, 12.3f, 2.2f)));
                Ink(cov, n, k, (x, y) => Seg(x, y, 4.8f, 1.8f, 4.8f, 3.7f));
                Ink(cov, n, k, (x, y) => Seg(x, y, 9.2f, 1.8f, 9.2f, 3.7f));
                Ink(cov, n, k, (x, y) => Seg(x, y, 2.0f, 6.4f, 12.0f, 6.4f));
                Fill(cov, n, k, (x, y) => Disk(x, y, 5.1f, 9.3f, 0.7f));
                Fill(cov, n, k, (x, y) => Disk(x, y, 8.9f, 9.3f, 0.7f));
            }

            /// <summary>说句话：对话气泡 + 小尾巴 + 两点。</summary>
            private static void ArtSay(float[] cov, int n, float k)
            {
                Ink(cov, n, k, (x, y) => Mathf.Abs(Box(x, y, 1.8f, 2.4f, 12.2f, 10.1f, 2.8f)));
                Ink(cov, n, k, (x, y) => Seg(x, y, 4.6f, 9.9f, 4.6f, 12.3f));
                Ink(cov, n, k, (x, y) => Seg(x, y, 4.6f, 12.3f, 7.5f, 9.9f));
                Fill(cov, n, k, (x, y) => Disk(x, y, 5.5f, 6.2f, 0.6f));
                Fill(cov, n, k, (x, y) => Disk(x, y, 8.5f, 6.2f, 0.6f));
            }

            /// <summary>让她…：一枚四角星（她做点什么）。</summary>
            private static void ArtDoing(float[] cov, int n, float k)
            {
                var star = new[]
                {
                    7f, 1.6f, 8.5f, 5.5f, 12.4f, 7f, 8.5f, 8.5f,
                    7f, 12.4f, 5.5f, 8.5f, 1.6f, 7f, 5.5f, 5.5f
                };
                Ink(cov, n, k, (x, y) => Poly(x, y, star));
            }

            /// <summary>安静一会：实心月牙（圆盘减去偏移圆盘，14px 也读得出）。</summary>
            private static void ArtQuiet(float[] cov, int n, float k)
            {
                Fill(cov, n, k, (x, y) => Disk(x, y, 7.0f, 7.2f, 4.9f));
                Cut(cov, n, k, (x, y) => Disk(x, y, 9.4f, 5.4f, 4.3f));
            }

            /// <summary>直播间：圆角方框 + 播放三角。</summary>
            private static void ArtLive(float[] cov, int n, float k)
            {
                Ink(cov, n, k, (x, y) => Mathf.Abs(Box(x, y, 1.8f, 3.0f, 12.2f, 11.6f, 2.6f)));
                var tri = new[] { 5.8f, 5.2f, 9.9f, 7.3f, 5.8f, 9.4f };
                Ink(cov, n, k, (x, y) => Poly(x, y, tri));
            }

            /// <summary>设置：齿轮（中心圈 + 六枚齿，14px 下比八齿更不像花）。</summary>
            private static void ArtSettings(float[] cov, int n, float k)
            {
                Ink(cov, n, k, (x, y) => Ring(x, y, 7f, 7f, 3.0f));
                Fill(cov, n, k, (x, y) => Disk(x, y, 7f, 7f, 0.95f));
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    Ink(cov, n, k, (x, y) => Seg(x, y, 7f + 3.6f * c, 7f + 3.6f * s,
                        7f + 5.5f * c, 7f + 5.5f * s));
                }
            }
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

        public class PlateView
        {
            public RectTransform Root;
            public Image Bg, Shadow;
            public CanvasGroup Group;
        }

        /// <summary>主题 B：底板（柔和投影 + 圆角填充 + 1px 描边）。永不参与点击，空白处仍按旧逻辑穿透。</summary>
        public static PlateView Plate(Transform parent, string name)
        {
            var rt = Node(parent, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var shadowRt = Node(rt, "Shadow");
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(-XqStyle.Fr(XqStyle.ShadowSpread),
                -XqStyle.Fr(XqStyle.ShadowSpread) - XqStyle.Fr(XqStyle.ShadowOffsetY));
            shadowRt.offsetMax = new Vector2(XqStyle.Fr(XqStyle.ShadowSpread),
                XqStyle.Fr(XqStyle.ShadowSpread) - XqStyle.Fr(XqStyle.ShadowOffsetY));
            var shadow = Panel(shadowRt, Shadow(XqStyle.Fr(XqStyle.SubPanelRadius)), XqStyle.ShadowSoft);
            shadow.raycastTarget = false;

            var bg = Panel(rt, Rounded(64, 64, XqStyle.Fr(XqStyle.SubPanelRadius), XqStyle.SubPanelFill,
                XqStyle.SubPanelLine), Color.white);
            bg.type = Image.Type.Simple;
            bg.raycastTarget = false;

            var grp = rt.gameObject.AddComponent<CanvasGroup>();
            grp.alpha = 0f;
            grp.interactable = false;
            grp.blocksRaycasts = false;
            return new PlateView { Root = rt, Bg = bg, Shadow = shadow, Group = grp };
        }

        /// <summary>按内容包围盒换底板尺寸与贴图；圆角、描边宽度都从 XqStyle 取。</summary>
        public static void FitPlate(PlateView p, Vector2 size)
        {
            if (p == null) return;
            int w = Mathf.Max(8, Mathf.RoundToInt(size.x));
            int h = Mathf.Max(8, Mathf.RoundToInt(size.y));
            p.Root.sizeDelta = new Vector2(w, h);
            p.Bg.sprite = Rounded(w, h, XqStyle.Fr(XqStyle.SubPanelRadius), XqStyle.SubPanelFill, XqStyle.SubPanelLine);
            p.Bg.type = Image.Type.Simple;
            p.Shadow.sprite = Shadow(XqStyle.Fr(XqStyle.SubPanelRadius));
        }

        // ------------------------------------- 主题 D：三张卡共用的卡面 / 标题区 / 区标题

        public class CardSkin
        {
            public RectTransform Root;
            public Image Bg;
            public Image Shadow;
        }

        /// <summary>
        /// 主题 D：卡面 = 柔和投影 + 圆角填充 + 1px 描边。卡根不再自带 Image，
        /// 点空白处由 Bg 吸收（rawcast 语义和原来一样），子节点一律排在 Bg 之上。
        /// </summary>
        public static CardSkin CardBase(RectTransform root, float radiusPx = -1f)
        {
            if (radiusPx < 0f) radiusPx = XqStyle.Fr(XqStyle.CardRadiusLg);
            var skin = new CardSkin { Root = root };

            var shadowRt = Node(root, "Shadow");
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(-XqStyle.Fr(XqStyle.ShadowSpread),
                -XqStyle.Fr(XqStyle.ShadowSpread) - XqStyle.Fr(XqStyle.ShadowOffsetY));
            shadowRt.offsetMax = new Vector2(XqStyle.Fr(XqStyle.ShadowSpread),
                XqStyle.Fr(XqStyle.ShadowSpread) - XqStyle.Fr(XqStyle.ShadowOffsetY));
            shadowRt.SetAsFirstSibling();
            var shadow = Panel(shadowRt, Shadow(radiusPx), XqStyle.ShadowSoft);
            shadow.raycastTarget = false;

            var bgRt = Node(root, "Bg");
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgRt.SetSiblingIndex(1);
            var bg = Panel(bgRt, Rounded(8, 8, radiusPx, XqStyle.CardFill, XqStyle.CardLine), Color.white);
            bg.type = Image.Type.Simple;
            bg.raycastTarget = true;

            skin.Bg = bg;
            skin.Shadow = shadow;
            return skin;
        }

        /// <summary>卡尺寸变了就换一张同尺寸的圆角贴图（精确尺寸，圆角不会被九宫格拉扁）。</summary>
        public static void CardSize(CardSkin skin, float w, float h)
        {
            if (skin == null) return;
            int iw = Mathf.Max(8, Mathf.RoundToInt(w));
            int ih = Mathf.Max(8, Mathf.RoundToInt(h));
            skin.Bg.sprite = Rounded(iw, ih, XqStyle.Fr(XqStyle.CardRadiusLg), XqStyle.CardFill, XqStyle.CardLine);
            skin.Bg.type = Image.Type.Simple;
            skin.Shadow.sprite = Shadow(XqStyle.Fr(XqStyle.CardRadiusLg));
        }

        /// <summary>标题区：左标题 + 右关闭 × + 底部 1px 分隔线。返回内容区要往下让出的高度。</summary>
        public static float CardHeader(RectTransform card, string title, Action onClose)
        {
            var t = Label(card, title, XqStyle.FontCardTitle, XqStyle.Ink, TextAnchor.MiddleLeft, XqStyle.CardTitleH);
            var trt = t.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(XqStyle.Fr(220f), XqStyle.Fr(XqStyle.CardTitleH));
            trt.anchoredPosition = new Vector2(XqStyle.Fr(XqStyle.CardHeaderPadX),
                -XqStyle.Fr((XqStyle.CardHeaderH - XqStyle.CardTitleH) * 0.5f));

            if (onClose != null)
            {
                var btn = IconButton(card, "×", onClose);
                var crt = btn.GetComponent<RectTransform>();
                crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot = new Vector2(1f, 1f);
                crt.anchoredPosition = new Vector2(-XqStyle.Fr(XqStyle.CardHeaderPadX - 8f),
                    -XqStyle.Fr((XqStyle.CardHeaderH - 28f) * 0.5f));
            }

            var rule = Node(card, "HeaderRule");
            rule.anchorMin = new Vector2(0f, 1f);
            rule.anchorMax = new Vector2(1f, 1f);
            rule.pivot = new Vector2(0.5f, 1f);
            rule.offsetMin = new Vector2(XqStyle.Fr(XqStyle.CardBodyPadX), -XqStyle.Fr(XqStyle.CardHeaderH));
            rule.offsetMax = new Vector2(-XqStyle.Fr(XqStyle.CardBodyPadX),
                -XqStyle.Fr(XqStyle.CardHeaderH - XqStyle.CardRuleH));
            var rimg = Panel(rule, null, XqStyle.CardRule);
            rimg.type = Image.Type.Simple;
            rimg.raycastTarget = false;

            return XqStyle.CardHeaderH;
        }

        /// <summary>区标题：12px 小字 + 向右延伸的 1px 细线，卡片内部的分区统一用这一种层级。</summary>
        public static RectTransform CardSection(Transform parent, string title, float width)
        {
            var rt = Node(parent, "Section");
            rt.sizeDelta = new Vector2(width, XqStyle.Fr(XqStyle.CardSectionH));
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(XqStyle.CardSectionH);
            le.minHeight = XqStyle.Fr(XqStyle.CardSectionH);

            var t = Label(rt, title, XqStyle.FontCardSection, XqStyle.CardSectionText,
                TextAnchor.MiddleLeft, XqStyle.CardSectionH);
            var trt = t.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0f, 0.5f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.sizeDelta = new Vector2(XqStyle.Fr(120f), XqStyle.Fr(XqStyle.CardSectionH));
            trt.anchoredPosition = Vector2.zero;

            float tx = Mathf.Min(width * 0.5f,
                XqStyle.Fr(EstimateText(title, XqStyle.FontCardSection) + 12f));
            float lineW = width - tx;
            if (lineW > 2f)
            {
                var line = Node(rt, "Rule");
                line.anchorMin = line.anchorMax = new Vector2(0f, 0.5f);
                line.pivot = new Vector2(0f, 0.5f);
                line.sizeDelta = new Vector2(lineW, XqStyle.Fr(XqStyle.CardRuleH));
                line.anchoredPosition = new Vector2(tx, 0f);
                var img = Panel(line, null, XqStyle.CardSectionRule);
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
            }
            return rt;
        }

        /// <summary>卡内按钮：主按钮浅粉底 + 粉描边 + 粉字；次按钮白底 + 粉描边 + 墨字。</summary>
        public static CapsuleView CardButton(Transform parent, string text, float w, float h, Action onClick,
            bool primary)
        {
            var cap = Capsule(parent, text, null, false, false);
            cap.Root.sizeDelta = new Vector2(w, h);
            int pw = Mathf.Max(8, Mathf.RoundToInt(w));
            int ph = Mathf.Max(8, Mathf.RoundToInt(h));
            cap.Bg.sprite = Pill(pw, ph,
                primary ? XqStyle.CardActionFill : XqStyle.CardActionSoftFill,
                primary ? XqStyle.CardActionLine : XqStyle.CardActionSoftLine);
            cap.Bg.type = Image.Type.Simple;
            cap.Text.color = primary ? XqStyle.CardActionText : XqStyle.CardActionSoftText;
            cap.Text.fontSize = Mathf.RoundToInt(XqStyle.FontBody * XqStyle.Scale);
            cap.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
            cap.Text.verticalOverflow = VerticalWrapMode.Overflow;
            cap.Hover.OnClick = onClick;
            return cap;
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

        public static CapsuleView Capsule(Transform parent, string text, Action onClick = null, bool dark = false,
            bool shadow = true)
        {
            var rt = Node(parent, "Capsule");

            // 主题 A：柔和投影（比本体略外扩、略下沉，跟着胶囊尺寸走）。
            // 主题 D：卡片内的按钮/分段项不投影——落在白卡上会变成一圈灰晕，压掉更干净。
            if (shadow)
            {
                var shadowRt = Node(rt, "Shadow");
                shadowRt.anchorMin = Vector2.zero;
                shadowRt.anchorMax = Vector2.one;
                shadowRt.offsetMin = new Vector2(-XqStyle.Fr(XqStyle.ShadowSpread),
                    -XqStyle.Fr(XqStyle.ShadowSpread) - XqStyle.Fr(XqStyle.ShadowOffsetY));
                shadowRt.offsetMax = new Vector2(XqStyle.Fr(XqStyle.ShadowSpread),
                    XqStyle.Fr(XqStyle.ShadowSpread) - XqStyle.Fr(XqStyle.ShadowOffsetY));
                var shadowImg = Panel(shadowRt, Shadow(XqStyle.CapsuleRadius), XqStyle.ShadowSoft);
                shadowImg.raycastTarget = false;
            }

            var bg = Panel(rt, Round(XqStyle.Paper, XqStyle.PinkLine, XqStyle.CapsuleRadius, XqStyle.StrokeWidth),
                Color.white);
            bg.raycastTarget = true;

            var hoverRt = Node(rt, "HoverBg");
            hoverRt.anchorMin = Vector2.zero;
            hoverRt.anchorMax = Vector2.one;
            hoverRt.offsetMin = Vector2.zero;
            hoverRt.offsetMax = Vector2.zero;
            var hoverImg = Panel(hoverRt, Round(XqStyle.PinkFill, XqStyle.PinkStrong, XqStyle.CapsuleRadius,
                XqStyle.StrokeWidth), new Color(1f, 1f, 1f, 0f));
            hoverImg.raycastTarget = false;

            var t = Label(rt, text, XqStyle.FontCapsule, dark ? XqStyle.TextDisabled : XqStyle.TextPrimary,
                TextAnchor.MiddleCenter, 0f);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(XqStyle.Fr(18f), 0f);
            t.rectTransform.offsetMax = new Vector2(-XqStyle.Fr(18f), 0f);

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = XqStyle.Fr(XqStyle.CapsuleHeight);
            le.minHeight = XqStyle.Fr(XqStyle.CapsuleHeight);

            var hover = rt.gameObject.AddComponent<XqHover>();
            if (onClick != null) hover.OnClick = onClick;
            rt.gameObject.AddComponent<XqPress>();

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
            int trackW = Mathf.RoundToInt(XqStyle.Fr(44f)), trackH = Mathf.RoundToInt(XqStyle.Fr(24f));
            int knobPx = Mathf.RoundToInt(XqStyle.Fr(18f));
            var track = Panel(trackRt, Rounded(trackW, trackH, trackH * 0.5f, XqStyle.GrayLine, XqStyle.GrayLine),
                Color.white);
            track.type = Image.Type.Simple;
            track.raycastTarget = true;

            // 主题 B 附带修：旋钮原本用 Node() 的 pivot=(0,1) 又锚在右边，半个身子露在轨道外。
            var knobRt = Node(trackRt, "Knob");
            knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta = new Vector2(knobPx, knobPx);
            var knob = Panel(knobRt, Rounded(knobPx, knobPx, knobPx * 0.5f, XqStyle.Paper, XqStyle.Paper), Color.white);
            knob.type = Image.Type.Simple;
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
                track.sprite = Rounded(trackW, trackH, trackH * 0.5f,
                    value ? XqStyle.PinkStrong : XqStyle.GrayLine,
                    value ? XqStyle.PinkStrong : XqStyle.GrayLine);
                track.type = Image.Type.Simple;
                knobRt.anchorMin = knobRt.anchorMax = new Vector2(0.5f, 0.5f);
                float inset = Mathf.Max(0f, (XqStyle.Fr(44f) - knobPx) * 0.5f - XqStyle.Fr(2f));
                knobRt.anchoredPosition = new Vector2(value ? inset : -inset, 0f);
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

        /// <summary>粗略字宽：CJK 记 1em、ASCII 0.58em、空白 0.33em。只用于给胶囊定首选宽度。</summary>
        public static float EstimateText(string s, float fontPx)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            float w = 0f;
            foreach (var ch in s)
                w += ch <= 0x20 ? fontPx * 0.33f : (ch < 0x7F ? fontPx * 0.58f : fontPx);
            return w;
        }

        private static void Apply(bool on, CapsuleView v)
        {
            v.Bg.sprite = Round(on ? XqStyle.PinkFill : XqStyle.Paper, on ? XqStyle.PinkStrong : XqStyle.PinkLine,
                XqStyle.CapsuleRadius, on ? XqStyle.StrokeWidth * 1.5f : XqStyle.StrokeWidth);
            v.Text.color = on ? XqStyle.PinkStrong : XqStyle.TextPrimary;
        }

        public static SegmentedView Segmented(Transform parent, string[] options, int index, Action<int> onChange,
            bool shadow = true)
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
                var cap = Capsule(rt, options[i], null, false, shadow);
                cap.Hover.OnClick = () => view.SetIndex(idx);
                // 分段按钮永远单行：空间不够时宁可让文字多占一点内边距，也不折成两行。
                cap.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
                cap.Text.verticalOverflow = VerticalWrapMode.Overflow;
                var cw = cap.Root.gameObject.GetComponent<LayoutElement>();
                // 主题 B 附带：按字数估算（CJK 1em / ASCII 0.58em / 空白 0.33em），比等宽估算准，
                // 也不会像 Text.preferredWidth 那样在 Wrap 模式下拿到被父宽影响的值。
                var textWidth = EstimateText(options[i], XqStyle.FontCapsule * XqStyle.Scale)
                                + XqStyle.Fr(2f * XqStyle.CapsulePadX + 2f);
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
            // 主题 D 修：拉伸锚点必须把 Node() 留下的 (100,100) 尺寸清零，否则 × 会被顶到卡外右下各 50px。
            t.rectTransform.sizeDelta = Vector2.zero;
            t.rectTransform.anchoredPosition = Vector2.zero;
            return btn;
        }
    }
}
