using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P2-3 近景界面的布局常量与脸区禁区校验（G59-P2 §3.1 / §3.2 / §5）。
    /// 位置写成屏幕百分比（不随分辨率变），尺寸写成样式稿 px（960×540 舞台），
    /// 落到物理像素时乘 u = 屏高 / 540（G59-P1 §4.3）。
    /// 所有数字只放这一处；Build 时每个元素过一遍 InFaceZone()。
    /// 脸区禁区：屏幕 x 38%–62%、y 18%–65%，任何卡片、竖栏、圆环都不得进入。
    /// </summary>
    public static class XqCloseLayout
    {
        /// <summary>样式稿短边。u = 屏高 / Stage。</summary>
        public const float Stage = 540f;

        // 脸区禁区（屏幕比例，0–1）
        public const float FaceL = 0.38f;
        public const float FaceT = 0.18f;
        public const float FaceW = 0.24f;
        public const float FaceH = 0.47f;

        public static Rect FaceRect { get { return new Rect(FaceL, FaceT, FaceW, FaceH); } }

        // ① 左侧竖栏：药丸 62 × 488，圆角 31
        public const float RailL = 0.023f, RailT = 0.048f;
        public const float RailW = 62f, RailH = 488f, RailR = 31f;
        public const float RailPad = 14f;

        // 竖栏里的圆
        public const float DotSize = 34f;
        public const float DotGap = 8f;
        public const float DotFont = 16f;

        // ⑦ 名牌：药丸，高 40。宽度表里没给，按文案长度取 220。
        public const float PlateL = 0.375f, PlateT = 0.826f;
        public const float PlateW = 220f, PlateH = 40f;
        public const float PlateFont = 17f;

        // ⑧⑨ 两个方钮：40 × 40，圆角 14。左 = 桌宠（可点），右 = 工作台（灰）。
        public const float BtnTop = 0.896f;
        public const float BtnPetL = 0.869f;
        public const float BtnBenchL = 0.919f;
        public const float BtnSize = 40f, BtnR = 14f, BtnFont = 15f;

        // 悬停提示（灰钮左侧）。尺寸只用于脸区自检，不吃布局。
        public const float TipW = 96f, TipH = 30f;
        public const float TipGap = 8f;

        // ② 下一件事卡 226 × 128
        public const float NextL = 0.113f, NextT = 0.178f;
        public const float NextW = 226f, NextH = 128f;
        // ③ 精力卡 206 × 92
        public const float EnergyL = 0.113f, EnergyT = 0.456f;
        public const float EnergyW = 206f, EnergyH = 92f;
        // ④ 她说卡 262 × 128（强调版玻璃）
        public const float SayL = 0.696f, SayT = 0.063f;
        public const float SayW = 262f, SayH = 128f;
        // ⑤ 今日小事卡 230 × 128
        public const float TinyL = 0.729f, TinyT = 0.344f;
        public const float TinyW = 230f, TinyH = 128f;
        // ⑥ 一起专注圆环 118 × 118（圆形）
        public const float RingL = 0.833f, RingT = 0.652f;
        public const float RingSize = 118f;

        // 卡片内边距与字号（样式稿 px，落屏时乘 u）
        public const float CardPadX = 16f, CardPadY = 14f;
        public const float CardR = 22f;
        public const float CardKickerFont = 15f;   // 小标题（「下一件事」「她说」）
        public const float CardBigFont = 26f;      // 大字（时间 / 精力百分比）
        public const float CardTextFont = 15f;     // 正文
        public const float CardMarkFont = 14f;     // 「占位」角标
        public const float EnergyArc = 46f;        // ③ 精力圆环直径（样式稿 px）
        public const float FocusZoom = 1.04f;      // P2-5 竖栏点击卡片聚焦的放大倍数
        public const float FocusSec = 0.25f;       // 聚焦动画时长

        /// <summary>样式稿 px 矩形 → 屏幕比例矩形（宽按屏宽、高按屏高换算）。</summary>
        public static Rect Norm(float left, float top, float wStage, float hStage)
        {
            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            float u = sh / Stage;
            return new Rect(left, top, wStage * u / sw, hStage * u / sh);
        }

        /// <summary>贴在某个方钮左边的提示矩形（屏幕比例），给脸区自检用。</summary>
        public static Rect TipNear(float btnLeft)
        {
            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            float u = sh / Stage;
            float w = TipW * u / sw;
            float h = TipH * u / sh;
            float left = btnLeft - w - TipGap * u / sw;
            float top = BtnTop + (BtnSize - TipH) * 0.5f * u / sh;
            return new Rect(left, top, w, h);
        }

        /// <summary>这个屏幕比例矩形有没有压到脸区禁区。入参是 0–1 的比例矩形。</summary>
        public static bool InFaceZone(Rect n)
        {
            return n.Overlaps(FaceRect);
        }
    }
}
