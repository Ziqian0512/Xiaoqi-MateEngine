using System.Collections.Generic;
using UnityEngine;

namespace Xiaoqi.Spike
{
    public class EmotionLineTuner : MonoBehaviour
    {
        private static EmotionLineTuner _instance;
        private static bool _open;
        private EmotionLineConfig _cfg;

        private void Awake()
        {
            _instance = this;
            _cfg = EmotionLineConfig.Load();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9)) Toggle();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) _open = false;
        }

        public static void Toggle()
        {
            if (_instance == null) _instance = FindFirstObjectByType<EmotionLineTuner>(FindObjectsInactive.Include);
            _open = _instance != null && !_open;
        }

        private void OnGUI()
        {
            if (!_open || _cfg == null) return;
            var r = new Rect(20, 80, 460, Mathf.Min(760, Screen.height - 100));
            GUI.Box(r, "情感线调参 · F9");
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 26, r.width - 24, r.height - 36));

            GUILayout.Label("当前：top=" + (XiaoqiLink.Current?.face?.top ?? "-")
                + " level=" + (XiaoqiLink.Current?.face?.level ?? 0).ToString("0.00")
                + " sleepy=" + (XiaoqiLink.Current?.body?.sleepy ?? 0).ToString("0")
                + " stamina=" + (XiaoqiLink.Current?.body?.stamina ?? 0).ToString("0"));
            GUILayout.Space(6);
            GUILayout.Label("模拟冒一下");
            GUILayout.BeginHorizontal();
            foreach (var e in new[] { "开心", "得意", "害羞", "委屈", "生气", "担心", "想念", "失望", "平常" })
                if (GUILayout.Button(e, GUILayout.Width(44))) XiaoqiFace.DebugSpike(e);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            _cfg.face.base_gain = Slider("base_gain", _cfg.face.base_gain, 0f, 1f);
            _cfg.face.spike_gain = Slider("spike_gain", _cfg.face.spike_gain, 0f, 1.5f);
            _cfg.face.spike_hold = Slider("spike_hold", _cfg.face.spike_hold, 0.2f, 6f);
            _cfg.face.spike_fade = Slider("spike_fade", _cfg.face.spike_fade, 0.2f, 6f);
            _cfg.body.sleepy_lid = Slider("sleepy_lid", _cfg.body.sleepy_lid, 0f, 0.8f);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存")) _cfg.Save();
            if (GUILayout.Button("恢复默认"))
            {
                _cfg.ResetSaved();
                _cfg = EmotionLineConfig.Load();
            }
            if (GUILayout.Button("关闭")) _open = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static float Slider(string label, float v, float a, float b)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            var n = GUILayout.HorizontalSlider(v, a, b);
            GUILayout.Label(n.ToString("0.00"), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            return n;
        }
    }
}
