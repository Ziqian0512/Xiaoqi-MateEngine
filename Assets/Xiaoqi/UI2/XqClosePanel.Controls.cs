using System;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V13 近景控件部分：
    /// - 前景环绕艺术字贴图（Resources/XqArtLogo.png：AMAMIYA Inori ✦）
    /// - 左下统一蓝紫粉毛玻璃形态切换坞（桌宠 / 近景 / 工作台 / ⚙）
    /// - 正底加长 Search 风格对话条（渐变图标 + 输入框 + 语音 + 发送）
    /// </summary>
    public partial class XqClosePanel
    {
        VisualElement _modeDock, _btnSet;
        VisualElement _chatDock, _chatInputBox, _chatMicBtn, _chatSendBtn;
        TextField _chatInput;
        Label _chatMicLbl, _chatSendLbl;
        bool _chatHooked;

        void BuildRail() { }

        /// <summary>⑦ 前景环绕艺术字贴图（无框，AMAMIYA Inori ✦）。</summary>
        void BuildPlate()
        {
            _plate = new VisualElement { name = "xq-close-plate", pickingMode = PickingMode.Ignore };
            _plate.style.alignItems = Align.Center;
            _plate.style.justifyContent = Justify.Center;
            _plate.style.backgroundColor = Color.clear;
            _plate.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(-2f, AngleUnit.Degree));

            var tex = Resources.Load<Texture2D>("XqArtLogo");
            if (tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                img.style.width = Length.Percent(100f);
                img.style.height = Length.Percent(100f);
                _plate.Add(img);
            }
            else
            {
                var fb = new Label("AMAMIYA Inori ✦") { pickingMode = PickingMode.Ignore };
                fb.style.color = new Color(0.45f, 0.25f, 0.88f, 1f);
                fb.style.unityFontStyleAndWeight = FontStyle.Bold;
                fb.style.fontSize = 24f;
                _plate.Add(fb);
            }

            Add("plate", XqCloseLayout.PlateL, XqCloseLayout.PlateT, XqCloseLayout.PlateW, XqCloseLayout.PlateH);
            _host.Add(_plate);
        }

        /// <summary>⑧⑨ 右下角统一三态切换胶囊（与工作台/桌宠一致）：🐾 桌宠 | ✦ 近景(高亮) | 📱 工作台。</summary>
        void BuildButtons()
        {
            _modeDock = new VisualElement { name = "xq-close-mode-dock", pickingMode = PickingMode.Position };
            _modeDock.style.flexDirection = FlexDirection.Row;
            _modeDock.style.alignItems = Align.Center;
            _modeDock.style.justifyContent = Justify.SpaceBetween;
            _modeDock.style.backgroundColor = new Color(0.99f, 0.98f, 1f, 0.92f);
            _modeDock.style.paddingLeft = 4f;
            _modeDock.style.paddingRight = 4f;
            _modeDock.style.paddingTop = 4f;
            _modeDock.style.paddingBottom = 4f;
            SetAllBorder(_modeDock, 1f, new Color(0.88f, 0.82f, 0.99f, 0.96f));
            SetRadius(_modeDock, 18f);

            _btnPet = MakeModePill("🐾 桌宠", false);
            _btnPet.name = "xq-close-btn-pet";
            _btnPet.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (_lastClickFrame == Time.frameCount) return;
                _lastClickFrame = Time.frameCount;
                XqMode.Set(XqView.Pet);
            });

            var btnClose = MakeModePill("✦ 近景", true);

            _btnBench = MakeModePill("📱 工作台", false);
            _btnBench.name = "xq-close-btn-bench";
            bool benchOn = XqMode.BenchAvailable;
            if (!benchOn) _btnBench.style.opacity = 0.45f;
            _tipBench = new Label(benchOn ? "打开工作台" : "还没开放") { pickingMode = PickingMode.Ignore };
            _tipBench.AddToClassList("xq-cl-tip");
            _btnBench.Add(_tipBench);
            _btnBench.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (!XqMode.BenchAvailable) return;
                if (_lastClickFrame == Time.frameCount) return;
                _lastClickFrame = Time.frameCount;
                XqMode.Set(XqView.Bench);
            });

            _modeDock.Add(_btnPet);
            _modeDock.Add(btnClose);
            _modeDock.Add(_btnBench);

            Add("mode-dock", XqCloseLayout.BtnPetL, XqCloseLayout.BtnTop, 206f, 36f);
            XqLiquidGlass.ApplyAcrylicB(_modeDock);
            _host.Add(_modeDock);
        }

        static VisualElement MakeModePill(string label, bool active)
        {
            var btn = new VisualElement { name = "xq-mode-pill", pickingMode = PickingMode.Position };
            btn.style.paddingLeft = 9f;
            btn.style.paddingRight = 9f;
            btn.style.height = 26f;
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.Center;
            SetRadius(btn, 13f);
            btn.style.backgroundColor = active
                ? new Color(0.58f, 0.42f, 0.96f, 0.96f)
                : Color.clear;
            var lbl = new Label(label) { pickingMode = PickingMode.Ignore };
            lbl.AddToClassList("xq-cl-btn-word");
            lbl.style.color = active ? Color.white : new Color(0.36f, 0.28f, 0.56f, 0.92f);
            lbl.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            lbl.style.fontSize = 10.5f;
            btn.Add(lbl);
            return btn;
        }

        /// <summary>正底加长 Search 风格对话条（单条清爽，回复走肩侧独立气泡）。</summary>
        void BuildChatDock()
        {
            XqVoiceChatClient.Ensure();
            _chatDock = new VisualElement { name = "xq-close-chat-dock", pickingMode = PickingMode.Position };
            _chatDock.style.flexDirection = FlexDirection.Row;
            _chatDock.style.alignItems = Align.Center;
            _chatDock.style.backgroundColor = new Color(0.99f, 0.98f, 1f, 0.92f);
            _chatDock.style.paddingLeft = 8f;
            _chatDock.style.paddingRight = 6f;
            _chatDock.style.paddingTop = 3f;
            _chatDock.style.paddingBottom = 3f;
            SetAllBorder(_chatDock, 1f, new Color(0.88f, 0.82f, 0.99f, 0.96f));
            SetRadius(_chatDock, 14f);

            var lead = new VisualElement { pickingMode = PickingMode.Ignore };
            lead.style.width = 28f; lead.style.height = 28f;
            lead.style.alignItems = Align.Center; lead.style.justifyContent = Justify.Center;
            lead.style.marginRight = 8f;
            lead.style.backgroundColor = new Color(0.68f, 0.45f, 0.96f, 0.95f);
            SetRadius(lead, 10f);
            var leadLbl = new Label("💬") { pickingMode = PickingMode.Ignore };
            leadLbl.style.color = Color.white;
            leadLbl.style.fontSize = 13f;
            lead.Add(leadLbl);
            _chatDock.Add(lead);

            _chatInput = new TextField { pickingMode = PickingMode.Position };
            _chatInput.value = "";
            _chatInput.style.color = new Color(0.27f, 0.19f, 0.45f, 1f);
            _chatInput.style.flexGrow = 1f;
            _chatInput.style.flexShrink = 1f;
            _chatInput.style.backgroundColor = Color.clear;
            _chatInput.style.borderLeftWidth = 0;
            _chatInput.style.borderRightWidth = 0;
            _chatInput.style.borderTopWidth = 0;
            _chatInput.style.borderBottomWidth = 0;
            _chatInput.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    e.StopPropagation();
                    SubmitChatInput();
                }
            });
            _chatDock.Add(_chatInput);

            _chatMicBtn = new VisualElement { pickingMode = PickingMode.Position };
            _chatMicBtn.style.backgroundColor = new Color(0.94f, 0.90f, 1f, 0.95f);
            _chatMicBtn.style.paddingLeft = 10f;
            _chatMicBtn.style.paddingRight = 10f;
            _chatMicBtn.style.paddingTop = 5f;
            _chatMicBtn.style.paddingBottom = 5f;
            _chatMicBtn.style.marginRight = 6f;
            SetRadius(_chatMicBtn, 10f);
            _chatMicLbl = new Label("语音") { pickingMode = PickingMode.Ignore };
            _chatMicLbl.style.color = new Color(0.36f, 0.24f, 0.62f, 1f);
            _chatMicLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _chatMicBtn.Add(_chatMicLbl);
            _chatMicBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                var vc = XqVoiceChatClient.Ensure();
                if (XqVoiceChatClient.IsRecording) vc.StopRecordingAndSend();
                else vc.StartRecording();
                RefreshChatDock();
            });
            _chatDock.Add(_chatMicBtn);

            _chatSendBtn = new VisualElement { pickingMode = PickingMode.Position };
            _chatSendBtn.style.backgroundColor = new Color(0.62f, 0.42f, 0.96f, 0.96f);
            _chatSendBtn.style.paddingLeft = 14f;
            _chatSendBtn.style.paddingRight = 14f;
            _chatSendBtn.style.paddingTop = 5f;
            _chatSendBtn.style.paddingBottom = 5f;
            SetRadius(_chatSendBtn, 10f);
            _chatSendLbl = new Label("发送") { pickingMode = PickingMode.Ignore };
            _chatSendLbl.style.color = Color.white;
            _chatSendLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _chatSendBtn.Add(_chatSendLbl);
            _chatSendBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                SubmitChatInput();
            });
            _chatDock.Add(_chatSendBtn);

            Add("chat-dock", XqCloseLayout.ChatL, XqCloseLayout.ChatT, XqCloseLayout.ChatW, XqCloseLayout.ChatH);
            XqLiquidGlass.ApplyAcrylicB(_chatDock);
            _host.Add(_chatDock);

            if (!_chatHooked)
            {
                _chatHooked = true;
                XqVoiceChatClient.OnChanged += RefreshChatDock;
            }
            RefreshChatDock();
        }

        void SubmitChatInput()
        {
            if (_chatInput == null) return;
            string txt = _chatInput.value;
            if (string.IsNullOrWhiteSpace(txt)) return;
            if (XqVoiceChatClient.IsThinking || XqVoiceChatClient.IsRecognizing || XqVoiceChatClient.IsRecording) return;
            _chatInput.value = "";
            XqVoiceChatClient.Ensure().SendText(txt, false);
        }

        void RefreshChatDock()
        {
            if (_chatMicBtn != null)
            {
                _chatMicBtn.style.backgroundColor = XqVoiceChatClient.IsRecording
                    ? new Color(0.96f, 0.42f, 0.72f, 0.30f)
                    : new Color(0.94f, 0.90f, 1f, 0.98f);
                if (_chatMicLbl != null)
                    _chatMicLbl.text = XqVoiceChatClient.IsRecording ? "录音中…" : "语音";
            }

            if (_sayText != null)
            {
                if (!string.IsNullOrEmpty(XqVoiceChatClient.Status))
                    _sayText.text = XqVoiceChatClient.Status;
                else if (XqVoiceChatClient.IsRecognizing)
                    _sayText.text = "正在识别语音…";
                else if (XqVoiceChatClient.IsRecording)
                    _sayText.text = "● 正在听你说话：" + XqVoiceChatClient.LiveTranscript;
                else if (!string.IsNullOrEmpty(XqVoiceChatClient.LatestReplyVisible))
                    _sayText.text = "“" + XqVoiceChatClient.LatestReplyVisible + (XqVoiceChatClient.IsStreaming ? "▍" : "”");
                else if (XqVoiceChatClient.IsThinking)
                    _sayText.text = "小祈正在想怎么回你…";
            }

            if (_sayText != null)
            {
                var config = XiaoqiRuntime.Config;
                _sayText.style.display = (config == null || config.close_subtitles || !string.IsNullOrEmpty(XqVoiceChatClient.Status) || XqVoiceChatClient.IsThinking || XqVoiceChatClient.IsRecognizing || XqVoiceChatClient.IsRecording) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_sayTtsLbl != null)
            {
                _sayTtsLbl.text = XqVoiceChatClient.IsPlayingTts
                    ? string.Format("♪ {0:0.0}s", XqVoiceChatClient.TtsRemainingSec)
                    : "♪ 播报";
            }
        }

        static void AddText(VisualElement parent, string word)
        {
            var label = new Label(word) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("xq-cl-btn-word");
            parent.Add(label);
        }

        void AssertFaceZone()
        {
            foreach (var s in _slots)
            {
                var n = XqCloseLayout.Norm(s.left, s.top, s.wStage, s.hStage);
                if (XqCloseLayout.InFaceZone(n))
                    Debug.LogWarning("XQ_CLOSE violate " + s.name
                        + " rect=" + Fmt(n) + " face=" + Fmt(XqCloseLayout.FaceRect));
                else
                    Debug.Log("XQ_CLOSE ok " + s.name + " rect=" + Fmt(n));
            }
        }

        static string Fmt(Rect r)
        {
            return r.x.ToString("0.000") + "," + r.y.ToString("0.000") + " "
                + r.width.ToString("0.000") + "x" + r.height.ToString("0.000");
        }

        void PlaceRail(float u) { }

        void PlacePlate(float u) { }

        void PlaceButtons(float u)
        {
            if (_modeDock != null)
            {
                SetRadius(_modeDock, 18f * u);
                foreach (var child in _modeDock.Children())
                {
                    if (child.name != "xq-mode-pill" && child.name != "xq-close-btn-pet" && child.name != "xq-close-btn-bench") continue;
                    child.style.height = 26f * u;
                    child.style.paddingLeft = 9f * u;
                    child.style.paddingRight = 9f * u;
                    SetRadius(child, 13f * u);
                    var w = child.Q(className: "xq-cl-btn-word");
                    if (w != null) w.style.fontSize = 10.5f * u;
                }
            }
            if (_tipBench != null) _tipBench.style.fontSize = 11f * u;
        }

        void PlaceChatDock(float u)
        {
            if (_chatDock == null) return;
            SetRadius(_chatDock, 16f * u);
            if (_chatMicLbl != null) _chatMicLbl.style.fontSize = 10.5f * u;
            if (_chatSendLbl != null) _chatSendLbl.style.fontSize = 11f * u;
            if (_chatInput != null) _chatInput.style.fontSize = 11.5f * u;
        }

        static void SetRadius(VisualElement el, float r)
        {
            el.style.borderTopLeftRadius = r;
            el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r;
            el.style.borderBottomRightRadius = r;
        }
    }
}
