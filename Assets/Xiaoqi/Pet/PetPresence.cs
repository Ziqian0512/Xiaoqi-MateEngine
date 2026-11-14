using System;
using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    public class PetPresence : MonoBehaviour
    {
        public static bool IsAsleep { get; private set; }

        private EmotionLineConfig _cfg;
        private HeadAnchor _head;
        private StageData _stage;
        private bool _dragWas;
        private float _wakeAt = -1f;
        private float _napUntil = -1f;

        public static void Nap(float minutes)
        {
            var self = FindFirstObjectByType<PetPresence>(FindObjectsInactive.Include);
            if (self == null) return;
            IsAsleep = true;
            if (PetHost.I != null) PetHost.I.SetSleeping(true);
            self._napUntil = Time.unscaledTime + Mathf.Max(0.1f, minutes) * 60f;
            Debug.Log("XQ_NAP start min=" + minutes);
        }

        public static void Wake()
        {
            var self = FindFirstObjectByType<PetPresence>(FindObjectsInactive.Include);
            IsAsleep = false;
            if (PetHost.I != null) PetHost.I.SetSleeping(false);
            if (self != null)
            {
                self._napUntil = -1f;
                self._wakeAt = Time.unscaledTime + self._cfg.presence.wake_on_drag_sec;
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
            _stage = s;
            if (_head == null) _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            var state = s.where == "dorm" && s.pose == "sleep" ? "睡觉"
                : s.where != "dorm" ? "不在" : "在宿舍";
            bool sleep = state == "睡觉";
            IsAsleep = sleep;
            if (PetHost.I != null) PetHost.I.SetSleeping(sleep);
            if (sleep) _wakeAt = -1f;

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
                else _head.SetLabel("");
            }
            Debug.Log("XQ_PRESENCE " + state + " seg=" + (s.seg != null ? s.seg.name : ""));
        }

        private void Update()
        {
            if (!XiaoqiRuntime.Config.emotion_line) return;
            if (PetHost.I == null) return;
            if (_napUntil > 0f && Time.unscaledTime >= _napUntil)
            {
                _napUntil = -1f;
                IsAsleep = false;
                PetHost.I.SetSleeping(false);
                Debug.Log("XQ_NAP end");
            }
            bool dragging = PetHost.I.IsDragging;
            if (dragging && !_dragWas && IsAsleep)
            {
                IsAsleep = false;
                PetHost.I.SetSleeping(false);
                _napUntil = -1f;
                _wakeAt = Time.unscaledTime + _cfg.presence.wake_on_drag_sec;
            }
            _dragWas = dragging;
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
