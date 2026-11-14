using System;
using UnityEngine;

namespace Xiaoqi.Spike
{
    public class PetPresence : MonoBehaviour
    {
        public static bool IsAsleep { get; private set; }

        private EmotionLineConfig _cfg;
        private HeadAnchor _head;
        private AvatarAnimatorController _avatar;
        private StageData _stage;
        private Animator _animator;
        private bool _sleepParam;
        private bool _dragWas;
        private float _wakeAt = -1f;
        private float _nextFind;

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
            FindRefs();
            var state = s.where == "dorm" && s.pose == "sleep" ? "睡觉"
                : s.where != "dorm" ? "不在" : "在宿舍";
            bool sleep = state == "睡觉";
            IsAsleep = sleep;
            if (_animator != null && _sleepParam) _animator.SetBool("IsSleeping", sleep);
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
                else _head.SetLabel("");
            }
            Debug.Log("XQ_PRESENCE " + state + " seg=" + (s.seg != null ? s.seg.name : ""));
        }

        private void Update()
        {
            if (!XiaoqiRuntime.Config.emotion_line) return;
            if (Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 1f;
                FindRefs();
            }
            if (_avatar == null)
                _avatar = FindFirstObjectByType<AvatarAnimatorController>(FindObjectsInactive.Include);
            if (_avatar != null)
            {
                bool dragging = _avatar.isDragging;
                if (dragging && !_dragWas && IsAsleep)
                {
                    IsAsleep = false;
                    if (_animator != null && _sleepParam) _animator.SetBool("IsSleeping", false);
                    _wakeAt = Time.unscaledTime + _cfg.presence.wake_on_drag_sec;
                }
                _dragWas = dragging;
                if (!dragging && _wakeAt > 0f && Time.unscaledTime >= _wakeAt)
                {
                    _wakeAt = -1f;
                    if (_stage != null && _stage.where == "dorm" && _stage.pose == "sleep")
                    {
                        IsAsleep = true;
                        if (_animator != null && _sleepParam) _animator.SetBool("IsSleeping", true);
                    }
                }
            }
        }

        private void FindRefs()
        {
            if (_head == null) _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            if (_animator == null)
            {
                var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
                var model = loader != null ? loader.GetCurrentModel() : null;
                if (model != null)
                {
                    _animator = model.GetComponentInChildren<Animator>();
                    if (_animator != null)
                    {
                        _sleepParam = false;
                        foreach (var p in _animator.parameters)
                            if (p.type == AnimatorControllerParameterType.Bool && p.name == "IsSleeping")
                                _sleepParam = true;
                        if (!_sleepParam) Debug.Log("XQ_NO_SLEEP_PARAM");
                    }
                }
            }
        }
    }
}
