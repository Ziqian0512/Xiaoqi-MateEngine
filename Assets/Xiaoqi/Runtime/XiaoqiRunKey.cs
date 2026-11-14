using System;
using Microsoft.Win32;
using UnityEngine;

public static class XiaoqiRunKey
{
    private const string RunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void Migrate()
    {
        try
        {
            var exe = "\"" + Application.dataPath.Replace("/", "\\") + "\\..\\Xiaoqi.exe\"";
            using (var key = Registry.CurrentUser.OpenSubKey(RunPath, true))
            {
                if (key == null) return;
                var old = key.GetValue("MateEngine") as string;
                if (!string.IsNullOrEmpty(old) && old.Trim().Trim('"') == exe.Trim('"'))
                {
                    key.DeleteValue("MateEngine", false);
                    Debug.Log("XQ_RUNKEY migrated");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiRunKey migrate: " + e.Message);
        }

        Sync(SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data != null
             && SaveLoadHandler.Instance.data.startWithWindows);
    }

    public static void Sync(bool enabled)
    {
        try
        {
            var h = UnityEngine.Object.FindFirstObjectByType<SystemStartHandler>(
                FindObjectsInactive.Include);
            if (h != null)
            {
                h.runKeyName = "Xiaoqi";
                h.SetStateFromCode(enabled);
                return;
            }
            using (var key = Registry.CurrentUser.OpenSubKey(RunPath, true))
            {
                if (key == null) return;
                if (enabled)
                {
                    var exe = "\"" + Application.dataPath.Replace("/", "\\") + "\\..\\Xiaoqi.exe\"";
                    key.SetValue("Xiaoqi", exe);
                }
                else key.DeleteValue("Xiaoqi", false);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiRunKey sync: " + e.Message);
        }
    }
}
