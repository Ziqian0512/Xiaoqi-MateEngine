using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public static class XiaoqiTray
{
    public static void Install()
    {
        TrayIcon.OnBuildMenu = Build;
        ApplyMute(); // 启动时按存盘值设一次音量（和设置页「静音」共用 cfg.muted / cfg.volume）
    }

    // 条目与顺序照 G59-P1 §6：显示/隐藏小祈、静音、安静一小时/不安静了、（打开工作台：P3 前不加）、小祈设置、退出小祈。
    // 原生菜单只有文字条目：不做玻璃、不做二级菜单。每次右键都会重新调用 Build，所以文字能跟着状态变。
    private static List<(string, Action)> Build()
    {
        var cfg = XiaoqiRuntime.Config;
        bool muted = cfg != null && cfg.muted;
        return new List<(string, Action)>
        {
            (XqFullscreenHide.ManualHidden ? "显示小祈" : "隐藏小祈", ToggleHidden),
            (muted ? "取消静音" : "静音", ToggleMute),
            (XiaoqiRuntime.Dnd ? "不安静了" : "安静一小时", ToggleQuiet),
            (Utils.TrayIcon.SEPARATOR, null),
            ("小祈设置", XiaoqiSettings.Open),
            ("退出小祈", () => Application.Quit())
        };
    }

    private static void ToggleHidden()
    {
        XqFullscreenHide.SetManualHidden(!XqFullscreenHide.ManualHidden);
    }

    private static void ToggleMute()
    {
        var cfg = XiaoqiRuntime.Config;
        if (cfg == null) return;
        cfg.muted = !cfg.muted;
        try { XiaoqiConfig.Save(cfg); }
        catch (Exception e) { Debug.LogWarning("XQ_TRAY save " + e.Message); }
        ApplyMute();
        Debug.Log("XQ_TRAY muted=" + cfg.muted);
    }

    private static void ApplyMute()
    {
        var cfg = XiaoqiRuntime.Config;
        if (cfg == null) return;
        AudioListener.volume = cfg.muted ? 0f : Mathf.Clamp01(cfg.volume);
    }

    private static void ToggleQuiet()
    {
        if (XiaoqiRuntime.Dnd) XiaoqiRuntime.ClearQuiet();
        else XiaoqiRuntime.SetQuietMinutes(60);
    }
}
