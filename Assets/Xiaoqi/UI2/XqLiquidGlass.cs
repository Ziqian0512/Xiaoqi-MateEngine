using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 近景模式质感与立体层次助手：
    /// - 柔和紫雾外投影（营造悬浮厚度，绝不使用 Overflow.Hidden）
    /// - 破框定制可爱贴纸（蝴蝶结 / 云朵）跨出卡片边界，打破平面矩形感
    /// - 人物背后极淡紫粉呼吸光晕（把 3D 小祈从杂乱桌面背景中托出）
    /// </summary>
    internal static class XqLiquidGlass
    {
        static VisualElement _bgHalo;
        static readonly List<VisualElement> _stickers = new List<VisualElement>();

        public static void ClearSlots()
        {
            _bgHalo = null;
            _stickers.Clear();
        }

        /// <summary>给卡片背后加一层柔和淡紫悬浮投影（位于 index 0）。</summary>
        public static void AddSoftShadow(VisualElement cardHost, float radius)
        {
            if (cardHost == null) return;
            var sh = new VisualElement { name = "xq-soft-shadow", pickingMode = PickingMode.Ignore };
            sh.style.position = Position.Absolute;
            sh.style.left = 1f;
            sh.style.right = -1f;
            sh.style.top = 3f;
            sh.style.bottom = -3f;
            sh.style.backgroundColor = new Color(0.46f, 0.34f, 0.72f, 0.09f);
            sh.style.borderTopLeftRadius = radius;
            sh.style.borderTopRightRadius = radius;
            sh.style.borderBottomLeftRadius = radius;
            sh.style.borderBottomRightRadius = radius;
            cardHost.Insert(0, sh);
        }

        /// <summary>在卡片边角挂一枚跨出边框 10~14px 的透明底定制贴纸。</summary>
        public static void AddBreakoutSticker(VisualElement cardHost, string resourceName,
            float w, float h, float rightOffset, float topOffset, float rotDeg)
        {
            if (cardHost == null) return;
            var tex = Resources.Load<Texture2D>(resourceName);
            if (tex == null) return;

            var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            img.style.position = Position.Absolute;
            img.style.width = w;
            img.style.height = h;
            img.style.right = rightOffset;
            img.style.top = topOffset;
            img.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(rotDeg, AngleUnit.Degree));
            cardHost.Add(img);
            _stickers.Add(img);
        }

        static Texture2D _acrylicTex;

        /// <summary>方案 B：预渲染亚克力材质（左上→右下柔白紫光泽渐变 + 3% 磨砂颗粒 + 顶边镜面高光 + 底边厚度暗边）。</summary>
        public static void ApplyAcrylicB(VisualElement el)
        {
            if (el == null) return;
            if (_acrylicTex == null) _acrylicTex = BakeAcrylic(256);
            el.style.backgroundColor = Color.clear;
            el.style.backgroundImage = Background.FromTexture2D(_acrylicTex);
            el.style.borderTopWidth = 1f; el.style.borderBottomWidth = 1.5f;
            el.style.borderLeftWidth = 1f; el.style.borderRightWidth = 1f;
            el.style.borderTopColor = new Color(1f, 1f, 1f, 0.98f);
            el.style.borderLeftColor = new Color(0.93f, 0.89f, 1f, 0.95f);
            el.style.borderRightColor = new Color(0.86f, 0.80f, 0.98f, 0.95f);
            el.style.borderBottomColor = new Color(0.76f, 0.68f, 0.95f, 0.95f);
            AddSpecular(el, 0.95f);
        }

        /// <summary>方案 C（精修）：柔紫雾面 + 1px 薰衣草紫细描边 + 顶边亮白镜面高光，纯白网页与深色壁纸都清晰高级。</summary>
        public static void ApplyGlassC(VisualElement el)
        {
            if (el == null) return;
            el.style.backgroundColor = new Color(0.97f, 0.94f, 1f, 0.28f);
            el.style.backgroundImage = StyleKeyword.None;
            el.style.borderTopWidth = 1f; el.style.borderBottomWidth = 1.2f;
            el.style.borderLeftWidth = 1f; el.style.borderRightWidth = 1f;
            el.style.borderTopColor = new Color(1f, 1f, 1f, 0.92f);
            el.style.borderLeftColor = new Color(0.73f, 0.64f, 0.93f, 0.52f);
            el.style.borderRightColor = new Color(0.73f, 0.64f, 0.93f, 0.52f);
            el.style.borderBottomColor = new Color(0.60f, 0.49f, 0.86f, 0.58f);
            AddSpecular(el, 0.90f);
            var parent = el.parent;
            if (parent != null)
            {
                var sh = parent.Q("xq-soft-shadow");
                if (sh != null) sh.RemoveFromHierarchy();
            }
        }

        static void AddSpecular(VisualElement el, float a)
        {
            var line = new VisualElement { name = "xq-material-specular", pickingMode = PickingMode.Ignore };
            line.style.position = Position.Absolute;
            line.style.top = 1f; line.style.height = 1f;
            line.style.left = Length.Percent(12f); line.style.right = Length.Percent(12f);
            line.style.backgroundColor = new Color(1f, 1f, 1f, a);
            el.Insert(0, line);
        }

        static Texture2D BakeAcrylic(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var px = new Color32[n * n];
            var cTop = new Color(1.00f, 0.995f, 1.00f, 0.90f);
            var cMid = new Color(0.975f, 0.955f, 1.00f, 0.84f);
            var cBot = new Color(0.935f, 0.905f, 0.995f, 0.82f);
            uint seed = 0x2545F491u;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    // 纹理 y=0 在底部；t=0 左上 → t=1 右下
                    float t = Mathf.Clamp01(((float)x / n + (1f - (float)y / n)) * 0.5f);
                    Color c = t < 0.5f ? Color.Lerp(cTop, cMid, t * 2f) : Color.Lerp(cMid, cBot, (t - 0.5f) * 2f);
                    // 左上柔光反射
                    float gx = (float)x / n - 0.22f, gy = (1f - (float)y / n) - 0.12f;
                    float glow = Mathf.Exp(-(gx * gx + gy * gy) * 9f) * 0.06f;
                    seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                    float nz = ((seed & 0xFF) / 255f - 0.5f) * 0.06f;
                    float v = glow + nz;
                    px[y * n + x] = new Color(
                        Mathf.Clamp01(c.r + v), Mathf.Clamp01(c.g + v), Mathf.Clamp01(c.b + v), c.a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        public static void Tick(float timeSec)
        {
            for (int i = _stickers.Count - 1; i >= 0; i--)
            {
                var st = _stickers[i];
                if (st == null || st.panel == null) { _stickers.RemoveAt(i); continue; }
                float dy = Mathf.Sin(timeSec * 1.1f + i * 1.7f) * 2.2f;
                st.style.translate = new Translate(0f, dy, 0f);
            }
            if (_bgHalo != null && _bgHalo.panel != null)
            {
                float s = 1f + Mathf.Sin(timeSec * 0.45f) * 0.03f;
                _bgHalo.style.scale = new Scale(new Vector3(s, s, 1f));
            }
        }
    }
}
