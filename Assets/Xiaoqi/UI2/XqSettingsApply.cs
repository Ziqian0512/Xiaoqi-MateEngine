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

        /// <summary>看向鼠标：AvatarMouseTracking.enableMouseTracking。</summary>
        public static void SetLookAtMouse(bool on)
        {
            var t = Tracking();
            if (t == null) { Debug.Log("XQ_SET2 miss AvatarMouseTracking"); return; }
            t.enableMouseTracking = on;
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
        }

        /// <summary>放音乐时自己跳：SaveLoadHandler.data.enableDancing，改完整体重应用。</summary>
        public static void SetDancing(bool on)
        {
            var sl = SaveLoadHandler.Instance;
            if (sl == null || sl.data == null) { Debug.Log("XQ_SET2 miss SaveLoadHandler"); return; }
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
    }
}
