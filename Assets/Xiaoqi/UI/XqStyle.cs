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

        // 主题 A：胶囊的描边 / 柔和投影 / 按下缩放，以及主次文字层级。
        public static readonly Color ShadowSoft = new Color(0.29f, 0.09f, 0.16f, 0.15f);
        public static readonly Color TextPrimary = Ink;
        public static readonly Color TextSecondary = InkSoft;
        public static readonly Color TextTertiary = Muted;
        public static readonly Color TextDisabled = GrayText;

        public const float StrokeWidth = 1f;
        public const float PressScale = 0.96f;
        public const float ShadowBlur = 6f;
        public const float ShadowOffsetY = 1.5f;
        public const float ShadowSpread = 3f;
        public const float CapsuleRadius = 18f;

        // 主题 B：二级面板——同色系浅粉底 + 1px 描边，整列胶囊收进一块圆角面板。
        public static readonly Color SubPanelFill = PinkFill;
        public static readonly Color SubPanelLine = PinkGuide;
        public static readonly Color SubRowFill = new Color(1f, 1f, 1f, 0f);
        public static readonly Color SubRowHoverFill = Paper;
        public static readonly Color SubRowHoverLine = PinkStrong;
        public static readonly Color SubRowText = TextPrimary;
        public static readonly Color SubRowTextGray = TextDisabled;

        public const float SubPanelRadius = 20f;
        public const float SubPanelPadX = 10f;
        public const float SubPanelPadY = 8f;
        public const float SubRowMinWidth = 96f;
        public const float SubStep = 44f;
        public const float SubGap = 16f;
        public const float SubColStep = 168f;

        // 主题 C：一级项的 14px 细线图标（纯代码绘制，颜色只取这三档）。
        public readonly struct IconSpec
        {
            public readonly float Size;
            public readonly float Stroke;
            public readonly float Gap;
            public readonly float InsetX;

            public IconSpec(float size, float stroke, float gap, float insetX)
            {
                Size = size; Stroke = stroke; Gap = gap; InsetX = insetX;
            }
        }

        public static readonly IconSpec IconGeo = new IconSpec(14f, 1.25f, 8f, 14f);
        public static readonly Color IconTint = TextSecondary;
        public static readonly Color IconTintMuted = TextDisabled;
        public static readonly Color IconTintActive = PinkStrong;

        // 主题 D：今天卡 / 设置卡 / 关于卡共用一套卡面、标题区、分隔线、行高与留白。
        public static readonly Color CardFill = Paper;
        public static readonly Color CardLine = PinkGuide;
        public static readonly Color CardRule = Hex("E9E6DE");
        public static readonly Color CardSectionText = TextTertiary;
        public static readonly Color CardSectionRule = Hex("F6D4DE");
        public static readonly Color CardActionFill = PinkFill;
        public static readonly Color CardActionLine = PinkStrong;
        public static readonly Color CardActionText = Ink;
        public static readonly Color CardActionSoftFill = GrayFill;
        public static readonly Color CardActionSoftLine = PinkLine;
        public static readonly Color CardActionSoftText = Ink;

        public const float CardHeaderH = 46f;
        public const float CardHeaderPadX = 18f;
        public const float CardTitleH = 26f;
        public const float CardBodyPadX = 16f;
        public const float CardBodyPadTop = 10f;
        public const float CardBodyPadBottom = 18f;
        public const float CardRuleH = 1f;
        public const float CardRowH = 32f;
        public const float CardRowHTall = 36f;
        public const float CardRowGap = 6f;
        public const float CardSectionH = 24f;
        public const float CardRadiusLg = 18f;
        public const float CardButtonH = 32f;
        public const float CardButtonGap = 10f;
        public const float CardMinH = 200f;
        public const float CardMaxH = 820f;
        public const int FontCardSection = 12;

        // 主题 E：浮在桌面上的两块「表面」——启动卡与气泡。和卡面同一套圆角 / 1px 描边 / 柔和投影，
        // 底色换成暖白（比 Paper 纯白低一档亮度，落在壁纸上不刺眼）。
        public static readonly Color SurfaceFill = Hex("FCF7F5");
        public static readonly Color SurfaceFillSoft = Hex("FDF9F8");
        public static readonly Color SurfaceLine = PinkGuide;
        public const float SurfaceRadius = 18f;
        public const float SurfaceStroke = 1f;

        // 启动卡：标题 17/Ink + 状态行 12/InkSoft + 6px 状态点。
        public static readonly Color StartTitleText = Ink;
        public static readonly Color StartStatusText = InkSoft;
        public static readonly Color StartDotWaking = PinkLine;
        public static readonly Color StartDotOk = PinkStrong;
        public static readonly Color StartDotOffline = TextDisabled;
        public const float StartCardW = 248f;
        public const float StartCardH = 78f;
        public const float StartCardTopY = 44f; // 让出设置卡顶部（约 140px），离线时启动卡不压在卡上
        public const float StartCardPadX = 20f;
        public const float StartCardPadTop = 14f;
        public const float StartCardPadBottom = 15f;
        public const float StartTitleH = 22f;
        public const float StartStatusH = 16f;
        public const float StartDotSize = 6f;
        public const float StartDotGap = 8f;
        public const int FontStartTitle = 17;
        public const int FontStartStatus = 12;

        // 气泡：同一套表面，底色再亮一档；正文 14/Ink，内边距 14×10。
        public static readonly Color BubbleFill = SurfaceFillSoft;
        public static readonly Color BubbleText = TextPrimary;
        public const float BubbleW = 320f;
        public const float BubbleH = 70f;
        public const float BubbleRadius = 16f;
        public const float BubblePadX = 14f;
        public const float BubblePadY = 10f;

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
