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
        var menu = new List<(string, Action)>
        {
            ("去房间", XiaoqiModeController.EnterRoom),
            (XiaoqiRuntime.Dnd ? "免打扰 ✔" : "免打扰", ToggleDnd),
            ("小祈设置", XiaoqiSettings.Open),
            ("调参（F9）", Xiaoqi.Spike.EmotionLineTuner.Toggle),
            ("关于", About),
            ("退出小祈", () => Application.Quit())
        };
        return menu;
    }

    private static void ToggleDnd()
    {
        XiaoqiRuntime.Dnd = !XiaoqiRuntime.Dnd;
        if (XiaoqiRuntime.Config != null) XiaoqiRuntime.Config.dnd = XiaoqiRuntime.Dnd;
    }

    private static void About()
    {
        XiaoqiAbout.Show();
    }
}
