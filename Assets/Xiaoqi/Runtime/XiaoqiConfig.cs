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
    public string pet_when_out = "hide";
    public bool bubble = true;
    public bool dnd = false;

    public static XiaoqiConfig LoadOrCreate()
    {
        var path = Path.Combine(Application.dataPath, "..", "xiaoqi_config.json");
        try
        {
            if (File.Exists(path))
                return JsonUtility.FromJson<XiaoqiConfig>(File.ReadAllText(path)) ?? new XiaoqiConfig();
            var cfg = new XiaoqiConfig();
            File.WriteAllText(path, JsonUtility.ToJson(cfg, true));
            return cfg;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Xiaoqi config: " + e.Message);
            return new XiaoqiConfig();
        }
    }
}
