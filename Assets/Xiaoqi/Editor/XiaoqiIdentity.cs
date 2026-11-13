using UnityEditor;
using UnityEngine;

public static class XiaoqiIdentity
{
    public static void Apply()
    {
        PlayerSettings.productName = "Xiaoqi";
        PlayerSettings.companyName = "Xiaoqi";
        PlayerSettings.runInBackground = true;

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Xiaoqi/Branding/xiaoqi_icon.png");
        if (icon != null)
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new[] { icon });
    }
}
