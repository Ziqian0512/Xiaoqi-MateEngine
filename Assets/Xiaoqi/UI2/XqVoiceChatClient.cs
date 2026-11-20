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
    /// - 文本对话：请求 Brain /v1/chat/completions，失败时明确提示，不生成预设回复
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
        private class ChatMessage { public string role = "user"; public string content; }

        [Serializable]
        private class ChatReq { public bool stream = false; public ChatMessage[] messages; }

        [Serializable]
        private class ChatChoice { public ChatMessage message; }

        [Serializable]
        private class ChatResp { public ChatChoice[] choices; }

        public sealed class UsageSnapshot
        {
            public int promptTokens, completionTokens, totalTokens;
        }
        public static UsageSnapshot LatestUsage { get; private set; }
        // Missing or invalid usage remains unknown; never infer a budget percentage.
        public static UsageSnapshot ParseUsage(string json)
        {
            try
            {
                var usage=Newtonsoft.Json.Linq.JObject.Parse(json)["usage"];
                int? input=(int?)usage?["prompt_tokens"],output=(int?)usage?["completion_tokens"],total=(int?)usage?["total_tokens"];
                if(!input.HasValue||!output.HasValue||!total.HasValue||input<0||output<0||total<=0||total.Value!=(long)input.Value+output.Value)return null;
                return new UsageSnapshot {promptTokens=input.Value,completionTokens=output.Value,totalTokens=total.Value};
            }
            catch { return null; }
        }

        [Serializable]
        private class VoiceResp { public bool ok; public string url; }

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
        public static bool IsRecognizing { get; private set; }
        public static string Status { get; private set; } = "";
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
        public static event Action<long,bool,string> OnMessage;
        static long _messageId;
        long _activeReplyId;
        public static bool IsPreparingTts { get; private set; }
        public static float LastChatSuccessAt { get; private set; } = -999;
        public static float LastSttSuccessAt { get; private set; } = -999;
        public static float LastTtsSuccessAt { get; private set; } = -999;

        private AudioSource _audio;
        private AudioClip _micClip;
        private string _micDevice;
        private float _recStartTime;
        private Coroutine _streamCo;
        private Coroutine _ttsCo;
        public static bool ShouldAutoSpeak(XiaoqiConfig cfg, Xiaoqi.Mode.XqView view, bool quiet, float draw)
        {
            if (cfg == null || cfg.muted || quiet || cfg.office_mode) return false;
            if (view == Xiaoqi.Mode.XqView.Close) return true;
            return view == Xiaoqi.Mode.XqView.Pet && XqConsoleVoicePolicy.ShouldSpeak(draw);
        }

        private string _lastDeliveredText = "";
        private float _lastDeliveredAt = -100f, _lastFeedVoiceAt = -100f;
        public void SpeakFeed(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || IsThinking || IsRecording || IsRecognizing) return;
            if (text == _lastDeliveredText && Time.unscaledTime - _lastDeliveredAt < 30f) return;
            var cfg = XiaoqiRuntime.Config;
            var view = Xiaoqi.Mode.XqMode.Current;
            _lastDeliveredText = text; _lastDeliveredAt = Time.unscaledTime;
            if (!ShouldAutoSpeak(cfg, view, XiaoqiRuntime.Dnd, UnityEngine.Random.value)) return;
            if (IsPlayingTts || IsStreaming || IsPreparingTts) return;
            _lastFeedVoiceAt = Time.unscaledTime;
            PlayTts(text);
        }

        private void Report(string message) { Status = message; NotifyChanged(); }

        private void Update()
        {
            XqCompanionUsage.Poll();
            if(IsRecording&&Time.unscaledTime-_recStartTime>=19.8f)StopRecordingAndSend();
            var cfg = XiaoqiRuntime.Config;
            if (cfg!=null && (cfg.muted || XiaoqiRuntime.Dnd || cfg.office_mode) && IsPreparingTts) StopSpeech();
            if (_audio == null || !_audio.isPlaying || cfg == null) return;
            if (cfg.muted || XiaoqiRuntime.Dnd || cfg.office_mode) { _audio.Stop(); NotifyChanged(); }
            else _audio.volume = Mathf.Clamp01(cfg.volume);
        }

        public static void NotifyChanged()
        {
            LastInteractionAt = Time.unscaledTime;
            if (OnChanged != null) OnChanged();
        }

        public void StartRecording()
        {
            if (IsRecording || IsRecognizing || IsThinking) return;
            StopSpeech();
            Status = "";
            IsRecording = true;
            _recStartTime = Time.unscaledTime;
            LiveTranscript = "正在聆听你的声音…";
            try
            {
                if (Microphone.devices != null && Microphone.devices.Length > 0)
                {
                    _micDevice = Microphone.devices[0];
                    _micClip = Microphone.Start(_micDevice, false, 20, 16000);
                    if(_micClip==null){IsRecording=false;Report("麦克风未能开始录音，请检查权限");}
                }
                else
                {
                    _micDevice = null;
                    _micClip = null;
                    IsRecording = false;
                    Report("未检测到麦克风，未发送任何内容");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_VOICE mic start: " + e.Message);
                _micClip = null;
                IsRecording = false;
                Report("无法开始录音，请检查麦克风权限");
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
                    if(pos<=0&&dur>=19.5f)pos=_micClip.samples;
                    if (pos > 800) wav = EncodeWav(_micClip, Mathf.Min(pos,_micClip.samples));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_VOICE mic stop: " + e.Message);
            }

            if(_micClip!=null){Destroy(_micClip);_micClip=null;}
            StartCoroutine(ProcessSttAndSend(wav, dur));
        }

        public void CancelRecording()
        {
            if(!IsRecording)return;
            if(!string.IsNullOrEmpty(_micDevice))Microphone.End(_micDevice);
            IsRecording=false;LiveTranscript="";
            if(_micClip!=null){Destroy(_micClip);_micClip=null;}
            Report("录音已取消，未发送内容");
        }

        private IEnumerator ProcessSttAndSend(byte[] wav, float dur)
        {
            if (wav == null) { Report("没有录到有效声音，请重试；未发送任何内容"); yield break; }
            IsRecognizing = true;
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg != null ? cfg.server : "") ?? "";
            string recognized = null;

            if(Application.platform==RuntimePlatform.WindowsPlayer||Application.platform==RuntimePlatform.WindowsEditor)
            {
                LiveTranscript="正在本机识别语音…";NotifyChanged();
                yield return XqLocalSpeech.Recognize(wav,text=>recognized=text);
            }

            if (string.IsNullOrEmpty(recognized) && wav != null && !string.IsNullOrEmpty(server) && !server.Contains("127.0.0.1:0"))
            {
                LiveTranscript = "正在识别语音…";
                NotifyChanged();
                string sttUrl = server.TrimEnd('/') + "/api/voice/stt";
                XqHttp.Resp stt=default(XqHttp.Resp);
                yield return XqHttp.PostAudio(sttUrl,cfg.token,wav,8,r=>stt=r);
                if(stt.ok)
                {
                    try
                    {
                        var sr=JsonUtility.FromJson<SttResp>(stt.text);
                        if(sr!=null)recognized=!string.IsNullOrEmpty(sr.text)?sr.text:sr.transcript;
                    }
                    catch{}
                }
            }

            if (string.IsNullOrEmpty(recognized))
            {
                IsRecognizing = false;
                Report("没有识别到文字，本次未发送。请按住 Ctrl+N 再试一次。");
                yield break;
            }
            IsRecognizing = false;
            LastSttSuccessAt=Time.unscaledTime;
            LiveTranscript = recognized;
            NotifyChanged();
            SendText(recognized, true);
        }

        public void SendText(string userText, bool fromVoice = false)
        {
            if (string.IsNullOrWhiteSpace(userText)) return;
            if (IsThinking || IsRecognizing || IsRecording) { Report("请等待当前回应完成"); return; }
            Status = "";
            if (_streamCo != null) { StopCoroutine(_streamCo); _streamCo = null; if(IsStreaming&&!string.IsNullOrEmpty(LatestReplyFull))OnMessage?.Invoke(_activeReplyId,true,LatestReplyFull); }
            IsStreaming = false;
            LatestReplyFull = "";
            LatestReplyVisible = "";
            LatestUsage = null;
            StopSpeech();
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
            long userId=++_messageId, replyId=++_messageId;
            OnMessage?.Invoke(userId,false,userText);
            NotifyChanged();
            StartCoroutine(RequestReply(userText, Turns.Count - 1,replyId));
        }

        private IEnumerator RequestReply(string userText, int turnIndex,long replyId)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg != null ? cfg.server : "") ?? "";
            string reply = null;
            string audioUrl = null;

            if (!string.IsNullOrEmpty(server))
            {
                string chatUrl = server.TrimEnd('/') + "/v1/chat/completions";
                string json = JsonUtility.ToJson(new ChatReq { messages = new[] { new ChatMessage { content = userText } } });
                XqHttp.Resp resp = default(XqHttp.Resp);
                yield return XqHttp.PostJson(chatUrl, cfg != null ? cfg.token : "", json, 120, r => resp = r);
                if (resp.ok && !string.IsNullOrEmpty(resp.text))
                {
                    LatestUsage=ParseUsage(resp.text);
                    try
                    {
                        var cr = JsonUtility.FromJson<ChatResp>(resp.text);
                        if (cr != null)
                        {
                            if (cr.choices != null && cr.choices.Length > 0 && cr.choices[0].message != null)
                                reply = cr.choices[0].message.content;
                        }
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(reply))
            {
                IsThinking = false;
                if (turnIndex >= 0 && turnIndex < Turns.Count)
                {
                    var failed = Turns[turnIndex]; failed.replyText = "[未收到回复]"; Turns[turnIndex] = failed;
                }
                Report("未收到云端回复，请检查连接后重试");
                yield break;
            }

            IsThinking = false;
            if (turnIndex >= 0 && turnIndex < Turns.Count)
            {
                var t = Turns[turnIndex];
                t.replyText = reply;
                Turns[turnIndex] = t;
            }
            LastChatSuccessAt=Time.unscaledTime;
            StartStream(reply,replyId);

            _lastDeliveredText = reply; _lastDeliveredAt = Time.unscaledTime;
            if (ShouldAutoSpeak(cfg, Xiaoqi.Mode.XqMode.Current, XiaoqiRuntime.Dnd, UnityEngine.Random.value))
            {
                PlayTts(reply, audioUrl);
            }
        }

        private void StartStream(string full,long replyId)
        {
            _activeReplyId=replyId;
            LatestReplyFull = full ?? "";
            LatestReplyVisible = "";
            if (_streamCo != null) StopCoroutine(_streamCo);
            _streamCo = StartCoroutine(StreamCharacters(LatestReplyFull,replyId));
        }

        private IEnumerator StreamCharacters(string full,long replyId)
        {
            IsStreaming = true;
            for (int i = 1; i <= full.Length; i++)
            {
                LatestReplyVisible = full.Substring(0, i);
                OnMessage?.Invoke(replyId,true,LatestReplyVisible);
                NotifyChanged();
                yield return new WaitForSecondsRealtime(0.032f);
            }
            IsStreaming = false;
            NotifyChanged();
        }

        public void PlayTts(string text, string explicitAudioUrl = null)
        {
            var cfg = XiaoqiRuntime.Config;
            if (cfg == null || cfg.muted || XiaoqiRuntime.Dnd || cfg.office_mode) return;
            StopSpeech();
            _ttsCo = StartCoroutine(PrepareSpeech(text, explicitAudioUrl));
        }

        void StopSpeech()
        {
            if (_audio!=null) _audio.Stop();
            if (_ttsCo!=null) { StopCoroutine(_ttsCo); _ttsCo=null; }
            IsPreparingTts=false;
        }
        IEnumerator PrepareSpeech(string text,string url)
        {
            IsPreparingTts=true;NotifyChanged();
            try { yield return FetchAndPlayTts(text,url); }
            finally { IsPreparingTts=false;_ttsCo=null;NotifyChanged(); }
        }

        private IEnumerator FetchAndPlayTts(string text, string explicitAudioUrl)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg != null ? cfg.server : "") ?? "";
            string url = explicitAudioUrl;
            if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(server))
            {
                XqHttp.Resp response = default(XqHttp.Resp);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { text = text ?? "" });
                yield return XqHttp.PostJson(server.TrimEnd('/') + "/api/voice/preview", cfg != null ? cfg.token : "", json, 190, r => response = r);
                VoiceResp voice = null;
                if (response.ok) { try { voice = JsonUtility.FromJson<VoiceResp>(response.text); } catch { } }
                if (voice == null || !voice.ok || string.IsNullOrEmpty(voice.url))
                { Report("文字回复已收到，语音合成失败"); yield break; }
                url = voice.url;
            }
            if (string.IsNullOrEmpty(url)) yield break;
            Uri resolved;
            if (!Uri.TryCreate(new Uri(server.TrimEnd('/') + "/"), url, out resolved))
            { Report("语音地址无效"); yield break; }
            url = resolved.AbsoluteUri;

            string cachedAudio = null;
            if (XqHttp.IsDirectHost(url))
            {
                var download = XqHttp.GetDirectAudio(url, cfg != null ? cfg.token : "");
                while (!download.IsCompleted) yield return null;
                if (download.IsFaulted || download.IsCanceled)
                {
                    if (download.IsFaulted) Debug.LogWarning("XQ_VOICE audio: " + download.Exception.GetBaseException().Message);
                    Report("文字回复已收到，语音下载失败"); yield break;
                }
                cachedAudio = Path.Combine(Application.temporaryCachePath, "xq-voice-" + Guid.NewGuid().ToString("N") + ".wav");
                bool saved = false;
                try { File.WriteAllBytes(cachedAudio, download.Result); saved = true; }
                catch (Exception e) { Debug.LogWarning("XQ_VOICE cache: " + e.Message); }
                if (!saved) { Report("语音缓存失败"); yield break; }
                url = new Uri(cachedAudio).AbsoluteUri;
            }
            try
            {
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                req.timeout = 30;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip != null && _audio != null)
                    {
                        if (cfg.muted || XiaoqiRuntime.Dnd || cfg.office_mode) yield break;
                        _audio.volume = cfg != null ? Mathf.Clamp01(cfg.volume) : 1f;
                        _audio.clip = clip;
                        _audio.Play();
                        LastTtsSuccessAt=Time.unscaledTime;
                        NotifyChanged();
                    }
                }
                else Report("文字回复已收到，语音下载失败");
            }
            }
            finally { if (cachedAudio != null && File.Exists(cachedAudio)) File.Delete(cachedAudio); }
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
