using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Xiaoqi.Spike
{
    [Serializable]
    public class XiaoqiVoice
    {
        public string name;
        public long mtime;
    }

    [Serializable]
    public class XiaoqiState
    {
        public string label;
        public float mood;
        public string line;
        public string action;
        public XiaoqiVoice voice;
    }

    /// <summary>轮询现有 Python /api/avatar/state，并在语音变化时拉 /api/avatar/audio。</summary>
    public class XiaoqiStateClient : MonoBehaviour
    {
        public string stateUrl = "http://127.0.0.1:8124/api/avatar/state";
        public string audioUrl = "http://127.0.0.1:8124/api/avatar/audio";
        public float interval = 1.5f;

        public event Action<XiaoqiState> OnState;
        public event Action<AudioClip> OnVoice;
        public AudioSource AudioSource => _audio;

        private AudioSource _audio;
        private long _lastVoiceMtime;
        private bool _busy;

        private void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
        }

        private void Start()
        {
            StartCoroutine(Poll());
        }

        private IEnumerator Poll()
        {
            while (true)
            {
                if (!_busy)
                {
                    _busy = true;
                    yield return FetchState();
                    _busy = false;
                }
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private IEnumerator FetchState()
        {
            using (var req = UnityWebRequest.Get(stateUrl))
            {
                req.timeout = 3;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    yield break;
                }

                XiaoqiState state = null;
                try { state = JsonUtility.FromJson<XiaoqiState>(req.downloadHandler.text); }
                catch (Exception e) { Debug.LogWarning("Xiaoqi state parse: " + e.Message); }
                if (state == null) yield break;

                OnState?.Invoke(state);
                if (state.voice != null && state.voice.mtime != 0 && state.voice.mtime != _lastVoiceMtime)
                {
                    _lastVoiceMtime = state.voice.mtime;
                    yield return FetchVoice(state.voice.mtime);
                }
            }
        }

        private IEnumerator FetchVoice(long mtime)
        {
            using (var req = UnityWebRequestMultimedia.GetAudioClip(audioUrl + "?t=" + mtime, AudioType.WAV))
            {
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null)
                {
                    _audio.clip = clip;
                    _audio.Play();
                    OnVoice?.Invoke(clip);
                }
            }
        }
    }
}
