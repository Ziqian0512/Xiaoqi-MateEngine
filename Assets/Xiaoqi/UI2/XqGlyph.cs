using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>右键菜单用的线条小图标（Painter2D，24x24 网格）。颜色可换，不依赖字体。</summary>
    public sealed class XqGlyph : VisualElement
    {
        readonly string _kind;
        public Color Stroke = Color.white;

        public XqGlyph(string kind, Color stroke)
        {
            _kind = kind;
            Stroke = stroke;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w < 4f || h < 4f) return;
            float u = Mathf.Min(w, h) / 24f;
            Vector2 o = new Vector2((w - 24f * u) * 0.5f, (h - 24f * u) * 0.5f);
            var p = ctx.painter2D;
            p.strokeColor = Stroke;
            p.lineWidth = 1.9f * u;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            Vector2 P(float x, float y) { return o + new Vector2(x, y) * u; }
            void Poly(bool close, params float[] xy)
            {
                p.BeginPath();
                p.MoveTo(P(xy[0], xy[1]));
                for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(P(xy[i], xy[i + 1]));
                if (close) p.ClosePath();
                p.Stroke();
            }
            void Circle(float cx, float cy, float r)
            {
                p.BeginPath();
                p.Arc(P(cx, cy), r * u, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Stroke();
            }
            switch (_kind)
            {
                case "sun":
                    Circle(12, 12, 4);
                    Poly(false, 12, 2, 12, 4); Poly(false, 12, 20, 12, 22);
                    Poly(false, 2, 12, 4, 12); Poly(false, 20, 12, 22, 12);
                    Poly(false, 5, 5, 6.5f, 6.5f); Poly(false, 17.5f, 17.5f, 19, 19);
                    Poly(false, 19, 5, 17.5f, 6.5f); Poly(false, 6.5f, 17.5f, 5, 19);
                    break;
                case "chat": Poly(true, 4, 5, 20, 5, 20, 16, 9, 16, 4, 20); break;
                case "spark": Poly(true, 12, 3, 14, 9, 20, 11, 14, 13, 12, 19, 10, 13, 4, 11, 10, 9); break;
                case "moon":
                    p.BeginPath();
                    p.Arc(P(12, 12), 8f * u, Angle.Degrees(50f), Angle.Degrees(320f));
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(P(15.5f, 9.5f), 6f * u, Angle.Degrees(110f), Angle.Degrees(330f));
                    p.Stroke();
                    break;
                case "live":
                    Poly(true, 4, 7, 16, 7, 16, 17, 4, 17);
                    Poly(true, 16, 10.5f, 21, 8, 21, 16, 16, 13.5f);
                    break;
                case "gear":
                    Circle(12, 12, 3.5f);
                    Poly(false, 12, 2, 12, 5); Poly(false, 12, 19, 12, 22);
                    Poly(false, 2, 12, 5, 12); Poly(false, 19, 12, 22, 12);
                    Poly(false, 5, 5, 7, 7); Poly(false, 17, 17, 19, 19);
                    Poly(false, 19, 5, 17, 7); Poly(false, 7, 17, 5, 19);
                    break;
                case "home": Poly(false, 3, 11, 12, 3, 21, 11); Poly(false, 5, 10, 5, 20, 19, 20, 19, 10); break;
                case "bed": Poly(false, 3, 18, 3, 7); Poly(false, 3, 14, 21, 14, 21, 18); Poly(false, 10, 14, 10, 9, 18, 9, 21, 12, 21, 14); break;
                case "win": Poly(true, 3, 4, 21, 4, 21, 18, 3, 18); Poly(false, 3, 9, 21, 9); Poly(false, 9, 21, 15, 21); break;
                case "music":
                    Poly(false, 9, 18, 9, 6, 19, 4, 19, 16);
                    Circle(7, 18, 2); Circle(17, 16, 2);
                    break;
                case "heart":
                    p.BeginPath();
                    p.MoveTo(P(12, 21));
                    p.BezierCurveTo(P(5, 16.5f), P(3, 12f), P(3, 10f));
                    p.BezierCurveTo(P(3, 5f), P(10f, 4f), P(12f, 9f));
                    p.BezierCurveTo(P(14f, 4f), P(21f, 5f), P(21f, 10f));
                    p.BezierCurveTo(P(21f, 12f), P(19f, 16.5f), P(12f, 21f));
                    p.Stroke();
                    break;
                case "user":
                    Circle(12, 8, 4);
                    p.BeginPath();
                    p.Arc(P(12, 22), 8f * u, Angle.Degrees(200f), Angle.Degrees(340f));
                    p.Stroke();
                    break;
                case "expand":
                    Poly(false, 4, 9, 4, 4, 9, 4); Poly(false, 20, 9, 20, 4, 15, 4);
                    Poly(false, 4, 15, 4, 20, 9, 20); Poly(false, 20, 15, 20, 20, 15, 20);
                    break;
                case "cal": Poly(true, 3, 5, 21, 5, 21, 21, 3, 21); Poly(false, 3, 10, 21, 10); Poly(false, 8, 3, 8, 7); Poly(false, 16, 3, 16, 7); break;
                case "chev": Poly(false, 9, 5, 16, 12, 9, 19); break;
            }
        }
    }
}
