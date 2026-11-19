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
        private const float CloseVx = 0.50f;
        private const float CloseVy = 0.57f;
        // XQ(WB): G74 W1 工作台原生版也借这套大屏窗口，只是镜头拉远、眼睛落点换到屏幕左侧。落点做成可改的。
        private static float TargetVx = CloseVx;
        private static float TargetVy = CloseVy;
        private static float _benchFrac = 0f;   // >0 = 工作台：每帧把她的身高收敛到屏高的这个比例
        private static float _win = 1.6f;       // 对眼/缩放的收敛窗口（秒，从进入起算）
        // XQ(WB): G74 W1c 工作台里她贴边时的歪头/转身（绕脚下原点：roll 绕镜头前向，yaw 绕竖轴），退出时还原
        private static Quaternion _baseRot = Quaternion.identity;
        private static bool _hasBaseRot, _tiltApplied;
        private static float _tRoll, _tYaw, _tRollT, _tYawT;
        private static float _enterAt;
        private static bool _loggedSettledEyes;
        private static float _xShift;
        private static EyeAlignRunner _runner;

        [DefaultExecutionOrder(200)]
        sealed class EyeAlignRunner : MonoBehaviour
        {
            void LateUpdate()
            {
                TickTilt();
                TickEyeAlign();
                if (_benchBottomNorm > 0.005f) ApplyBenchViewport();
            }
        }

        static float _benchBottomNorm;

        /// <summary>V13 工作台物理裁剪底边：把 3D 主相机视口底边抬高到相框下沿，彻底杜绝腿脚穿出面板。</summary>
        public static void SetBenchViewportBottom(float bottomNorm)
        {
            _benchBottomNorm = Mathf.Clamp(bottomNorm, 0f, 0.55f);
            SetAvatarRenderersVisible(_benchBottomNorm <= 0.005f);
            ApplyBenchViewport();
        }

        static void SetAvatarRenderersVisible(bool visible)
        {
            var h = Handler();
            var root = h != null && h.avatarAnimator != null ? h.avatarAnimator.gameObject : null;
            if (root == null) return;
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
                if (rend != null) rend.enabled = visible;
        }

        static int _origMask = -1;

        private static void ApplyBenchViewport()
        {
            // 始终保持主相机全屏 (0,0,1,1) 且不改 cullingMask，确保工作台切回桌宠/近景时 3D 本体永不丢失且全屏清屏
            var r = new Rect(0f, 0f, 1f, 1f);
            var h = Handler();
            var cam = h != null && h.MainCamera != null ? h.MainCamera : Camera.main;
            if (cam != null)
            {
                if (_origMask < 0) _origMask = cam.cullingMask;
                cam.rect = r;
                cam.enabled = true;
                if (cam.cullingMask == 0 && _origMask > 0) cam.cullingMask = _origMask;
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
                Debug.Log("XQ_CLOSE eyes view=(" + vp.x.ToString("0.00") + "," + vp.y.ToString("0.00") + ") target=(" + TargetVx.ToString("0.00") + "," + TargetVy.ToString("0.00") + ")"
                    + (_benchFrac > 0f ? " bench frac=" + CurFrac().ToString("0.00") + "/" + _benchFrac.ToString("0.00") : ""));
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
            // XQ: B1 第二轮 #16 对眼只在进入后的收敛窗口里做（到 1.6s 为止）。之后她做动作、头在动，每帧再「追」眼睛位置，
            // 整个身体就跟着头一起抽（之前每帧 0.65 增益追到天荒地老）。收敛完位置就定住，动作的幅度该是多少就是多少。
            if (elapsed > _win) { if (!_loggedSettledEyes) { _loggedSettledEyes = true; LogEyes(h); } return; }
            if (_benchFrac > 0f && h != null) ZoomStep(h);
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
                    h.YOffset = Mathf.Clamp(h.YOffset + dyOff, -2.5f, 2.5f); // XQ(WB): 工作台落点离中心远，原 ±0.4 不够
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

        private static void TickTilt()
        {
            if (!_active || !_hasBaseRot) return;
            var h = Handler();
            var a = h != null ? h.avatarAnimator : null;
            if (a == null) return;
            float dt = Time.unscaledDeltaTime;
            _tRoll = Mathf.MoveTowards(_tRoll, _tRollT, 150f * dt);
            _tYaw = Mathf.MoveTowards(_tYaw, _tYawT, 150f * dt);
            bool moving = !Mathf.Approximately(_tRoll, _tRollT) || !Mathf.Approximately(_tYaw, _tYawT);
            if (moving) _enterAt = Time.unscaledTime - (_win - 0.6f); // 转的时候让对眼继续跑
            if (_benchFrac > 0f && Mathf.Abs(a.GetFloat("IdleIndex")) > 0.01f) a.SetFloat("IdleIndex", 0f);   // W3：工作台里不换待机
            if (_tRoll == 0f && _tYaw == 0f && !_tiltApplied) return;
            a.transform.rotation = Quaternion.AngleAxis(_tRoll, Vector3.forward) * Quaternion.AngleAxis(_tYaw, Vector3.up) * _baseRot;
            _tiltApplied = (_tRoll != 0f || _tYaw != 0f);
        }

        /// <summary>工作台：设置她歪头 / 转身的目标角度（度）。roll 正=顺时针（头往屏幕右），yaw 正=脸转向屏幕左。</summary>
        public static void SetTilt(float roll, float yaw)
        {
            if (!_active || !_hasBaseRot) return;
            _tRollT = roll; _tYawT = yaw;
        }

        private static void ResetTilt(Animator a)
        {
            _tRoll = _tYaw = _tRollT = _tYawT = 0f;
            if (a != null && _hasBaseRot && _tiltApplied) a.transform.rotation = _baseRot;
            _tiltApplied = false;
        }

        private static float CurFrac()
        {
            if (Xiaoqi.Host.PetHost.I == null) return 0f;
            return Xiaoqi.Host.PetHost.I.ScreenRect().height / Mathf.Max(1f, Screen.height);
        }

        /// <summary>工作台：用她当前外框高度/屏高 与目标比例的比，逐帧修正镜头缩放（不管镜头是透视还是正交）。</summary>
        private static void ZoomStep(AvatarBigScreenHandler h)
        {
            var cam = h.MainCamera != null ? h.MainCamera : Camera.main;
            if (cam == null || h.avatarAnimator == null) return;
            float cur = CurFrac();
            if (cur < 0.03f) return;
            float err = cur / _benchFrac;
            if (Mathf.Abs(err - 1f) < 0.03f) return;
            float step = Mathf.Pow(err, 0.7f);
            float scale = Mathf.Max(0.01f, h.avatarAnimator.transform.lossyScale.y);
            float z;
            if (cam.orthographic)
            {
                float z0 = h.TargetZoom > 0f ? h.TargetZoom : cam.orthographicSize / scale;
                z = Mathf.Max(0.01f, z0 * step);
            }
            else
            {
                float z0 = h.TargetZoom > 0f ? h.TargetZoom : cam.fieldOfView;
                float t = Mathf.Tan(z0 * 0.5f * Mathf.Deg2Rad) * step;
                z = Mathf.Clamp(2f * Mathf.Atan(t) * Mathf.Rad2Deg, 3f, 150f);
            }
            h.TargetZoom = z;
        }

        /// <summary>工作台里拖她：把眼睛落点改到 (vx,vy)，并让对眼再跑 0.6 秒。</summary>
        public static void NudgeEyes(float vx, float vy)
        {
            if (!_active || Handler() == null) return;
            TargetVx = vx; TargetVy = vy;
            _enterAt = Time.unscaledTime - (_win - 0.6f);   // 拖动时每帧调：不重置「已记日志」标记，免得每帧刷 XQ_CLOSE eyes
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

        /// <summary>
        /// 按「她的身高占屏高多少」算出 TargetZoom（0 = 保持 ME 自动）。用镜头当前值按比例缩放，
        /// 所以从小窗口进入、从近景切过来都能用。取不到她的外框就返回 0。
        /// </summary>
        private static float ZoomForFrac(AvatarBigScreenHandler h, float newFrac)
        {
            var cam = h.MainCamera != null ? h.MainCamera : Camera.main;
            if (cam == null || h.avatarAnimator == null || Xiaoqi.Host.PetHost.I == null || newFrac <= 0.05f) return 0f;
            float curFrac = Xiaoqi.Host.PetHost.I.ScreenRect().height / Mathf.Max(1f, Screen.height);
            if (curFrac < 0.05f) return 0f;
            float k = curFrac / newFrac;
            float scale = Mathf.Max(0.01f, h.avatarAnimator.transform.lossyScale.y);
            float z;
            if (cam.orthographic) z = cam.orthographicSize * k / scale;
            else z = Mathf.Clamp(2f * Mathf.Atan(Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * k) * Mathf.Rad2Deg, 5f, 110f);
            Debug.Log("XQ_BENCH zoom cur=" + curFrac.ToString("0.00") + " new=" + newFrac.ToString("0.00") + " z=" + z.ToString("0.00") + (cam.orthographic ? " ortho" : " persp"));
            return z;
        }

        /// <summary>工作台镜头：窗口铺满显示器，她以 bodyFrac×屏高的身高站在屏幕左侧（眼睛落在 vx,vy）。</summary>
        public static bool BeginEnterBench(float bodyFrac, float vx, float vy)
        {
            var h = Handler();
            if (h == null) { Debug.Log("XQ_HOST_MISS AvatarBigScreenHandler"); return false; }
            float z = _active ? 0f : ZoomForFrac(h, bodyFrac);
            bool ok = BeginEnter();
            TargetVx = vx; TargetVy = vy; // BeginEnter 会把落点复位成近景的，这里再设一次
            _benchFrac = bodyFrac; _win = 2.6f;
            if (ok && z > 0f) h.TargetZoom = z;
            return ok;
        }

        /// <summary>已经在大屏里（近景↔工作台）直接换镜头，不退出窗口。</summary>
        public static bool Retarget(bool toBench, float bodyFrac, float vx, float vy)
        {
            var h = Handler();
            if (h == null || !_active) return false;
            var c = XqModeConfig.I;
            if (toBench)
            {
                TargetVx = vx; TargetVy = vy;
                _benchFrac = bodyFrac; _win = 2.6f;
                _enterAt = Time.unscaledTime - 0.6f; // 已经在大屏里，不用等淡入
                if (h.avatarAnimator != null)   // W3：工作台里她固定一个乖巧站姿，恢复正常眨眼速率
                {
                    h.avatarAnimator.SetFloat("IdleIndex", 0f);
                    h.avatarAnimator.speed = 1f;
                }
            }
            else
            {
                SetBenchViewportBottom(0f);
                h.TargetZoom = c.close_zoom > 0f ? c.close_zoom : 0f;
                h.YOffset = c.close_yoffset;
                TargetVx = CloseVx; TargetVy = CloseVy;
                _benchFrac = 0f; _win = 1.6f;
                if (h.avatarAnimator != null) h.avatarAnimator.speed = 1f;
                ResetTilt(h.avatarAnimator);
                _enterAt = Time.unscaledTime;
            }
            _loggedSettledEyes = false;
            return true;
        }

        /// <summary>开始进入。返回 false = 找不到 ME 的大屏组件（不崩，调用方写日志）。</summary>
        public static bool BeginEnter()
        {
            var h = Handler();
            if (h == null) { Debug.Log("XQ_HOST_MISS AvatarBigScreenHandler"); return false; }
            var c = XqModeConfig.I;
            if (!_active) { TargetVx = CloseVx; TargetVy = CloseVy; _benchFrac = 0f; _win = 1.6f; }
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
                    _baseRot = a.transform.rotation; _hasBaseRot = true;
                    _tRoll = _tYaw = _tRollT = _tYawT = 0f; _tiltApplied = false;
                    a.SetBool("isBigScreen", false);
                    a.SetFloat("IdleIndex", 0f);
                    a.Play("Idle", 0, 0.55f);
                    a.speed = 1f;
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
                ResetTilt(h.avatarAnimator);
                if (h.avatarAnimator != null && _hasSavedPos)
                    h.avatarAnimator.transform.position = _savedAvatarPos + new Vector3(_xShift, 0f, 0f);
                LogEyes(h);
                _active = false;
                _benchFrac = 0f; _win = 1.6f;
                SetBenchViewportBottom(0f);
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
