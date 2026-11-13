using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;

public static class XiaoqiBuild
{
    public static void Win64()
    {
        XiaoqiIdentity.Apply();
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent(out var addressablesResult);
        if (!string.IsNullOrEmpty(addressablesResult.Error))
        {
            UnityEngine.Debug.LogError(addressablesResult.Error);
            EditorApplication.Exit(1);
        }

        var scenes = new System.Collections.Generic.List<string>
        {
            "Assets/MATE ENGINE - Scenes/Mate Engine Main.unity"
        };
        const string room = "Assets/Xiaoqi/_Private/XiaoqiRoom.unity";
        if (File.Exists(room)) scenes.Add(room);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Build/Xiaoqi/Xiaoqi.exe",
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
