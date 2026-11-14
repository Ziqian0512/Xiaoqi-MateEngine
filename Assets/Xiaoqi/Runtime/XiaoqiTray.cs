using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public static class XiaoqiTray
{
    public static void Install()
    {
        TrayIcon.OnBuildMenu = Build;
    }

    private static List<(string, Action)> Build()
    {
        return new List<(string, Action)>
        {
            ("小祈设置", XiaoqiSettings.Open),
            (XiaoqiRuntime.Dnd ? "不安静了" : "安静一小时", ToggleQuiet),
            (Utils.TrayIcon.SEPARATOR, null),
            ("退出小祈", () => Application.Quit())
        };
    }

    private static void ToggleQuiet()
    {
        if (XiaoqiRuntime.Dnd) XiaoqiRuntime.ClearQuiet();
        else XiaoqiRuntime.SetQuietMinutes(60);
    }
}
