using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>Read and control the current Windows media session. One owned hidden helper, no simulated state.</summary>
    public static class XqMusicBridge
    {
        public struct Track { public string title, artist; public float durationSec; public bool liked; }
        [Serializable] class Session
        {
            public bool available, playing, canToggle, canNext, canPrev, coverChanged;
            public string source, title, artist, error, cover;
            public float duration, position;
        }
        static Session _state = new Session();
        static Process _helper;
        static readonly Queue<string> Replies = new Queue<string>();
        static readonly object Gate = new object();
        static bool _pending;
        static string _queuedCommand, _queuedSource;
        static float _requestAt, _nextPoll;
        static Texture2D _cover;
        public static event Action OnChanged;
        public static bool Available { get { return _state.available; } }
        public static bool IsPlaying { get { return Available && _state.playing; } }
        public static bool CanToggle { get { return Available && _state.canToggle; } }
        public static bool CanNext { get { return Available && _state.canNext; } }
        public static bool CanPrev { get { return Available && _state.canPrev; } }
        public static float PositionSec { get { return Available ? Mathf.Clamp(_state.position, 0f, Mathf.Max(0f, _state.duration)) : 0f; } }
        public static float Progress { get { return _state.duration > 0f ? PositionSec / _state.duration : 0f; } }
        public static Texture2D Cover { get { return _cover; } }
        public static string Source { get { return Available ? _state.source : ""; } }
        public static string Status { get; private set; } = "打开播放器，开始一起听";
        public static Track Current { get { return new Track { title = Available ? (string.IsNullOrEmpty(_state.title) ? "未提供曲名" : _state.title) : "暂无媒体会话", artist = Available ? (_state.artist ?? "") : Status, durationSec = _state.duration }; } }
        public static string Clock(float seconds) { var n = Mathf.Max(0, Mathf.FloorToInt(seconds)); return string.Format("{0}:{1:00}", n / 60, n % 60); }
        public static void TogglePlay() { if (CanToggle) Send("toggle"); }
        public static void Next() { if (CanNext) Send("next"); }
        public static void Prev() { if (CanPrev) Send("prev"); }
        public static void ToggleLike() { } // Favorites are not part of system media control.

        public static void Tick(float dt)
        {
            string line = null;
            lock (Gate) { if (Replies.Count > 0) line = Replies.Dequeue(); }
            if (line != null)
            {
                _pending = false;
                try
                {
                    var snapshot = JsonUtility.FromJson<Session>(line);
                    if (snapshot != null)
                    {
                        _state = snapshot;
                        Status = snapshot.available ? "" : (snapshot.error == "No active media session" ? "打开播放器，开始一起听" : "媒体会话暂不可用");
                        if (snapshot.coverChanged || !snapshot.available)
                        {
                            if (_cover != null) UnityEngine.Object.Destroy(_cover);
                            _cover = null;
                            if (!string.IsNullOrEmpty(snapshot.cover))
                            {
                                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                if (texture.LoadImage(Convert.FromBase64String(snapshot.cover))) _cover = texture;
                                else UnityEngine.Object.Destroy(texture);
                            }
                        }
                        if (OnChanged != null) OnChanged();
                    }
                }
                catch (Exception e) { UnityEngine.Debug.LogWarning("XQ_MEDIA response: " + e.Message); }
            }
            if (_pending && Time.unscaledTime - _requestAt > 10f)
            { Shutdown(); _state = new Session(); Status = "媒体连接超时，正在重试"; _nextPoll = Time.unscaledTime + 5f; if (OnChanged != null) OnChanged(); }
            if (!_pending && _queuedCommand != null)
            {
                var command = _queuedCommand; var source = _queuedSource; _queuedCommand = null; _queuedSource = null; Send(command, source);
            }
            if (!_pending && Time.unscaledTime >= _nextPoll)
            { _nextPoll = Time.unscaledTime + 1f; Send("poll"); }
        }

        static bool EnsureHelper()
        {
            if (_helper != null && !_helper.HasExited) return true;
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor) return false;
            try
            {
                var script = Resources.Load<TextAsset>("XqMediaSession");
                if (script == null) throw new InvalidOperationException("Media helper missing");
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script.text));
                _helper = new Process { StartInfo = new ProcessStartInfo {
                    FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell/v1.0/powershell.exe"),
                    Arguments = "-NoProfile -NonInteractive -EncodedCommand " + encoded,
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                } };
                _helper.OutputDataReceived += (sender, args) => { if (args.Data != null) lock (Gate) { if (Replies.Count < 8) Replies.Enqueue(args.Data); } };
                _helper.ErrorDataReceived += (sender, args) => { }; // Drain stderr without blocking the helper.
                _helper.Start(); _helper.BeginOutputReadLine(); _helper.BeginErrorReadLine();
                return true;
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("XQ_MEDIA start: " + e.Message); Status = "无法连接系统媒体"; return false; }
        }

        static void Send(string command, string requestedSource = null)
        {
            if (_pending) { if (command != "poll") { _queuedCommand = command; _queuedSource = Source; } return; }
            if (!EnsureHelper()) return;
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { command, source = requestedSource ?? Source });
                _helper.StandardInput.WriteLine(json); _helper.StandardInput.Flush();
                _pending = true; _requestAt = Time.unscaledTime;
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("XQ_MEDIA command: " + e.Message); Shutdown(); }
        }

        public static void Shutdown()
        {
            var process = _helper; _helper = null; _pending = false; _queuedCommand = null; _queuedSource = null;
            if (process != null) { try { if (!process.HasExited) process.Kill(); } catch { } process.Dispose(); }
            lock (Gate) Replies.Clear();
        }
    }
}