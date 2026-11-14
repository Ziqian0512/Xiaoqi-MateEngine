using UnityEngine;
using UnityEngine.UI;

namespace Xiaoqi.Spike
{
    /// <summary>头顶气泡的最小版本：屏幕顶部一条可自动隐藏的对话条。</summary>
    public class BubbleUI : MonoBehaviour
    {
        private CanvasGroup group;
        private Text label;
        private float hideAt;

        private void Awake()
        {
            var canvasGo = new GameObject("BubbleCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(420, 680);
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var panel = new GameObject("BubblePanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.78f);
            rt.anchorMax = new Vector2(0.92f, 0.97f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var textGo = new GameObject("BubbleText");
            textGo.transform.SetParent(panel.transform, false);
            label = textGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.96f, 0.94f, 0.92f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.06f, 0.08f);
            trt.anchorMax = new Vector2(0.94f, 0.92f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        public bool IsVisible => group != null && group.alpha > 0.01f;

        public void Show(string text, float seconds = 4f)
        {
            if (label == null) return;
            label.text = text;
            group.alpha = 1f;
            hideAt = Time.time + seconds;
        }

        private void Update()
        {
            if (group == null || hideAt <= 0f) return;
            if (Time.time >= hideAt)
            {
                hideAt = 0f;
                group.alpha = 0f;
            }
        }
    }
}
