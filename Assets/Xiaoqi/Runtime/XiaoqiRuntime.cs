using System;
using UnityEngine;

public static class XiaoqiRuntime
{
    public static XiaoqiConfig Config;

    /// <summary>安静中：dnd_until 不为空且（forever 或还没到点）。</summary>
    public static bool Dnd => IsQuiet();

    public static bool IsQuiet()
    {
        var s = Config != null ? Config.dnd_until : null;
        if (string.IsNullOrEmpty(s)) return false;
        if (s == "forever") return true;
        return DateTime.TryParse(s, out var t) && DateTime.Now < t;
    }

    public static void SetQuietMinutes(int minutes)
    {
        if (Config == null) return;
        Config.dnd_until = DateTime.Now.AddMinutes(minutes).ToString("yyyy-MM-ddTHH:mm");
        XiaoqiConfig.Save(Config);
        Debug.Log("XQ_DND until=" + Config.dnd_until);
    }

    public static void SetQuietTomorrow8()
    {
        if (Config == null) return;
        Config.dnd_until = DateTime.Now.Date.AddDays(1).AddHours(8).ToString("yyyy-MM-ddTHH:mm");
        XiaoqiConfig.Save(Config);
        Debug.Log("XQ_DND until=" + Config.dnd_until);
    }

    public static void SetQuietForever()
    {
        if (Config == null) return;
        Config.dnd_until = "forever";
        XiaoqiConfig.Save(Config);
        Debug.Log("XQ_DND until=forever");
    }

    public static void ClearQuiet()
    {
        if (Config == null) return;
        Config.dnd_until = "";
        XiaoqiConfig.Save(Config);
        Debug.Log("XQ_DND off");
    }

    /// <summary>旧配置迁移：dnd=true 且 dnd_until 为空时写成 forever。</summary>
    public static void MigrateDnd()
    {
        if (Config == null) return;
        if (string.IsNullOrEmpty(Config.dnd_until) && Config.dnd)
        {
            Config.dnd_until = "forever";
            XiaoqiConfig.Save(Config);
            Debug.Log("XQ_DND migrate forever");
        }
    }

    /// <summary>到点自动关；返回 true 表示这次刚结束。</summary>
    public static bool ExpireQuiet()
    {
        var s = Config != null ? Config.dnd_until : null;
        if (string.IsNullOrEmpty(s) || s == "forever") return false;
        if (DateTime.TryParse(s, out var t) && DateTime.Now >= t)
        {
            Config.dnd_until = "";
            XiaoqiConfig.Save(Config);
            return true;
        }
        return false;
    }

    public static string QuietLabel()
    {
        var s = Config != null ? Config.dnd_until : null;
        if (string.IsNullOrEmpty(s)) return "";
        if (s == "forever") return "安静中";
        return DateTime.TryParse(s, out var t) ? "安静中 · 到 " + t.ToString("HH:mm") : "安静中";
    }
}
