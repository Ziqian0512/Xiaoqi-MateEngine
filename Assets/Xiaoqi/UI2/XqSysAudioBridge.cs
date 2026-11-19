using System;
using System.Diagnostics;
using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V13 系统音频识别与横穿全屏频谱驱动器：
    /// - 自动检测本机正在运行的音乐/媒体播放器窗口标题（网易云 / QQ音乐 / Spotify / foobar2000 / PotPlayer 等）
    /// - 当检测到任意播放器播放或本地 TTS/音乐组件开启时，实时驱动 40 根全屏毛玻璃频谱柱律动
    /// </summary>
    public static class XqSysAudioBridge
    {
        public const int BarCount = 160;
        public static readonly float[] Bars = new float[BarCount];
        public static string CurrentAudioTitle = "Baby you · Yuka (内置)";
        public static string CurrentPlayerName = "等待媒体播放";
        public static bool IsSystemMediaDetected { get; private set; }

        private static float _nextScanAt;
        private static readonly string[] PlayerProcs =
        {
            "cloudmusic", "QQMusic", "Spotify", "foobar2000", "PotPlayerMini64", "PotPlayerMini", "vlc", "lx-music-desktop"
        };
        private static readonly string[] PlayerLabels =
        {
            "网易云音乐", "QQ音乐", "Spotify", "foobar2000", "PotPlayer", "PotPlayer", "VLC", "洛雪音乐"
        };

        public static void Tick(float dt)
        {
            if (Time.unscaledTime >= _nextScanAt)
            {
                _nextScanAt = Time.unscaledTime + 1.5f;
                ScanActivePlayer();
            }

            bool active = IsSystemMediaDetected || XqMusicBridge.IsPlaying || XqVoiceChatClient.IsPlayingTts;
            float t = Time.unscaledTime;
            for (int i = 0; i < BarCount; i++)
            {
                float norm = i / (float)(BarCount - 1);
                float env = 0.55f + 0.45f * Mathf.Sin(norm * Mathf.PI); // 中部略高，两侧渐收
                float target;
                if (active)
                {
                    float w1 = Mathf.Abs(Mathf.Sin(t * 4.2f + i * 0.47f));
                    float w2 = Mathf.Abs(Mathf.Cos(t * 7.1f - i * 0.83f));
                    float w3 = Mathf.Abs(Mathf.Sin(t * 2.3f + i * 0.21f));
                    target = Mathf.Clamp01((0.16f + 0.52f * w1 + 0.32f * w2 * w3) * env);
                }
                else
                {
                    target = 0.06f + 0.04f * Mathf.Abs(Mathf.Sin(t * 1.1f + i * 0.3f));
                }
                Bars[i] = Mathf.Lerp(Bars[i], target, Mathf.Clamp01(dt * 12f));
            }
        }

        private static void ScanActivePlayer()
        {
            try
            {
                for (int p = 0; p < PlayerProcs.Length; p++)
                {
                    var procs = Process.GetProcessesByName(PlayerProcs[p]);
                    for (int i = 0; i < procs.Length; i++)
                    {
                        string title = procs[i].MainWindowTitle;
                        if (string.IsNullOrWhiteSpace(title)) continue;
                        if (title.Contains(" - ") || title.Contains("—") || title.Length > 3)
                        {
                            // 过滤纯播放器空标题
                            if (string.Equals(title.Trim(), PlayerLabels[p], StringComparison.OrdinalIgnoreCase)) continue;
                            CurrentAudioTitle = title.Trim();
                            CurrentPlayerName = PlayerLabels[p];
                            IsSystemMediaDetected = true;
                            return;
                        }
                    }
                }
            }
            catch { }

            IsSystemMediaDetected = false;
            var trk = XqMusicBridge.Current;
            CurrentAudioTitle = trk.title + " · " + trk.artist;
            CurrentPlayerName = XqMusicBridge.IsPlaying ? "内置歌单" : "待机中";
        }
    }
}
