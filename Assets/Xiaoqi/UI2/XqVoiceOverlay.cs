using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V6 桌宠态按住热键实时语音对话叠层（默认按住 N 键）：
    /// - 在桌宠头顶上方浮出梦幻蓝紫粉半透明毛玻璃对话流
    /// - 实时显示你说的话（上）与小祈的回复（下），最多保留最近 4 轮
    /// - 松开热键后自动发送并逐字流式回复 + 播放 TTS，静置 9 秒后优雅淡出
    /// </summary>
    public static class XqVoiceOverlay
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static VisualElement _box;
        private static VisualElement _micPill;
        private static Label _micLabel;
        private static VisualElement _turnsBox;
        private static VisualElement _hintPill;
        private static Label _hintLabel;
        private static bool _hooked;
        private static bool _keyWasDown;
        private static float _alpha;

        public static bool OverNow
        {
            get
            {
                if (_box == null || _box.panel == null || _box.style.display == DisplayStyle.None || _alpha < 0.05f) return false;
                var picked = _box.panel.Pick(XqPanelXf.FromScreen(_box.panel, Input.mousePosition));
                for (var e = picked; e != null; e = e.parent) if (e == _box) return true;
                return false;
            }
        }

        private static int HotkeyVk()
        {
            var cfg = XiaoqiRuntime.Config;
            string hk = cfg != null && !string.IsNullOrEmpty(cfg.voice_hotkey) ? cfg.voice_hotkey.Trim().ToUpperInvariant() : "N";
            if (hk == "SPACE") return 0x20;
            if (hk.Length == 1 && hk[0] >= 'A' && hk[0] <= 'Z') return (int)hk[0];
            return 0x4E; // 'N'
        }

        private static string HotkeyName()
        {
            var cfg = XiaoqiRuntime.Config;
            return cfg != null && !string.IsNullOrEmpty(cfg.voice_hotkey) ? cfg.voice_hotkey.Trim().ToUpperInvariant() : "N";
        }

        public static void Poll(VisualElement root)
        {
            if (root == null || root.panel == null) return;
            XqVoiceChatClient.Ensure();
            EnsureBuilt(root);

            if (XqMode.Current != XqView.Pet)
            {
                if (_keyWasDown)
                {
                    _keyWasDown = false;
                    XqVoiceChatClient.Ensure().StopRecordingAndSend();
                }
                _box.style.display = DisplayStyle.None;
                return;
            }

            // 监听全局/窗口热键（避免在输入框打字时误触发）
            bool keyDown = (GetAsyncKeyState(HotkeyVk()) & 0x8000) != 0;
            if (keyDown && !_keyWasDown)
            {
                _keyWasDown = true;
                XqVoiceChatClient.Ensure().StartRecording();
            }
            else if (!keyDown && _keyWasDown)
            {
                _keyWasDown = false;
                XqVoiceChatClient.Ensure().StopRecordingAndSend();
            }

            bool active = XqVoiceChatClient.IsRecording
                || XqVoiceChatClient.IsThinking
                || XqVoiceChatClient.IsStreaming
                || XqVoiceChatClient.IsPlayingTts
                || (Time.unscaledTime - XqVoiceChatClient.LastInteractionAt < 9f && XqVoiceChatClient.Turns.Count > 0);

            float targetAlpha = active ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, targetAlpha, Time.unscaledDeltaTime * (active ? 8f : 2.2f));
            if (_alpha <= 0.01f)
            {
                _box.style.display = DisplayStyle.None;
                return;
            }

            _box.style.display = DisplayStyle.Flex;
            _box.style.opacity = _alpha;
            PlaceAbovePet();
        }

        private static void EnsureBuilt(VisualElement root)
        {
            if (_box != null && _box.parent == root) return;
            _box = new VisualElement { name = "xq-voice-overlay", pickingMode = PickingMode.Ignore };
            _box.style.position = Position.Absolute;
            _box.style.width = 336f;
            _box.style.backgroundColor = Color.clear;
            _box.style.display = DisplayStyle.None;

            _micPill = new VisualElement { pickingMode = PickingMode.Ignore };
            _micPill.style.flexDirection = FlexDirection.Row;
            _micPill.style.alignItems = Align.Center;
            _micPill.style.paddingLeft = 10f;
            _micPill.style.paddingRight = 10f;
            _micPill.style.paddingTop = 5f;
            _micPill.style.paddingBottom = 5f;
            _micPill.style.marginBottom = 8f;
            _micPill.style.borderTopLeftRadius = 12f;
            _micPill.style.borderTopRightRadius = 12f;
            _micPill.style.borderBottomLeftRadius = 12f;
            _micPill.style.borderBottomRightRadius = 12f;
            _micPill.style.alignSelf = Align.FlexEnd;
            _micPill.style.backgroundColor = new Color(0.55f, 0.36f, 0.96f, 0.14f);

            _micLabel = new Label("🎙️ 按住 " + HotkeyName() + " 说话…");
            _micLabel.style.fontSize = 12f;
            _micLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _micLabel.style.color = new Color(0.42f, 0.22f, 0.72f, 1f);
            _micPill.Add(_micLabel);
            _box.Add(_micPill);

            _turnsBox = new VisualElement { pickingMode = PickingMode.Ignore };
            _box.Add(_turnsBox);

            _hintPill = new VisualElement { pickingMode = PickingMode.Ignore };
            _hintPill.style.marginTop = 6f;
            _hintLabel = new Label("松开自动发送 · 按住 " + HotkeyName() + " 继续说");
            _hintLabel.style.fontSize = 10f;
            _hintLabel.style.color = new Color(0.45f, 0.38f, 0.60f, 0.85f);
            _hintPill.Add(_hintLabel);
            _box.Add(_hintPill);

            root.Add(_box);

            if (!_hooked)
            {
                _hooked = true;
                XqVoiceChatClient.OnChanged += RefreshTurns;
            }
            RefreshTurns();
        }

        private static void RefreshTurns()
        {
            if (_turnsBox == null) return;
            _turnsBox.Clear();

            if (XqVoiceChatClient.IsRecording)
            {
                _micPill.style.display = DisplayStyle.Flex;
                _micPill.style.backgroundColor = new Color(0.96f, 0.45f, 0.71f, 0.22f);
                _micLabel.text = "● 正在听你说话：" + XqVoiceChatClient.LiveTranscript;
                _micLabel.style.color = new Color(0.75f, 0.18f, 0.50f, 1f);
            }
            else if (XqVoiceChatClient.IsPlayingTts)
            {
                _micPill.style.display = DisplayStyle.Flex;
                _micPill.style.backgroundColor = new Color(0.55f, 0.36f, 0.96f, 0.16f);
                _micLabel.text = string.Format("🔊 语音播放中 {0:0.0}s", XqVoiceChatClient.TtsRemainingSec);
                _micLabel.style.color = new Color(0.42f, 0.22f, 0.72f, 1f);
            }
            else
            {
                _micPill.style.display = DisplayStyle.None;
            }

            var turns = XqVoiceChatClient.Turns;
            int start = Mathf.Max(0, turns.Count - 2);
            for (int i = start; i < turns.Count; i++)
            {
                var t = turns[i];
                bool isLast = (i == turns.Count - 1);
                var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
                wrap.style.flexDirection = FlexDirection.Column;
                wrap.style.marginBottom = isLast ? 0f : 8f;

                // 用户气泡：靠右整齐柔紫气泡
                var uBubble = new VisualElement { pickingMode = PickingMode.Ignore };
                uBubble.style.alignSelf = Align.FlexEnd;
                uBubble.style.width = Length.Percent(88f);
                uBubble.style.paddingLeft = 12f; uBubble.style.paddingRight = 12f;
                uBubble.style.paddingTop = 6f; uBubble.style.paddingBottom = 6f;
                uBubble.style.marginBottom = 5f;
                uBubble.style.borderTopLeftRadius = 12f; uBubble.style.borderTopRightRadius = 12f;
                uBubble.style.borderBottomLeftRadius = 12f; uBubble.style.borderBottomRightRadius = 4f;
                uBubble.style.backgroundColor = new Color(0.55f, 0.36f, 0.96f, isLast ? 0.92f : 0.72f);
                var u = new Label(t.userText);
                u.style.fontSize = 12f;
                u.style.whiteSpace = WhiteSpace.Normal;
                u.style.color = Color.white;
                u.style.unityFontStyleAndWeight = FontStyle.Bold;
                uBubble.Add(u);
                wrap.Add(uBubble);

                string replyShown = (isLast && XqVoiceChatClient.IsStreaming && !string.IsNullOrEmpty(XqVoiceChatClient.LatestReplyVisible))
                    ? XqVoiceChatClient.LatestReplyVisible
                    : t.replyText;

                // 小祈回复气泡：靠左整齐粉白气泡
                var rBubble = new VisualElement { pickingMode = PickingMode.Ignore };
                rBubble.style.alignSelf = Align.FlexStart;
                rBubble.style.width = Length.Percent(88f);
                rBubble.style.paddingLeft = 12f; rBubble.style.paddingRight = 12f;
                rBubble.style.paddingTop = 7f; rBubble.style.paddingBottom = 7f;
                rBubble.style.borderTopLeftRadius = 12f; rBubble.style.borderTopRightRadius = 12f;
                rBubble.style.borderBottomRightRadius = 12f; rBubble.style.borderBottomLeftRadius = 4f;
                rBubble.style.backgroundColor = new Color(1f, 0.96f, 0.99f, isLast ? 0.95f : 0.74f);
                var r = new Label(replyShown);
                r.style.fontSize = 13f;
                r.style.unityFontStyleAndWeight = FontStyle.Bold;
                r.style.whiteSpace = WhiteSpace.Normal;
                r.style.color = new Color(0.18f, 0.11f, 0.30f, 1f);
                rBubble.Add(r);
                wrap.Add(rBubble);

                _turnsBox.Add(wrap);
            }

            if (_hintPill != null) _hintPill.style.display = DisplayStyle.None;
        }

        private static void PlaceAbovePet()
        {
            if (_box == null || _box.panel == null) return;
            var body = XqBodyAnchor.Stable();
            var bc = body.height >= 8f ? body.center : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var wa = XqMonitor.WorkArea(bc);
            float upp = Mathf.Max(0.01f, XqPanelXf.UnitsPerPx(_box.panel));
            float wPx = 336f / upp;
            float hPx = (!float.IsNaN(_box.resolvedStyle.height) && _box.resolvedStyle.height > 20f ? _box.resolvedStyle.height : 160f) / upp;

            float cx = body.height >= 8f ? body.center.x : wa.center.x;
            float topTarget = body.height >= 8f ? body.yMax + 14f + hPx : wa.center.y + 120f;
            cx = Mathf.Clamp(cx, wa.xMin + wPx * 0.5f + 12f, wa.xMax - wPx * 0.5f - 12f);
            topTarget = Mathf.Clamp(topTarget, wa.yMin + hPx + 12f, wa.yMax - 12f);

            var p = XqPanelXf.FromScreen(_box.panel, new Vector2(cx - wPx * 0.5f, topTarget));
            _box.style.left = p.x;
            _box.style.top = p.y;
        }
    }
}
