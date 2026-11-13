using System.Text.RegularExpressions;
using UnityEngine;
using VRM;

namespace Xiaoqi.Spike
{
    /// <summary>把 Python 的 label 映射到 VRM 表情。沿用 web 版 EMOTION_MAP 的逻辑。</summary>
    public class XiaoqiExpression : MonoBehaviour
    {
        private VRMBlendShapeProxy _proxy;

        public void Setup(GameObject root)
        {
            _proxy = root.GetComponent<VRMBlendShapeProxy>();
            if (_proxy == null) _proxy = root.GetComponentInChildren<VRMBlendShapeProxy>();
            if (_proxy == null) Debug.LogWarning("XiaoqiExpression: no VRMBlendShapeProxy");
        }

        public void Apply(string label)
        {
            if (_proxy == null) return;
            Clear();

            label = label ?? "";
            if (Regex.IsMatch(label, "困|蔫|累|没精神"))
            {
                Set(BlendShapePreset.Sorrow, 0.38f);
                Set(BlendShapePreset.Neutral, 0.15f);
            }
            else if (Regex.IsMatch(label, "开心|兴奋|得意|高兴|好耶"))
            {
                Set(BlendShapePreset.Joy, 0.82f);
                Set(BlendShapePreset.Fun, 0.25f);
            }
            else if (Regex.IsMatch(label, "生气|烦|气"))
            {
                Set(BlendShapePreset.Angry, 0.8f);
            }
            else if (Regex.IsMatch(label, "委屈|难过|低落|想哭|伤心"))
            {
                Set(BlendShapePreset.Sorrow, 0.72f);
            }
            else if (Regex.IsMatch(label, "害羞|脸红|不好意思"))
            {
                Set(BlendShapePreset.Joy, 0.32f);
                Set(BlendShapePreset.Neutral, 0.2f);
            }
            else if (Regex.IsMatch(label, "惊讶|震惊|吃惊"))
            {
                Set(BlendShapePreset.Fun, 0.55f);
                Set(BlendShapePreset.Joy, 0.28f);
            }
            else
            {
                Set(BlendShapePreset.Neutral, 0.2f);
            }
        }

        private void Clear()
        {
            foreach (BlendShapePreset p in new[]
                     { BlendShapePreset.Joy, BlendShapePreset.Angry, BlendShapePreset.Sorrow,
                       BlendShapePreset.Fun, BlendShapePreset.Neutral })
            {
                _proxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(p), 0f);
            }
        }

        private void Set(BlendShapePreset preset, float value)
        {
            _proxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(preset), Mathf.Clamp01(value));
        }
    }
}
