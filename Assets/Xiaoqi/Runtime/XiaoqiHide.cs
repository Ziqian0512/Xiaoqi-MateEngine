using System;
using System.IO;
using UnityEngine;

public static class XiaoqiHide
{
    public static void Apply()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "xiaoqi_hide.json");
        string[] paths;
        try
        {
            paths = File.Exists(path)
                ? JsonUtility.FromJson<Wrapper>(File.ReadAllText(path))?.paths
                : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiHide: " + e.Message);
            return;
        }

        if (paths == null || paths.Length == 0)
        {
            paths = new[]
            {
                "ChatBot AI",
                "MateEngineAPI/CircleMenu",
                "MateEngineAPI/ModLoader",
                "MateEngineAPI/FoodMenu",
                "= Steam Exklusives",
                "= ME Modding",
                "Food"
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(new Wrapper { paths = paths }, true));
            }
            catch { }
        }

        foreach (var p in paths)
        {
            var go = GameObject.Find(p);
            if (go != null) go.SetActive(false);
            else Debug.Log("XiaoqiHide: not found " + p);
        }
    }

    [Serializable]
    private class Wrapper
    {
        public string[] paths;
    }
}
