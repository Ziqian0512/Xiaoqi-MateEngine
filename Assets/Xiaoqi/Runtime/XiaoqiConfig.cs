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
