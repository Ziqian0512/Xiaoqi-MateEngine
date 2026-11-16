using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// XqClosePanel 的自绘部分：⑥ 占位圆环、⑤ 占位折线、③ 精力圆弧。
    /// 与 XqClosePanel.Cards.cs 是同一个 partial 类，拆开只为单文件不超 300 行。
    /// 三个都是 generateVisualContent + Painter2D，与 XqPetHud 的 XqArc / XqIcon 同一套写法。
    /// </summary>
    public partial class XqClosePanel
    {
        /// <summary>⑥ 占位圆环：白色底圈 + 一段灰色进度，写死 40%，不接倒计时。</summary>
        void DrawRing(MeshGenerationContext ctx)
        {
            if (_ringFocus == null) return;
            float w = _ringFocus.contentRect.width;
            float h = _ringFocus.contentRect.height;
            if (w < 8f || h < 8f) return;
            var p = ctx.painter2D;
            var c = new Vector2(w * 0.5f, h * 0.5f);
            float stroke = Mathf.Max(3f, w * 0.085f);
            float rad = Mathf.Min(w, h) * 0.5f - stroke;
            p.lineWidth = stroke;

            p.strokeColor = new Color(1f, 1f, 1f, 0.6f);
            p.BeginPath();
            p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(359f));
            p.Stroke();

            p.strokeColor = new Color(0.62f, 0.58f, 0.70f, 0.6f);
            p.BeginPath();
            p.Arc(c, rad, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * 0.4f));
            p.Stroke();
        }

        /// <summary>自绘元素的尺寸与字号跟 u 走。</summary>
        void PlaceGlyphs(float u)
        {
            if (_energyArc != null)
            {
                float d = XqCloseLayout.EnergyArc * u;
                _energyArc.style.width = d;
                _energyArc.style.height = d;
                _energyArc.MarkDirtyRepaint();
            }
            if (_ringWord != null) _ringWord.style.fontSize = XqCloseLayout.CardTextFont * u;
            if (_ringFocus != null) _ringFocus.MarkDirtyRepaint();
        }

        /// <summary>⑤ 的小折线（占位）。值写死在 TinyDemo。</summary>
        sealed class XqMiniLine : VisualElement
        {
            readonly float[] _v;
            public XqMiniLine(float[] values)
            {
                _v = values;
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width, h = contentRect.height;
                if (_v == null || _v.Length < 2 || w < 8f || h < 4f) return;
                var p = ctx.painter2D;
                p.lineWidth = 2f;
                p.strokeColor = new Color(1f, 0.56f, 0.72f, 0.95f);
                p.BeginPath();
                for (int i = 0; i < _v.Length; i++)
                {
                    float x = w * i / (_v.Length - 1);
                    float y = h - Mathf.Clamp01(_v[i]) * h;
                    if (i == 0) p.MoveTo(new Vector2(x, y));
                    else p.LineTo(new Vector2(x, y));
                }
                p.Stroke();
            }
        }

        /// <summary>③ 精力圆弧。Amount 0–1；小于 0 表示没数据，只画灰底圈。</summary>
        sealed class XqArc : VisualElement
        {
            public float Amount = -1f;
            public XqArc()
            {
                generateVisualContent += Draw;
                pickingMode = PickingMode.Ignore;
            }

            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 6f) return;
                var p = ctx.painter2D;
                var c = new Vector2(w * 0.5f, h * 0.5f);
                float stroke = Mathf.Max(2.5f, w * 0.13f);
                float rad = Mathf.Min(w, h) * 0.5f - stroke;

                p.lineWidth = stroke;
                p.strokeColor = new Color(1f, 1f, 1f, 0.65f);
                p.BeginPath();
                p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(359f));
                p.Stroke();

                if (Amount < 0f)
                {
                    // 没数据：只留一圈很淡的灰，不画进度
                    p.strokeColor = new Color(0.62f, 0.58f, 0.70f, 0.35f);
                    p.BeginPath();
                    p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(359f));
                    p.Stroke();
                    return;
                }
                if (Amount <= 0.01f) return;
                p.strokeColor = new Color(1f, 0.56f, 0.72f, 1f);
                p.BeginPath();
                p.Arc(c, rad, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * Mathf.Clamp01(Amount)));
                p.Stroke();
            }
        }
    }
}
