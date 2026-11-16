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
        // XQ: P2-2 R-1 双眼视口收敛状态（vx=0.50, vy=0.57）
        private const float TargetVx = 0.50f;
        private const float TargetVy = 0.57f;
        private static float _enterAt;
        private static bool _loggedSettledEyes;
        private static float _xShift;
        private static EyeAlignRunner _runner;

        [DefaultExecutionOrder(200)]
        sealed class EyeAlignRunner : MonoBehaviour
        {
            void LateUpdate()
            {
                TickEyeAlign();
            }
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("XqCloseEyeAlign");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<EyeAlignRunner>();
        }

        private static bool TryGetEyeWorld(AvatarBigScreenHandler h, Animator a, out Vector3 eyeWorld)
        {
            var le = a.GetBoneTransform(HumanBodyBones.LeftEye);
            var re = a.GetBoneTransform(HumanBodyBones.RightEye);
            if (le != null && re != null)
            {
                eyeWorld = 0.5f * (le.position + re.position);
                return true;
            }
            var head = a.GetBoneTransform(h.attachBone);
            if (head != null)
            {
                eyeWorld = head.position + new Vector3(0f, 0.08f * a.transform.lossyScale.y, 0f);
                return true;
            }
            eyeWorld = Vector3.zero;
            return false;
        }

        private static bool TryGetEyeViewport(AvatarBigScreenHandler h, out Camera cam, out Animator a, out Vector3 eyeWorld, out Vector3 vp)
        {
            cam = null;
            a = h != null ? h.avatarAnimator : null;
            eyeWorld = Vector3.zero;
            vp = Vector3.zero;
            if (h == null || a == null) return false;
            cam = h.MainCamera != null ? h.MainCamera : Camera.main;
            if (cam == null) return false;
            if (!TryGetEyeWorld(h, a, out eyeWorld)) return false;
            vp = cam.WorldToViewportPoint(eyeWorld);
            return vp.z > 0.01f;
        }

        private static void LogEyes(AvatarBigScreenHandler h)
        {
            if (TryGetEyeViewport(h, out _, out _, out _, out var vp))
            {
                Debug.Log("XQ_CLOSE eyes view=(" + vp.x.ToString("0.00") + "," + vp.y.ToString("0.00") + ") target=(0.50,0.57)");
            }
        }

        private static void TickEyeAlign()
        {
            if (!_active) return;
            float elapsed = Time.unscaledTime - _enterAt;
            if (elapsed < 0.95f) return; // 等 ME BigScreenEnterSequence (0.4+0.5s) 淡入结束
            var h = Handler();
            var a0 = h != null ? h.avatarAnimator : null;
            if (a0 != null && _hasSavedPos)
                a0.transform.position = _savedAvatarPos + new Vector3(_xShift, 0f, 0f);
            if (!TryGetEyeViewport(h, out var cam, out var a, out var eyeWorld, out var vp)) return;

            float ex = Mathf.Abs(vp.x - TargetVx);
            float ey = Mathf.Abs(vp.y - TargetVy);
            if (ex > 0.002f || ey > 0.002f)
            {
                Vector3 targetWorld = cam.ViewportToWorldPoint(new Vector3(TargetVx, TargetVy, vp.z));
                if (ex > 0.002f)
                {
                    _xShift += (targetWorld.x - eyeWorld.x) * 0.65f;
                    if (_hasSavedPos)
                        a.transform.position = _savedAvatarPos + new Vector3(_xShift, 0f, 0f);
                    else
                        a.transform.position += new Vector3((targetWorld.x - eyeWorld.x) * 0.65f, 0f, 0f);
                }
                if (ey > 0.002f)
                {
                    float scale = Mathf.Max(0.01f, a.transform.lossyScale.y);
                    float dyOff = ((eyeWorld.y - targetWorld.y) / scale) * 0.65f;
                    h.YOffset = Mathf.Clamp(h.YOffset + dyOff, -0.4f, 0.4f);
                }
            }

            if (TryGetEyeViewport(h, out _, out _, out _, out var vpNow))
            {
                ex = Mathf.Abs(vpNow.x - TargetVx);
                ey = Mathf.Abs(vpNow.y - TargetVy);
            }
            if (!_loggedSettledEyes && ((elapsed >= 1.15f && ex <= 0.02f && ey <= 0.02f) || elapsed >= 1.50f))
            {
                _loggedSettledEyes = true;
                LogEyes(h);
            }
        }

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
                _enterAt = Time.unscaledTime;
                _loggedSettledEyes = false;
                _xShift = 0f;
                EnsureRunner();
                // G59-P2 §3.1/§3.3：保持站姿待机平稳帧，退出时原样恢复位置与动画相位
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
                if (h.avatarAnimator != null && _hasSavedPos)
                    h.avatarAnimator.transform.position = _savedAvatarPos + new Vector3(_xShift, 0f, 0f);
                LogEyes(h);
                _active = false;
                _xShift = 0f;
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
