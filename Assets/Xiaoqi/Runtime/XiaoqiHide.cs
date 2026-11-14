using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class XiaoqiHide
{
    private static readonly string[] DefaultPaths =
    {
        "ChatBot AI", "MateEngineAPI/CircleMenu", "MateEngineAPI/ModLoader", "MateEngineAPI/FoodMenu",
        "= Steam Exklusives", "= ME Modding", "= Settings", "= AI", "= FOOD SYSTEM", "Main Menu",
        "Chat", "AI INPUT", "AI Speech", "AI Alarm Speech", "AiSystemPrompt", "Food", "Food System",
        "FoodMenu", "ModLoader", "AlarmMenuCanvas", "BlendshapeMenuCanvas", "DancePlayerMenuCanvas",
        "MainMenuAlarm", "MenuPanel",
        "Welcome Window", "Welcome", "Tutorial", "Discord", "Discord RPC", "DiscordPresence",
        "SteamSDK", "SteamAPI", "SteamDRM", "SteamWorkshop", "SteamContent", "STEAM_DLC",
        "STEAM_MC", "STEAM_FS", "LOCK_STEAM_DLC", "= STEAM DLC", "SystemStart"
    };

    private static readonly string[] DefaultComponents =
    {
        "DiscordPresence", "AvatarRandomMessages", "AvatarMinecraftMessages", "MenuAudioHandler",
        "TutorialMenu", "SystemStartHandler", "LaunchMateEngineInstances", "SteamWorkshopAutoLoader",
        "SteamWorkshopHandler", "PetVoiceReactionHandler"
    };

    public static void Apply()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "xiaoqi_hide.json");
        var merged = new Wrapper();
        try
        {
            if (File.Exists(path))
                merged = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path)) ?? new Wrapper();
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiHide: " + e.Message);
        }
        merged.paths = Union(DefaultPaths, merged.paths);
        merged.components = Union(DefaultComponents, merged.components);

        foreach (var p in merged.paths)
        {
            var go = GameObject.Find(p);
            if (go != null) go.SetActive(false);
            else Debug.Log("XiaoqiHide: not found " + p);
        }

        foreach (var name in merged.components)
        {
            var found = false;
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != name) continue;
                mb.enabled = false;
                found = true;
            }
            Debug.Log("XQ_HIDE comp " + name + (found ? " off" : " not found"));
        }

        // F1 圆盘菜单：所有 MenuActions.radialMenuKey 设为 None。
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb.GetType().Name != "MenuActions") continue;
            var f = mb.GetType().GetField("radialMenuKey");
            if (f != null && f.FieldType == typeof(KeyCode)) f.SetValue(mb, KeyCode.None);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(merged, true));
        }
        catch { }
    }

    private static string[] Union(string[] a, string[] b)
    {
        var set = new HashSet<string>(a ?? new string[0]);
        if (b != null) foreach (var x in b) set.Add(x);
        var arr = new string[set.Count];
        set.CopyTo(arr);
        Array.Sort(arr);
        return arr;
    }

    [Serializable]
    private class Wrapper
    {
        public string[] paths;
        public string[] components;
    }
}
