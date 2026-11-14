using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XiaoqiAudit : MonoBehaviour
{
    private string _path;

    private void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "-deme-audit");
        if (i < 0)
        {
            enabled = false;
            return;
        }
        _path = i + 1 < args.Length ? args[i + 1] : "deme-audit.txt";
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(20f);
        var sb = new StringBuilder();
        sb.AppendLine("XIAOQI DEME AUDIT " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        sb.AppendLine("\n[1] visible text containing brand words");
        int hits = 0;
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;
            if (Brand(t.text)) { hits++; sb.AppendLine(PathOf(t.transform) + " | " + t.text); }
        }
        foreach (var t in FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;
            if (Brand(t.text)) { hits++; sb.AppendLine(PathOf(t.transform) + " | " + t.text); }
        }
        sb.AppendLine("hits=" + hits);

        sb.AppendLine("\n[2] ME components");
        var names = new[] { "DiscordPresence", "AvatarRandomMessages", "AvatarMinecraftMessages",
            "MenuAudioHandler", "TutorialMenu", "SystemStartHandler", "LaunchMateEngineInstances",
            "SteamWorkshopAutoLoader", "SteamWorkshopHandler", "PetVoiceReactionHandler" };
        foreach (var n in names)
        {
            var found = 0;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != n) continue;
                found++;
                sb.AppendLine(n + " " + PathOf(mb.transform) + " enabled=" + mb.enabled);
            }
            if (found == 0) sb.AppendLine(n + " not found");
        }

        sb.AppendLine("\n[3] KeyCode fields");
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            foreach (var f in mb.GetType().GetFields(System.Reflection.BindingFlags.Public
                         | System.Reflection.BindingFlags.Instance))
            {
                if (f.FieldType != typeof(KeyCode)) continue;
                sb.AppendLine(mb.GetType().Name + "." + f.Name + "=" + f.GetValue(mb));
            }
        }

        sb.AppendLine("\n[4] build/StreamingAssets");
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        foreach (var f in new[] { "steam_appid.txt", "steam_api64.dll", "discord-rpc.dll" })
            sb.AppendLine(f + "=" + File.Exists(Path.Combine(root, f)));
        var sa = Path.Combine(Application.dataPath, "StreamingAssets");
        if (Directory.Exists(sa))
            foreach (var d in Directory.GetDirectories(sa))
                sb.AppendLine("SA " + Path.GetFileName(d) + "=" + DirSize(d) + " bytes");

        sb.AppendLine("\n[5] HKCU Run");
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (k != null)
                {
                    sb.AppendLine("MateEngine=" + (k.GetValue("MateEngine") ?? "<null>"));
                    sb.AppendLine("Xiaoqi=" + (k.GetValue("Xiaoqi") ?? "<null>"));
                }
            }
        }
        catch (Exception e) { sb.AppendLine("registry error " + e.Message); }

        try { File.WriteAllText(_path, sb.ToString()); Debug.Log("XQ_AUDIT " + _path); }
        catch (Exception e) { Debug.LogError("XQ_AUDIT write: " + e.Message); }
        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    private static bool Brand(string s)
    {
        s = s.ToLowerInvariant();
        return s.Contains("mate") || s.Contains("shinymoon") || s.Contains("steam")
               || s.Contains("discord") || s.Contains("buy") || s.Contains("version");
    }

    private static string PathOf(Transform t)
    {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    private static long DirSize(string d)
    {
        long n = 0;
        try { foreach (var f in Directory.GetFiles(d, "*", SearchOption.AllDirectories)) n += new FileInfo(f).Length; }
        catch { }
        return n;
    }
}

