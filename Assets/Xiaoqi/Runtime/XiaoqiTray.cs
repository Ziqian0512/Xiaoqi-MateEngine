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
            (XiaoqiRuntime.Dnd ? "免打扰 ✔" : "免打扰", ToggleDnd),
            (Utils.TrayIcon.SEPARATOR, null),
            ("退出小祈", () => Application.Quit())
        };
    }

    private static void ToggleDnd()
    {
        XiaoqiRuntime.Dnd = !XiaoqiRuntime.Dnd;
        if (XiaoqiRuntime.Config != null)
        {
            XiaoqiRuntime.Config.dnd = XiaoqiRuntime.Dnd;
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        }
    }
}
