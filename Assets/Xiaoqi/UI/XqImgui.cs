using UnityEngine;

namespace Xiaoqi.UI
{
    /// <summary>G55 5.5：给还留在 IMGUI 的 F9 情感页一套白底圆角皮肤。</summary>
    public static class XqImgui
    {
        private static GUISkin _skin;

        public static GUISkin Skin()
        {
            if (_skin != null) return _skin;
            var s = Object.Instantiate(GUI.skin);
            s.font = XqUi.Font;
            s.name = "XqSkin";

            var box = Tex(XqStyle.Paper, XqStyle.GrayLine, 14f);
            var win = Tex(XqStyle.Paper, XqStyle.GrayLine, 14f);
            var btn = Tex(XqStyle.Paper, XqStyle.PinkLine, 10f);
            var btnHover = Tex(XqStyle.PinkFill, XqStyle.PinkStrong, 10f);
            var btnActive = Tex(XqStyle.PinkFill, XqStyle.PinkStrong, 10f);
            var field = Tex(XqStyle.GrayFill, XqStyle.GrayLine, 8f);
            var track = Tex(XqStyle.PinkGuide, XqStyle.PinkGuide, 3f);
            var thumb = Tex(XqStyle.Paper, XqStyle.PinkStrong, 8f);

            s.box = Clone(s.box, box, XqStyle.Paper, XqStyle.Body, 14);
            s.window = Clone(s.window, win, XqStyle.Paper, XqStyle.Ink, 16);
            s.label = Clone(s.label, null, XqStyle.Paper, XqStyle.Body, 14);
            s.button = Clone(s.button, btn, XqStyle.Paper, XqStyle.Ink, 14);
            s.button.hover = CloneState(s.button.hover, btnHover, XqStyle.PinkFill, XqStyle.PinkStrong);
            s.button.active = CloneState(s.button.active, btnActive, XqStyle.PinkFill, XqStyle.PinkStrong);
            s.textField = Clone(s.textField, field, XqStyle.GrayFill, XqStyle.Body, 14);
            s.horizontalSlider = Clone(s.horizontalSlider, track, XqStyle.Paper, XqStyle.Body, 6);
            s.horizontalSliderThumb = Clone(s.horizontalSliderThumb, thumb, XqStyle.Paper, XqStyle.PinkStrong, 16);

            _skin = s;
            return s;
        }

        private static GUIStyle Clone(GUIStyle src, Texture2D bg, Color bgColor, Color text, int border)
        {
            var st = new GUIStyle(src);
            if (bg != null)
            {
                st.normal.background = bg;
                st.normal.textColor = text;
                st.hover.background = bg;
                st.hover.textColor = text;
                st.active.background = bg;
                st.active.textColor = text;
                st.border = new RectOffset(border, border, border, border);
            }
            else
            {
                st.normal.background = null;
                st.normal.textColor = text;
                st.hover.textColor = text;
                st.active.textColor = text;
            }
            st.font = XqUi.Font;
            return st;
        }

        private static GUIStyleState CloneState(GUIStyleState src, Texture2D bg, Color bgColor, Color text)
        {
            var st = new GUIStyleState { background = bg, textColor = text };
            return st;
        }

        private static Texture2D Tex(Color fill, Color line, float radius)
        {
            const int size = 48;
            int r = Mathf.Clamp(Mathf.RoundToInt(radius), 2, 22);
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Dist(x + 0.5f, y + 0.5f, size, size, r);
                float inside = Mathf.Clamp01(-d + 0.5f);
                float border = Mathf.Clamp01(-Mathf.Abs(d) + 0.75f);
                Color c = Color.Lerp(fill, line, border);
                c.a *= Mathf.Max(inside, border * line.a);
                px[y * size + x] = c;
            }
            t.SetPixels32(px);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.Apply();
            return t;
        }

        private static float Dist(float px, float py, float w, float h, float r)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float qx = Mathf.Abs(px - cx) - (cx - r);
            float qy = Mathf.Abs(py - cy) - (cy - r);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
    }
}
