using System.Collections;
using UnityEngine;

namespace Xiaoqi.Mode
{
    /// <summary>
    /// 近景镜头（P2-2）。只是 ME 大屏模式 AvatarBigScreenHandler 的包装：
    /// ME 自己负责「窗口铺满她所在的显示器 → 镜头对准头部 → 淡入淡出 → 退出时窗口还原」，
    /// 这里不改它的任何逻辑，只负责：幂等（已在近景再 Enter 不会退出）、调两个参数、等它走完。
    /// 状态判断用 Animator 的 isBigScreen（ME 在进入时置 true，退出走完才置 false）。
    /// </summary>
    public static class XqCloseCamera
    {
        // ME：进入 = 窗口滑 0.4 + 镜头淡入 0.5；退出 = 淡出 0.5 + 滑 0.4。留一点余量。
        public const float SettleSec = 1.0f;

        private static AvatarBigScreenHandler _h;

        private static AvatarBigScreenHandler Handler()
        {
            if (_h == null) _h = Object.FindFirstObjectByType<AvatarBigScreenHandler>(FindObjectsInactive.Include);
            return _h;
        }

        public static bool Available { get { return Handler() != null; } }

        public static bool IsActive
        {
            get
            {
                var h = Handler();
                return h != null && h.avatarAnimator != null && h.avatarAnimator.GetBool("isBigScreen");
            }
        }

        /// <summary>开始进入。返回 false = 找不到 ME 的大屏组件（不崩，调用方写日志）。</summary>
        public static bool BeginEnter()
        {
            var h = Handler();
            if (h == null) { Debug.Log("XQ_HOST_MISS AvatarBigScreenHandler"); return false; }
            var c = XqModeConfig.I;
            if (c.close_zoom > 0f) h.TargetZoom = c.close_zoom;
            h.YOffset = c.close_yoffset;
            if (!IsActive) h.ToggleBigScreenFromUI();
            return true;
        }

        /// <summary>开始退出。已经不在近景就什么都不做。</summary>
        public static bool BeginExit()
        {
            var h = Handler();
            if (h == null) return false;
            if (IsActive) h.ToggleBigScreenFromUI();
            return true;
        }

        public static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(SettleSec);
        }
    }
}
