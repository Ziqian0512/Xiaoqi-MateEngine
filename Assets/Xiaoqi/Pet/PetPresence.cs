using System;
using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    public class PetPresence : MonoBehaviour
    {
        public static bool IsAsleep { get; private set; }
        private static PetPresence _inst;

        private EmotionLineConfig _cfg;
        private HeadAnchor _head;
        private StageData _stage;
        private bool _dragWas;
        private bool _quietWas;
        private float _wakeAt = -1f;
        private float _napUntil = -1f;

        private void Awake()
        {
            _inst = this;

        }

        public static void Nap(float minutes)
        {
            if (_inst == null) return;
            IsAsleep = true;
            if (PetHost.I != null) PetHost.I.SetSleeping(true);
            _inst._napUntil = Time.unscaledTime + Mathf.Max(0.1f, minutes) * 60f;

            Debug.Log("XQ_NAP start min=" + minutes);
        }

        public static void Wake()
        {
            IsAsleep = false;
            if (PetHost.I != null) PetHost.I.SetSleeping(false);
            if (_inst != null)
            {
                _inst._napUntil = -1f;
                _inst._wakeAt = Time.unscaledTime + (_inst._cfg != null ? _inst._cfg.presence.wake_on_drag_sec : 30f);
            }
            Debug.Log("XQ_NAP wake");
        }

        private void Start()
        {
            _cfg = EmotionLineConfig.Load();
            XiaoqiLink.OnStage += OnStage;
            if (XiaoqiLink.Current != null) OnStage(XiaoqiLink.Current);
        }

        private void OnDestroy() { XiaoqiLink.OnStage -= OnStage; }

        private void OnStage(StageData s)
        {
            if (s == null || !XiaoqiRuntime.Config.emotion_line) return;
            bool serverSleep = s.where == "dorm" && s.pose == "sleep";
            // 服务器每 poll_sec 秒就推一次，不能每次都把本地的决定冲掉：
            // - 本地小睡（菜单「睡一会」/ 自己睡着）还没到点 → 保持睡着；
            // - 刚被叫醒、30 秒宽限内 → 即使服务器说睡，也先醒着（到点由 Update 再判断）。
            bool sleep = serverSleep;
            if (_napUntil > 0f) sleep = true;
            else if (_wakeAt > 0f && serverSleep) sleep = false;
            bool changed = sleep != IsAsleep || _stage == null;
            _stage = s;
            if (_head == null) _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            IsAsleep = sleep;
            if (changed && PetHost.I != null) PetHost.I.SetSleeping(sleep);

            if (_head != null)
            {
                if (sleep) _head.SetLabel(_cfg.presence.label_sleep);
                else if (s.where != "dorm")
                {
                    var seg = s.seg != null ? s.seg.name : "";
                    var back = s.next != null ? s.next.at : "";
                    _head.SetLabel(!string.IsNullOrEmpty(back)
                        ? _cfg.presence.label_out.Replace("{seg}", seg).Replace("{back}", back)
                        : _cfg.presence.label_out_noback.Replace("{seg}", seg));
                }
                else if (XiaoqiRuntime.IsQuiet()) _head.SetLabel(XiaoqiRuntime.QuietLabel());
                else _head.SetLabel(XqStageBody.DormLabel(s));
            }
            Debug.Log("XQ_PRESENCE dorm=" + (s.where == "dorm") + " sleep=" + sleep
                      + " seg=" + (s.seg != null ? s.seg.name : ""));
        }

        private void Update()
        {
            if (PetHost.I == null) return;
            if (_napUntil > 0f && Time.unscaledTime >= _napUntil)
            {
                _napUntil = -1f;
                IsAsleep = false;
                PetHost.I.SetSleeping(false);
                Debug.Log("XQ_NAP end");
            }
            if (PetHost.I.IsDragging && !_dragWas && IsAsleep)
            {
                IsAsleep = false;
                PetHost.I.SetSleeping(false);
                _napUntil = -1f;
                if (_cfg != null) _wakeAt = Time.unscaledTime + _cfg.presence.wake_on_drag_sec;
                Debug.Log("XQ_NAP wake_drag");
            }
            _dragWas = PetHost.I.IsDragging;
            if (!XiaoqiRuntime.Config.emotion_line) return;

            bool dragging = PetHost.I.IsDragging;
            bool quiet = XiaoqiRuntime.IsQuiet();
            if (quiet != _quietWas)
            {
                _quietWas = quiet;
                if (_head == null) _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
                if (_stage != null && _head != null && _stage.where == "dorm" && !IsAsleep)
                    _head.SetLabel(quiet ? XiaoqiRuntime.QuietLabel() : XqStageBody.DormLabel(_stage));
            }

            if (!dragging && _wakeAt > 0f && Time.unscaledTime >= _wakeAt)
            {
                _wakeAt = -1f;
                if (_stage != null && _stage.where == "dorm" && _stage.pose == "sleep")
                {
                    IsAsleep = true;
                    PetHost.I.SetSleeping(true);
                }
            }
        }
    }
}
