using System;

namespace Xiaoqi.UI2
{
    /// <summary>No playback provider is connected yet; do not simulate successful playback.</summary>
    public static class XqMusicBridge
    {
        public struct Track
        {
            public string title, artist;
            public float durationSec;
            public bool liked;
        }
        public static bool IsPlaying { get { return false; } }
        public static float Progress { get { return 0f; } }
        public static event Action OnChanged;
        public static Track Current { get { return new Track { title = "音乐尚未接入", artist = "暂无真实播放源" }; } }
        public static void TogglePlay() { Notify(); }
        public static void Next() { Notify(); }
        public static void Prev() { Notify(); }
        public static void ToggleLike() { Notify(); }
        public static void Tick(float dt) { }
        static void Notify() { if (OnChanged != null) OnChanged(); }
    }
}