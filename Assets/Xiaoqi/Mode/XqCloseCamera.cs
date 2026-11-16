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
        private static bool _active;
        private static bool _hasSavedPos;
        private static Vector3 _savedAvatarPos;
        private static bool _hasSavedAnim;
        private static int _savedStateHash;
        private static float _savedNormTime;
        private static float _savedClipLen;
        private static float _savedIdleIdx;

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
                return _active && Handler() != null;
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
            if (!_active)
            {
                _active = true;
                // G59-P2 §3.1/§3.3：保持站姿待机平稳帧并把头部水平对齐镜头中线（50%），退出时原样恢复位置与动画相位
                if (h.avatarAnimator != null)
                {
                    var a = h.avatarAnimator;
                    if (!_hasSavedAnim)
                    {
                        var st = a.GetCurrentAnimatorStateInfo(0);
                        _savedStateHash = st.fullPathHash;
                        _savedNormTime = st.normalizedTime;
                        _savedClipLen = st.length;
                        _savedIdleIdx = a.GetFloat("IdleIndex");
                        _hasSavedAnim = true;
                    }
                    _savedAvatarPos = a.transform.position;
                    _hasSavedPos = true;
                    a.SetBool("isBigScreen", false);
                    a.SetFloat("IdleIndex", 0f);
                    a.Play("Idle", 0, 0.55f);
                    a.speed = 0.15f;
                    a.Update(0f);
                    var cam = h.MainCamera != null ? h.MainCamera : Camera.main;
                    var head = a.GetBoneTransform(h.attachBone);
                    if (cam != null && head != null)
                    {
                        float dx = cam.transform.position.x - head.position.x;
                        a.transform.position += new Vector3(dx, 0f, 0f);
                    }
                }
                h.ToggleBigScreenFromUI();
                if (h.avatarAnimator != null) h.avatarAnimator.SetBool("isBigScreen", false);
            }
            return true;
        }

        /// <summary>开始退出。已经不在近景就什么都不做。</summary>
        public static bool BeginExit()
        {
            var h = Handler();
            if (h == null) { _active = false; return false; }
            if (_active)
            {
                _active = false;
                if (h.avatarAnimator != null) h.avatarAnimator.speed = 1f;
                h.ToggleBigScreenFromUI();
            }
            return true;
        }

        public static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(SettleSec);
            var h = Handler();
            if (!_active && h != null && h.avatarAnimator != null)
            {
                var a = h.avatarAnimator;
                a.speed = 1f;
                if (_hasSavedPos)
                {
                    a.transform.position = _savedAvatarPos;
                    _hasSavedPos = false;
                }
                if (_hasSavedAnim && _savedStateHash != 0)
                {
                    float rewind = _savedClipLen > 0.1f ? (1.0f / _savedClipLen) : 0f;
                    a.SetFloat("IdleIndex", _savedIdleIdx);
                    a.Play(_savedStateHash, 0, Mathf.Max(0f, _savedNormTime - rewind));
                    a.Update(0f);
                }
            }
        }
    }
}
