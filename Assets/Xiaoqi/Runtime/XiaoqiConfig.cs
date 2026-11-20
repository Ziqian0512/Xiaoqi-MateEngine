using System;
using System.IO;
using UnityEngine;

[Serializable]
public class XiaoqiConfig
{
    public string server = "http://127.0.0.1:8787";
    public string token = "";
    public string model_path = "XiaoqiData/16.vrm";
    public int poll_sec = 2;
    public string pet_when_out = "show";
    public bool bubble = true;
    public bool dnd = false;
    public string dnd_until = "";
    public int nap_min = 20;
    public bool emotion_line = false;
    public bool topmost = true;
    public bool hide_on_fullscreen = true;
    // 身边小胶囊：stay 常驻 / fade 淡出（默认）/ off 全关。设置页 P1-5 再接。
    public string chips = "fade";

    // XQ: P1-5 新设置页（B4）。默认关，WB 验证后再翻默认值。
    public bool settings_v2 = false;
    // XQ: P1-5 主题色 pink / blue / violet。
    public string theme = "pink";
    // XQ: P1-5 行为页。看向鼠标。
    public bool look_at_mouse = true;
    // XQ: P1-5 看向的力度 0-1（1 = ME 原值）。
    public float look_strength = 1f;
    // XQ: P1-5 坐姿高度微调 -50 ~ +50。ME 侧暂无对应字段，仅存盘。
    public float sit_offset = 0f;
    // XQ: P1-5 多久不动就睡着，0 = 关，可选 1/3/5/10（分钟）。
    public int idle_sleep_min = 0;
    // XQ: P1-5 双击摸头。
    public bool pat_double = true;
    // XQ: P1-5 静音。
    public bool muted = false;
    // XQ: P1-5 总音量 0-1。
    public float volume = 1f;
    // XQ: P1-5 放音乐时自己跳（宿主 SaveLoadHandler.data.enableDancing）。默认关。
    public bool dance_on_music = false;
    // XQ: P1-5 坐在窗口上（PetHost.WindowSitEnabled）。默认开。
    public bool window_sit = true;
    // XQ: V6 按住说话快捷键（N / V / B / Space）。
    public string voice_hotkey = "Ctrl+N";
    public bool office_mode = false;
    // XQ: V6 自动语音播报回复。
    public bool voice_tts_auto = true;
    public float pet_voice_probability = 0.35f;
    public bool close_subtitles = true;
    // XQ: V6 网易云音乐 API 地址（可选，如 http://127.0.0.1:3000）。
    public string netease_api = "";

    public static void Save(XiaoqiConfig cfg)
    {
        try
        {
            var path = Path.Combine(Application.dataPath, "..", "xiaoqi_config.json");
            File.WriteAllText(path, JsonUtility.ToJson(cfg, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Xiaoqi config save: " + e.Message);
        }
    }

    public static XiaoqiConfig LoadOrCreate()
    {
        var path = Path.Combine(Application.dataPath, "..", "xiaoqi_config.json");
        try
        {
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                var cfg = JsonUtility.FromJson<XiaoqiConfig>(text) ?? new XiaoqiConfig();
                bool dirty = false;
                if (text.IndexOf("\"topmost\"", StringComparison.Ordinal) < 0)
                {
                    cfg.topmost = true;
                    dirty = true;
                }
                if (text.IndexOf("\"hide_on_fullscreen\"", StringComparison.Ordinal) < 0)
                {
                    cfg.hide_on_fullscreen = true;
                    dirty = true;
                }
                if (text.IndexOf("\"chips\"", StringComparison.Ordinal) < 0)
                {
                    cfg.chips = "fade";
                    dirty = true;
                }
                // XQ: P1-5 新设置页字段补缺。
                if (text.IndexOf("\"settings_v2\"", StringComparison.Ordinal) < 0)
                {
                    cfg.settings_v2 = false;
                    dirty = true;
                }
                if (text.IndexOf("\"theme\"", StringComparison.Ordinal) < 0)
                {
                    cfg.theme = "pink";
                    dirty = true;
                }
                if (text.IndexOf("\"look_at_mouse\"", StringComparison.Ordinal) < 0)
                {
                    cfg.look_at_mouse = true;
                    dirty = true;
                }
                if (text.IndexOf("\"look_strength\"", StringComparison.Ordinal) < 0)
                {
                    cfg.look_strength = 1f;
                    dirty = true;
                }
                if (text.IndexOf("\"sit_offset\"", StringComparison.Ordinal) < 0)
                {
                    cfg.sit_offset = 0f;
                    dirty = true;
                }
                if (text.IndexOf("\"idle_sleep_min\"", StringComparison.Ordinal) < 0)
                {
                    cfg.idle_sleep_min = 0;
                    dirty = true;
                }
                if (text.IndexOf("\"pat_double\"", StringComparison.Ordinal) < 0)
                {
                    cfg.pat_double = true;
                    dirty = true;
                }
                if (text.IndexOf("\"muted\"", StringComparison.Ordinal) < 0)
                {
                    cfg.muted = false;
                    dirty = true;
                }
                if (text.IndexOf("\"volume\"", StringComparison.Ordinal) < 0)
                {
                    cfg.volume = 1f;
                    dirty = true;
                }
                if (text.IndexOf("\"dance_on_music\"", StringComparison.Ordinal) < 0)
                {
                    cfg.dance_on_music = false;
                    dirty = true;
                }
                if (text.IndexOf("\"window_sit\"", StringComparison.Ordinal) < 0)
                {
                    cfg.window_sit = true;
                    dirty = true;
                }
                if (cfg.voice_hotkey != "Ctrl+N")
                {
                    cfg.voice_hotkey = "Ctrl+N";
                    dirty = true;
                }
                if (text.IndexOf("\"office_mode\"", StringComparison.Ordinal) < 0) { cfg.office_mode = !cfg.bubble; dirty = true; }
                if (text.IndexOf("\"voice_tts_auto\"", StringComparison.Ordinal) < 0)
                {
                    cfg.voice_tts_auto = true;
                    dirty = true;
                }
                if (text.IndexOf("\"netease_api\"", StringComparison.Ordinal) < 0)
                {
                    cfg.netease_api = "";
                    dirty = true;
                }
                if (text.IndexOf("\"pet_voice_probability\"", StringComparison.Ordinal) < 0)
                { cfg.pet_voice_probability = 0.35f; dirty = true; }
                if (text.IndexOf("\"close_subtitles\"", StringComparison.Ordinal) < 0)
                { cfg.close_subtitles = true; dirty = true; }
                cfg.pet_voice_probability = Mathf.Clamp01(cfg.pet_voice_probability);
                if (dirty) Save(cfg);
                return cfg;
            }
            var created = new XiaoqiConfig();
            File.WriteAllText(path, JsonUtility.ToJson(created, true));
            return created;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Xiaoqi config: " + e.Message);
            return new XiaoqiConfig();
        }
    }
}
