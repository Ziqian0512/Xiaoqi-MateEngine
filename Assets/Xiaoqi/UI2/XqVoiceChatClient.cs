using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V6 统一对话与语音客户端：
    /// - 文本对话：优先请求 Brain /api/chat，离线或假数据时平滑回退本地小祈口吻回复
    /// - 麦克风录音 + STT：支持按住热键或点击麦克风录音，编码 16kHz WAV 并预留 /api/voice/stt 接口
    /// - 逐字流式输出 + TTS 语音播报：支持 /api/voice/preview 音频拉取并在本地 AudioSource 播放
    /// </summary>
    public class XqVoiceChatClient : MonoBehaviour
    {
        public struct Turn
        {
            public string userText;
            public string replyText;
            public bool fromVoice;
            public float at;
        }

        [Serializable]
        private class ChatReq { public string text; public string source; }

        [Serializable]
        private class ChatResp { public string reply; public string text; public string audio_url; }

        [Serializable]
        private class SttResp { public string text; public string transcript; }

        static XqVoiceChatClient _i;
        public static XqVoiceChatClient Ensure()
        {
            if (_i != null) return _i;
            var go = new GameObject("XqVoiceChatClient");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<XqVoiceChatClient>();
            _i._audio = go.AddComponent<AudioSource>();
            _i._audio.playOnAwake = false;
            return _i;
        }

        public static readonly List<Turn> Turns = new List<Turn>();
        public static string LiveTranscript = "";
        public static string LatestReplyFull = "";
        public static string LatestReplyVisible = "";
        public static bool IsRecording { get; private set; }
        public static bool IsThinking { get; private set; }
        public static bool IsStreaming { get; private set; }
        public static bool IsPlayingTts { get { return _i != null && _i._audio != null && _i._audio.isPlaying; } }
        public static float TtsRemainingSec
        {
            get
            {
                if (_i == null || _i._audio == null || !_i._audio.isPlaying || _i._audio.clip == null) return 0f;
                return Mathf.Max(0f, _i._audio.clip.length - _i._audio.time);
            }
        }
        public static float LastInteractionAt { get; private set; } = -999f;

        public static event Action OnChanged;

        private AudioSource _audio;
        private AudioClip _micClip;
        private string _micDevice;
        private float _recStartTime;
        private Coroutine _streamCo;
        private Coroutine _ttsCo;
        private int _offlineIdx;

        private static readonly string[] OfflineReplies =
        {
            "嗯哼？我在听呢，刚才还以为你在忙工作~",
            "那我押零号大坝——水泥味，猜错可不准笑我哦~",
            "收到收到！今天也要记得多喝点温水呀，我就在这儿陪着你。",
            "哇，这个想法挺可爱的嘛，要不要我记到今天的手账里？",
            "好呀好呀，随时喊我一声就行，我一直都在~"
        };

        public static void NotifyChanged()
        {
            LastInteractionAt = Time.unscaledTime;
            if (OnChanged != null) OnChanged();
        }

        public void StartRecording()
        {
            if (IsRecording) return;
            IsRecording = true;
            _recStartTime = Time.unscaledTime;
            LiveTranscript = "正在聆听你的声音…";
            try
            {
                if (Microphone.devices != null && Microphone.devices.Length > 0)
                {
                    _micDevice = Microphone.devices[0];
                    _micClip = Microphone.Start(_micDevice, false, 20, 16000);
                }
                else
                {
                    _micDevice = null;
                    _micClip = null;
                    LiveTranscript = "正在聆听…（未检测到麦克风，松开将发送模拟语音）";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_VOICE mic start: " + e.Message);
                _micClip = null;
            }
            NotifyChanged();
        }

        public void StopRecordingAndSend()
        {
            if (!IsRecording) return;
            IsRecording = false;
            float dur = Mathf.Max(0.2f, Time.unscaledTime - _recStartTime);
            byte[] wav = null;
            try
            {
                if (!string.IsNullOrEmpty(_micDevice) && _micClip != null)
                {
                    int pos = Microphone.GetPosition(_micDevice);
                    Microphone.End(_micDevice);
                    if (pos > 800) wav = EncodeWav(_micClip, pos);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_VOICE mic stop: " + e.Message);
            }

            StartCoroutine(ProcessSttAndSend(wav, dur));
        }

        private IEnumerator ProcessSttAndSend(byte[] wav, float dur)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg != null ? cfg.server : "") ?? "";
            string recognized = null;

            if (wav != null && !string.IsNullOrEmpty(server) && !server.Contains("127.0.0.1:0"))
            {
                LiveTranscript = "正在识别语音…";
                NotifyChanged();
                string sttUrl = server.TrimEnd('/') + "/api/voice/stt";
                using (var req = new UnityWebRequest(sttUrl, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(wav);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "audio/wav");
                    if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                    req.timeout = 8;
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
                    {
                        try
                        {
                            var sr = JsonUtility.FromJson<SttResp>(req.downloadHandler.text);
                            if (sr != null) recognized = !string.IsNullOrEmpty(sr.text) ? sr.text : sr.transcript;
                        }
                        catch { }
                    }
                }
            }

            if (string.IsNullOrEmpty(recognized))
            {
                recognized = string.Format("（语音 {0:0.0}s）小祈，今天过得怎么样？", dur);
            }
            LiveTranscript = recognized;
            NotifyChanged();
            SendText(recognized, true);
        }

        public void SendText(string userText, bool fromVoice = false)
        {
            if (string.IsNullOrWhiteSpace(userText)) return;
            userText = userText.Trim();
            LiveTranscript = userText;
            IsThinking = true;

            var turn = new Turn
            {
                userText = userText,
                replyText = "……",
                fromVoice = fromVoice,
                at = Time.unscaledTime
            };
            Turns.Add(turn);
            if (Turns.Count > 4) Turns.RemoveAt(0);
            NotifyChanged();
            StartCoroutine(RequestReply(userText, Turns.Count - 1));
        }

        private IEnumerator RequestReply(string userText, int turnIndex)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg != null ? cfg.server : "") ?? "";
            string reply = null;
            string audioUrl = null;

            if (!string.IsNullOrEmpty(server))
            {
                string chatUrl = server.TrimEnd('/') + "/api/chat";
                string json = JsonUtility.ToJson(new ChatReq { text = userText, source = "desktop" });
                XqHttp.Resp resp = default(XqHttp.Resp);
                yield return XqHttp.PostJson(chatUrl, cfg != null ? cfg.token : "", json, 8, r => resp = r);
                if (resp.ok && !string.IsNullOrEmpty(resp.text))
                {
                    try
                    {
                        var cr = JsonUtility.FromJson<ChatResp>(resp.text);
                        if (cr != null)
                        {
                            reply = !string.IsNullOrEmpty(cr.reply) ? cr.reply : cr.text;
                            audioUrl = cr.audio_url;
                        }
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(reply))
            {
                reply = OfflineReplies[_offlineIdx % OfflineReplies.Length];
                _offlineIdx++;
            }

            IsThinking = false;
            if (turnIndex >= 0 && turnIndex < Turns.Count)
            {
                var t = Turns[turnIndex];
                t.replyText = reply;
                Turns[turnIndex] = t;
            }
            StartStream(reply);

            if (cfg == null || cfg.voice_tts_auto)
            {
                PlayTts(reply, audioUrl);
            }
        }

        private void StartStream(string full)
        {
            LatestReplyFull = full ?? "";
            LatestReplyVisible = "";
            if (_streamCo != null) StopCoroutine(_streamCo);
            _streamCo = StartCoroutine(StreamCharacters(LatestReplyFull));
        }

        private IEnumerator StreamCharacters(string full)
        {
            IsStreaming = true;
            for (int i = 1; i <= full.Length; i++)
            {
                LatestReplyVisible = full.Substring(0, i);
                NotifyChanged();
                yield return new WaitForSecondsRealtime(0.032f);
            }
            IsStreaming = false;
            NotifyChanged();
        }

        public void PlayTts(string text, string explicitAudioUrl = null)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg != null && cfg.muted) return;
            if (_ttsCo != null) StopCoroutine(_ttsCo);
            _ttsCo = StartCoroutine(FetchAndPlayTts(text, explicitAudioUrl));
        }

        private IEnumerator FetchAndPlayTts(string text, string explicitAudioUrl)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg != null ? cfg.server : "") ?? "";
            string url = explicitAudioUrl;
            if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(server))
            {
                url = server.TrimEnd('/') + "/api/voice/preview?text=" + UnityWebRequest.EscapeURL(text ?? "");
            }
            if (string.IsNullOrEmpty(url)) yield break;

            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip != null && _audio != null)
                    {
                        _audio.volume = cfg != null ? Mathf.Clamp01(cfg.volume) : 1f;
                        _audio.clip = clip;
                        _audio.Play();
                        NotifyChanged();
                    }
                }
            }
        }

        private static byte[] EncodeWav(AudioClip clip, int sampleCount)
        {
            int channels = clip.channels;
            int freq = clip.frequency;
            float[] data = new float[sampleCount * channels];
            clip.GetData(data, 0);

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                int byteRate = freq * channels * 2;
                int dataLen = data.Length * 2;
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataLen);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)channels);
                bw.Write(freq);
                bw.Write(byteRate);
                bw.Write((short)(channels * 2));
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataLen);
                for (int i = 0; i < data.Length; i++)
                {
                    short s = (short)Mathf.Clamp(data[i] * 32767f, -32768f, 32767f);
                    bw.Write(s);
                }
                return ms.ToArray();
            }
        }
    }
}
