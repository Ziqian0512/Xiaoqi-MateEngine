using UnityEngine;
using VRM;

namespace Xiaoqi.Spike
{
    public class XiaoqiMouth : MonoBehaviour
    {
        private VRMBlendShapeProxy _proxy;
        private BubbleUI _bubble;
        private float _t;

        public void Setup(GameObject root, BubbleUI bubble)
        {
            _proxy = root.GetComponent<VRMBlendShapeProxy>();
            if (_proxy == null) _proxy = root.GetComponentInChildren<VRMBlendShapeProxy>();
            _bubble = bubble;
        }

        private void Update()
        {
            if (_proxy == null) return;
            if (_bubble == null || !_bubble.IsVisible)
            {
                Set(BlendShapePreset.A, 0f);
                Set(BlendShapePreset.I, 0f);
                Set(BlendShapePreset.U, 0f);
                return;
            }

            _t += Time.deltaTime * 8f;
            var a = Mathf.Abs(Mathf.Sin(_t)) * 0.55f;
            var i = Mathf.Abs(Mathf.Sin(_t * 0.7f + 1.1f)) * 0.35f;
            Set(BlendShapePreset.A, a);
            Set(BlendShapePreset.I, i);
            Set(BlendShapePreset.U, 0f);
        }

        private void Set(BlendShapePreset preset, float value)
        {
            _proxy.SetValue(BlendShapeKey.CreateFromPreset(preset), value);
        }
    }
}
