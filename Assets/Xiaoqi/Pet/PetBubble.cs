using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Xiaoqi.Spike
{
    public class PetBubble : MonoBehaviour
    {
        private class Item
        {
            public string text;
            public string media;
        }

        private static PetBubble _instance;
        private readonly Queue<Item> _queue = new Queue<Item>();
        private EmotionLineConfig _cfg;
        private HeadAnchor _head;
        private RectTransform _panel;
        private Text _text;
        private Item _current;
        private float _nextChar;
        private float _hideAt;
        private int _shown;
        private float _nextMouth;
        private GameObject _model;
        private UniversalBlendshapes _uni;

        private void Awake() { _instance = this; }

        private void Start()
        {
            _cfg = EmotionLineConfig.Load();
            Build();
        }

        public static void Enqueue(string text, string media)
        {
            if (_instance == null) _instance = FindFirstObjectByType<PetBubble>(FindObjectsInactive.Include);
            if (_instance == null || string.IsNullOrEmpty(text)) return;
            while (_instance._queue.Count >= _instance._cfg.bubble.queue) _instance._queue.Dequeue();
            _instance._queue.Enqueue(new Item { text = text, media = media });
        }

        private void Build()
        {
            var go = new GameObject("XiaoqiPetBubbleCanvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 310;

            var panel = new GameObject("PetBubble");
            panel.transform.SetParent(go.transform, false);
            var img = panel.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.94f);
            _panel = panel.GetComponent<RectTransform>();
            _panel.sizeDelta = new Vector2(320f, 70f);
            _panel.pivot = new Vector2(0.5f, 0f);

            var t = new GameObject("Text");
            t.transform.SetParent(panel.transform, false);
            _text = t.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 15;
            _text.color = new Color(0.15f, 0.15f, 0.18f);
            _text.alignment = TextAnchor.MiddleLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Truncate;
            var rt = _text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);
            panel.SetActive(false);
        }

        private void Update()
        {
            if (_head == null) _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            if (_current == null && _queue.Count > 0) Begin(_queue.Dequeue());
            if (_current == null) { if (_panel != null) _panel.gameObject.SetActive(false); return; }

            if (_panel != null)
            {
                _panel.gameObject.SetActive(true);
                var p = _head != null ? _head.ScreenPoint : new Vector2(Screen.width / 2f, Screen.height / 2f);
                var x = Mathf.Clamp(p.x, _panel.sizeDelta.x / 2 + 8, Screen.width - _panel.sizeDelta.x / 2 - 8);
                var y = Mathf.Clamp(p.y + 12f, _panel.sizeDelta.y + 8, Screen.height - 8);
                _panel.position = new Vector3(x, y, 0f);
            }

            if (_shown < _current.text.Length && Time.unscaledTime >= _nextChar)
            {
                _shown++;
                _nextChar = Time.unscaledTime + 1f / Mathf.Max(1f, _cfg.bubble.cps);
                _text.text = _current.text.Substring(0, _shown);
            }
            if (Time.unscaledTime >= _hideAt && _shown >= _current.text.Length)
            {
                _current = null;
                _text.text = "";
                return;
            }
            Mouth();
        }

        private void Begin(Item item)
        {
            _current = item;
            _shown = 0;
            _text.text = "";
            bool voice = IsVoice(item.media);
            var body = voice ? "（语音）" + item.text : item.text;
            _current.text = body;
            _nextChar = Time.unscaledTime;
            float sec = Mathf.Clamp(_current.text.Length * _cfg.bubble.per_char_sec,
                _cfg.bubble.min_sec, _cfg.bubble.max_sec);
            _hideAt = Time.unscaledTime + sec + 0.4f;
        }

        private bool IsVoice(string media)
        {
            if (string.IsNullOrEmpty(media)) return false;
            foreach (var v in _cfg.bubble.voice_media)
                if (media == v) return true;
            return false;
        }

        private void Mouth()
        {
            if (_current == null || !IsVoice(_current.media)) return;
            if (Time.unscaledTime < _nextMouth) return;
            _nextMouth = Time.unscaledTime + 1f / Mathf.Max(1f, _cfg.bubble.mouth_hz);
            if (_model == null)
            {
                var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
                _model = loader != null ? loader.GetCurrentModel() : null;
            }
            if (_model == null) return;
            _uni = _model.GetComponent<UniversalBlendshapes>();
            if (_uni == null) return;
            var t = _uni.GetType();
            foreach (var k in new[] { "A", "I", "U", "O" })
            {
                var f = t.GetField(k);
                if (f != null) f.SetValue(_uni, 0f);
            }
            var pick = new[] { "A", "I", "U", "O" }[Random.Range(0, 4)];
            var field = t.GetField(pick);
            if (field != null)
                field.SetValue(_uni, Random.Range(_cfg.bubble.mouth_w[0], _cfg.bubble.mouth_w[1]));
        }
    }
}
