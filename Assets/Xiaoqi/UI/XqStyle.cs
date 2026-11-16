using UnityEngine;

namespace Xiaoqi.UI
{
    /// <summary>G55 §3：所有界面的颜色、尺寸、时长只从这里取。</summary>
    public static class XqStyle
    {
        /// <summary>主题：命令行 -xq-theme blue|violet|pink（缺省粉）。与 UI2 的 XqUi2Sample 同一个参数。
        /// 颜色在启动时一次定下（菜单、设置页、关于卡的贴图都在 Awake 里按这些色画好）。
        /// 字段名里的 Pink 是历史名字，现在含义是「主题色」：Fill 浅底 / Line 描边 / Strong 强调 / Guide 引导点。</summary>
        public static readonly string Theme = ReadTheme();

        public static readonly Color Paper = Hex("FFFFFF");
        public static readonly Color PinkFill = Pick("FBEAF0", "EAF3FF", "F1EAFF");
        public static readonly Color PinkLine = Pick("ED93B1", "8CC3FF", "C3A6FF");
        public static readonly Color PinkStrong = Pick("D4537E", "5AA8FF", "B184FF");
        public static readonly Color PinkGuide = Pick("F4C0D1", "C4DEFF", "DCCBFF");
        public static readonly Color Ink = Pick("72243E", "1F3F6B", "3F2A72");
        public static readonly Color InkSoft = Pick("993556", "2A6FC4", "7B4FD0");
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

        private static string ReadTheme()
        {
            string t = "pink";
            try
            {
                var a = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < a.Length; i++)
                {
                    string v = null;
                    if ((a[i] == "-xq-theme" || a[i] == "--xq-theme") && i + 1 < a.Length) v = a[i + 1];
                    else if (a[i].StartsWith("-xq-theme=")) v = a[i].Substring("-xq-theme=".Length);
                    else if (a[i].StartsWith("--xq-theme=")) v = a[i].Substring("--xq-theme=".Length);
                    if (v == null) continue;
                    v = v.Trim().ToLowerInvariant();
                    if (v == "blue" || v == "xq-theme-blue") t = "blue";
                    else if (v == "violet" || v == "purple" || v == "xq-theme-violet") t = "violet";
                    break;
                }
            }
            catch (System.Exception) { }
            return t;
        }

        private static Color Pick(string pink, string blue, string violet)
        {
            return Hex(Theme == "blue" ? blue : (Theme == "violet" ? violet : pink));
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
