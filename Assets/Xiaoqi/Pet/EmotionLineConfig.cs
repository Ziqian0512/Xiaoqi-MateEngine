using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class EmotionLineConfig
{
    public FaceCfg face = new FaceCfg();
    public BodyCfg body = new BodyCfg();
    public PresenceCfg presence = new PresenceCfg();
    public BubbleCfg bubble = new BubbleCfg();
    public CardCfg card = new CardCfg();

    [Serializable]
    public class FaceCfg
    {
        public Dictionary<string, Dictionary<string, float>> map = new Dictionary<string, Dictionary<string, float>>();
        public float base_min_level = 0.15f;
        public float base_gain = 0.35f;
        public float spike_gain = 0.9f;
        public float spike_hold = 2.5f;
        public float spike_fade = 1.5f;
        public float spike_max_age_sec = 120f;
        public float blend_sec = 0.6f;
    }

    [Serializable]
    public class BodyCfg
    {
        public float sleepy_hi = 70f;
        public float sleepy_lid = 0.15f;
        public float[] yawn_every_min = { 3, 6 };
        public float yawn_sec = 1.8f;
        public Dictionary<string, float> yawn = new Dictionary<string, float>();
        public float tired_lo = 30f;
        public Dictionary<string, float> tired_add = new Dictionary<string, float>();
        // P1-7：困意高时自己睡着（主人 11-16 定「要」）。总开关仍是 emotion_line。
        public bool self_sleep = true;
        public float self_sleep_hold_sec = 90f;     // sleepy ≥ sleepy_hi 要持续这么久
        public float self_sleep_min = 15f;          // 每次最多睡多久（分钟）
        public float self_sleep_cooldown_min = 20f; // 醒后多久内不再自己睡
        public float self_sleep_idle_sec = 20f;     // 没被碰过（拖/按/鼠标停在她身上）这么久才睡
    }

    [Serializable]
    public class PresenceCfg
    {
        public string label_out = "{seg} · {back}回";
        public string label_out_noback = "{seg}";
        public string label_sleep = "睡着了";
        public float wake_on_drag_sec = 30f;
    }

    [Serializable]
    public class BubbleCfg
    {
        public float cps = 20f;
        public float min_sec = 4f;
        public float per_char_sec = 0.25f;
        public float max_sec = 12f;
        public int queue = 3;
        public float head_offset_m = 0.28f;
        public string[] voice_media = { "voice" };
        public float mouth_hz = 8f;
        public float[] mouth_w = { 0.3f, 0.5f };
    }

    [Serializable]
    public class CardCfg
    {
        public float click_max_sec = 0.3f;
        public float click_max_px = 6f;
        public float auto_close_sec = 10f;
        public int max_marks = 6;
        public Dictionary<string, string> colors = new Dictionary<string, string>();
        public Dictionary<string, string> where_colors = new Dictionary<string, string>();
    }

    public static EmotionLineConfig Load()
    {
        var defaults = LoadFile(Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "emotion_line.json"));
        var saved = LoadFile(Path.Combine(Application.persistentDataPath, "emotion_line.json"));
        if (saved == null) return defaults ?? new EmotionLineConfig();
        if (defaults == null) return saved;
        // 主人保存值整体覆盖默认值；null 项保留默认。
        var merged = JsonConvert.SerializeObject(defaults);
        var patch = JsonConvert.SerializeObject(saved);
        var a = Newtonsoft.Json.Linq.JObject.Parse(merged);
        var b = Newtonsoft.Json.Linq.JObject.Parse(patch);
        a.Merge(b, new Newtonsoft.Json.Linq.JsonMergeSettings
        {
            MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Replace
        });
        return a.ToObject<EmotionLineConfig>() ?? defaults;
    }

    private static EmotionLineConfig LoadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<EmotionLineConfig>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning("EmotionLineConfig: " + e.Message);
            return null;
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "emotion_line.json"),
                JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception e)
        {
            Debug.LogWarning("EmotionLineConfig save: " + e.Message);
        }
    }

    public void ResetSaved()
    {
        try
        {
            var p = Path.Combine(Application.persistentDataPath, "emotion_line.json");
            if (File.Exists(p)) File.Delete(p);
        }
        catch { }
    }
}
