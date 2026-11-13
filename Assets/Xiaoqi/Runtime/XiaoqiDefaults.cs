using UnityEngine;

public static class XiaoqiDefaults
{
    public static void Apply(XiaoqiConfig cfg)
    {
        var h = SaveLoadHandler.Instance;
        if (h == null || h.data == null)
        {
            Debug.LogWarning("XiaoqiDefaults: SaveLoadHandler not ready");
            return;
        }

        var d = h.data;
        d.enableDiscordRPC = false;
        d.alarmsEnabled = false;
        d.alarms.Clear();
        d.timers.Clear();
        d.enableDancing = false;
        d.enableDanceSwitch = false;
        d.enableRandomMessages = false;
        d.enableMinecraftMessages = false;
        d.enableFeedSystem = false;
        d.enableRandomAvatar = false;
        d.enableHusbandoMode = false;
        d.bigScreenScreenSaverEnabled = false;
        d.enableParticles = false;
        d.tutorialDone = true;
        d.selectedLocaleCode = "zh";
        d.selectedModelPath = cfg.model_path;
        d.enableWindowSitting = true;
        d.enableLocomotion = false;

        h.SaveToDisk();
        SaveLoadHandler.ApplyAllSettingsToAllAvatars();
    }
}
