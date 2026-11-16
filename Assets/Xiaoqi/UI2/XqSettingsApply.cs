using System;
using UnityEngine;
using Xiaoqi.Host;
using Xiaoqi.Spike;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-5 设置值 → 宿主（ME）的连接。所有「连什么」都写在这里，面板只管调用。
    /// 拿不到组件：打一条 XQ_SET2 miss ...，原样不动，不抛异常，也不改 ME 文件。
    /// </summary>
    public static class XqSettingsApply
    {
        // 看向力度：把 ME 原值当 100%。模型重载会重建组件，用 instanceID 判断是否要重记。
        static int _trackingId;
        static bool _blendSaved;
        static float _headBase = 1f, _spineBase = 1f, _eyeBase = 1f;

        static AvatarMouseTracking Tracking()
        {
            var t = UnityEngine.Object.FindFirstObjectByType<AvatarMouseTracking>(FindObjectsInactive.Include);
            if (t == null) return null;
            if (t.GetInstanceID() != _trackingId)
            {
                _trackingId = t.GetInstanceID();
                _blendSaved = false;
            }
            return t;
        }

        public static void SetWindowSit(bool on)
        {
            var h = PetHost.I;
            if (h == null) { Debug.Log("XQ_SET2 miss PetHost"); return; }
            h.WindowSitEnabled = on;
            h.SaveSettings();
        }

        /// <summary>看向鼠标：AvatarMouseTracking.enableMouseTracking，同时写存档 data（否则被 ApplyAll 冲回）。</summary>
        public static void SetLookAtMouse(bool on)
        {
            var t = Tracking();
            if (t == null) { Debug.Log("XQ_SET2 miss AvatarMouseTracking"); return; }
            t.enableMouseTracking = on;
            var sl = SaveLoadHandler.Instance;
            if (sl == null || sl.data == null) { Debug.Log("XQ_SET2 miss SaveLoadHandler"); return; }
            sl.data.enableMouseTracking = on;
        }

        /// <summary>看向的力度 0-1：headBlend / spineBlend / eyeBlend 同乘一个系数。</summary>
        public static void SetLookStrength(float strength01)
        {
            var t = Tracking();
            if (t == null) { Debug.Log("XQ_SET2 miss AvatarMouseTracking"); return; }
            if (!_blendSaved)
            {
                _headBase = t.headBlend;
                _spineBase = t.spineBlend;
                _eyeBase = t.eyeBlend;
                _blendSaved = true;
                Debug.Log("XQ_SET2 look base " + _headBase.ToString("0.00")
                    + "/" + _spineBase.ToString("0.00") + "/" + _eyeBase.ToString("0.00"));
            }
            float s = Mathf.Clamp01(strength01);
            t.headBlend = _headBase * s;
            t.spineBlend = _spineBase * s;
            t.eyeBlend = _eyeBase * s;

            // 同步写存档，否则下一次 ApplyAll 会把上面三个值写回旧值
            var sl = SaveLoadHandler.Instance;
            if (sl == null || sl.data == null) { Debug.Log("XQ_SET2 miss SaveLoadHandler"); return; }
            sl.data.headBlend = t.headBlend;
            sl.data.spineBlend = t.spineBlend;
            sl.data.eyeBlend = t.eyeBlend;
        }

        /// <summary>
        /// 放音乐时自己跳：SaveLoadHandler.data.enableDancing。
        /// 只在值真的变化时才调 ApplyAll —— 它会把 isDancing 置 false，会停掉正在跳的舞（见 ISSUES 第 5 行）。
        /// </summary>
        public static void SetDancing(bool on)
        {
            var sl = SaveLoadHandler.Instance;
            if (sl == null || sl.data == null) { Debug.Log("XQ_SET2 miss SaveLoadHandler"); return; }
            if (sl.data.enableDancing == on) return;
            sl.data.enableDancing = on;
            SaveLoadHandler.ApplyAllSettingsToAllAvatars();
        }

        public static bool GetDancing()
        {
            var sl = SaveLoadHandler.Instance;
            if (sl == null || sl.data == null) return false;
            return sl.data.enableDancing;
        }

        /// <summary>多久不动就睡着。0 = 关。sleepTimer 是秒，不碰 xqHold（菜单「睡一会」用它）。</summary>
        public static void SetIdleSleep(int minutes)
        {
            var c = UnityEngine.Object.FindFirstObjectByType<AvatarSleepController>(FindObjectsInactive.Include);
            if (c == null) { Debug.Log("XQ_SET2 miss AvatarSleepController"); return; }
            if (minutes <= 0)
            {
                c.enableSleep = false;
                return;
            }
            c.enableSleep = true;
            // ME 的 [Range(30,360)] 只限制 Inspector，运行时 10 分钟 = 600 秒照用。
            c.sleepTimer = minutes * 60f;
        }

        /// <summary>双击摸头。</summary>
        public static void SetPatDouble(bool on)
        {
            PetPat.Enabled = on;
        }

        /// <summary>
        /// 静音 / 总音量一起作用于 AudioListener.volume。
        /// 音量只在其它设置把 AudioListener 改回去之后才需要重调，所以两个入口都走这里。
        /// </summary>
        public static void ApplyVolume()
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg == null) { Debug.Log("XQ_SET2 miss cfg"); return; }
            AudioListener.volume = cfg.muted ? 0f : Mathf.Clamp01(cfg.volume);
            Debug.Log("XQ_SET2 volume=" + AudioListener.volume.ToString("0.00"));
        }

        /// <summary>静音开关：cfg.muted 由 Bind 写入，这里只把结果应用到 AudioListener。</summary>
        public static void SetMuted(bool on)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg != null) cfg.muted = on;
            ApplyVolume();
        }

        /// <summary>总音量 0-1：写 cfg.volume 并立即应用（滑条拖动时也调）。</summary>
        public static void SetVolume(float volume01)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg != null) cfg.volume = Mathf.Clamp01(volume01);
            ApplyVolume();
        }
    }
}
