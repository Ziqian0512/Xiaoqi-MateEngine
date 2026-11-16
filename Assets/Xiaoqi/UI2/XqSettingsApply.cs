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
        /// log 只在提交（松手 / 切开关）时为 true：拖滑条每帧调，不能每帧一行日志。
        /// </summary>
        public static void ApplyVolume(bool log = false)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg == null) { Debug.Log("XQ_SET2 miss cfg"); return; }
            AudioListener.volume = cfg.muted ? 0f : Mathf.Clamp01(cfg.volume);
            if (log) Debug.Log("XQ_SET2 volume=" + AudioListener.volume.ToString("0.00"));
        }

        /// <summary>静音开关：cfg.muted 由 Bind 写入，这里只把结果应用到 AudioListener。</summary>
        public static void SetMuted(bool on)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg != null) cfg.muted = on;
            ApplyVolume(true);
        }

        /// <summary>总音量 0-1：拖动中调用，写 cfg 并应用，不打日志。</summary>
        public static void SetVolume(float volume01)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg != null) cfg.volume = Mathf.Clamp01(volume01);
            ApplyVolume();
        }

        /// <summary>总音量提交（松手 / 双击回默认）：写 cfg、存盘、应用，只打一行日志。</summary>
        public static void CommitVolume(float volume01)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg == null) { Debug.Log("XQ_SET2 miss cfg"); return; }
            cfg.volume = Mathf.Clamp01(volume01);
            AudioListener.volume = cfg.muted ? 0f : cfg.volume;
            XiaoqiConfig.Save(cfg);
            Debug.Log("XQ_SET2 set volume=" + cfg.volume.ToString("0.00"));
        }

        /// <summary>
        /// 把 cfg 里的设置值全部重新应用一遍。
        /// 两处调用：面板第一次 Open() 时（§4 D4 要求）、启动时 ApplyOnStart()。
        /// 拿不到组件只打 miss，不抛也不重试。
        /// 注意：SetDancing / WindowSit 的宿主 setter 会触发 ApplyAll 并把 isDancing 置 false，
        /// 所以这两项只在值真的不同时才调，避免一次启动连调好几遍、停掉正在跳的舞。
        /// </summary>
        public static void ApplyAll(XiaoqiConfig cfg)
        {
            if (cfg == null) { Debug.Log("XQ_SET2 miss cfg"); return; }

            // 看向鼠标 + 力度（会同时写 SaveLoadHandler.data，避免被宿主 ApplyAll 冲回）
            SetLookAtMouse(cfg.look_at_mouse);
            SetLookStrength(cfg.look_strength);

            // 多久不动就睡着：0 = 关；非 0 时同时设 enableSleep 与 sleepTimer
            SetIdleSleep(cfg.idle_sleep_min);

            // 双击摸头
            SetPatDouble(cfg.pat_double);

            // 静音 / 总音量 → AudioListener.volume
            AudioListener.volume = cfg.muted ? 0f : Mathf.Clamp01(cfg.volume);
            Debug.Log("XQ_SET2 volume=" + AudioListener.volume.ToString("0.00"));

            // 放音乐时自己跳：只在与现值不同时调（会触发宿主 ApplyAll）
            bool dancing = GetDancing();
            if (dancing != cfg.dance_on_music) SetDancing(cfg.dance_on_music);

            // 坐在窗口上：同上，只在与现值不同时写（setter 会触发宿主 ApplyAll）
            var h = PetHost.I;
            if (h == null) { Debug.Log("XQ_SET2 miss PetHost"); }
            else if (h.WindowSitEnabled != cfg.window_sit) h.WindowSitEnabled = cfg.window_sit;
        }

        /// <summary>
        /// 启动时把存盘的值重新应用一遍：读 XiaoqiRuntime.Config 后交给 ApplyAll。
        /// 由 XiaoqiSystems 在模型加载完成后调一次（见 docs/unity-progress/2026-11-16-P1/B4-REPORT.md）。
        /// 前提：XiaoqiRuntime.Config 已赋值；AvatarMouseTracking / SaveLoadHandler / AvatarSleepController
        /// 这些宿主组件已随模型加载出来。拿不到就只打 miss，不抛不重试。
        /// </summary>
        public static void ApplyOnStart()
        {
            try
            {
                ApplyAll(XiaoqiRuntime.Config);
                Debug.Log("XQ_SET2 apply start done");
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_SET2 apply start " + e.Message);
            }
        }
    }
}
