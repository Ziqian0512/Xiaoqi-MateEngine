using System;
using UnityEngine;
using UnityEngine.UI;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    public class HeadAnchor : MonoBehaviour
    {
        public Vector2 ScreenPoint { get; private set; }

        private Transform _head;
        private Canvas _canvas;
        private Text _label;
        private float _nextFind;

        public void SetLabel(string text)
        {
            if (_label == null) return;
            if (Xiaoqi.UI2.XqGlassMenu.Enabled) text = "";
            if (Xiaoqi.UI2.XqGlassMenu.Enabled) text = "";
            _label.text = text ?? "";
            _label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private void Update()
        {
            if (_head == null && Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 1f;
                var a = PetHost.I != null ? PetHost.I.Animator : null;
                if (a != null && a.isHuman) _head = a.GetBoneTransform(HumanBodyBones.Head);
            }
            if (_head == null) return;
            if (_canvas == null) Build();

            var cam = Camera.main;
            if (cam == null) return;
            var sp = cam.WorldToScreenPoint(_head.position + Vector3.up * 0.28f);
            if (sp.z < 0) { if (_label != null) _label.gameObject.SetActive(false); return; }
            ScreenPoint = new Vector2(sp.x, sp.y);
            var rt = _label.rectTransform;
            var size = rt.sizeDelta;
            var x = Mathf.Clamp(sp.x, size.x / 2 + 8, Screen.width - size.x / 2 - 8);
            var y = Mathf.Clamp(sp.y, size.y / 2 + 8, Screen.height - size.y / 2 - 8);
            rt.position = new Vector3(x, y, 0f);
        }

        private void Build()
        {
            var go = new GameObject("XiaoqiHeadCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;
            var t = new GameObject("XiaoqiPresenceLabel");
            t.transform.SetParent(go.transform, false);
            _label = t.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 14;
            _label.color = Color.white;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = t.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.08f, 0.1f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
            var rt = _label.rectTransform;
            rt.sizeDelta = new Vector2(360f, 24f);
            rt.pivot = new Vector2(0.5f, 0f);
            _label.gameObject.SetActive(false);
        }
    }
}
