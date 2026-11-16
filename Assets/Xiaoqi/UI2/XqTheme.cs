using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>UI Toolkit 里用 Painter2D 画的东西（环、图标）取色用，和 xq.uss 的三套主题同值。</summary>
    public static class XqTheme
    {
        static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString("#" + h, out c); return c; }

        public static readonly Color Accent = Pick("FF8FB8", "5AA8FF", "B184FF");
        public static readonly Color AccentInk = Pick("D6477F", "2A6FC4", "7B4FD0");
        public static readonly Color Ink = Hex("3B2F55");

        static Color Pick(string pink, string blue, string violet)
        {
            string t = Xiaoqi.UI.XqStyle.Theme;
            return Hex(t == "blue" ? blue : (t == "violet" ? violet : pink));
        }
    }
}
