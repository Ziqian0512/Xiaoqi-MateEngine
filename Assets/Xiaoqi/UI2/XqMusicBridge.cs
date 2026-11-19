using System;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// V6 音乐小组件数据桥：预留网易云音乐 API（XiaoqiConfig.netease_api）接入点，
    /// 当前提供切歌 / 播放 / 暂停 / 喜欢交互与进度推进，后续可直接挂载真实歌单接口。
    /// </summary>
    public static class XqMusicBridge
    {
        public struct Track
        {
            public string title;
            public string artist;
            public float durationSec;
            public bool liked;
        }

        private static readonly Track[] Playlist =
        {
            new Track { title = "Baby you", artist = "Yuka · 和小祈一起听歌中 ♪", durationSec = 212f, liked = true },
            new Track { title = "群青", artist = "YOASOBI · 网易云歌单", durationSec = 248f, liked = false },
            new Track { title = "夜に駆ける", artist = "YOASOBI · 深夜陪伴", durationSec = 258f, liked = true },
            new Track { title = "Sparkle", artist = "RADWIMPS · 晚霞精选", durationSec = 285f, liked = false }
        };

        private static int _idx;
        public static bool IsPlaying = true;
        public static float Progress = 0.42f;
        public static event Action OnChanged;

        public static Track Current { get { return Playlist[_idx]; } }

        public static void TogglePlay()
        {
            IsPlaying = !IsPlaying;
            if (OnChanged != null) OnChanged();
        }

        public static void Next()
        {
            _idx = (_idx + 1) % Playlist.Length;
            Progress = 0.05f;
            IsPlaying = true;
            if (OnChanged != null) OnChanged();
        }

        public static void Prev()
        {
            _idx = (_idx - 1 + Playlist.Length) % Playlist.Length;
            Progress = 0.05f;
            IsPlaying = true;
            if (OnChanged != null) OnChanged();
        }

        public static void ToggleLike()
        {
            var t = Playlist[_idx];
            t.liked = !t.liked;
            Playlist[_idx] = t;
            if (OnChanged != null) OnChanged();
        }

        public static void Tick(float dt)
        {
            if (!IsPlaying) return;
            float dur = Current.durationSec > 1f ? Current.durationSec : 200f;
            Progress += dt / dur;
            if (Progress >= 1f) Next();
        }
    }
}
