using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V13 近景态布局常量与脸区禁区校验：
    /// - 左上：带秒大时钟 + 天气穿衣叮嘱 + 主线
    /// - 左下：大专辑封面音乐卡 + 形态切换坞
    /// - 四周散落：4 颗快捷表情按钮 + 无框颜文字
    /// - 右上：系统音频检测胶囊；右侧错落：肩侧气泡 / 圆环元气心跳卡 / 专注计时器 / 互动双钮
    /// - 全屏底部：横穿全屏的毛玻璃频谱柱 + 加长 Search 风格输入条 + 前景 AMAMIYA Inori 艺术字贴图
    /// </summary>
    public static class XqCloseLayout
    {
        public const float Stage = 540f;

        // 脸区禁区（屏幕比例，0–1）
        public const float FaceL = 0.38f;
        public const float FaceT = 0.16f;
        public const float FaceW = 0.24f;
        public const float FaceH = 0.48f;

        public static Rect FaceRect { get { return new Rect(FaceL, FaceT, FaceW, FaceH); } }

        // 兼容常量
        public const float RailL = 0.024f, RailT = 0.080f;
        public const float RailW = 64f, RailH = 450f, RailR = 32f;
        public const float RailPad = 18f;
        public const float DotSize = 40f, DotGap = 12f, DotFont = 16f;

        // ⑦ 正中更大更长前景艺术字贴图（Place() 中精确水平居中）
        public const float PlateL = 0.270f, PlateT = 0.560f;
        public const float PlateW = 360f, PlateH = 92f;
        public const float PlateFont = 15f;

        // 正底窄长通透 Search 风格对话条（加宽至 540，保持在任务栏上方）
        public const float ChatL = 0.255f, ChatT = 0.745f;
        public const float ChatW = 540f, ChatH = 36f;

        // 右下角形态切换坞（桌宠同款紧凑圆角方块按钮）
        public const float BtnTop = 0.805f;
        public const float BtnPetL = 0.840f;
        public const float BtnBenchL = 0.880f;
        public const float BtnSize = 38f, BtnR = 12f, BtnFont = 13f;

        public const float TipW = 96f, TipH = 30f, TipGap = 8f;

        // ② 左上：无外框三段式（大时钟日期天气 + 穿衣提醒条 + 正在做进度条）
        public const float NextL = 0.038f, NextT = 0.062f;
        public const float NextW = 248f, NextH = 144f;

        // ③ 左下：横向精致紫夜星空音乐卡（参考 starry nights 版式）
        public const float MusicL = 0.038f, MusicT = 0.455f;
        public const float MusicW = 258f, MusicH = 152f;

        // ⑤ 右上：@Amamiya_Inori 肩侧气泡（留足间距，不拥挤）
        public const float SayL = 0.695f, SayT = 0.095f;
        public const float SayW = 248f, SayH = 92f;

        // ⑥ 右中：圆环元气电量 + 实时心跳 + 黏人象限
        public const float TinyL = 0.715f, TinyT = 0.285f;
        public const float TinyW = 252f, TinyH = 96f;

        // ⑦ 右下拆分为两个错落独立模块：摸摸她的头 + 看我屏幕
        public const float EnergyL = 0.770f, EnergyT = 0.545f;
        public const float EnergyW = 126f, EnergyH = 40f;

        public const float RingL = 0.715f, RingT = 0.465f;
        public const float RingSize = 118f;
        public const float RingW = 126f, RingH = 40f;

        public const float CardPadX = 16f, CardPadY = 12f;
        public const float CardR = 22f;
        public const float CardKickerFont = 13f;
        public const float CardBigFont = 24f;
        public const float CardTextFont = 13f;
        public const float CardMarkFont = 12f;
        public const float EnergyArc = 44f;
        public const float FocusZoom = 1.04f;
        public const float FocusSec = 0.25f;

        public static Rect Norm(float left, float top, float wStage, float hStage)
        {
            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            float u = sh / Stage;
            return new Rect(left, top, wStage * u / sw, hStage * u / sh);
        }

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

        public static bool InFaceZone(Rect n)
        {
            return n.Overlaps(FaceRect);
        }
    }
}
