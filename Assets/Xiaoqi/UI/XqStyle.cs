using UnityEngine;

namespace Xiaoqi.UI
{
    /// <summary>G55 §3：所有界面的颜色、尺寸、时长只从这里取。</summary>
    public static class XqStyle
    {
        public static readonly Color Paper = Hex("FFFFFF");
        public static readonly Color PinkFill = Hex("FBEAF0");
        public static readonly Color PinkLine = Hex("ED93B1");
        public static readonly Color PinkStrong = Hex("D4537E");
        public static readonly Color PinkGuide = Hex("F4C0D1");
        public static readonly Color Ink = Hex("72243E");
        public static readonly Color InkSoft = Hex("993556");
        public static readonly Color Body = Hex("2C2C2A");
        public static readonly Color Muted = Hex("888780");
        public static readonly Color GrayFill = Hex("F6F5F3");
        public static readonly Color GrayLine = Hex("E3E1DA");
        public static readonly Color GrayText = Hex("B4B2A9");
        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.10f);

        public const float InSec = 0.18f;
        public const float OutSec = 0.12f;
        public const float HoverSec = 0.10f;
        public const float StaggerSec = 0.03f;

        public const int FontCapsule = 16;
        public const int FontStatus = 13;
        public const int FontCardTitle = 16;
        public const int FontBody = 14;
        public const int FontSmall = 12;

        public const float CapsuleHeight = 36f;
        public const float CapsulePadX = 18f;
        public const float CapsuleMinWidth = 88f;
        public const float CardRadius = 14f;
        public const float CardPad = 16f;

        private static float _scale = -1f;

        /// <summary>DPI 缩放，Screen.dpi 为 0 时按 1。</summary>
        public static float Scale
        {
            get
            {
                if (_scale < 0f)
                {
                    float dpi = Screen.dpi;
                    _scale = dpi <= 1f ? 1f : Mathf.Clamp(dpi / 96f, 1f, 2f);
                }
                return _scale;
            }
        }

        public static float Fr(float px) => px * Scale;
        public static int Px(float px) => Mathf.RoundToInt(px * Scale);

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
