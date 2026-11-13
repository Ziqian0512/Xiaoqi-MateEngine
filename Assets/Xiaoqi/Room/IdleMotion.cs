using System;
using UnityEngine;
using VRM;

namespace Xiaoqi.Spike
{
    /// <summary>最小待机：放下 T-pose 手臂、呼吸、轻微晃头、眨眼。</summary>
    public class IdleMotion : MonoBehaviour
    {
        private Transform _lua, _rua, _chest, _head;
        private Quaternion _luaBase, _ruaBase, _chestBase, _headBase;
        private VRMBlendShapeProxy _proxy;
        private float _t;
        private float _blinkAt;
        private bool _blinking;

        public void Setup(GameObject root)
        {
            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                _lua = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                _rua = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _chest = anim.GetBoneTransform(HumanBodyBones.Chest);
                _head = anim.GetBoneTransform(HumanBodyBones.Head);
            }
            if (_lua != null) _luaBase = _lua.localRotation;
            if (_rua != null) _ruaBase = _rua.localRotation;
            if (_chest != null) _chestBase = _chest.localRotation;
            if (_head != null) _headBase = _head.localRotation;

            Debug.Log($"IDLE anim={(anim != null)} lua={(_lua != null)} rua={(_rua != null)} chest={(_chest != null)} head={(_head != null)}");

            _proxy = root.GetComponent<VRMBlendShapeProxy>();
            if (_proxy == null) _proxy = root.GetComponentInChildren<VRMBlendShapeProxy>();
            _blinkAt = Time.unscaledTime + UnityEngine.Random.Range(2.5f, 5f);
        }

        private void LateUpdate()
        {
            if (_lua == null && _rua == null) return;
            _t += Time.unscaledDeltaTime;

            // T-pose → 手臂自然下垂
            if (_lua != null) _lua.localRotation = _luaBase * Quaternion.Euler(0f, 0f, 68f);
            if (_rua != null) _rua.localRotation = _ruaBase * Quaternion.Euler(0f, 0f, -68f);

            // 呼吸
            if (_chest != null)
                _chest.localRotation = _chestBase * Quaternion.Euler(Mathf.Sin(_t * 1.4f) * 1.2f, 0f, 0f);
            // 轻微晃头
            if (_head != null)
                _head.localRotation = _headBase * Quaternion.Euler(Mathf.Sin(_t * 0.8f) * 1.5f, Mathf.Sin(_t * 0.6f) * 3f, 0f);

            // 眨眼
            if (_proxy != null)
            {
                var now = Time.unscaledTime;
                if (!_blinking && now >= _blinkAt)
                {
                    _blinking = true;
                    _blinkAt = now + 0.18f;
                }
                float v = 0f;
                if (_blinking)
                {
                    v = Mathf.Sin(Mathf.Clamp01((_blinkAt - now) / 0.18f) * Mathf.PI);
                    if (now >= _blinkAt)
                    {
                        _blinking = false;
                        _blinkAt = now + UnityEngine.Random.Range(3f, 6f);
                        v = 0f;
                    }
                }
                _proxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink), v);
            }
        }
    }
}

