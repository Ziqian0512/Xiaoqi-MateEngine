using UnityEngine;
using VRM;

namespace Xiaoqi.Spike
{
    /// <summary>用 TTS 音频的 RMS 驱动 VRM 元音口型（A/I/U/E/O）。</summary>
    public class XiaoqiLipSync : MonoBehaviour
    {
        private VRMBlendShapeProxy _proxy;
        private AudioSource _audio;
        private readonly float[] _samples = new float[256];
        private float _smoothed;

        public void Setup(GameObject root, AudioSource audio)
        {
            _proxy = root.GetComponent<VRMBlendShapeProxy>();
            if (_proxy == null) _proxy = root.GetComponentInChildren<VRMBlendShapeProxy>();
            _audio = audio;
        }

        private void LateUpdate()
        {
            if (_proxy == null || _audio == null) return;

            float level = 0f;
            if (_audio.isPlaying)
            {
                _audio.GetOutputData(_samples, 0);
                float sum = 0f;
                for (int i = 0; i < _samples.Length; i++) sum += _samples[i] * _samples[i];
                level = Mathf.Sqrt(sum / _samples.Length);
            }
            _smoothed = Mathf.Lerp(_smoothed, Mathf.Clamp01(level * 8f), Time.unscaledDeltaTime * 18f);

            // 没有 viseme 数据时，用 A 为主、轻微 I/E 混合作近似
            Set(BlendShapePreset.A, _smoothed * 0.85f);
            Set(BlendShapePreset.I, _smoothed * 0.15f);
            Set(BlendShapePreset.U, _smoothed * 0.08f);
            Set(BlendShapePreset.E, _smoothed * 0.12f);
            Set(BlendShapePreset.O, _smoothed * 0.06f);
        }

        private void Set(BlendShapePreset preset, float value)
        {
            _proxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(preset), Mathf.Clamp01(value));
        }
    }
}
