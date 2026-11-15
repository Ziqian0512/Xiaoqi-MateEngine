// XQ: G57 期间的自助触发：主人玩游戏时不去点菜单，改由外部放一个 flag 文件，
// 编辑器自己打开预览窗（省得 DSH 操作界面打扰全屏游戏）。用完即删，残留无害。
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xiaoqi.UI2
{
    [InitializeOnLoad]
    public static class XqAutoPilot
    {
        const string TriggerPath = @"E:\dsh-pet-projects\dspet-main\.xq-trigger";

        static XqAutoPilot()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(TriggerPath)) return;
            var cmd = File.ReadAllText(TriggerPath).Trim();
            File.Delete(TriggerPath);
            Debug.Log("[XQ] auto trigger: " + cmd);
            if (cmd == "preview-today-card")
            {
                try { TodayCardPreviewWindow.Open(); } catch (System.Exception e) { Debug.LogError("[XQ] open failed: " + e); }
            }
        }
    }
}
