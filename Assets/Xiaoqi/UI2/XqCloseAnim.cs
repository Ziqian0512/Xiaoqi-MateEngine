using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 近景进/出动画的常量与缓动（G59-P2 §4）。所有时间以秒计，偏移以「样式稿 px」计，
    /// 落屏时乘 u = 面板高 / 540。
    ///
    /// 时间表的自洽性说明：§4 写「她开始放大后 0.45 秒起、每个错开 90 毫秒、每个 0.7 秒」，
    /// 同时又写「进入总时长 ≤ 1.3 秒」。按 0.45 + 6×0.09 + 0.7 算是 1.69 秒，超了 0.39 秒。
    /// 原因是那 0.45 秒是相对「她开始放大」的，而 XqClosePanel.Show() 是在 XqMode 的
    /// OnCloseReady 里被调用的——那时镜头已经 settle 完（XqCloseCamera.SettleSec = 1.0），
    /// 她早就开始放大了。所以面板自己的基准延迟取 0，总时长 6×0.09 + 0.7 = 1.24 秒，
    /// 落在 1.3 秒以内，与「每个 0.7 秒 / 错开 90 毫秒」的逐元素数值一致。
    /// </summary>
    public static class XqCloseAnim
    {
        public const float EnterDur = 0.7f;      // 每个元素飞入时长
        public const float EnterStagger = 0.09f; // 依次错开
        public const float EnterBase = 0f;       // 面板自身基准延迟（镜头已 settle，见上）
        public const float EnterFade = 0.25f;    // 淡入用多长一段进度
        public const float EnterScaleFrom = 0.5f;

        public const float ExitDur = 0.35f;      // 退出总时长（同时收回，不错开）
        public const float ExitScaleTo = 0.62f;
        public const float ExitTravel = 0.45f;   // 退出时走起始偏移的多少比例

        public const float BobPeriod = 7f;       // 到位后的上下浮动
        public const float BobAmp = 3f;          // 样式稿 px
        public const float BobPhaseStep = 0.7f;  // 每个元素错开相位，免得齐步走

        /// <summary>
        /// 各元素相对终点的起始偏移（样式稿 px，x 正 = 向右），下标即出场顺序：
        /// 0 竖栏、1 下一件事卡、2 精力卡、3 她说卡、4 今日小事卡、5 圆环、6 名牌。
        /// ⑧⑨ 两个方钮在 §4 表里没有条目，只做整体淡入，不臆造位移。
        /// </summary>
        public static readonly Vector2[] Offsets =
        {
            new Vector2(300f, 0f),
            new Vector2(300f, 60f),
            new Vector2(240f, -30f),
            new Vector2(-260f, 90f),
            new Vector2(-220f, 20f),
            new Vector2(-300f, -60f),
            new Vector2(0f, -120f),
        };

        public static int Count { get { return Offsets.Length; } }

        /// <summary>某个元素开始飞入的时刻。</summary>
        public static float Start(int order)
        {
            return EnterBase + order * EnterStagger;
        }

        /// <summary>进入：cubic-bezier(.2, .9, .3, 1.15)，带一点过冲。</summary>
        public static float Enter(float p)
        {
            return Bezier(p, 0.2f, 0.9f, 0.3f, 1.15f);
        }

        /// <summary>
        /// 退出：把进入的曲线镜像回来，感觉上是同一套缓动反向走。
        /// 进入曲线末段有过冲，镜像之后会在起点出现负值——那会让元素先往目标位
        /// 外侧弹一下再收回。退出只有 0.35 秒，这点反弹看不出来还容易显得抖，直接裁掉。
        /// </summary>
        public static float Exit(float p)
        {
            return Mathf.Clamp01(1f - Enter(1f - Mathf.Clamp01(p)));
        }

        /// <summary>
        /// 三次贝塞尔在 x 处的 y。CSS 的 cubic-bezier 控制点固定首尾 (0,0)/(1,1)，
        /// 这里按 x 求解参数 t（先牛顿、退化时二分），再算 y。
        /// </summary>
        public static float Bezier(float x, float x1, float y1, float x2, float y2)
        {
            x = Mathf.Clamp01(x);
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;

            float t = x;
            for (int i = 0; i < 8; i++)
            {
                float cx = Cx(t, x1, x2) - x;
                if (Mathf.Abs(cx) < 1e-4f) break;
                float d = CxPrime(t, x1, x2);
                if (Mathf.Abs(d) < 1e-5f) break;
                t -= cx / d;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
            }
            // 牛顿没收敛时二分兜底（曲线单调，一定收敛）
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 20; i++)
            {
                float cx = Cx(t, x1, x2);
                if (Mathf.Abs(cx - x) < 1e-4f) break;
                if (cx < x) lo = t; else hi = t;
                t = (lo + hi) * 0.5f;
            }
            return Cy(t, y1, y2);
        }

        static float Cx(float t, float x1, float x2)
        {
            float mt = 1f - t;
            return 3f * mt * mt * t * x1 + 3f * mt * t * t * x2 + t * t * t;
        }

        static float CxPrime(float t, float x1, float x2)
        {
            float mt = 1f - t;
            return 3f * mt * mt * x1 + 6f * mt * t * (x2 - x1) + 3f * t * t * (1f - x2);
        }

        static float Cy(float t, float y1, float y2)
        {
            float mt = 1f - t;
            return 3f * mt * mt * t * y1 + 3f * mt * t * t * y2 + t * t * t;
        }
    }
}
