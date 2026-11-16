using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 屏幕像素（Unity 屏幕坐标，原点左下）→ UI Toolkit 面板坐标（原点左上）。
    /// B1 实测（ISSUES #4/#5/#6）：这台机器上 RuntimePanelUtils.ScreenToPanel 不翻 Y（日志 p0.y = mouse.y/scale），
    /// 直接喂左下坐标，位置全部上下镜像：人物小时菜单飞到屏幕上方、方钮跑到上面、设置页沉底。
    /// 这里先探一下它翻不翻（看 (0,0) 落在面板顶还是底），不翻就自己翻；翻的话原样用。两种环境都对。
    /// </summary>
    public static class XqPanelXf
    {
        static int _flips = -1; // -1 未探测  0 不翻  1 翻
        static int _probeFrame = -100;

        static bool Flips(IPanel panel)
        {
            if (_flips >= 0 && Time.frameCount - _probeFrame < 600) return _flips == 1;
            var o = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            int f = o.y > 1f ? 1 : 0;
            if (f != _flips)
                Debug.Log("XQ_PANEL ScreenToPanel flipsY=" + (f == 1) + " origin=" + o.x.ToString("0.0") + "," + o.y.ToString("0.0") + " H=" + Screen.height);
            _flips = f;
            _probeFrame = Time.frameCount;
            return f == 1;
        }

        /// <summary>屏幕左下原点的点 → 面板左上原点的点。</summary>
        public static Vector2 FromScreen(IPanel panel, Vector2 bl)
        {
            if (Flips(panel)) return RuntimePanelUtils.ScreenToPanel(panel, bl);
            return RuntimePanelUtils.ScreenToPanel(panel, new Vector2(bl.x, Screen.height - bl.y));
        }

        /// <summary>1 个屏幕像素 = 多少面板单位（即 1/scale）。</summary>
        public static float UnitsPerPx(IPanel panel)
        {
            var a = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            var b = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(100f, 0f));
            float u = Mathf.Abs(b.x - a.x) / 100f;
            return u > 0.0001f ? u : 1f;
        }
    }
}
