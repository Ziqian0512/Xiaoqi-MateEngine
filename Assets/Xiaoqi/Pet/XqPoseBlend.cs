using System.Collections.Generic;
using UnityEngine;

namespace Xiaoqi.Spike
{
    /// <summary>
    /// XQ: P1-4b 姿势淡入。为什么要有它：
    /// ME 的待机是 Animator 里一棵一维混合树（参数 IdleIndex，阈值 0,1,2…），从 2 切到 7 要滑过 3、4、5、6，
    /// 中间会出现几条动作的混合姿势。这里的做法：切换前先记下当前每块骨骼的姿势，参数立刻跳到目标值，
    /// 然后在 LateUpdate 里把「旧姿势」按 smoothstep 淡成「动画此刻算出来的新姿势」。不改控制器、不覆盖控制器。
    /// 不碰：头/脖子/眼睛/下巴（留给 ME 的鼠标追踪和脸部表情，淡入它们会和追踪打架）。
    /// 用法：Begin(dur) 必须在改 Animator 参数之前、同一帧的 Update 里调；Apply() 每帧在 LateUpdate 里调。
    /// [未编]
    /// </summary>
    public class XqPoseBlend
    {
        private static readonly HumanBodyBones[] Skip =
        {
            HumanBodyBones.Head, HumanBodyBones.Neck, HumanBodyBones.Jaw,
            HumanBodyBones.LeftEye, HumanBodyBones.RightEye,
        };

        private readonly List<Transform> _bones = new List<Transform>();
        private readonly List<Quaternion> _snapRot = new List<Quaternion>();
        private Transform _hips;
        private Vector3 _snapHipsPos;
        private Animator _bound;
        private float _t = 1f, _dur = 0.6f;

        public bool Active => _t < 1f;

        private void Bind(Animator a)
        {
            _bones.Clear();
            _snapRot.Clear();
            _hips = null;
            _bound = a;
            if (a == null || !a.isHuman) return;
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var hb = (HumanBodyBones)i;
                if (System.Array.IndexOf(Skip, hb) >= 0) continue;
                var tr = a.GetBoneTransform(hb);
                if (tr == null) continue;
                _bones.Add(tr);
                _snapRot.Add(tr.localRotation);
                if (hb == HumanBodyBones.Hips) _hips = tr;
            }
        }

        /// <summary>记下此刻（上一帧最终）的姿势，开始一次淡入。</summary>
        public bool Begin(Animator a, float duration)
        {
            if (a == null) return false;
            if (_bound != a || _bones.Count == 0) Bind(a);
            if (_bones.Count == 0) return false;
            for (int i = 0; i < _bones.Count; i++)
                _snapRot[i] = _bones[i] != null ? _bones[i].localRotation : Quaternion.identity;
            if (_hips != null) _snapHipsPos = _hips.localPosition;
            _dur = Mathf.Max(0.1f, duration);
            _t = 0f;
            return true;
        }

        public void Cancel() { _t = 1f; }

        /// <summary>在 LateUpdate 里每帧调。新姿势 = 此刻骨骼上的值（Animator 已经算完）。</summary>
        public void Apply(float dt)
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + dt / _dur);
            float w = _t * _t * (3f - 2f * _t); // smoothstep
            for (int i = 0; i < _bones.Count; i++)
            {
                var b = _bones[i];
                if (b == null) continue;
                b.localRotation = Quaternion.Slerp(_snapRot[i], b.localRotation, w);
            }
            if (_hips != null) _hips.localPosition = Vector3.Lerp(_snapHipsPos, _hips.localPosition, w);
        }
    }
}
