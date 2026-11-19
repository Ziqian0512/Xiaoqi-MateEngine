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
        public static string CurrentAudioTitle = "未检测到媒体窗口";
        public static string CurrentPlayerName = "等待媒体播放";
        public static bool IsSystemMediaDetected { get; private set; }

        private static readonly float[] Spectrum = new float[256];
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

            bool active = XqVoiceChatClient.IsPlayingTts;
            if (active) AudioListener.GetSpectrumData(Spectrum, 0, FFTWindow.BlackmanHarris);
            for (int i = 0; i < BarCount; i++)
            {
                float norm = i / (float)(BarCount - 1);
                float env = 0.55f + 0.45f * Mathf.Sin(norm * Mathf.PI); // 中部略高，两侧渐收
                float target;
                if (active)
                {
                    target = Mathf.Clamp01(Spectrum[Mathf.Min(i, 255)] * 30f);
                }
                else
                {
                    target = 0f;
                }
                Bars[i] = Mathf.Lerp(Bars[i], target, Mathf.Clamp01(dt * 12f));
            }
        }

        private static void ScanActivePlayer()
        {
            IsSystemMediaDetected = XqMusicBridge.Available;
            CurrentAudioTitle = XqMusicBridge.Current.title;
            CurrentPlayerName = XqMusicBridge.Available ? "系统媒体会话" : "等待播放器连接";
        }
    }
}