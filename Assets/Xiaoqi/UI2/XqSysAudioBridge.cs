using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 系统音频频谱桥接：通过 Windows CoreAudio (IAudioMeterInformation) 读取实时系统主音量峰值，
    /// 配合 XqMusicBridge / XqVoiceChatClient 驱动近景底部 160 根粉色透明频谱柱随任意播放声音实时律动。
    /// </summary>
    public static class XqSysAudioBridge
    {
        public const int BarCount = 160;
        public static readonly float[] Bars = new float[BarCount];
        public static bool IsSystemMediaDetected { get; private set; }
        public static string CurrentAudioTitle { get; private set; } = "待机中";
        public static string CurrentPlayerName { get; private set; } = "等待播放器连接";

        static readonly float[] Spectrum = new float[256];
        static float _nextScanAt;
        static float _smoothPeak;

        // Windows CoreAudio COM (IMMDeviceEnumerator -> IAudioMeterInformation)
        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        class MMDeviceEnumeratorCom { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioMeterInformation
        {
            int GetPeakValue(out float pfPeak);
        }

        static IAudioMeterInformation _meter;
        static bool _meterInitTried;

        static float ReadSystemPeak()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
                return 0f;
            try
            {
                if (_meter == null && !_meterInitTried)
                {
                    _meterInitTried = true;
                    var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                    if (enumerator.GetDefaultAudioEndpoint(0, 1, out var dev) == 0 && dev != null)
                    {
                        var iid = typeof(IAudioMeterInformation).GUID;
                        if (dev.Activate(ref iid, 1, IntPtr.Zero, out var obj) == 0)
                            _meter = obj as IAudioMeterInformation;
                    }
                }
                if (_meter != null && _meter.GetPeakValue(out float peak) == 0)
                    return Mathf.Clamp01(peak);
            }
            catch { _meter = null; }
            return 0f;
        }

        public static void Tick(float dt)
        {
            if (Time.unscaledTime >= _nextScanAt)
            {
                _nextScanAt = Time.unscaledTime + 1.5f;
                ScanActivePlayer();
            }

            float sysPeak = ReadSystemPeak();
            bool ttsOn = XqVoiceChatClient.IsPlayingTts;
            if (ttsOn) AudioListener.GetSpectrumData(Spectrum, 0, FFTWindow.BlackmanHarris);

            float fallbackAmp = (XqMusicBridge.IsPlaying && sysPeak < 0.01f) ? 0.38f : 0f;
            float targetPeak = Mathf.Max(sysPeak, fallbackAmp);
            _smoothPeak = Mathf.Lerp(_smoothPeak, targetPeak, Mathf.Clamp01(dt * 14f));

            bool active = ttsOn || _smoothPeak > 0.008f;
            float t = Time.unscaledTime;

            for (int i = 0; i < BarCount; i++)
            {
                float norm = i / (float)(BarCount - 1);
                float env = 0.35f + 0.65f * Mathf.Sin(norm * Mathf.PI);
                float target;
                if (ttsOn)
                {
                    target = Mathf.Clamp01(Spectrum[Mathf.Min(i, 255)] * 30f);
                }
                else if (active)
                {
                    float w1 = 0.5f + 0.5f * Mathf.Sin(t * 7.2f + i * 0.24f);
                    float w2 = 0.5f + 0.5f * Mathf.Cos(t * 11.5f - i * 0.13f);
                    float w3 = 0.5f + 0.5f * Mathf.Sin(t * 4.1f + norm * 9.0f);
                    target = Mathf.Clamp01(_smoothPeak * 1.65f * env * (0.28f + 0.72f * (w1 * 0.5f + w2 * 0.3f + w3 * 0.2f)));
                }
                else
                {
                    target = 0.04f;
                }
                Bars[i] = Mathf.Lerp(Bars[i], target, Mathf.Clamp01(dt * 14f));
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
